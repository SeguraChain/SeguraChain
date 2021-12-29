using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Peer.Mining.Instance
{
    public class ClassSoloMiningInstance
    {
        /// <summary>
        /// Node Info.
        /// </summary>
        private string _apiServerIp;
        private string _apiServerOpenNatIp;
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;

        /// <summary>
        /// Wallet info.
        /// </summary>
        private string _walletAddress;

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
        private bool _miningDataInitialized;

        /// <summary>
        /// Mining threads.
        /// </summary>
        private int _totalThreads;
        private Task[] _miningTasks;
        private CancellationTokenSource _cancellationTokenMiningTasks;
        private bool _miningPauseStatus;

        /// <summary>
        /// Mining Stats.
        /// </summary>
        private int[] _totalHash;
        private BigInteger[] _totalHashes;
        private long[] _totalShare;
        private int[] _totalAlreadyShare;
        private int[] _totalUnlockShare;
        private int[] _totalRefusedShare;
        private int[] _totalLowDifficultyShare;
        private List<byte[]> _pocRandomData;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="totalThreads"></param>
        /// <param name="apiServerIp"></param>
        /// <param name="apiServerOpenNatIp"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassSoloMiningInstance(string walletAddress, int totalThreads, string apiServerIp, string apiServerOpenNatIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            _walletAddress = walletAddress;
            _totalThreads = totalThreads;
            _apiServerIp = apiServerIp;
            _apiServerOpenNatIp = apiServerOpenNatIp;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
        }

        #region Manage the mining instance.

        /// <summary>
        /// Initialize mining instance.
        /// </summary>
        private void InitializeMiningInstance()
        {
            if (_currentMiningPocSettingObject == null)
                _currentMiningPocSettingObject = BlockchainSetting.CurrentMiningPoWaCSettingObject(_currentBlockHeight);

            if (_miningTasks == null)
                _miningTasks = new Task[_totalThreads];
            else
                DestroyMiningInstance();

            _pocRandomData = new List<byte[]>();
            _totalHashes = new BigInteger[_totalThreads];
            _totalShare = new long[_totalThreads];
            _totalAlreadyShare = new int[_totalThreads];
            _totalUnlockShare = new int[_totalThreads];
            _totalRefusedShare = new int[_totalThreads];
            _totalLowDifficultyShare = new int[_totalThreads];
            _totalHash = new int[_totalThreads];
            _nextNonce = new long[_totalThreads];
            _maxRangeNonce = new long[_totalThreads];
            _minRangeNonce = new long[_totalThreads];
            _sha3512Mining = new ClassSha3512DigestDisposable[_totalThreads];
            GetTotalHashrate = 0;
            GetMiningStatus = true;
            _miningPauseStatus = false;
            for (int i = 0; i < _totalThreads; i++)
            {
                _pocRandomData.Add(null);
                _sha3512Mining[i] = new ClassSha3512DigestDisposable();
            }
            _walletAddressDecoded = ClassBase58.DecodeWithCheckSum(_walletAddress, true);
        }

        /// <summary>
        /// Destroy mining instance.
        /// </summary>
        private void DestroyMiningInstance()
        {
            // Clean up stats.
            Array.Clear(_totalHashes, 0, _totalThreads);
            Array.Clear(_totalShare, 0, _totalThreads);
            Array.Clear(_totalAlreadyShare, 0, _totalThreads);
            Array.Clear(_totalRefusedShare, 0, _totalThreads);
            Array.Clear(_totalUnlockShare, 0, _totalThreads);
            Array.Clear(_totalLowDifficultyShare, 0, _totalThreads);
            Array.Clear(_totalHash, 0, _totalThreads);
            Array.Clear(_nextNonce, 0, _totalThreads);
            Array.Clear(_minRangeNonce, 0, _totalThreads);
            Array.Clear(_maxRangeNonce, 0, _totalThreads);
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
        /// Return the mining status.
        /// </summary>
        public bool GetMiningStatus { get; private set; }

        /// <summary>
        /// Return the total hashrate.
        /// </summary>
        public int GetTotalHashrate { get; private set; }

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

        /// <summary>
        /// Return the total amount of valid share.
        /// </summary>
        public int GetTotalAlreadyShare
        {
            get
            {
                int totalValidShare = 0;
                foreach (var total in _totalAlreadyShare)
                    totalValidShare += total;

                return totalValidShare;
            }
        }

        /// <summary>
        /// Return the total amount of unlock share.
        /// </summary>
        public int GetTotalUnlockShare
        {
            get
            {
                int totalUnlockShare = 0;
                foreach (var total in _totalUnlockShare)
                    totalUnlockShare += total;

                return totalUnlockShare;
            }
        }

        /// <summary>
        /// Return the total amount of invalid share.
        /// </summary>
        public int GetTotalRefusedShare
        {
            get
            {
                int totalInvalidShare = 0;
                foreach (var total in _totalRefusedShare)
                    totalInvalidShare += total;

                return totalInvalidShare;
            }
        }

        /// <summary>
        /// Return the total amount of low difficulty share.
        /// </summary>
        public int GetTotalLowDifficultyShare
        {
            get
            {
                int totalLowDifficultyShare = 0;
                foreach (var total in _totalLowDifficultyShare)
                    totalLowDifficultyShare += total;

                return totalLowDifficultyShare;
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
            {
                StopMining();
            }

            InitializeMiningInstance();
            _cancellationTokenMiningTasks = new CancellationTokenSource();
            UpdateMiningBlockFinalTransactionKey();
            UpdateMiningHashrate();

            for (int i = 0; i < _totalThreads; i++)
            {
                RunMiningTask(i);
            }
        }

        /// <summary>
        /// Stop the mining process.
        /// </summary>
        public void StopMining()
        {
            // Indicate the mining status is stopped.
            GetMiningStatus = false;
            _miningPauseStatus = true;
            _miningDataInitialized = false;

            // Cancel tasks.
            try
            {
                if (!_cancellationTokenMiningTasks.IsCancellationRequested)
                {
                    _cancellationTokenMiningTasks.Cancel();
                }
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
        /// Update the mining block final transaction key automatically.
        /// </summary>
        private void UpdateMiningBlockFinalTransactionKey()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (GetMiningStatus)
                    {
                        try
                        {
                            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                            if (lastBlockHeight > BlockchainSetting.GenesisBlockHeight)
                            {
                                if (_currentBlockHeight != lastBlockHeight)
                                {
                                    // Get current mining poc settings.
                                    if (_currentMiningPocSettingObject == null)
                                        _currentMiningPocSettingObject = BlockchainSetting.CurrentMiningPoWaCSettingObject(lastBlockHeight);

                                    bool cancel = false;
                                    long currentPreviousBlockHeight = lastBlockHeight - 1;

                                    var previousBlockObjectInformations = await ClassBlockchainStats.GetBlockInformationData(currentPreviousBlockHeight, _cancellationTokenMiningTasks);
                                    var previousBlockTransactionCount = previousBlockObjectInformations.TotalTransaction;
                                    cancel = previousBlockTransactionCount == 0;

                                    if (previousBlockObjectInformations != null && !cancel)
                                    {

                                        if (_currentPreviousFinalBlockTransactionHash != previousBlockObjectInformations.BlockFinalHashTransaction)
                                        {
                                            ClassBlockObject currentBlockObjectInformations = await ClassBlockchainStats.GetBlockInformationData(lastBlockHeight, _cancellationTokenMiningTasks);

                                            if (currentBlockObjectInformations != null)
                                            {
                                                // Get current mining poc settings.
                                                _currentMiningPocSettingObject = BlockchainSetting.CurrentMiningPoWaCSettingObject(lastBlockHeight);

                                                // Get infos from previous block height.
                                                _currentPreviousFinalBlockTransactionHash = previousBlockObjectInformations.BlockFinalHashTransaction;
                                                _previousFinalBlockTransactionHashKey = ClassMiningPoWaCUtility.GenerateFinalBlockTransactionHashMiningKey(_currentPreviousFinalBlockTransactionHash);
                                                _previousBlockTransactionCount = previousBlockTransactionCount;

                                                // Get infos from current block height.
                                                _currentBlockHash = currentBlockObjectInformations.BlockHash;
                                                _currentBlockDifficulty = currentBlockObjectInformations.BlockDifficulty;

                                                #region Generate ranges of nonce and the new poc random data.

                                                long rangeNonce = BlockchainSetting.CurrentMiningPoWaCSettingObject(_currentBlockHeight).PocShareNonceMax / _totalThreads;

                                                for (int i = 0; i < _nextNonce.Length; i++)
                                                {
                                                    _minRangeNonce[i] = ((rangeNonce * i) - 1);
                                                    _maxRangeNonce[i] = ((rangeNonce * (i + 1)) - 1);
                                                    _nextNonce[i] = _minRangeNonce[i];
                                                    _pocRandomData[i] = ClassMiningPoWaCUtility.GenerateRandomPocData(_currentMiningPocSettingObject, _previousBlockTransactionCount, lastBlockHeight, ClassUtility.GetCurrentTimestampInSecond(), _walletAddressDecoded, _nextNonce[i], out _);

                                                }

                                                #endregion

                                                _currentBlockHeight = lastBlockHeight;
                                                _miningDataInitialized = true;
                                            }
                                        }
                                    }
                                }
                            }
                            await Task.Delay(1000, _cancellationTokenMiningTasks.Token);
                        }
                        catch (Exception error)
                        {
                            if (GetMiningStatus)
                                ClassLog.WriteLine("Error on solo mining instance. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
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

                        await Task.Delay(1000, _cancellationTokenMiningTasks.Token);
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
                    // Initialize timestamp of share.
                    long timestampShare = ClassUtility.GetCurrentTimestampInSecond();

                    while (GetMiningStatus)
                    {
                        try
                        {
                            if (_miningDataInitialized)
                            {
                                // Retrieve back the current mining poc setting if null.
                                if (_currentMiningPocSettingObject == null)
                                    _currentMiningPocSettingObject = BlockchainSetting.CurrentMiningPoWaCSettingObject(_currentBlockHeight);
                                else
                                {

                                    // Put the thread in pause pending an update.
                                    while (_miningPauseStatus)
                                        await Task.Delay(100, _cancellationTokenMiningTasks.Token);

                                    // Restart explored nonces, and generate a new random PoC data.
                                    if (_nextNonce[idThread] >= BlockchainSetting.CurrentMiningPoWaCSettingObject(_currentBlockHeight).PocShareNonceMax || _nextNonce[idThread] >= _maxRangeNonce[idThread])
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
                                    ClassMiningPoWaCShareObject pocShareObject = ClassMiningPoWaCUtility.DoPoWaCShare(_currentMiningPocSettingObject, _walletAddress, _currentBlockHeight, _currentBlockHash, _currentBlockDifficulty, _previousFinalBlockTransactionHashKey, _pocRandomData[idThread], _nextNonce[idThread], timestampShare, _sha3512Mining[idThread], _walletAddressDecoded);

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
                                        {
                                            ClassBlockEnumMiningShareVoteStatus unlockResult = await ClassBlockchainDatabase.UnlockCurrentBlockAsync(_currentBlockHeight, pocShareObject, false, _apiServerIp, _apiServerOpenNatIp, false, false, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenMiningTasks);
                                         
                                            switch (unlockResult)
                                            {
                                                case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED:
                                                    _totalUnlockShare[idThread]++;
                                                    await ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_apiServerIp, _apiServerOpenNatIp, string.Empty, pocShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                    break;
                                                case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS:
                                                case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND:
                                                    _totalAlreadyShare[idThread]++;
                                                    await ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_apiServerIp, _apiServerOpenNatIp, string.Empty, pocShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                    break;
                                                case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED:
                                                    _totalRefusedShare[idThread]++;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                await Task.Delay(1, _cancellationTokenMiningTasks.Token);
                        }
                        catch (Exception error)
                        {
                            if (GetMiningStatus)
                                ClassLog.WriteLine("Error on solo mining instance. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
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

        #endregion
    }

}
