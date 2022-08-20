using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;
using static SeguraChain_Lib.Other.Object.Network.ClassCustomSocket;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object
{
    public class ClassPeerNetworkClientSyncObject : IDisposable
    {
        /// <summary>
        /// Tcp info and tcp client object.
        /// </summary>
        private ClassCustomSocket _peerSocketClient;
        public bool PeerConnectStatus;

        /// <summary>
        /// Peer informations.
        /// </summary>
        public string PeerIpTarget;
        public int PeerPortTarget;
        public string PeerUniqueIdTarget;

        /// <summary>
        /// Packet received.
        /// </summary>
        public ClassPeerPacketRecvObject PeerPacketReceived;
        public ClassPeerEnumPacketResponse PeerPacketTypeReceived;
        public bool PeerPacketReceivedStatus;
        public bool PeerPacketReceivedIgnored;

        /// <summary>
        /// Peer task status.
        /// </summary>
        public bool PeerTaskStatus;
        private bool _peerTaskKeepAliveStatus;
        private CancellationTokenSource _peerCancellationTokenMain;
        private CancellationTokenSource _peerCancellationTokenDoConnection;
        private CancellationTokenSource _peerCancellationTokenTaskListenPeerPacketResponse;
        private CancellationTokenSource _peerCancellationTokenTaskSendPeerPacketKeepAlive;

        /// <summary>
        /// Network settings.
        /// </summary>
        private ClassPeerNetworkSettingObject _peerNetworkSetting;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;

        /// <summary>
        /// Specifications of the connection opened.
        /// </summary>
        private ClassPeerEnumPacketResponse _packetResponseExpected;
        private DisposableList<ClassReadPacketSplitted> listPacketReceived;
        private bool _keepAlive;


        #region Dispose functions

        ~ClassPeerNetworkClientSyncObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            PeerTaskStatus = false;
            CleanUpTask();
            DisconnectFromTarget();
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerIpTarget"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkClientSyncObject(string peerIpTarget, int peerPort, string peerUniqueId, CancellationTokenSource peerCancellationTokenMain, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            PeerIpTarget = peerIpTarget;
            PeerPortTarget = peerPort;
            PeerUniqueIdTarget = peerUniqueId;
            _peerNetworkSetting = peerNetworkSetting;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _peerCancellationTokenMain = peerCancellationTokenMain;
        }

        /// <summary>
        /// Attempt to send a packet to a peer target.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="cancellation"></param>
        /// <param name="packetResponseExpected"></param>
        /// <param name="keepAlive"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        public async Task<bool> TrySendPacketToPeerTarget(byte[] packet, int peerPort, string peerUniqueId, CancellationTokenSource cancellation, ClassPeerEnumPacketResponse packetResponseExpected, bool keepAlive, bool broadcast)
        {
            bool result = false;

            #region Clean up and cancel previous task.

            CleanUpTask();

            #endregion

            #region Init the client sync object.

            _packetResponseExpected = packetResponseExpected;
            _keepAlive = keepAlive;
            PeerPortTarget = peerPort;
            PeerUniqueIdTarget = peerUniqueId;

            #endregion

            #region Check the current connection status opened to the target.

            if (!PeerConnectStatus || !CheckConnection || !keepAlive)
            {
                DisconnectFromTarget();


                if (!await DoConnection(cancellation))
                    return false;
            }

            #endregion

            #region Send packet and wait packet response.


            if (!await SendPeerPacket(packet, cancellation))
            {
                ClassPeerCheckManager.InputPeerClientNoPacketConnectionOpened(PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject);
                DisconnectFromTarget();
            }
            else
                result = broadcast ? true : await WaitPacketExpected(cancellation);

            #endregion

            return result;
        }

        #region Initialize connection functions.

        /// <summary>
        /// Clean up the task.
        /// </summary>
        private void CleanUpTask()
        {
            PeerPacketReceived = null;
            PeerPacketReceivedStatus = false;
            PeerPacketReceivedIgnored = false;

            CancelTaskDoConnection();

            if (!PeerConnectStatus)
                CancelTaskPeerPacketKeepAlive();

            CancelTaskListenPeerPacketResponse();
        }

        /// <summary>
        /// Check the connection.
        /// </summary>
        private bool CheckConnection => _peerSocketClient.IsConnected();

        /// <summary>
        /// Do connection.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> DoConnection(CancellationTokenSource cancellation)
        {
            bool successConnect = false;
            CancelTaskDoConnection();

            bool taskDone = false;
            long timestampEnd = TaskManager.TaskManager.CurrentTimestampMillisecond + (_peerNetworkSetting.PeerMaxDelayToConnectToTarget * 1000);

            _peerCancellationTokenDoConnection = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationTokenMain.Token, new CancellationTokenSource(_peerNetworkSetting.PeerMaxDelayToConnectToTarget * 1000).Token);

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (!successConnect && timestampEnd > TaskManager.TaskManager.CurrentTimestampMillisecond)
                {
                    try
                    {
                        if (_peerCancellationTokenDoConnection.IsCancellationRequested)
                            break;

                        _peerSocketClient = new ClassCustomSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), false);

                        if (await _peerSocketClient.ConnectAsync(PeerIpTarget, PeerPortTarget))
                            successConnect = _peerSocketClient.IsConnected();
                        else _peerSocketClient?.Kill(SocketShutdown.Both);

                    }
                    catch
                    {
                        // Ignored, catch the exception once the attempt to connect to a peer failed.
                    }


                    await Task.Delay(10);
                }

                taskDone = true;

            }), timestampEnd, _peerCancellationTokenDoConnection, null);


            while (!successConnect && !taskDone)
            {
                if (timestampEnd < TaskManager.TaskManager.CurrentTimestampMillisecond ||
                    _peerCancellationTokenDoConnection.IsCancellationRequested || taskDone)
                    break;

                try
                {
                    await Task.Delay(10, cancellation.Token);
                }
                catch
                {
                    break;
                }
            }

            CancelTaskDoConnection();

            if (successConnect)
            {
                PeerConnectStatus = true;
                return true;
            }
            else
            {
                PeerConnectStatus = false;
                ClassLog.WriteLine("Failed to connect to peer " + PeerIpTarget, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                ClassPeerCheckManager.InputPeerClientAttemptConnect(PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject);
                DisconnectFromTarget();
                return false;
            }

        }


        /// <summary>
        /// Wait the packet expected.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> WaitPacketExpected(CancellationTokenSource cancellation)
        {
            TaskWaitPeerPacketResponse(cancellation);

            while (!PeerPacketReceivedStatus && !PeerPacketReceivedIgnored)
            {

                if (cancellation.IsCancellationRequested ||
                    !PeerConnectStatus || !PeerTaskStatus ||
                    /*_lastPacketReceivedTimestamp + (_peerNetworkSetting.PeerMaxDelayAwaitResponse * 1000) < TaskManager.TaskManager.CurrentTimestampMillisecond ||*/
                    PeerPacketReceived != null)
                {
                    break;
                }

                try
                {
                    await Task.Delay(10, cancellation.Token);
                }
                catch
                {
                    break;
                }
            }

            CancelTaskListenPeerPacketResponse();

            if (PeerPacketReceived == null)
            {
                ClassLog.WriteLine(PeerPacketReceivedIgnored ? "Peer " + PeerIpTarget + " send a packet error." : "Peer " + PeerIpTarget + " don't send a response to the packet sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY, true);
                DisconnectFromTarget();
                return false;
            }
            else
            {
                if (!_keepAlive)
                    DisconnectFromTarget();
                else // Enable keep alive.
                    TaskEnablePeerPacketKeepAlive();
            }

            return PeerPacketReceivedIgnored ? false : true;
        }

        #endregion

        #region Wait packet to receive functions.

        /// <summary>
        /// Task in waiting a packet of response sent by the peer target.
        /// </summary>
        private void TaskWaitPeerPacketResponse(CancellationTokenSource cancellation)
        {
            PeerPacketReceived = null;
            PeerPacketTypeReceived = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET;
            CancelTaskListenPeerPacketResponse();
            _peerCancellationTokenTaskListenPeerPacketResponse = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, _peerCancellationTokenMain.Token);

            PeerTaskStatus = true;

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                listPacketReceived?.Clear();

                using (listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                {
                    try
                    {
                        bool peerTargetExist = false;


                        listPacketReceived.Add(new ClassReadPacketSplitted());

                        while (PeerTaskStatus && PeerConnectStatus)
                        {
                            using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSetting.PeerMaxPacketBufferSize, _peerCancellationTokenTaskListenPeerPacketResponse))
                            {

                                if (!readPacketData.Status)
                                    break;

                                #region Compile the packet.

                                listPacketReceived = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, _peerCancellationTokenTaskListenPeerPacketResponse);

                                #endregion

                                if (listPacketReceived.GetList.Count(x => x.Complete) == 0)
                                    continue;

                                for (int i = 0; i < listPacketReceived.Count; i++)
                                {
                                    if (listPacketReceived[i].Used ||
                                    !listPacketReceived[i].Complete ||
                                     listPacketReceived[i].Packet == null ||
                                     listPacketReceived[i].Packet.Length == 0)
                                        continue;

                                    byte[] base64Packet = null;
                                    bool failed = false;

                                    try
                                    {
                                        base64Packet = Convert.FromBase64String(listPacketReceived[i].Packet);
                                    }
                                    catch
                                    {
                                        failed = true;
                                    }

                                    listPacketReceived[i].Used = true;
                                    listPacketReceived[i].Packet.Clear();

                                    if (failed)
                                        continue;

                                    ClassPeerPacketRecvObject peerPacketReceived = new ClassPeerPacketRecvObject(base64Packet, out bool status);

                                    if (!status)
                                        continue;


                                    if (!peerTargetExist)
                                    {
                                        if (ClassPeerDatabase.ContainsPeer(PeerIpTarget, PeerUniqueIdTarget))
                                        {
                                            peerTargetExist = true;
                                            ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                                        }
                                    }

                                    PeerPacketTypeReceived = peerPacketReceived.PacketOrder;

                                    if (peerPacketReceived.PacketOrder != _packetResponseExpected)
                                    {
                                        if (peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET ||
                                            peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION ||
                                            peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_TIMESTAMP ||
                                            peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                                            peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                                        {
                                            PeerPacketReceivedIgnored = true;
                                        }
                                    }
                                    else
                                    {
                                        if (peerTargetExist)
                                            ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketReceived.PeerLastTimestampSignatureWhitelist;

                                        PeerPacketReceived = peerPacketReceived;


                                        PeerPacketReceivedStatus = true;
                                        PeerTaskStatus = false;
                                        break;
                                    }
                                }


                                listPacketReceived.GetList.RemoveAll(x => x.Used);
                            }
                        }
                        

                    }
                    catch
                    {
                        PeerTaskStatus = false;

                        if (!CheckConnection)
                            PeerConnectStatus = false;
                    }
                }

                PeerTaskStatus = false;


            }), 0, _peerCancellationTokenTaskListenPeerPacketResponse, null);

        }

        /// <summary>
        /// Cancel the token dedicated to the networkstream who listen peer packets.
        /// </summary>
        private void CancelTaskListenPeerPacketResponse()
        {
            try
            {
                if (_peerCancellationTokenTaskListenPeerPacketResponse != null)
                {
                    if (!_peerCancellationTokenTaskListenPeerPacketResponse.IsCancellationRequested)
                        _peerCancellationTokenTaskListenPeerPacketResponse.Cancel();

                }
            }
            catch
            {
                // Ignored.
            }
        }

        #endregion

        #region Enable Keep alive functions.

        /// <summary>
        /// Enable a task who send a packet of keep alive to the peer target.
        /// </summary>
        private void TaskEnablePeerPacketKeepAlive()
        {

            CancelTaskPeerPacketKeepAlive();
            _peerCancellationTokenTaskSendPeerPacketKeepAlive = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationTokenMain.Token);

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                _peerTaskKeepAliveStatus = true;

                var peerObject = ClassPeerDatabase.GetPeerObject(PeerIpTarget, PeerUniqueIdTarget);

                while (peerObject == null && PeerConnectStatus && _peerTaskKeepAliveStatus)
                {
                    peerObject = ClassPeerDatabase.GetPeerObject(PeerIpTarget, PeerUniqueIdTarget);

                    if (_peerCancellationTokenTaskSendPeerPacketKeepAlive.IsCancellationRequested)
                        break;

                    await Task.Delay(1000);

                }

                ClassPeerPacketSendObject sendObject = null;

                while (PeerConnectStatus && PeerConnectStatus && _peerTaskKeepAliveStatus && sendObject == null)
                {
                    if (sendObject == null)
                    {
                        sendObject = new ClassPeerPacketSendObject(_peerNetworkSetting.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                        {
                            PacketOrder = ClassPeerEnumPacketSend.ASK_KEEP_ALIVE,
                            PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                            {
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }),
                        };
                    }
                    await Task.Delay(1000);
                }

                while (PeerConnectStatus && _peerTaskKeepAliveStatus)
                {
                    try
                    {
                        if (_peerCancellationTokenTaskSendPeerPacketKeepAlive.IsCancellationRequested)
                        {
                            _peerTaskKeepAliveStatus = false;
                            break;
                        }

                        sendObject.PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                        {
                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                        });

                        if (!await SendPeerPacket(sendObject.GetPacketData(), _peerCancellationTokenTaskSendPeerPacketKeepAlive))
                        {
                            PeerConnectStatus = false;
                            _peerTaskKeepAliveStatus = false;
                            break;
                        }

                        await Task.Delay(5 * 1000);
                    }
                    catch (SocketException)
                    {
                        _peerTaskKeepAliveStatus = false;

                        if (!CheckConnection)
                            PeerConnectStatus = false;

                        break;
                    }
                    catch (TaskCanceledException)
                    {
                        _peerTaskKeepAliveStatus = false;
                        break;
                    }
                }

            }), 0, _peerCancellationTokenTaskSendPeerPacketKeepAlive, null);

        }

        /// <summary>
        /// Cancel the token of the task who send packet of keep alive to the part target.
        /// </summary>
        private void CancelTaskPeerPacketKeepAlive()
        {
            _peerTaskKeepAliveStatus = false;

            try
            {
                if (_peerCancellationTokenTaskSendPeerPacketKeepAlive != null)
                {
                    if (!_peerCancellationTokenTaskSendPeerPacketKeepAlive.IsCancellationRequested)
                        _peerCancellationTokenTaskSendPeerPacketKeepAlive.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }
        }

        /// <summary>
        /// Cancel the token of the task who open the connection to the peer target.
        /// </summary>
        private void CancelTaskDoConnection()
        {
            try
            {
                if (_peerCancellationTokenDoConnection != null)
                {
                    if (!_peerCancellationTokenDoConnection.IsCancellationRequested)
                        _peerCancellationTokenDoConnection.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }
        }

        #endregion

        #region Manage TCP Connection.

        /// <summary>
        /// Send a packet to the peer target.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> SendPeerPacket(byte[] packet, CancellationTokenSource cancellation)
        {
            try
            {
                if (!_peerSocketClient.IsConnected())
                    return false;

                return await _peerSocketClient.TrySendSplittedPacket(ClassUtility.GetByteArrayFromStringUtf8(Convert.ToBase64String(packet) + ClassPeerPacketSetting.PacketPeerSplitSeperator), cancellation, _peerNetworkSetting.PeerMaxPacketSplitedSendSize);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnect from target.
        /// </summary>
        public void DisconnectFromTarget()
        {
            listPacketReceived?.Clear();
            PeerConnectStatus = false;
            CancelTaskDoConnection();
            CancelTaskPeerPacketKeepAlive();
            CancelTaskListenPeerPacketResponse();
            _peerSocketClient?.Kill(SocketShutdown.Both);
        }



        #endregion
    }
}
