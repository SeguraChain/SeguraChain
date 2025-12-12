using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Other.Object.List;
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
        /// <param name="socket"></param>
        public ClassCustomSocket(Socket socket, bool isServer)
        {
            _socket = socket;

            if (_socket != null)
            {
                // Optimisations TCP (net4.8 OK)
                try
                {
                    ConfigureSocket(_socket);
                }
                catch
                {
                    // On ne casse pas le flux si certaines options ne sont pas supportées.
                }
            }

            if (isServer)
            {
                try
                {
                    _networkStream = new NetworkStream(_socket, ownsSocket: false);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }
        }

        /// <summary>
        /// Configure TCP options (latency + robustness).
        /// </summary>
        private static void ConfigureSocket(Socket socket)
        {
            // Latence plus faible (désactive Nagle)
            socket.NoDelay = true;

            // Buffers un peu plus grands (ajuste si besoin)
            socket.SendBufferSize = 256 * 1024;
            socket.ReceiveBufferSize = 256 * 1024;

            // Evite certains blocages si jamais une API sync est utilisée ailleurs
            socket.SendTimeout = 10_000;
            socket.ReceiveTimeout = 10_000;

            // Fermeture plus “nette”
            socket.LingerState = new LingerOption(false, 0);

            // KeepAlive (utile pour détecter un lien mort)
            EnableKeepAlive(socket, timeMs: 30_000, intervalMs: 10_000);
        }

        private static void EnableKeepAlive(Socket socket, uint timeMs, uint intervalMs)
        {
            // SIO_KEEPALIVE_VALS : [onOff(4)][time(4)][interval(4)]
            byte[] inOptionValues = new byte[12];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(timeMs).CopyTo(inOptionValues, 4);
            BitConverter.GetBytes(intervalMs).CopyTo(inOptionValues, 8);

            try
            {
                socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            }
            catch
            {
                // Selon OS/config, ça peut refuser : on ignore.
            }
        }

        /// <summary>
        /// Connect (delay is in seconds like original code).
        /// </summary>
        public async Task<bool> Connect(string ip, int port, int delay, CancellationTokenSource cancellation)
        {
            if (Closed || _socket == null)
                return false;

            // Si déjà connecté, on s'assure juste d'avoir le stream
            if (_socket.Connected)
            {
                if (_networkStream == null)
                    _networkStream = new NetworkStream(_socket, ownsSocket: false);
                return true;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // On conserve le comportement original : la tentative de connexion est planifiée via TaskManager
            await TaskManager.TaskManager.InsertTask(async () =>
            {
                try
                {
                    // net4.8 : pas de token ici -> on gère l'annulation via WhenAny / fermeture socket
                    await _socket.ConnectAsync(ip, port).ConfigureAwait(false);
                    tcs.TrySetResult(true);
                }
                catch
                {
                    tcs.TrySetResult(false);
                }
            }, delay * 1000, cancellation);

            // Attente propre (sans boucle active)
            using (cancellation.Token.Register(() => tcs.TrySetCanceled()))
            {
                bool success;
                try
                {
                    success = await tcs.Task.ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }

                if (!success || _socket == null || !_socket.Connected)
                    return false;

                try
                {
                    _networkStream = new NetworkStream(_socket, ownsSocket: false);
                    return true;
                }
                catch
                {
                    Close(SocketShutdown.Both);
                    return false;
                }
            }
        }

        public string GetIp
        {
            get
            {
                try
                {
                    return _socket?.RemoteEndPoint != null
                        ? ((IPEndPoint)(_socket.RemoteEndPoint)).Address.ToString()
                        : string.Empty;
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
                if (Closed || _socket == null || !_socket.Connected || _networkStream == null)
                    return false;

                // Poll+Available pour détecter une déconnexion propre
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
                if (Closed || _networkStream == null || _socket == null || !_socket.Connected)
                    return false;

                return await _networkStream
                    .TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator, singleWrite)
                    .ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        /// <summary>
        /// Read exactly packetLength bytes (TCP can return partial reads).
        /// delayReading is in ms like original code (CancellationTokenSource(delayReading)).
        /// </summary>
        public async Task<ReadPacketData> TryReadPacketData(int packetLength, int delayReading, bool isHttp, CancellationTokenSource cancellation)
        {
            ReadPacketData readPacketData = new ReadPacketData
            {
                Data = new byte[packetLength]
            };

            if (Closed || _networkStream == null || _socket == null || !_socket.Connected)
            {
                readPacketData.Status = false;
                readPacketData.Data = Array.Empty<byte>();
                return readPacketData;
            }

            try
            {
                using (var timeoutCts = new CancellationTokenSource(delayReading))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, timeoutCts.Token))
                {
                    int offset = 0;

                    while (offset < packetLength)
                    {
                        int read = await _networkStream
                            .ReadAsync(readPacketData.Data, offset, packetLength - offset, linkedCts.Token)
                            .ConfigureAwait(false);

                        // 0 = remote closed
                        if (read == 0)
                            break;

                        offset += read;
                    }

                    // Si on a lu moins que demandé, on tronque
                    if (offset <= 0)
                    {
                        readPacketData.Data = Array.Empty<byte>();
                    }
                    else if (offset < packetLength)
                    {
                        byte[] trimmed = new byte[offset];
                        Buffer.BlockCopy(readPacketData.Data, 0, trimmed, 0, offset);
                        readPacketData.Data = trimmed;
                    }
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
#endif
            }

            // Filtrage identique à ton code (mais plus safe si Data est vide)
            using (DisposableList<byte> listOfData = new DisposableList<byte>())
            {
                if (readPacketData.Data != null)
                {
                    foreach (byte data in readPacketData.Data)
                    {
                        if ((char)data == '\0')
                            continue;

                        if (!isHttp)
                        {
                            if (ClassUtility.CharIsABase64Character((char)data) ||
                                ClassPeerPacketSetting.PacketPeerSplitSeperator == (char)data)
                                listOfData.Add(data);
                        }
                        else
                        {
                            listOfData.Add(data);
                        }
                    }
                }

                readPacketData.Data = listOfData.GetList.ToArray();
            }

            readPacketData.Status = readPacketData.Data != null && readPacketData.Data.Length > 0;
            return readPacketData;
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
                try { _networkStream?.Close(); } catch { }
                try { _networkStream?.Dispose(); } catch { }

                if (_socket != null)
                {
                    try
                    {
                        if (_socket.Connected)
                            _socket.Shutdown(shutdownType);
                    }
                    catch { }

                    try { _socket.Close(); } catch { }
                    try { _socket.Dispose(); } catch { }
                }
            }
            catch
            {
                // Ignored.
            }
            finally
            {
                _networkStream = null;
                _socket = null;
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
