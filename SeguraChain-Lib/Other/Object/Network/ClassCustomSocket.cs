using SeguraChain_Lib.Utility;
using System;
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
        public bool Disposed { private set; get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="socket"></param>
        public ClassCustomSocket(Socket socket, bool isServer)
        {
            _socket = socket;

            if (isServer)
            {
                try
                {
                    _networkStream = new NetworkStream(_socket);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                    Dispose();
                }
            }
        }

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                await _socket.ConnectAsync(ip, port);
            }
            catch
            {
                return false;
            }
            _networkStream = new NetworkStream(_socket);
            return true;
        }

        public string GetIp
        {
            get
            {
                try
                {
                    return ((IPEndPoint)(_socket.RemoteEndPoint)).Address.ToString();
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
        /// <returns></returns>
        public bool IsConnected()
        {
            try
            {
                // return !((_socket.Poll(10, SelectMode.SelectRead) && (_socket.Available == 0)));
                return (Disposed || Closed || _socket == null || !_socket.Connected || _networkStream == null) ? false : true;
            }
            catch
            {
                return false;
            }

        }

        public async Task<bool> TrySendSplittedPacket(byte[] packetData, CancellationTokenSource cancellation, int packetPeerSplitSeperator)
        {
            if (!IsConnected())
                return false;

            try
            {
                return await _networkStream.TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator);
            }
            catch
            {
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, CancellationTokenSource cancellation)
        {
            ReadPacketData readPacketData = new ReadPacketData();
            _networkStream = new NetworkStream(_socket);

            if (IsConnected())
            {
                try
                {
                    /*
                    while(!_socket.Blocking)
                    {
                        if (cancellation.IsCancellationRequested || !IsConnected())
                            return readPacketData;

                        await Task.Delay(1);
                    }*/
                    readPacketData.Data = new byte[packetLength];
                    readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;
                }
                catch
                {

                }
            }
            return readPacketData;
        }


        public void Kill(SocketShutdown shutdownType)
        {
            Close(shutdownType);
            Dispose();
        }

        private void Close(SocketShutdown shutdownType)
        {
            if (Closed)
                return;

            Closed = true;

            if (IsConnected())
            {
                _socket?.Shutdown(shutdownType);
                _socket?.Close();
            }
        }

        private void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            _socket?.Dispose();
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
