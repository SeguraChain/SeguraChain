using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Other.Object.Network
{
    /// <summary>
    /// Custom socket class.
    /// </summary>
    public class ClassCustomSocket
    {
        private Socket _socket;
        private NetworkStream _networkStream;

        public bool Closed { private set; get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassCustomSocket(Socket socket, bool isServer)
        {
            _socket = socket;

            // Optimisations TCP (P2P/HTTP) : à faire le plus tôt possible.
            TryConfigureTcpSocket(_socket);

            if (isServer)
            {
                try
                {
                    if (_socket != null)
                        _networkStream = new NetworkStream(_socket, true);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }
        }

        /// <summary>
        /// Connect with delay (seconds) using TaskManager. Compatible .NET Framework 4.8.
        /// </summary>
        public async Task<bool> Connect(string ip, int port, int delay, CancellationTokenSource cancellation)
        {
            bool success = false;

            // Utilise un TCS pour éviter le busy-wait.
            var tcsDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await TaskManager.TaskManager.InsertTask(async () =>
            {
                try
                {
                    if (cancellation == null || cancellation.IsCancellationRequested)
                    {
                        tcsDone.TrySetResult(false);
                        return;
                    }

                    if (_socket == null)
                    {
                        tcsDone.TrySetResult(false);
                        return;
                    }

                    TryConfigureTcpSocket(_socket);

                    // .NET 4.8: pas de ConnectAsync avec CancellationToken => on "simule" via Task.WhenAny.
                    var connectTask = _socket.ConnectAsync(ip, port);

                    // Si le token est annulé, on se débloque.
                    var cancelTask = WaitCancellationAsync(cancellation.Token);

                    var completed = await Task.WhenAny(connectTask, cancelTask).ConfigureAwait(false);

                    if (completed == cancelTask || cancellation.IsCancellationRequested)
                    {
                        // Forcer l'arrêt du connect si besoin.
                        SafeKillSocket();
                        tcsDone.TrySetResult(false);
                        return;
                    }

                    // Propager les exceptions éventuelles.
                    await connectTask.ConfigureAwait(false);

                    success = _socket != null && _socket.Connected;
                    if (success)
                    {
                        // Stream après connexion.
                        _networkStream = new NetworkStream(_socket, true);
                    }

                    tcsDone.TrySetResult(success);
                }
                catch
                {
                    SafeKillSocket();
                    tcsDone.TrySetResult(false);
                }
            }, System.Math.Max(0, delay) * 1000, cancellation);


           
            return await tcsDone.Task.ConfigureAwait(false);
        }

        private static Task WaitCancellationAsync(CancellationToken token)
        {
            if (!token.CanBeCanceled)
                return Task.Delay(Timeout.Infinite);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => tcs.TrySetResult(true));
            return tcs.Task;
        }

        public string GetIp
        {
            get
            {
                try
                {
                    var ep = _socket?.RemoteEndPoint as IPEndPoint;
                    return ep != null ? ep.Address.ToString() : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Socket is connected.
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                if (Closed || _socket == null || _networkStream == null)
                    return false;

                if (!_socket.Connected)
                    return false;

                // Poll + Available: check classique.
                return !(_socket.Poll(10, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TrySendSplittedPacket(byte[] packetData, CancellationTokenSource cancellation, int packetPeerSplitSeperator, bool singleWrite)
        {
            try
            {
                if (Closed || _networkStream == null || packetData == null || packetData.Length == 0)
                    return false;

                return await _networkStream.TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator, singleWrite)
                    .ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, int delayReading, bool isHttp, CancellationTokenSource cancellation)
        {
            var readPacketData = new ReadPacketData
            {
                Status = false,
                Data = packetLength > 0 ? new byte[packetLength] : Array.Empty<byte>()
            };

            if (Closed || _networkStream == null || packetLength <= 0 || cancellation == null || cancellation.IsCancellationRequested)
                return readPacketData;

            CancellationTokenSource ctsDelay = null;
            CancellationTokenSource ctsLinked = null;

            try
            {
                ctsDelay = delayReading > 0 ? new CancellationTokenSource(delayReading) : new CancellationTokenSource();
                ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, ctsDelay.Token);

                // IMPORTANT: ReadAsync peut lire moins que packetLength => boucle pour remplir (ou timeout/annulation).
                int offset = 0;
                while (offset < packetLength)
                {
                    int read;
#if NET5_0_OR_GREATER
                    read = await _networkStream.ReadAsync(readPacketData.Data.AsMemory(offset, packetLength - offset), ctsLinked.Token).ConfigureAwait(false);
#else
                    read = await _networkStream.ReadAsync(readPacketData.Data, offset, packetLength - offset, ctsLinked.Token).ConfigureAwait(false);
#endif
                    if (read <= 0)
                        break;

                    offset += read;

                    // Optimisation: si HTTP, on peut sortir tôt si on a déjà \r\n\r\n (fin headers).
                    // (Sans casser le comportement existant: ça ne force pas la sortie si pas trouvé.)
                    if (isHttp && offset >= 4)
                    {
                        // Recherche rapide uniquement sur la fenêtre utile.
                        if (ContainsHttpHeaderTerminator(readPacketData.Data, offset))
                            break;
                    }
                }

                // Si on a lu moins, on réduit (évite de trim ensuite sur des zéros).
                if (readPacketData.Data.Length != 0)
                {
                    int actualLen = GetActualLength(readPacketData.Data);
                    if (actualLen != readPacketData.Data.Length)
                    {
                        var resized = new byte[actualLen];
                        Buffer.BlockCopy(readPacketData.Data, 0, resized, 0, actualLen);
                        readPacketData.Data = resized;
                    }
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
#endif
            }
            finally
            {
                try { ctsLinked?.Dispose(); } catch { }
                try { ctsDelay?.Dispose(); } catch { }
            }

            // Filtrage/normalisation du buffer lu
            readPacketData.Data = FilterReadBytes(readPacketData.Data, isHttp);
            readPacketData.Status = readPacketData.Data != null && readPacketData.Data.Length > 0;

            return readPacketData;
        }

        private static bool ContainsHttpHeaderTerminator(byte[] buffer, int length)
        {
            // Cherche \r\n\r\n dans [0..length)
            // Optim: ne parcourt que jusqu'à length-4
            for (int i = 0; i <= length - 4; i++)
            {
                if (buffer[i] == (byte)'\r' &&
                    buffer[i + 1] == (byte)'\n' &&
                    buffer[i + 2] == (byte)'\r' &&
                    buffer[i + 3] == (byte)'\n')
                    return true;
            }
            return false;
        }

        private static int GetActualLength(byte[] buffer)
        {
            // Coupe les \0 en fin si présents (cas fréquent si lecture partielle du buffer préalloué).
            int end = buffer.Length;
            while (end > 0 && buffer[end - 1] == 0)
                end--;
            return end;
        }

        private static byte[] FilterReadBytes(byte[] data, bool isHttp)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            // On compacte en place dans un nouveau buffer de même taille puis Resize.
            var tmp = new byte[data.Length];
            int count = 0;
            char sep = ClassPeerPacketSetting.PacketPeerSplitSeperator;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];

                // Ignore les \0.
                if (b == 0)
                    continue;

                if (!isHttp)
                {
                    char c = (char)b;
                    if (ClassUtility.CharIsABase64Character(c) || c == sep)
                        tmp[count++] = b;
                }
                else
                {
                    tmp[count++] = b;
                }
            }

            if (count == tmp.Length)
                return tmp;

            var result = new byte[count];
            if (count > 0)
                Buffer.BlockCopy(tmp, 0, result, 0, count);

            return result;
        }

        public void Kill(SocketShutdown shutdownType)
        {
            Close(shutdownType);
        }

        private void Close(SocketShutdown shutdownType)
        {
            if (Closed)
                return;

            Closed = true;

            try
            {
                // Fermer le stream même si socket déjà tombé.
                try { _networkStream?.Close(); } catch { }
                try { _networkStream?.Dispose(); } catch { }
                _networkStream = null;

                if (_socket != null)
                {
                    try
                    {
                        if (_socket.Connected)
                            _socket.Shutdown(shutdownType);
                    }
                    catch { /* ignore */ }

                    try { _socket.Close(); } catch { /* ignore */ }
                    try { _socket.Dispose(); } catch { /* ignore */ }

                    _socket = null;
                }
            }
            catch
            {
                // Ignored.
            }
        }

        private void SafeKillSocket()
        {
            try
            {
                if (_socket != null)
                {
                    try { _socket.Close(); } catch { }
                    try { _socket.Dispose(); } catch { }
                }
            }
            catch { }
            finally
            {
                _socket = null;
            }
        }

        /// <summary>
        /// Centralise les options TCP utiles (P2P + HTTP).
        /// Compatible .NET Framework 4.8.
        /// </summary>
        private static void TryConfigureTcpSocket(Socket socket)
        {
            if (socket == null)
                return;

            try
            {
                // Latence: désactive Nagle (utile P2P + petites trames HTTP).
                socket.NoDelay = true;

                // Buffers: ajustables selon ton workload (ici valeurs "raisonnables" sans exploser la RAM).
                // Tu peux les pousser plus haut si tu envoies des gros blocs souvent.
                if (socket.SendBufferSize < 256 * 1024)
                    socket.SendBufferSize = 256 * 1024;

                if (socket.ReceiveBufferSize < 256 * 1024)
                    socket.ReceiveBufferSize = 256 * 1024;

                // KeepAlive: aide à détecter des peers morts sur connexions longues.
                // (Option simple; réglages fins via IOControl possibles mais pas indispensables ici.)
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Linger: évite de bloquer sur Close; 0 = envoi RST si données non envoyées.
                // Pour du P2P (où on préfère libérer vite), c'est souvent souhaitable.
                socket.LingerState = new LingerOption(true, 0);
            }
            catch
            {
                // Ignored.
            }
        }

        public class ReadPacketData : IDisposable
        {
            public bool Status;
            public byte[] Data;

            private bool _disposed;

            public void Dispose()
            {
                Dispose(true);
            }

            private void Dispose(bool dispose)
            {
                if (_disposed || !dispose)
                    return;

                Data = null;
                Status = false;
                _disposed = true;
            }
        }
    }
}
