using Newtonsoft.Json;
using SeguraChain_IO_Cache_Network_System.Config;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Config;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Packet.Recv;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Packet.Recv.Enum;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Packet.Recv.Object;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Packet.Send.Object;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_IO_Cache_Network_System.Client
{
    public class ClassIoCacheClientObject
    {
        public bool IoCacheClientStatus;
        private TcpClient _tcpClient;
        private CancellationTokenSource _cancellationIoCacheClient;
        private ClassCacheIoSystem _cacheIoSystem;
        private ClassConfigIoNetworkCache _configIoNetworkCache;
        private DisposableList<string> _listPacketData;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="cacheIoSystem"></param>
        /// <param name="cancellationIoCacheServer"></param>
        public ClassIoCacheClientObject(TcpClient tcpClient, ClassCacheIoSystem cacheIoSystem, ClassConfigIoNetworkCache configIoNetworkCache, CancellationTokenSource cancellationIoCacheServer)
        {
            IoCacheClientStatus = true;
            _tcpClient = tcpClient;
            _cacheIoSystem = cacheIoSystem;
            _configIoNetworkCache = configIoNetworkCache;
            _cancellationIoCacheClient = CancellationTokenSource.CreateLinkedTokenSource(cancellationIoCacheServer.Token);
        }

        /// <summary>
        /// Listen the IO Cache client packets.
        /// </summary>
        /// <returns></returns>
        public void ListenIoCacheClient()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        using (_listPacketData = new DisposableList<string>(false, 0, new List<string>()))
                        {
                            using (NetworkStream networkStream = new NetworkStream(_tcpClient.Client))
                            {
                                while (IoCacheClientStatus)
                                {
                                    byte[] packetData = new byte[_configIoNetworkCache.BlockchainConfigIoNetworkCacheServer.server_packet_size];

                                    int packetLength = await networkStream.ReadAsync(packetData, 0, packetData.Length, _cancellationIoCacheClient.Token);

                                    if (packetLength > 0)
                                    {
                                        _listPacketData.Add(packetData.GetStringFromByteArrayUtf8());

                                        if (_listPacketData[_listPacketData.Count - 1].Contains(ClassIoCacheConfigPacket.IoCacheConfigPacket))
                                        {
                                            if (!ClassUtility.TryDeserialize(_listPacketData.GetList.SelectMany(x => x).ToString(), out ClassIoCachePacketRecv ioCacheBlockRecv, ObjectCreationHandling.Auto))
                                                break;

                                            // Clean up.
                                            _listPacketData.Clear();

                                            if (!await HandlePacket(ioCacheBlockRecv))
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception error)
                    {
#if DEBUG
                        Debug.WriteLine("Error on listen io cache client packets. Exception: " + error.Message);
#endif
                        CloseIoCacheClient();
                    }

                }, _cancellationIoCacheClient.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Close IO Cache client.
        /// </summary>
        public void CloseIoCacheClient()
        {
            IoCacheClientStatus = false;

            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
#if !DEBUG
            catch
            {
#else
            catch (Exception error)
            { 
                Debug.WriteLine(error.Message);
#endif
            }

            _listPacketData?.Clear();
        }

        /// <summary>
        /// Handle packet.
        /// </summary>
        /// <param name="ioCacheBlockRecv"></param>
        /// <returns></returns>
        private async Task<bool> HandlePacket(ClassIoCachePacketRecv ioCacheBlockRecv)
        {
            try
            {
                switch (ioCacheBlockRecv.IoPacketRecvType)
                {
                    case ClassIoPacketRecvEnumType.GetIoBlockIndexes:
                        {
                            if (!await SendPacketData(new ClassPacketIoCacheBlockIndexes(await _cacheIoSystem.GetIoCacheBlockIndexes(_cancellationIoCacheClient))))
                                return false;
                        }
                        break;
                    case ClassIoPacketRecvEnumType.GetIoBlock:
                        {
                            if (!ClassUtility.TryDeserialize(ioCacheBlockRecv.IoPacketReceived, out ClassPacketIoCacheAskBlock ioCacheBlockAskBlock, ObjectCreationHandling.Auto))
                            {
                                if (!await SendPacketData(new ClassPacketIoCacheSendBlock() { ListBlockObject = await _cacheIoSystem.GetBlockObjectListFromBlockHeightRange(ioCacheBlockAskBlock.BlockHeightStart, ioCacheBlockAskBlock.BlockHeightEnd, new HashSet<long>(), new SortedList<long, ClassBlockObject>(), true, false, _cancellationIoCacheClient) }))
                                    return false;
                            }
                        }
                        break;
                    case ClassIoPacketRecvEnumType.PushIoBlock:
                        {
                            if (!ClassUtility.TryDeserialize(ioCacheBlockRecv.IoPacketReceived, out ClassPacketIoCachePushBlock ioCachePushBlock, ObjectCreationHandling.Auto))
                                return false;

                            if (await _cacheIoSystem.PushOrUpdateListIoBlockObject(ioCachePushBlock.ListBlockObject.Values.ToList(), true, _cancellationIoCacheClient))
                            {
                                if (!await SendPacketData(new ClassPacketIoCachePushBlockResponse() { CountBlockPushed = ioCachePushBlock.ListBlockObject.Count }))
                                    return false;
                            }
                        }
                        break;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send packet data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetData"></param>
        /// <returns></returns>
        public async Task<bool> SendPacketData<T>(T packetData)
        {
            try
            {
                byte[] packetContent = ClassUtility.GetByteArrayFromStringUtf8(JsonConvert.SerializeObject(packetData));

                using(NetworkStream network = new NetworkStream(_tcpClient.Client))
                {
                    await network.WriteAsync(packetContent, 0, packetContent.Length, _cancellationIoCacheClient.Token);
                    await network.FlushAsync(_cancellationIoCacheClient.Token);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
