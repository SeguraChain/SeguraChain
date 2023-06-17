using Org.BouncyCastle.Bcpg;
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

            if (isServer)
            {
                try
                {
                    _networkStream = new NetworkStream(_socket);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }

        }

        public bool Connect(string ip, int port, int timeout)
        {
            try
            {
                var result = _socket.BeginConnect(ip, port, null, null);

                bool success = result.AsyncWaitHandle.WaitOne(timeout, true);

                if (success)
                {
                    _networkStream = new NetworkStream(_socket);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public string GetIp
        {
            get
            {
                try
                {
                    if (_socket?.RemoteEndPoint == null)
                    {
                        // Is dead.
                        Kill(SocketShutdown.Both);
                        return string.Empty;
                    }

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
                return (Closed || _socket == null || !_socket.Connected || _networkStream == null) ? false : true;
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
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, CancellationTokenSource cancellation)
        {
            ReadPacketData readPacketData = new ReadPacketData();



            try
            {
                if (!IsConnected())
                    return readPacketData;
                /*
                while (_socket.Available == 0)
                {
                    if (cancellation.IsCancellationRequested || !IsConnected())
                        return readPacketData;

                    await Task.Delay(1);
                }
                */
                readPacketData.Data = new byte[packetLength];
                readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;
            }
            catch (Exception error)
            {
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
            }
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

            if (IsConnected())
            {
                _socket?.Shutdown(shutdownType);
                _socket?.Close();
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
