using System.Collections.Concurrent;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Object;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Object
{
    public class BlockchainMemoryObject
    {
        public bool CacheUpdated;
        public CacheBlockMemoryEnumState ObjectCacheType;
        public bool ObjectIndexed;

        private ClassBlockObject _content;
        private ClassBlockObject _contentMirror;

        /// <summary>
        /// Store the block transaction cache.
        /// </summary>
        public ConcurrentDictionary<string, ClassCacheIoBlockTransactionObject> BlockTransactionCache;


        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockchainMemoryObject()
        {
            BlockTransactionCache = new ConcurrentDictionary<string, ClassCacheIoBlockTransactionObject>();
        }

        /// <summary>
        /// Store the block data.
        /// </summary>
        public ClassBlockObject Content
        {
            get => _content;
            set
            {
                if (value != null)
                {

                    if (_content != null)
                    {
                        if (_content.BlockLastChangeTimestamp <= value.BlockLastChangeTimestamp)
                        {
                            if (value.BlockFromCache || value.BlockCloned || !value.BlockFromMemory)
                            {
                                _content?.Dispose();
                                _content = value.DirectCloneBlockObject();
                            }
                            else _content = value;

                            _content.BlockCloned = false;
                            _content.BlockFromCache = false;
                            _content.BlockIsUpdated = false;
                            _content.BlockFromMemory = true;
                        }
                    }
                    else
                    {
                        _content = value.BlockFromCache || value.BlockCloned || !value.BlockFromMemory ? value.DirectCloneBlockObject() : value;
                        _content.BlockCloned = false;
                        _content.BlockFromCache = false;
                        _content.BlockIsUpdated = false;
                        _content.BlockFromMemory = true;
                    }

                    if (value.IsChecked)
                        ContentMirror = value;

                }
                else
                {
                    _content?.Dispose();
                    _content = null;
                }
            }
        }

        /// <summary>
        /// Make a clone of the block data without his transactions.
        /// </summary>
        public ClassBlockObject ContentMirror
        {
            get => _contentMirror;
            set
            {
                if (value != null)
                {
                    if (value.IsChecked)
                    {
                        if (_contentMirror == null)
                            _contentMirror = new ClassBlockObject(value.BlockHeight, value.BlockDifficulty, value.BlockHash, value.TimestampCreate, value.TimestampFound, value.BlockStatus, value.BlockUnlockValid, value.BlockTransactionConfirmationCheckTaskDone);

                        _contentMirror.BlockHeight = value.BlockHeight;
                        _contentMirror.BlockDifficulty = value.BlockDifficulty;
                        _contentMirror.BlockHash = value.BlockHash;
                        _contentMirror.BlockMiningPowShareUnlockObject = value.BlockMiningPowShareUnlockObject;
                        _contentMirror.TimestampCreate = value.TimestampCreate;
                        _contentMirror.TimestampFound = value.TimestampFound;
                        _contentMirror.BlockUnlockValid = value.BlockUnlockValid;
                        _contentMirror.BlockLastChangeTimestamp = value.BlockLastChangeTimestamp;
                        _contentMirror.BlockNetworkAmountConfirmations = value.BlockNetworkAmountConfirmations;
                        _contentMirror.BlockTransactionCountInSync = value.BlockTransactionCountInSync;
                        _contentMirror.BlockSlowNetworkAmountConfirmations = value.BlockSlowNetworkAmountConfirmations;
                        _contentMirror.BlockTransactionConfirmationCheckTaskDone = value.BlockTransactionConfirmationCheckTaskDone;
                        _contentMirror.BlockTotalTaskTransactionConfirmationDone = value.BlockTotalTaskTransactionConfirmationDone;
                        _contentMirror.BlockLastHeightTransactionConfirmationDone = value.BlockLastHeightTransactionConfirmationDone;
                        _contentMirror.BlockFinalHashTransaction = value.BlockFinalHashTransaction;
                        _contentMirror.BlockTransactionFullyConfirmed = value.BlockTransactionFullyConfirmed;
                        _contentMirror.TotalCoinConfirmed = value.TotalCoinConfirmed;
                        _contentMirror.TotalCoinPending = value.TotalCoinPending;
                        _contentMirror.TotalFee = value.TotalFee;
                        _contentMirror.TotalTransaction = value.TotalTransaction;
                        _contentMirror.TotalTransactionConfirmed = value.TotalTransactionConfirmed;
                        _contentMirror.BlockStatus = value.BlockStatus;

                        if (value.BlockTransactions?.Count > 0)
                            _contentMirror.TotalTransaction = value.BlockTransactions.Count;

                        if (_contentMirror.BlockTransactions != null)
                            _contentMirror.BlockTransactions.Clear();

                    }
                }
            }
        }
    }
}
