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

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                await _socket.ConnectAsync(ip, port);
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
                return (Closed || 
                    _socket == null || 
                    !ClassUtility.SocketIsConnected(_socket) ||
                    _networkStream == null) ? false : true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// Try to send a splitted packet data.
        /// </summary>
        /// <param name="packetData"></param>
        /// <param name="cancellation"></param>
        /// <param name="packetPeerSplitSeperator"></param>
        /// <returns></returns>
        public async Task<bool> TrySendSplittedPacket(byte[] packetData, CancellationTokenSource cancellation, int packetPeerSplitSeperator)
        {

            try
            {

               // packetData = packetData.InputData(ClassPeerPacketSetting.PacketSeperatorBegin.GetByteArray(), true);
               // packetData = packetData.InputData(ClassPeerPacketSetting.PacketSeperatorEnd.GetByteArray(), false);

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
                
                /*while (_socket.Available == 0)
                {
                    if (cancellation.IsCancellationRequested || !IsConnected())
                        return readPacketData;

                    await Task.Delay(1);
                }*/

               /* bool foundBegin = false;
                bool foundEnd = false;*/

                readPacketData.Data = new byte[packetLength];
                readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;


                /*using (DisposableList<byte> listData = new DisposableList<byte>())
                {
                    while (!foundBegin && !foundEnd)
                    {
                        readPacketData.Data = new byte[packetLength];
                        readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;

                        if (!readPacketData.Status || !IsConnected())
                            break;

                        if (!foundBegin)
                        {
                            string data = readPacketData.Data.GetStringFromByteArrayUtf8();
                            int indexOfBegin = data.IndexOf(ClassPeerPacketSetting.PacketSeperatorBegin);

                            if (indexOfBegin > -1)
                                foundBegin = true;

                            int indexOfEnd = data.IndexOf(ClassPeerPacketSetting.PacketSeperatorEnd);

                            if (indexOfBegin > -1)
                                foundEnd = true;

                            if (foundBegin && foundEnd)
                               data = data.GetStringBetweenTwoStrings(ClassPeerPacketSetting.PacketSeperatorBegin, ClassPeerPacketSetting.PacketSeperatorEnd);
                            else if (foundBegin && !foundEnd)
                               data = data.Substring(indexOfBegin + ClassPeerPacketSetting.PacketSeperatorBegin.Length);

                            foreach (var dataByte in data.GetByteArray())
                                listData.Add(dataByte);

                           
                            if (foundBegin && foundEnd)
                                break;
                        }
                        
                    }

                    if (foundBegin && foundEnd)
                    {
                        readPacketData.Data = listData.GetList.ToArray();
                        readPacketData.Status = true;
                    }    
                }*/

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

            try
            {
                _socket?.Shutdown(shutdownType);
                _socket?.Close();
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
