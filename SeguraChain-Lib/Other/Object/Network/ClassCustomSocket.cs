using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
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

        public async Task<bool> ConnectAsync(string ip, int port, int timeout)
        {
            long timestampStart = ClassUtility.GetCurrentTimestampInMillisecond();
            while (timestampStart + timeout > ClassUtility.GetCurrentTimestampInMillisecond())
            {
                try
                {
                    await _socket.ConnectAsync(ip, port);
                    _networkStream = new NetworkStream(_socket);
                    break;
                }
                catch
                {
                    return false;
                }
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
        public async Task<bool> TrySendSplittedPacket(byte[] packetData, byte[] packetBegin, byte[] packetEnd, CancellationTokenSource cancellation, int packetPeerSplitSeperator)
        {

            try
            {
                Debug.WriteLine("Packet data to send length: " + packetData.Length);
                if (packetBegin != null && packetEnd != null)
                {
                    //packetData = ClassUtility.CompressDataLz4(packetData);
                    //packetData = packetData.InputData(packetBegin, true);
                    //packetData = packetData.InputData(packetEnd, false);
                    //Debug.WriteLine("Packet data to send: " + packetData.GetStringFromByteArrayUtf8());
                }
                return await _networkStream.TrySendSplittedPacket(packetData, cancellation, packetPeerSplitSeperator);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        public async Task<ReadPacketData> TryReadPacketData(int packetLength, byte[] packetBegin, byte[] packetEnd, CancellationTokenSource cancellation, bool fromApi = false)
        {
            ReadPacketData readPacketData = new ReadPacketData();



            try
            {

                while (_socket.Available == 0 && IsConnected())
                {
                    if (cancellation.IsCancellationRequested)
                        return readPacketData;

                    await Task.Delay(1);
                }


                if (!IsConnected())
                    return readPacketData;

                //readPacketData.Data = new byte[packetLength];
                //readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;

                if (fromApi)
                {
                    readPacketData.Data = new byte[packetLength];
                    readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;
                }
                else
                {
                    /*bool foundBegin = false;
                    bool foundEnd = false;*/

                  //  using (DisposableList<byte> listData = new DisposableList<byte>())
                  //  {

 
                   //     while (!foundBegin && !foundEnd)
                 //       {
                            readPacketData.Data = new byte[packetLength];
                            readPacketData.Status = await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, cancellation.Token) > 0;

                  
                            /*
                            if (!readPacketData.Status || !IsConnected())
                                break;*/

                            using(DisposableList<byte> listDataSorting = new DisposableList<byte>())
                            {
                                foreach(byte data in readPacketData.Data)
                                {
                                    if (data != '\0')
                                        listDataSorting.Add(data);
                                }
                                readPacketData.Data = listDataSorting.GetList.ToArray();
                            }

                            //Debug.WriteLine("Packet data received: " + readPacketData.Data.GetStringFromByteArrayUtf8());

                            /*
                            if (packetBegin == null || packetEnd == null)
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(GetIp))
                                {
                                    foreach (ClassPeerObject peerObject in ClassPeerDatabase.DictionaryPeerDataObject[GetIp].Values)
                                    {
                                        if (cancellation.IsCancellationRequested)
                                            break;

                                        if (!foundBegin && !foundEnd)
                                        {
                                            string packetBeginString = peerObject.PeerInternPacketBegin.GetStringFromByteArrayUtf8();
                                            string packetEndString = peerObject.PeerInternPacketEnd.GetStringFromByteArrayUtf8();

                                            Debug.WriteLine("Use packet seperator: " + packetBeginString + " | " + packetEndString + " for: " + GetIp);


                                            string data = readPacketData.Data.GetStringFromByteArrayUtf8();
                                            int indexOfBegin = data.IndexOf(packetBeginString);

                                            if (indexOfBegin > -1)
                                                foundBegin = true;

                                            int indexOfEnd = data.IndexOf(packetEndString);

                                            if (indexOfBegin > -1)
                                                foundEnd = true;

                                            if (foundBegin && foundEnd)
                                                data = data.GetStringBetweenTwoStrings(packetBeginString, packetEndString);
                                            else if (foundBegin && !foundEnd)
                                                data = data.Substring(indexOfBegin + packetBeginString.Length);

                                            foreach (var dataByte in data.GetByteArray())
                                                listData.Add(dataByte);


                                            if (foundBegin && foundEnd)
                                                break;
                                        }
                                    }
                                }

                                if (!foundBegin && !foundEnd)
                                {
                                    Debug.WriteLine("Use default packet seperator for: " + GetIp);

                                    string packetBeginString = ClassPeerPacketSetting.PacketSeperatorBegin.GetStringFromByteArrayUtf8();
                                    string packetEndString = ClassPeerPacketSetting.PacketSeperatorEnd.GetStringFromByteArrayUtf8();

                                    string data = readPacketData.Data.GetStringFromByteArrayUtf8();
                                    int indexOfBegin = data.IndexOf(packetBeginString);

                                    if (indexOfBegin > -1)
                                        foundBegin = true;

                                    int indexOfEnd = data.IndexOf(packetEndString);

                                    if (indexOfBegin > -1)
                                        foundEnd = true;

                                    if (foundBegin && foundEnd)
                                        data = data.GetStringBetweenTwoStrings(packetBeginString, packetEndString);
                                    else if (foundBegin && !foundEnd)
                                        data = data.Substring(indexOfBegin + packetBeginString.Length);

                                    foreach (var dataByte in data.GetByteArray())
                                        listData.Add(dataByte);


                                    if (foundBegin && foundEnd)
                                        break;

                                }
                            }
                            else
                            {

                                string packetBeginString = packetBegin.GetStringFromByteArrayUtf8();
                                string packetEndString = packetEnd.GetStringFromByteArrayUtf8();

                                string data = readPacketData.Data.GetStringFromByteArrayUtf8();
                                int indexOfBegin = data.IndexOf(packetBeginString);

                                if (indexOfBegin > -1)
                                    foundBegin = true;

                                int indexOfEnd = data.IndexOf(packetEndString);

                                if (indexOfBegin > -1)
                                    foundEnd = true;

                                if (foundBegin && foundEnd)
                                    data = data.GetStringBetweenTwoStrings(packetBeginString, packetEndString);
                                else if (foundBegin && !foundEnd)
                                    data = data.Substring(indexOfBegin + packetBeginString.Length);

                                Debug.WriteLine("Begin: " + indexOfBegin + " | End: " + indexOfEnd);

                                foreach (var dataByte in data.GetByteArray())
                                    listData.Add(dataByte);


                                if (foundBegin && foundEnd)
                                    break;


                            }

                            */
                       // }

                        /*
                        if (foundBegin && foundEnd)
                        {
                            readPacketData.Data = listData.GetList.ToArray();
                            readPacketData.Status = true;
                            Debug.WriteLine("Packet data result: " + readPacketData.Data.GetStringFromByteArrayUtf8());
                        }*/
                    //}
                }
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
