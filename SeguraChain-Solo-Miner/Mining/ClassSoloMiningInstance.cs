using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;
using SeguraChain_Solo_Miner.Network.Function;
using SeguraChain_Solo_Miner.Network.Object;
using SeguraChain_Solo_Miner.Setting.Enum;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner.Mining
{
    public class ClassSoloMiningInstance
    {
        /// <summary>
        /// Network objects.
        /// </summary>
        private ClassMiningNetworkStatsObject _miningNetworkStatsObject;
        private ClassMiningNetworkFunction _miningNetworkFunction;

        /// <summary>
        /// Mining settings.
        /// </summary>
        private ClassSoloMinerSettingObject _soloMinerSettingObject;


        /// <summary>
        /// Current block informations.
        /// </summary>
        private byte[] _previousFinalBlockTransactionHashKey;
        private int _previousBlockTransactionCount;
        private long _currentBlockHeight;
        private string _currentBlockHash;
        private string _currentPreviousFinalBlockTransactionHash;
        private BigInteger _currentBlockDifficulty;
        private long[] _nextNonce;
        private long[] _minRangeNonce;
        private long[] _maxRangeNonce;
        private ClassSha3512DigestDisposable[] _sha3512Mining;
        private ClassMiningPoWaCSettingObject _currentMiningPocSettingObject;
        private byte[] _walletAddressDecoded;

        /// <summary>
        /// Mining threads.
        /// </summary>
        private Task[] _miningTasks;
        private CancellationTokenSource _cancellationTokenMiningTasks;

        /// <summary>
        /// Mining Stats.
        /// </summary>
        private int[] _totalHash;
        private BigInteger[] _totalHashes;
        private long[] _totalShare;
        private List<byte[]> _pocRandomData;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="soloMinerSettingObject"></param>
        /// <param name="miningNetworkStatsObject"></param>
        /// <param name="miningNetworkFunction"></param>
        public ClassSoloMiningInstance(ClassSoloMinerSettingObject soloMinerSettingObject, ClassMiningNetworkStatsObject miningNetworkStatsObject, ClassMiningNetworkFunction miningNetworkFunction)
        {
            _soloMinerSettingObject = soloMinerSettingObject;
            _miningNetworkStatsObject = miningNetworkStatsObject;
            _miningNetworkFunction = miningNetworkFunction;
            GetSetMiningPauseStatus = false;
        }

        #region Manage the mining instance.

        /// <summary>
        /// Initialize mining instance.
        /// </summary>
        private void InitializeMiningInstance()
        {
            if (_currentMiningPocSettingObject == null)
                _currentMiningPocSettingObject = _miningNetworkStatsObject.GetCurrentMiningPoWacSetting();

            if (_miningTasks == null)
                _miningTasks = new Task[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            else
                DestroyMiningInstance();

            _pocRandomData = new List<byte[]>();
            _totalHashes = new BigInteger[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _totalShare = new long[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _totalHash = new int[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _nextNonce = new long[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _maxRangeNonce = new long[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _minRangeNonce = new long[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            _sha3512Mining = new ClassSha3512DigestDisposable[_soloMinerSettingObject.SoloMinerThreadSetting.max_thread];
            GetTotalHashrate = 0;
            GetMiningStatus = true;
            GetSetMiningPauseStatus = true;
            for (int i = 0; i < _soloMinerSettingObject.SoloMinerThreadSetting.max_thread; i++)
            {
                _pocRandomData.Add(null);
                _sha3512Mining[i] = new ClassSha3512DigestDisposable();
            }
            _walletAddressDecoded = ClassBase58.DecodeWithCheckSum(_soloMinerSettingObject.SoloMinerWalletSetting.wallet_address, true);
        }

        /// <summary>
        /// Destroy mining instance.
        /// </summary>
        private void DestroyMiningInstance()
        {
            // Clean up stats.
            Array.Clear(_totalHashes, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_totalShare, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_totalHash, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_nextNonce, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_minRangeNonce, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_maxRangeNonce, 0, _soloMinerSettingObject.SoloMinerThreadSetting.max_thread);
            Array.Clear(_sha3512Mining, 0, _sha3512Mining.Length);
            Array.Clear(_walletAddressDecoded, 0, _walletAddressDecoded.Length);
            foreach (var task in _miningTasks)
            {
                try
                {
                    task.Dispose();
                }
                catch
                {
                    // Ignored.
                }
            }
            _pocRandomData.Clear();

            GetTotalHashrate = 0;
        }

        #endregion

        #region Mining stats.

        /// <summary>
        /// Return the mining pause status and the also permit to change it.
        /// </summary>
        public bool GetSetMiningPauseStatus { get; set; }

        /// <summary>
        /// Return the mining status.
        /// </summary>
        public bool GetMiningStatus { get; private set; }

        /// <summary>
        /// Return the total hashrate.
        /// </summary>
        public BigInteger GetTotalHashrate { get; private set; }

        /// <summary>
        /// Return the total amount of hashes.
        /// </summary>
        public BigInteger GetTotalHashes
        {
            get
            {
                BigInteger totalHashes = 0;
                foreach (var total in _totalHashes)
                    totalHashes += total;

                return totalHashes;
            }
        }

        /// <summary>
        /// Return the total amount of share.
        /// </summary>
        public long GetTotalShare
        {
            get
            {
                long totalShare = 0;
                foreach (var total in _totalShare)
                    totalShare += total;

                return totalShare;
            }
        }

        #endregion

        #region Manage mining.

        /// <summary>
        /// Start the mining process.
        /// </summary>
        public void StartMining()
        {
            if (GetMiningStatus)
                StopMining();

            InitializeMiningInstance();
            _cancellationTokenMiningTasks = new CancellationTokenSource();
            UpdateMiningBlockTemplateTarget();
            UpdateMiningHashrate();

            for (int i = 0; i < _soloMinerSettingObject.SoloMinerThreadSetting.max_thread; i++)
                RunMiningTask(i);
        }

        /// <summary>
        /// Stop the mining process.
        /// </summary>
        public void StopMining()
        {
            // Indicate the mining status is stopped.
            GetMiningStatus = false;
            GetSetMiningPauseStatus = true;

            // Cancel tasks.
            try
            {
                if (!_cancellationTokenMiningTasks.IsCancellationRequested)
                    _cancellationTokenMiningTasks.Cancel();
            }
            catch
            {
                // Ignored.
            }
            DestroyMiningInstance();

        }

        #endregion

        #region Mining tasks.

        /// <summary>
        /// Update the mining blocktemplate target.
        /// </summary>
        private void UpdateMiningBlockTemplateTarget()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (GetMiningStatus)
                    {
                        try
                        {
                            // Wait blocktemplate and current mining setting.
                            while (_miningNetworkStatsObject.GetBlockTemplateObject == null && _miningNetworkStatsObject.GetCurrentMiningPoWacSetting() == null) 
                            {
                                GetSetMiningPauseStatus = true;
                                await Task.Delay(100, _cancellationTokenMiningTasks.Token);
                            }

                            ClassBlockTemplateObject currentBlockTemplate = _miningNetworkStatsObject.GetBlockTemplateObject;
                            _currentMiningPocSettingObject = _miningNetworkStatsObject.GetCurrentMiningPoWacSetting();

                            long lastBlockHeight = currentBlockTemplate.BlockHeight;


                            if (_currentMiningPocSettingObject != null)
                            {
                                if (_currentBlockHeight != lastBlockHeight || _currentPreviousFinalBlockTransactionHash != currentBlockTemplate.BlockPreviousFinalTransactionHash)
                                {
                                    ClassLog.SimpleWriteLine("New blocktemplate target - Block Height: " + currentBlockTemplate.BlockHeight + " | Difficulty: " + currentBlockTemplate.BlockDifficulty + " | Hash: " + currentBlockTemplate.BlockHash, ConsoleColor.Cyan);

                                    // Get infos from previous block height.
                                    _currentPreviousFinalBlockTransactionHash = currentBlockTemplate.BlockPreviousFinalTransactionHash;
                                    _previousFinalBlockTransactionHashKey = ClassMiningPoWaCUtility.GenerateFinalBlockTransactionHashMiningKey(_currentPreviousFinalBlockTransactionHash);
                                    _previousBlockTransactionCount = currentBlockTemplate.BlockPreviousTransactionCount;

                                    // Get infos from current block height.
                                    _currentBlockHash = currentBlockTemplate.BlockHash;
                                    _currentBlockDifficulty = currentBlockTemplate.BlockDifficulty;

                                    #region Generate ranges of nonce and the new poc random data.

                                    long rangeNonce = _currentMiningPocSettingObject.PocShareNonceMax / _soloMinerSettingObject.SoloMinerThreadSetting.max_thread;

                                    for (int i = 0; i < _nextNonce.Length; i++)
                                    {
                                        _minRangeNonce[i] = ((rangeNonce * i) - 1);
                                        _maxRangeNonce[i] = ((rangeNonce * (i + 1)) - 1);
                                        _nextNonce[i] = _minRangeNonce[i];
                                        _pocRandomData[i] = ClassMiningPoWaCUtility.GenerateRandomPocData(_currentMiningPocSettingObject, _previousBlockTransactionCount, lastBlockHeight, ClassUtility.GetCurrentTimestampInSecond(), _walletAddressDecoded, _nextNonce[i], out _);

                                    }

                                    #endregion

                                    _currentBlockHeight = lastBlockHeight;
                                    GetSetMiningPauseStatus = false;
                                }
                            }

                            await Task.Delay(1000, _cancellationTokenMiningTasks.Token);
                        }
                        catch (System.Exception error)
                        {
                            ClassLog.SimpleWriteLine("Error on solo mining instance. Exception: " + error.Message, ConsoleColor.Red);
                        }
                    }
                }, _cancellationTokenMiningTasks.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Update the mining hashrate.
        /// </summary>
        private void UpdateMiningHashrate()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {

                    while (GetMiningStatus)
                    {
                        int totalHashrate = 0;

                        for (int i = 0; i < _totalHash.Length; i++)
                        {
                            int total = _totalHash[i];
                            _totalHash[i] -= total;
                            totalHashrate += total;
                        }

                        GetTotalHashrate = totalHashrate;

                        await Task.Delay(1000);
                    }
                }, _cancellationTokenMiningTasks.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Run a mining task.
        /// </summary>
        /// <param name="idThread"></param>
        private void RunMiningTask(int idThread)
        {
            try
            {
                _miningTasks[idThread] = new Task(async () =>
                {
                    // Apply thread priority.
                    SetMiningThreadPriority();

                    // Initialize timestamp of share.
                    long timestampShare = ClassUtility.GetCurrentTimestampInSecond();

                    while (GetMiningStatus)
                    {
                        try
                        {
                            // Retrieve back the current mining poc setting if null.
                            if (_currentMiningPocSettingObject != null)
                            {

                                // Put the thread in pause pending an update.
                                while (GetSetMiningPauseStatus)
                                    await Task.Delay(100, _cancellationTokenMiningTasks.Token);

                                // Restart explored nonces, and generate a new random PoC data.
                                if (_nextNonce[idThread] >= _currentMiningPocSettingObject.PocShareNonceMax || _nextNonce[idThread] >= _maxRangeNonce[idThread])
                                {
                                    _nextNonce[idThread] = _minRangeNonce[idThread];
                                    timestampShare = ClassUtility.GetCurrentTimestampInSecond();
                                    _pocRandomData[idThread] = ClassMiningPoWaCUtility.GenerateRandomPocData(_currentMiningPocSettingObject, _previousBlockTransactionCount, _currentBlockHeight, timestampShare, _walletAddressDecoded, _nextNonce[idThread], out _);
                                }
                                // Increase nonce.
                                else
                                    _nextNonce[idThread]++;

                                // Intialize PoC random data if null.
                                if (_pocRandomData[idThread] == null)
                                {
                                    timestampShare = ClassUtility.GetCurrentTimestampInSecond();
                                    _pocRandomData[idThread] = ClassMiningPoWaCUtility.GenerateRandomPocData(_currentMiningPocSettingObject, _previousBlockTransactionCount, _currentBlockHeight, timestampShare, _walletAddressDecoded, _nextNonce[idThread], out _);
                                }
                                // Update the timestamp share data of the random poc data.
                                else
                                    _pocRandomData[idThread] = ClassMiningPoWaCUtility.UpdateRandomPocDataTimestampAndBlockHeightTarget(_currentMiningPocSettingObject, _pocRandomData[idThread], _currentBlockHeight, _nextNonce[idThread], out timestampShare);

                                // Build a poc share.
                                ClassMiningPoWaCShareObject pocShareObject = ClassMiningPoWaCUtility.DoPoWaCShare(_currentMiningPocSettingObject, _soloMinerSettingObject.SoloMinerWalletSetting.wallet_address, _currentBlockHeight, _currentBlockHash, _currentBlockDifficulty, _previousFinalBlockTransactionHashKey, _pocRandomData[idThread], _nextNonce[idThread], timestampShare, _sha3512Mining[idThread]);

                                if (pocShareObject != null)
                                {
                                    #region Update miner stats.

                                    _totalHash[idThread]++;
                                    _totalShare[idThread]++;
                                    if (pocShareObject.PoWaCShareDifficulty > 0)
                                        _totalHashes[idThread] += pocShareObject.PoWaCShareDifficulty;

                                    #endregion

                                    // Submit the share if this one reach the difficulty of the block or if this one is higher.
                                    if (pocShareObject.PoWaCShareDifficulty >= _currentBlockDifficulty)
                                        _miningNetworkFunction.UnlockCurrentBlockTemplate(pocShareObject);
                                }
                            }
                        }
                        catch (System.Exception error)
                        {
                            ClassLog.SimpleWriteLine("Error on solo mining instance. Exception: " + error.Message, ConsoleColor.Red);
                        }
                    }

                }, _cancellationTokenMiningTasks.Token);
                _miningTasks[idThread].Start();
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Set the mining thread priority.
        /// </summary>
        private void SetMiningThreadPriority()
        {
            switch (_soloMinerSettingObject.SoloMinerThreadSetting.thread_priority)
            {
                case (int)ClassSoloMinerSettingThreadPriorityEnum.LOWEST:
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    break;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.LOW:
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    break;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.NORMAL:
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    break;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.ABOVE:
                    Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                    break;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.HIGHEST:
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    break;
            }
        }

        #endregion
    }

}
