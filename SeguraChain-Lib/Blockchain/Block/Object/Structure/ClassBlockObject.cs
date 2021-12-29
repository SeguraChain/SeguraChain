using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Block.Object.Structure
{
    public class ClassBlockObject : IDisposable
    {
        #region Dispose functions

        public bool Disposed;

        ~ClassBlockObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed && _blockTransactions?.Count == 0)
                return;


            if (disposing)
            {
                _blockTransactions?.Clear();
                _blockTransactions?.TrimExcess();
                _blockTransactions = null;
                Disposed = true;
            }
        }

        #endregion

        #region Block Data

        public long BlockHeight;
        public BigInteger BlockDifficulty;

        public string BlockHash;
        public ClassMiningPoWaCShareObject BlockMiningPowShareUnlockObject;
        public long TimestampCreate;
        public long TimestampFound;

        public string BlockWalletAddressWinner
        {
            get
            {
                if (BlockHeight > BlockchainSetting.GenesisBlockHeight)
                    return BlockMiningPowShareUnlockObject?.WalletAddress;

                if (BlockHeight == BlockchainSetting.GenesisBlockHeight)
                    return BlockchainSetting.WalletAddressDev(0);

                return null;
            }
        }

        public ClassBlockEnumStatus BlockStatus;
        public bool BlockUnlockValid;
        public long BlockLastChangeTimestamp;
        public long BlockNetworkAmountConfirmations;

        #region About transactions.

        public SortedList<string, ClassBlockTransaction> BlockTransactions
        {
            get
            {
                bool isLocked = false;
                try
                {
                    if (_blockTransactions != null)
                    {
                        if (!Monitor.IsEntered(_blockTransactions))
                        {
                            if (Monitor.TryEnter(_blockTransactions))
                            {
                                isLocked = true;
                                return _blockTransactions;
                            }
                        }
                        else
                            return _blockTransactions;
                    }
                }
                finally
                {
                    if (isLocked)
                        Monitor.Exit(_blockTransactions);
                }

                return null;
            }
            set => _blockTransactions = value;
        }

        private SortedList<string, ClassBlockTransaction> _blockTransactions; // Contains transactions hash has index of key with their associated BlockTransaction.

        public bool BlockTransactionConfirmationCheckTaskDone;
        public long BlockTotalTaskTransactionConfirmationDone;
        public long BlockLastHeightTransactionConfirmationDone;
        public string BlockFinalHashTransaction;
        public bool BlockTransactionFullyConfirmed;
        public BigInteger TotalCoinConfirmed;
        public BigInteger TotalCoinPending;
        public BigInteger TotalFee;
        public int TotalTransaction;
        public int TotalTransactionConfirmed;

        #endregion

        #endregion

        #region Internal stats.

        [JsonIgnore]
        public int BlockTransactionCountInSync;

        [JsonIgnore]
        public int BlockSlowNetworkAmountConfirmations;

        [JsonIgnore]
        public bool BlockIsUpdated;

        [JsonIgnore]
        public bool BlockCloned;

        [JsonIgnore]
        public bool BlockFromCache;

        [JsonIgnore]
        public bool BlockFromMemory;

        #endregion

        #region Shortcut functions.

        [JsonIgnore]
        public bool IsConfirmedByNetwork => BlockNetworkAmountConfirmations >= BlockchainSetting.BlockAmountNetworkConfirmations &&
                                            BlockUnlockValid && BlockStatus == ClassBlockEnumStatus.UNLOCKED;

        [JsonIgnore]
        public bool IsChecked => IsConfirmedByNetwork && BlockTransactionConfirmationCheckTaskDone;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassBlockObject(long blockHeight, BigInteger blockDifficulty, string blockHash, long timestampCreate, long timestampFound, ClassBlockEnumStatus blockStatus, bool blockUnlockValid, bool blockTransactionConfirmationCheckTaskDone)
        {
            BlockHeight = blockHeight;
            BlockDifficulty = blockDifficulty;
            BlockHash = blockHash;
            TimestampCreate = timestampCreate;
            TimestampFound = timestampFound;
            BlockStatus = blockStatus;
            BlockUnlockValid = blockUnlockValid;
            _blockTransactions = new SortedList<string, ClassBlockTransaction>();
            BlockTransactionConfirmationCheckTaskDone = blockTransactionConfirmationCheckTaskDone;
            TotalCoinConfirmed = 0;
            TotalCoinPending = 0;
            TotalFee = 0;
            TotalTransaction = 0;
            TotalTransactionConfirmed = 0;
        }


        #region Clone functions.

        /// <summary>
        /// Permit to copy the block object and his transaction completly, to bypass the GC memory indexing process.
        /// </summary>
        /// <param name="retrieveTx"></param>
        /// <param name="blockObjectCopy"></param>
        public void DeepCloneBlockObject(bool retrieveTx, out ClassBlockObject blockObjectCopy)
        {
            try
            {

                /*if (ClassBlockUtility.StringToBlockObject(ClassBlockUtility.SplitBlockObject(this), out blockObjectCopy) && retrieveTx)
                    blockObjectCopy.BlockTransactions = new SortedList<string, ClassBlockTransaction>(_blockTransactions.ToDictionary(x => x.Key, x => x.Value));*/

                blockObjectCopy = new ClassBlockObject(BlockHeight, BlockDifficulty, BlockHash, TimestampCreate, TimestampFound, BlockStatus, BlockUnlockValid, BlockTransactionConfirmationCheckTaskDone)
                {
                    BlockDifficulty = BlockDifficulty,
                    BlockFromMemory = BlockFromMemory,
                    BlockFromCache = BlockFromCache,
                    BlockFinalHashTransaction = BlockFinalHashTransaction,
                    BlockHash = BlockHash,
                    BlockHeight = BlockHeight,
                    BlockIsUpdated = BlockIsUpdated,
                    BlockLastChangeTimestamp = BlockLastChangeTimestamp,
                    BlockLastHeightTransactionConfirmationDone = BlockLastHeightTransactionConfirmationDone,
                    BlockMiningPowShareUnlockObject = BlockMiningPowShareUnlockObject,
                    BlockNetworkAmountConfirmations = BlockNetworkAmountConfirmations,
                    BlockSlowNetworkAmountConfirmations = BlockSlowNetworkAmountConfirmations,
                    BlockStatus = BlockStatus,
                    BlockTotalTaskTransactionConfirmationDone = BlockTotalTaskTransactionConfirmationDone,
                    BlockTransactionConfirmationCheckTaskDone = BlockTransactionConfirmationCheckTaskDone,
                    BlockTransactionCountInSync = BlockTransactionCountInSync,
                    BlockTransactionFullyConfirmed = BlockTransactionFullyConfirmed,
                    BlockUnlockValid = BlockUnlockValid,
                    Disposed = Disposed,
                    TimestampCreate = TimestampCreate,
                    TimestampFound = TimestampFound,
                    TotalCoinConfirmed = TotalCoinConfirmed,
                    TotalCoinPending = TotalCoinPending,
                    TotalFee = TotalFee,
                    TotalTransactionConfirmed = TotalTransactionConfirmed,
                    BlockCloned = true,
                    BlockTransactions = retrieveTx ? new SortedList<string, ClassBlockTransaction>(_blockTransactions.ToDictionary(x => x.Key.DeepCopy(), x => x.Value.Clone())) : new SortedList<string, ClassBlockTransaction>()
                };

                blockObjectCopy.TotalTransaction = retrieveTx ? blockObjectCopy.BlockTransactions.Count : _blockTransactions.Count;
            }
            catch
            {
                blockObjectCopy = null;
            }
        
        }

        /// <summary>
        /// Direct clone block object.
        /// </summary>
        /// <returns></returns>
        public ClassBlockObject DirectCloneBlockObject()
        {
            ClassBlockObject blockObjectCopy = null;
            bool locked = false;

            try
            {
                locked = Monitor.TryEnter(this);
                if (locked)
                    DeepCloneBlockObject(true, out blockObjectCopy);
            }
            finally
            {
                if (locked)
                    Monitor.Exit(this);
            }

            return blockObjectCopy;
        }

        #endregion
    }
}
