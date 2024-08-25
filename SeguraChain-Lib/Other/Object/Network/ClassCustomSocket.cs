using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Lib.Utility;
using System;
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

        public bool Connect(string ip, int port, int delay)
        {
            try
            {
                var result = _socket.BeginConnect(ip, port, null, null);

                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(delay));

                if (_socket == null || !success)
                    return false;

                _networkStream = new NetworkStream(_socket);
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
                    return _socket?.RemoteEndPoint != null ? ((IPEndPoint)(_socket.RemoteEndPoint)).Address.ToString() : string.Empty;
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
                if ((Closed || _socket == null || !_socket.Connected || _networkStream == null))
                    return false;

                return !((_socket.Poll(10, SelectMode.SelectRead) && (_socket.Available == 0)));
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
                return await _networkStream.TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator, singleWrite).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, int delayReading, bool isHttp, CancellationTokenSource cancellation)
        {
            ReadPacketData readPacketData = new ReadPacketData();
            readPacketData.Data = new byte[packetLength];


            try
            {
#if NET5_0_OR_GREATER
                await _networkStream.ReadAsync(readPacketData.Data.AsMemory(0, packetLength), CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, new CancellationTokenSource(delayReading).Token).Token).ConfigureAwait(false);
#else
                await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, new CancellationTokenSource(delayReading).Token).Token).ConfigureAwait(false);
#endif
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
#endif
            }


            using (DisposableList<byte> listOfData = new DisposableList<byte>())
            {
                foreach (byte data in readPacketData.Data)
                {
                    if ((char)data == '\0')
                        continue;

                    if (!isHttp)
                    {
                        if (ClassUtility.CharIsABase64Character((char)data) || ClassPeerPacketSetting.PacketPeerSplitSeperator == (char)data)
                            listOfData.Add(data);
                    }
                    else listOfData.Add(data);
                }
                readPacketData.Data = listOfData.GetList.ToArray();
            }
            

            readPacketData.Status = readPacketData.Data.Length > 0;

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
                _socket?.Shutdown(shutdownType);
                _socket?.Close();
                _networkStream.Close();
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
