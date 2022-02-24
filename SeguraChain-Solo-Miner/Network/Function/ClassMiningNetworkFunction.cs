using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using SeguraChain_Solo_Miner.Network.Object;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner.Network.Function
{
    public class ClassMiningNetworkFunction
    {
        private ClassSoloMinerSettingObject _minerSettingObject;
        private ClassMiningNetworkStatsObject _miningNetworkStatsObject;
        private CancellationTokenSource _cancellationTokenMiningNetworkTask;
        private const int DelayTaskAskBlockTemplate = 1000;

        /// <summary>
        /// Contructor.
        /// </summary>
        /// <param name="minerSettingObject"></param>
        /// <param name="miningNetworkStatsObject"></param>
        public ClassMiningNetworkFunction(ClassSoloMinerSettingObject minerSettingObject, ClassMiningNetworkStatsObject miningNetworkStatsObject)
        {
            _minerSettingObject = minerSettingObject;
            _miningNetworkStatsObject = miningNetworkStatsObject;
            _cancellationTokenMiningNetworkTask = new CancellationTokenSource();
        }

        /// <summary>
        /// Start every mining network tasks.
        /// </summary>
        public void StartMiningNetworkTask()
        {
            TaskAskBlockTemplateFromPeer();
        }

        #region Network tasks


        /// <summary>
        /// Start a task who contact peers to retrieve back the current blocktemplate.
        /// </summary>
        private void TaskAskBlockTemplateFromPeer()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {

                        ClassApiPeerPacketSendBlockTemplate apiPeerPacketSendBlockTemplate = await ClassApiClientUtility.GetBlockTemplateFromExternalSyncMode(_minerSettingObject.SoloMinerNetworkSetting.peer_ip_target, _minerSettingObject.SoloMinerNetworkSetting.peer_api_port_target, _minerSettingObject.SoloMinerNetworkSetting.peer_api_max_connection_delay, _cancellationTokenMiningNetworkTask);

                        string currentBlockHash = string.Empty;
                        string currentMiningPowacSettingSerializedString = string.Empty;

                        if (apiPeerPacketSendBlockTemplate != null)
                        {
                            if (!apiPeerPacketSendBlockTemplate.CurrentBlockHash.IsNullOrEmpty(false, out _) && apiPeerPacketSendBlockTemplate.CurrentMiningPoWaCSetting != null)
                            {
                                if (apiPeerPacketSendBlockTemplate.CurrentBlockHash.Length == BlockchainSetting.BlockHashHexSize)
                                {

                                    currentBlockHash = apiPeerPacketSendBlockTemplate.CurrentBlockHash;

                                    if (ClassMiningPoWaCUtility.CheckMiningPoWaCSetting(apiPeerPacketSendBlockTemplate.CurrentMiningPoWaCSetting))
                                        currentMiningPowacSettingSerializedString = ClassUtility.SerializeData(apiPeerPacketSendBlockTemplate.CurrentMiningPoWaCSetting);
#if DEBUG
                                    else
                                        Debug.WriteLine("invalid setting: " + ClassUtility.SerializeData(apiPeerPacketSendBlockTemplate.CurrentMiningPoWaCSetting));
#endif
                                }
                            }
                        }

                        if (!currentMiningPowacSettingSerializedString.IsNullOrEmpty(false, out _))
                        {
                            if (ClassUtility.TryDeserialize(currentMiningPowacSettingSerializedString, out ClassMiningPoWaCSettingObject miningPoWaCSettingObject, ObjectCreationHandling.Replace))
                            {
                                if (miningPoWaCSettingObject != null)
                                    _miningNetworkStatsObject.UpdateMiningPoWacSetting(miningPoWaCSettingObject);
                            }


                            if (ClassBlockUtility.GetBlockTemplateFromBlockHash(currentBlockHash, out ClassBlockTemplateObject blockTemplateObject))
                            {
                                if (blockTemplateObject != null)
                                    _miningNetworkStatsObject.UpdateBlocktemplate(blockTemplateObject);
                            }
                        }

                        try
                        {
                            await Task.Delay(DelayTaskAskBlockTemplate, _cancellationTokenMiningNetworkTask.Token);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }, _cancellationTokenMiningNetworkTask.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once cancelled.
            }
        }

        #endregion

        #region Packet API Request


        /// <summary>
        /// Send a request who push a mining share to a peer target, then retrieve back the response from the peer.
        /// </summary>
        /// <param name="miningPoWaCShareObject"></param>
        /// <returns></returns>
        private async Task<ClassMiningPoWaCEnumStatus> SendMiningShareRequest(ClassMiningPoWaCShareObject miningPoWaCShareObject, CancellationTokenSource cancellation)
        {
            ClassMiningPoWaCEnumStatus miningShareResult = ClassMiningPoWaCEnumStatus.SUBMIT_NETWORK_ERROR;

            try
            {
                miningShareResult = await ClassApiClientUtility.SubmitSoloMiningShareFromExternalSyncMode(_minerSettingObject.SoloMinerNetworkSetting.peer_ip_target, _minerSettingObject.SoloMinerNetworkSetting.peer_api_port_target, _minerSettingObject.SoloMinerNetworkSetting.peer_api_max_connection_delay, miningPoWaCShareObject, cancellation);
            }
            catch (System.Exception error)
            {
                ClassLog.WriteLine("Exception on sending the mining share request to the peer API Server " + _minerSettingObject.SoloMinerNetworkSetting.peer_ip_target +":"+ _minerSettingObject.SoloMinerNetworkSetting.peer_api_port_target+" | Details: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
            }
            return miningShareResult;
        }

        #endregion

        #region Mining network task

        /// <summary>
        /// Send a mining share who potentialy can found the block.
        /// </summary>
        /// <param name="miningPoWaCShareObject"></param>
        public void UnlockCurrentBlockTemplate(ClassMiningPoWaCShareObject miningPoWaCShareObject)
        {
            try
            {
#if DEBUG
                Debug.WriteLine(miningPoWaCShareObject.BlockHeight + " share seems to potentially found the block, send the request..");
#endif

                Task.Factory.StartNew(async () =>
                {
                    ClassMiningPoWaCEnumStatus miningShareResponse = ClassMiningPoWaCEnumStatus.EMPTY_SHARE;

                    bool taskDone = false;
                    long taskTimestampEnd = ClassUtility.GetCurrentTimestampInSecond() + BlockchainSetting.PeerApiMaxConnectionDelay;
                    CancellationTokenSource cancellationTokenRequest = new CancellationTokenSource();

                    try
                    {
                        await Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                miningShareResponse = await SendMiningShareRequest(miningPoWaCShareObject, cancellationTokenRequest);
                            }
                            catch
                            {
                                // Ignored.
                            }

                            taskDone = true;
                        }, cancellationTokenRequest.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignored, catch the exception once the task is cancelled.
                    }


                    // Waiting all tasks done.
                    while (taskTimestampEnd >= ClassUtility.GetCurrentTimestampInSecond())
                    {
                        if (taskDone)
                            break;

                        await Task.Delay(100, _cancellationTokenMiningNetworkTask.Token);
                    }

                    cancellationTokenRequest.Cancel();

#if DEBUG
                    Debug.WriteLine("Max vote mining share(s) returned: " + miningShareResponse + " from the peers.");
#endif
                    switch (miningShareResponse)
                    {
                        case ClassMiningPoWaCEnumStatus.BLOCK_ALREADY_FOUND:
                            ClassLog.WriteLine("The Block Height: " + miningPoWaCShareObject.BlockHeight + " seems to be already found.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                            _miningNetworkStatsObject.IncrementTotalOrphanedBlock();
                            break;
                        case ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE:
                            ClassLog.WriteLine("The Block Height: " + miningPoWaCShareObject.BlockHeight + " seems to be accepted by peers.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                            _miningNetworkStatsObject.IncrementTotalUnlockedBlock();
                            break;
                        case ClassMiningPoWaCEnumStatus.INVALID_SHARE_DATA:
                            ClassLog.WriteLine("The Block Height: " + miningPoWaCShareObject.BlockHeight + " return invalid shares from peers.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                            _miningNetworkStatsObject.IncrementTotalInvalidShare();
                            break;
                        default:
                            ClassLog.WriteLine("The Block Height: " + miningPoWaCShareObject.BlockHeight + " refused from peers.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            _miningNetworkStatsObject.IncrementTotalRefusedShare();
                            break;
                    }

                }, _cancellationTokenMiningNetworkTask.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        #endregion
    }
}
