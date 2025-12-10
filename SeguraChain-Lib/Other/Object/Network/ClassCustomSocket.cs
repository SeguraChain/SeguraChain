using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Lib.Utility;
using System;
#if NET5_0_OR_GREATER
using System.Buffers;
#endif
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Other.Object.Network
{
    /// <summary>
    /// Custom socket class.
    /// </summary>
    public class ClassCustomSocket : IDisposable
    {
        private Socket _socket;
        private NetworkStream _networkStream;
        private readonly object _closeLock = new object();
        private int _closeState = 0; // 0 = open, 1 = closing/closed

        // Cache de l'IP pour éviter les appels répétés
        private string _cachedIp;

        public bool Closed => _closeState == 1;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassCustomSocket(Socket socket, bool isServer)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            if (isServer)
            {
                try
                {
                    _networkStream = new NetworkStream(_socket, ownsSocket: false);
                    CacheIpAddress();
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }
        }

        private void CacheIpAddress()
        {
            try
            {
                _cachedIp = _socket?.RemoteEndPoint is IPEndPoint endpoint
                    ? endpoint.Address.ToString()
                    : string.Empty;
            }
            catch
            {
                _cachedIp = string.Empty;
            }
        }

        public async Task<bool> Connect(string ip, int port, int delay, CancellationTokenSource cancellation)
        {
            if (string.IsNullOrEmpty(ip))
                return false;

            bool success = false;
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                // Utilisation de TaskCompletionSource au lieu de polling actif
                using (var timeoutCts = new CancellationTokenSource(delay * 1000))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, timeoutCts.Token))
                {
                    var connectTask = Task.Run(async () =>
                    {
                        try
                        {
#if NET6_0_OR_GREATER
                            await _socket.ConnectAsync(ip, port, linkedCts.Token).ConfigureAwait(false);
#else
                            await _socket.ConnectAsync(ip, port).ConfigureAwait(false);
#endif
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }, linkedCts.Token);

                    success = await connectTask.ConfigureAwait(false);
                }
            }
            catch
            {
                return false;
            }

            if (!success || _socket == null)
                return false;

            try
            {
                _networkStream = new NetworkStream(_socket, ownsSocket: false);
                CacheIpAddress();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetIp => _cachedIp ?? (_cachedIp = GetIpInternal());

        private string GetIpInternal()
        {
            try
            {
                return _socket?.RemoteEndPoint is IPEndPoint endpoint
                    ? endpoint.Address.ToString()
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Socket is connected.
        /// </summary>
        public bool IsConnected()
        {
            if (Closed || _socket == null || _networkStream == null)
                return false;

            try
            {
                // Vérification optimisée de la connexion
                return _socket.Connected && !(_socket.Poll(10, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TrySendSplittedPacket(byte[] packetData, CancellationTokenSource cancellation, int packetPeerSplitSeperator, bool singleWrite)
        {
            if (packetData == null || packetData.Length == 0)
                return false;

            try
            {
                return await _networkStream.TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator, singleWrite).ConfigureAwait(false);
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine($"Sending packet exception to {GetIp} | Exception: {error.Message}");
#endif
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, int delayReading, bool isHttp, CancellationTokenSource cancellation)
        {
            if (packetLength <= 0)
                return new ReadPacketData { Status = false, Data = Array.Empty<byte>() };

            var readPacketData = new ReadPacketData { Data = new byte[packetLength] };

            try
            {
                using (var timeoutCts = new CancellationTokenSource(delayReading))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, timeoutCts.Token))
                {
#if NET5_0_OR_GREATER
                    await _networkStream.ReadAsync(readPacketData.Data.AsMemory(0, packetLength), linkedCts.Token).ConfigureAwait(false);
#else
                    await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, linkedCts.Token).ConfigureAwait(false);
#endif
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine($"Reading packet exception from {GetIp} | Exception: {error.Message}");
#endif
                readPacketData.Status = false;
                return readPacketData;
            }

            // Filtrage optimisé des données
            readPacketData.Data = FilterPacketData(readPacketData.Data, isHttp);
            readPacketData.Status = readPacketData.Data.Length > 0;

            return readPacketData;
        }

        private byte[] FilterPacketData(byte[] data, bool isHttp)
        {
            if (isHttp)
                return RemoveNullBytes(data);

            // Utilisation d'un buffer réutilisable pour éviter les allocations
            int writeIndex = 0;
            byte separatorByte = (byte)ClassPeerPacketSetting.PacketPeerSplitSeperator;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];

                if (b == 0)
                    continue;

                if (ClassUtility.CharIsABase64Character((char)b) || b == separatorByte)
                {
                    data[writeIndex++] = b;
                }
            }

            // Redimensionner uniquement si nécessaire
            if (writeIndex == data.Length)
                return data;

            var result = new byte[writeIndex];
            Array.Copy(data, result, writeIndex);
            return result;
        }

        private byte[] RemoveNullBytes(byte[] data)
        {
            int writeIndex = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0)
                {
                    data[writeIndex++] = data[i];
                }
            }

            if (writeIndex == data.Length)
                return data;

            var result = new byte[writeIndex];
            Array.Copy(data, result, writeIndex);
            return result;
        }

        public void Kill(SocketShutdown shutdownType)
        {
            Close(shutdownType);
        }

        private void Close(SocketShutdown shutdownType)
        {
            // Utilisation d'Interlocked pour éviter les double-close
            if (Interlocked.CompareExchange(ref _closeState, 1, 0) == 1)
                return;

            try
            {
                if (_socket?.Connected == true)
                {
                    _socket.Shutdown(shutdownType);
                }
            }
            catch
            {
                // Ignored
            }

            try
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
            }
            catch
            {
                // Ignored
            }

            try
            {
                _socket?.Close();
                _socket?.Dispose();
            }
            catch
            {
                // Ignored
            }

            _networkStream = null;
            _socket = null;
        }

        public void Dispose()
        {
            Close(SocketShutdown.Both);
        }

        public class ReadPacketData : IDisposable
        {
            public bool Status;
            public byte[] Data;
            private int _disposed = 0;

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                    return;

                Data = null;
                Status = false;
            }
        }
    }
}