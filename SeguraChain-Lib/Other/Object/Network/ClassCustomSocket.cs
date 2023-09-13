using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Other.Object.List;
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
        private TcpClient _socket;
        private NetworkStream _networkStream;


        public bool Closed { private set; get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="socket"></param>
        public ClassCustomSocket(TcpClient socket, bool isServer)
        {
            _socket = socket;

            if (isServer)
            {
                try
                {
                    _networkStream = new NetworkStream(_socket.Client);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }

        }

        public bool Connect(string ip, int port)
        {
            try
            {
                var result = _socket.BeginConnect(ip, port, null, null);

                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                if (_socket == null || _socket.Client == null || !success)
                    return false;

                _networkStream = new NetworkStream(_socket.Client);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string GetIp
        {
            get
            {
                try
                {
                    return _socket?.Client?.RemoteEndPoint != null ? ((IPEndPoint)(_socket.Client.RemoteEndPoint)).Address.ToString() : string.Empty;
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

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, bool clientSide, CancellationTokenSource cancellation)
        {
            ReadPacketData readPacketData = new ReadPacketData();



            try
            {
                if (!clientSide)
                {
                    using (DisposableList<byte> packetData = new DisposableList<byte>())
                    {
                        readPacketData.Data = new byte[packetLength];
                        readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;


                        using (DisposableList<byte> listOfData = new DisposableList<byte>())
                        {
                            foreach (byte data in readPacketData.Data)
                            {
                                if ((char)data == '\0')
                                    continue;

                                if (ClassUtility.CharIsABase64Character((char)data) || ClassPeerPacketSetting.PacketPeerSplitSeperator == (char)data)
                                    listOfData.Add(data);
                            }

                            readPacketData.Data = listOfData.GetList.ToArray();
                        }
                    }
                }
                else
                {
                    using (DisposableList<byte> dataList = new DisposableList<byte>())
                    {
                        while (!cancellation.IsCancellationRequested)
                        {
                            byte data = (byte)_networkStream.ReadByte();

                            if ((int)data == -1)
                                break;

                            if (ClassUtility.CharIsABase64Character((char)data) || ClassPeerPacketSetting.PacketPeerSplitSeperator == (char)data)
                                dataList.Add(data);

                            if (ClassPeerPacketSetting.PacketPeerSplitSeperator == (char)data)
                            {
                                break;
                            }
                        }

                        readPacketData.Status = dataList.GetList.Count > 0;
                        readPacketData.Data = dataList.GetList.ToArray();
                    }
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
#endif
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

            try
            {
                _socket?.Client?.Shutdown(shutdownType);
                _socket?.Close();
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
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
