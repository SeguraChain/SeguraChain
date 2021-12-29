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
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object
{
    public class ClassPeerNetworkClientSyncObject : IDisposable
    {
        /// <summary>
        /// Tcp info and tcp client object.
        /// </summary>
        private Socket _peerSocketClient;
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
        public bool PeerPacketReceivedStatus;
        public bool PeerPacketReceivedIgnored;
        private long _lastPacketReceivedTimestamp;

        /// <summary>
        /// Peer task status.
        /// </summary>
        public bool PeerTaskStatus;
        private bool _peerTaskKeepAliveStatus;
        private CancellationTokenSource _peerCancellationTokenMain;
        private CancellationTokenSource _peerCancellationTokenDoConnection;
        private CancellationTokenSource _peerCancellationTokenTaskWaitPeerPacketResponse;
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

        private Task _taskDoConnection;
        private Task _taskKeepAlive;
        private Task _taskListenPacket;

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
        public async Task<bool> TrySendPacketToPeerTarget(byte[] packet, CancellationTokenSource cancellation, ClassPeerEnumPacketResponse packetResponseExpected, bool keepAlive, bool broadcast)
        {
            try
            {

                #region Clean up and cancel previous task.

                CleanUpTask();

                #endregion

                _packetResponseExpected = packetResponseExpected;
                _keepAlive = keepAlive;

                #region Check the current connection status opened to the target.

                if (PeerConnectStatus)
                {
                    if (!CheckConnection())
                        DisconnectFromTarget();
                }
                else
                    DisconnectFromTarget();

                #endregion

                #region Reconnect to the target ip if the connection is not opened or dead.

                if (!PeerConnectStatus)
                {
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
                   return broadcast ? true : await WaitPacketExpected(cancellation);

                #endregion

            }
            catch (Exception error)
            {
                if (error is OperationCanceledException || error is TaskCanceledException)
                    Dispose();
            }


            return false;
        }

        #region Initialize connection functions.

        /// <summary>
        /// Clean up the task.
        /// </summary>
        private void CleanUpTask()
        {
            PeerPacketReceivedStatus = false;
            PeerPacketReceivedIgnored = false;

            CancelTaskDoConnection();

            if (!PeerConnectStatus)
                CancelTaskPeerPacketKeepAlive();

            CancelTaskWaitPeerPacketResponse();
            CancelTaskListenPeerPacketResponse();
        }

        /// <summary>
        /// Check the connection.
        /// </summary>
        private bool CheckConnection()
        {
            if (_peerSocketClient != null)
            {
                try
                {
                    PeerConnectStatus = ClassUtility.SocketIsConnected(_peerSocketClient);
                }
                catch
                {
                    PeerConnectStatus = false;
                }
            }
            else
                PeerConnectStatus = false;

            return PeerConnectStatus;
        }

        /// <summary>
        /// Do connection.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> DoConnection(CancellationTokenSource cancellation)
        {
            bool successConnect = false;
            CancelTaskDoConnection();

            long timestampEnd = ClassUtility.GetCurrentTimestampInMillisecond() + _peerNetworkSetting.PeerMaxDelayToConnectToTarget * 1000;

            _peerCancellationTokenDoConnection = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, _peerCancellationTokenMain.Token);

            try
            {
                _taskDoConnection = Task.Factory.StartNew(async () =>
                {
                    while (!successConnect)
                    {
                        try
                        {
                            _peerSocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            await _peerSocketClient.ConnectAsync(PeerIpTarget, PeerPortTarget);
                            successConnect = true;
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }

                }, _peerCancellationTokenDoConnection.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);
            }
            catch
            {
                // ignored.
            }


            while (!successConnect)
            {
                if (timestampEnd < ClassUtility.GetCurrentTimestampInMillisecond())
                    break;

                try
                {
                    await Task.Delay(100, cancellation.Token);
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
                CancelTaskDoConnection();
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
            long timeSpendOnWaiting = 0;
            _lastPacketReceivedTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();

            TaskWaitPeerPacketResponse(cancellation);

            while (!PeerPacketReceivedStatus && !PeerPacketReceivedIgnored)
            {
                if (cancellation.IsCancellationRequested)
                {
                    PeerConnectStatus = false;
                    PeerTaskStatus = false;
                    break;
                }

                if (!PeerConnectStatus)
                {
                    PeerConnectStatus = false;
                    PeerTaskStatus = false;
                    break;
                }

                if (_lastPacketReceivedTimestamp + (_peerNetworkSetting.PeerMaxDelayAwaitResponse * 1000) < ClassUtility.GetCurrentTimestampInMillisecond())
                {
                    PeerConnectStatus = false;
                    PeerTaskStatus = false;
                    break;
                }

                if (PeerPacketReceived != null)
                {
                    PeerConnectStatus = false;
                    PeerTaskStatus = false;
                    break;
                }

                if (timeSpendOnWaiting >= 1000)
                {
                    timeSpendOnWaiting = 0;

                    if (!ClassUtility.SocketIsConnected(_peerSocketClient))
                    {
                        PeerConnectStatus = false;
                        PeerTaskStatus = false;
                        break;
                    }
                }

                try
                {
                    await Task.Delay(100, cancellation.Token);
                    timeSpendOnWaiting += 100;
                }
                catch
                {
                    break;
                }
            }

            CancelTaskListenPeerPacketResponse();
            CancelTaskWaitPeerPacketResponse();

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
            CancelTaskWaitPeerPacketResponse();
            CancelTaskListenPeerPacketResponse();
            _peerCancellationTokenTaskWaitPeerPacketResponse = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, _peerCancellationTokenMain.Token);
            _peerCancellationTokenTaskListenPeerPacketResponse = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, _peerCancellationTokenMain.Token);

            PeerTaskStatus = true;
            try
            {
                 _taskListenPacket = Task.Factory.StartNew(async () =>
                 {
                     bool peerTargetExist = false;
                     long packetSizeCount = 0;

                     listPacketReceived?.Clear();

                     using (listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                     {
                         listPacketReceived.Add(new ClassReadPacketSplitted());

                         try
                         {
                             byte[] packetBufferOnReceive = new byte[_peerNetworkSetting.PeerMaxPacketBufferSize];

                             using (NetworkStream networkStream = new NetworkStream(_peerSocketClient))
                             {
                                 while (PeerTaskStatus && PeerConnectStatus)
                                 {
                                     if (IsCancelledOrDisconnected())
                                         break;

                                     int packetLength = await networkStream.ReadAsync(packetBufferOnReceive, 0, packetBufferOnReceive.Length, _peerCancellationTokenTaskListenPeerPacketResponse.Token);

                                     _lastPacketReceivedTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();

                                     if (packetLength > 0)
                                     {
                                         foreach (byte dataByte in packetBufferOnReceive)
                                         {
                                             if (IsCancelledOrDisconnected()) break;

                                             char character = (char)dataByte;

                                             if (character != '\0')
                                             {

                                                 if (character == ClassPeerPacketSetting.PacketPeerSplitSeperator)
                                                 {
                                                     listPacketReceived[listPacketReceived.Count - 1].Complete = true;
                                                     break;
                                                 }
                                                 else
                                                 {
                                                     if (ClassUtility.CharIsABase64Character(character))
                                                     {
                                                         listPacketReceived[listPacketReceived.Count - 1].Packet.Add(dataByte);
                                                         packetSizeCount++;
                                                     }
                                                 }
                                             }
                                         }

                                         if (listPacketReceived.GetList.Count(x => x.Complete && x.Packet.Count > 0) > 0)
                                         {

                                             byte[] base64Packet = null;
                                             bool failed = false;

                                             try
                                             {
                                                 base64Packet = Convert.FromBase64String(listPacketReceived[listPacketReceived.Count - 1].Packet.GetList.ToArray().GetStringFromByteArrayUtf8());
                                             }
                                             catch
                                             {
                                                 failed = true;
                                             }

                                             listPacketReceived[listPacketReceived.Count - 1].Packet.Clear();
                                             listPacketReceived[listPacketReceived.Count - 1].Complete = false;

                                             if (!failed)
                                             {
                                                 ClassPeerPacketRecvObject peerPacketReceived = new ClassPeerPacketRecvObject(base64Packet, out bool status);

                                                 if (!peerTargetExist)
                                                     peerTargetExist = ClassPeerDatabase.ContainsPeer(PeerIpTarget, PeerUniqueIdTarget);

                                                 if (peerTargetExist)
                                                     ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerLastPacketReceivedTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                 if (status)
                                                 {
                                                     if (peerPacketReceived != null)
                                                     {
                                                         if (peerPacketReceived.PacketOrder != _packetResponseExpected)
                                                         {
                                                             if (peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET ||
                                                                 peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION ||
                                                                 peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_TIMESTAMP ||
                                                                 peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                                                             {
                                                                 PeerPacketReceivedIgnored = true;
                                                             }
                                                             else
                                                                 ClassPeerCheckManager.InputPeerClientInvalidPacket(PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject);
                                                         }
                                                         else
                                                         {
                                                             if (peerTargetExist)
                                                                 ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketReceived.PeerLastTimestampSignatureWhitelist;

                                                             PeerPacketReceived = peerPacketReceived;
                                                         }
                                                     }
                                                     else
                                                         ClassPeerCheckManager.InputPeerClientInvalidPacket(PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject);
                                                 }
                                             }

                                             PeerPacketReceivedStatus = true;
                                             PeerTaskStatus = false;
                                             break;

                                         }

                                        // If above the max data to receive.
                                        if (packetSizeCount > 0)
                                         {
                                             if (packetSizeCount / 1024 >= ClassPeerPacketSetting.PacketMaxLengthReceive)
                                                 listPacketReceived.Clear();
                                         }
                                     }
                                     else
                                     {
                                         PeerTaskStatus = false;
                                         break;
                                     }

                                     if (IsCancelledOrDisconnected())
                                         break;
                                 }
                             }
                         }
                         catch
                         {
                             PeerTaskStatus = false;

                             if (!CheckConnection())
                                 PeerConnectStatus = false;
                         }
                     }

                 }, _peerCancellationTokenTaskWaitPeerPacketResponse.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Cancel the token of the task who listen packet in receive from a peer target.
        /// </summary>
        public void CancelTaskWaitPeerPacketResponse()
        {
            try
            {
                if (_peerCancellationTokenTaskWaitPeerPacketResponse != null)
                {
                    if (!_peerCancellationTokenTaskWaitPeerPacketResponse.IsCancellationRequested)
                    {
                        _peerCancellationTokenTaskWaitPeerPacketResponse.Cancel();
                        _peerCancellationTokenTaskWaitPeerPacketResponse.Dispose();
                    }
                }
            }
            catch
            {
                // Ignored.
            }
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
                    {
                        _peerCancellationTokenTaskListenPeerPacketResponse.Cancel();
                        _peerCancellationTokenTaskListenPeerPacketResponse.Dispose();
                    }
                }
                if (_taskListenPacket != null)
                {
                    if (_taskListenPacket.IsCompleted || _taskListenPacket.IsFaulted || _taskListenPacket.IsCanceled)
                        _taskListenPacket.Dispose();
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

            if (!_peerTaskKeepAliveStatus)
            {
                CancelTaskPeerPacketKeepAlive();
                _peerCancellationTokenTaskSendPeerPacketKeepAlive = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationTokenMain.Token);

                try
                {
                    _taskKeepAlive = Task.Factory.StartNew(async () =>
                    {
                        _peerTaskKeepAliveStatus = true;

                        ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSetting.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[PeerIpTarget][PeerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                        {
                            PacketOrder = ClassPeerEnumPacketSend.ASK_KEEP_ALIVE,
                            PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                            {
                                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond()
                            }),
                        };

                        while (PeerConnectStatus && _peerTaskKeepAliveStatus)
                        {
                            try
                            {
                                sendObject.PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                                {
                                    PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond()
                                });

                                if (!await SendPeerPacket(sendObject.GetPacketData(), _peerCancellationTokenTaskSendPeerPacketKeepAlive))
                                {
                                    PeerConnectStatus = false;
                                    _peerTaskKeepAliveStatus = false;
                                    break;
                                }

                                await Task.Delay(5000);
                            }
                            catch (SocketException)
                            {
                                _peerTaskKeepAliveStatus = false;

                                if (!CheckConnection())
                                    PeerConnectStatus = false;

                                break;
                            }
                            catch (TaskCanceledException)
                            {
                                _peerTaskKeepAliveStatus = false;
                                break;
                            }
                        }

                    }, _peerCancellationTokenTaskSendPeerPacketKeepAlive.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);

                }
                catch
                {
                    // Ignored, catch the exception once the task is cancelled.
                }
            }
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
                    {
                        _peerCancellationTokenTaskSendPeerPacketKeepAlive.Cancel();
                        _peerCancellationTokenTaskSendPeerPacketKeepAlive.Dispose();
                    }
                }

                if (_taskKeepAlive != null)
                {
                    if (_taskKeepAlive.IsCompleted || _taskKeepAlive.IsFaulted || _taskKeepAlive.IsCanceled)
                        _taskKeepAlive.Dispose();
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
                    {
                        _peerCancellationTokenDoConnection.Cancel();
                        _peerCancellationTokenDoConnection.Dispose();
                    }
                }
                if (_taskDoConnection != null)
                {
                    if (_taskDoConnection.IsCompleted || _taskDoConnection.IsFaulted || _taskDoConnection.IsCanceled)
                        _taskDoConnection.Dispose();
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
                using (NetworkStream networkStream = new NetworkStream(_peerSocketClient))
                    return await networkStream.TrySendSplittedPacket(ClassUtility.GetByteArrayFromStringUtf8(Convert.ToBase64String(packet) + ClassPeerPacketSetting.PacketPeerSplitSeperator), cancellation, _peerNetworkSetting.PeerMaxPacketSplitedSendSize);
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Can't send packet to peer: " + PeerIpTarget + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
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
            

            try
            {
                if (_peerSocketClient != null)
                {
                    if (_peerSocketClient.Connected)
                    {
                        try
                        {
                            _peerSocketClient.Shutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            if (_peerSocketClient != null)
                            {
                                if (_peerSocketClient.Connected)
                                    _peerSocketClient.Close();
                            }
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    if (_peerSocketClient != null)
                        _peerSocketClient.Close();
                }
                catch
                {
                    // Ignored.
                }
            }


        }
        

        /// <summary>
        /// Indicate if the task of client sync is cancelled or connected.
        /// </summary>
        /// <returns></returns>
        private bool IsCancelledOrDisconnected()
        {
            return !PeerTaskStatus || !PeerConnectStatus || _peerCancellationTokenTaskListenPeerPacketResponse.IsCancellationRequested || _peerCancellationTokenTaskWaitPeerPacketResponse.IsCancellationRequested ? true : false;
        }

        #endregion
    }
}
