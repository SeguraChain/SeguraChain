using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain;
using SeguraChain_Lib.Other.Object.List;


namespace SeguraChain_Lib.Blockchain.Stats.Function
{
    public class ClassBlockchainStats
    {
        /// <summary>
        /// Semaphore(s).
        /// </summary>
        private static SemaphoreSlim SemaphoreUpdateBlockchainNetworkStats = new SemaphoreSlim(1, 1);
        public static ClassBlockchainNetworkStatsObject BlockchainNetworkStatsObject = new ClassBlockchainNetworkStatsObject();

        #region Functions to get/update network sync status.

        /// <summary>
        /// Get the total amount of coins circulating, the amount of fee and the amount of coin in pending on the chain.
        /// </summary>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task UpdateBlockchainNetworkStats(bool useSemaphore, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            try
            {
                if (useSemaphore)
                {
                    await SemaphoreUpdateBlockchainNetworkStats.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }

                if (ClassBlockchainDatabase.BlockchainMemoryManagement != null)
                {
                    if (BlockCount > 0)
                    {
                        BlockchainNetworkStatsObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockchainNetworkStatsObjectAsync(BlockchainNetworkStatsObject, cancellation);
                        BlockchainNetworkStatsObject.NetworkHashrateEstimated = await GetNetworkHashrate(BlockchainNetworkStatsObject.LastBlockHeight, cancellation);
                        BlockchainNetworkStatsObject.FormatBlockchainStats();
                    }
                }
            }
            finally
            {

                if (semaphoreUsed)
                    SemaphoreUpdateBlockchainNetworkStats.Release();
            }
        }

        /// <summary>
        /// Get estimated network hashrate.
        /// </summary>
        /// <returns></returns>
        public static async Task<BigInteger> GetNetworkHashrate(long blockHeight, CancellationTokenSource cancellation)
        {
            if (blockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                ClassBlockObject blockObjectInformations = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeight, cancellation);

                if (blockObjectInformations != null)
                    return BigInteger.Divide(blockObjectInformations.BlockDifficulty, BlockchainSetting.BlockTime);
            }
            return 0;
        }

        /// <summary>
        /// Update the last network block height.
        /// </summary>
        /// <param name="lastNetworkBlockHeight"></param>
        public static void UpdateLastNetworkBlockHeight(long lastNetworkBlockHeight)
        {
            if (BlockchainNetworkStatsObject != null)
                BlockchainNetworkStatsObject.LastNetworkBlockHeight = lastNetworkBlockHeight;
        }

        /// <summary>
        /// Update the last block height synced.
        /// </summary>
        /// <param name="lastBlockHeightSynced"></param>
        public static void UpdateLastBlockHeightSynced(long lastBlockHeightSynced)
        {
            BlockchainNetworkStatsObject.LastBlockHeight = lastBlockHeightSynced;
        }

        #endregion

        #region Functions to get current blockchain database progress and stats.

        /// <summary>
        /// Return the amount of blocks inside of the blockchain database.
        /// </summary>
        /// <returns></returns>
        public static int BlockCount => ClassBlockchainDatabase.BlockchainMemoryManagement.Count;

        /// <summary>
        /// Check if a block height exist.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public static bool ContainsBlockHeight(long blockHeight)
        {
            return blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight;
        }

        /// <summary>
        /// Get last block height.
        /// </summary>
        /// <returns></returns>
        public static long GetLastBlockHeight()
        {
            if (BlockCount > 0)
                return ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight;

            return 0;
        }

        /// <summary>
        /// Get the last block height unlocked.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightUnlocked(CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeightUnlocked(cancellation);
        }

        /// <summary>
        /// Get the last block height checked with the network.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightNetworkConfirmationChecked(CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeightConfirmationNetworkChecked(cancellation);
        }

        /// <summary>
        /// Return a list of block height who is missing.
        /// </summary>
        /// <param name="blockHeightTarget"></param>
        /// <param name="enableMaxRange"></param>
        /// <param name="ignoreLockedBlocks"></param>
        /// <param name="cancellation"></param>
        /// <param name="maxRange"></param>
        /// <returns></returns>
        public static async Task<DisposableList<long>> GetListBlockMissing(long blockHeightTarget, bool enableMaxRange, bool ignoreLockedBlocks, CancellationTokenSource cancellation, int maxRange)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockMissing(blockHeightTarget, enableMaxRange, ignoreLockedBlocks, cancellation, maxRange);
        }

        /// <summary>
        /// Return the amount of block(s) locked.
        /// </summary>
        /// <returns></returns>
        public static long GetCountBlockLocked()
        {
            return ClassBlockchainDatabase.BlockchainMemoryManagement.GetCountBlockLocked();
        }

        /// <summary>
        /// Get the list of blocks unconfirmed.
        /// </summary>
        /// <returns></returns>
        public static async Task<DisposableList<long>> GetListBlockNetworkUnconfirmed(CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockNetworkUnconfirmed(cancellation);
        }

        /// <summary>
        /// Get the last block height transaction confirmation done.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightTransactionConfirmationDone(CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeightTransactionConfirmationDone(cancellation);
        }

        #endregion

        #region Functions about block data.

        /// <summary>
        /// Attempt to generate a new block from informations provided, and return the status of this attempt.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="newBlockHeight"></param>
        /// <param name="timestampFound"></param>
        /// <param name="walletAddressWinner"></param>
        /// <param name="isGenesis"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> GenerateNewMiningBlock(long blockHeight, long newBlockHeight, long timestampFound, string walletAddressWinner, bool isGenesis, bool remakeBlockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.GenerateNewMiningBlockObject(blockHeight, newBlockHeight, timestampFound, walletAddressWinner, isGenesis, remakeBlockHeight, cancellation);
        }

        /// <summary>
        /// Return a block information data, from the blockchain database.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassBlockObject> GetBlockInformationData(long blockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeight, cancellation);
        }

        /// <summary>
        /// Return the amount of block transactions of a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<int> GetBlockTransactionCount(long blockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockTransactionCountStrategy(blockHeight, cancellation);
        }

        #endregion

        #region Functions about transactions.

        /// <summary>
        /// Return if a block height target contains a block reward transaction.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> CheckBlockHeightContainsBlockReward(long blockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.CheckBlockHeightContainsBlockReward(blockHeight, cancellation);
        }

        /// <summary>
        /// Check a transaction, this function is basic, she is normally used alone when an incoming transaction is sent by a user.
        /// This function is also used has completement with the Peer transaction check for increment block confirmations.
        /// </summary>
        /// <param name="transactionObject">The transaction object data to check.</param>
        /// <param name="blockObjectSource">The block object source if provided.</param>
        /// <param name="checkFromBlockData">If true, check the transaction with the blockchain data.</param>
        /// <param name="listWalletAndPublicKeysCache"></param>
        /// <param name="cancellation"></param>
        /// <returns>Return the check status result of the transaction.</returns>
        public static async Task<ClassTransactionEnumStatus> CheckTransaction(ClassTransactionObject transactionObject, ClassBlockObject blockObjectSource, bool checkFromBlockData, DisposableDictionary<string, string> listWalletAndPublicKeysCache, CancellationTokenSource cancellation, bool external)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.CheckTransaction(transactionObject, blockObjectSource, checkFromBlockData, listWalletAndPublicKeysCache, cancellation, external);
        }

        /// <summary>
        /// Get a transaction list from a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableSortedList<string, ClassBlockTransaction>> GetTransactionListFromBlockHeightTarget(long blockHeight, bool keepAlive, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetTransactionListFromBlockHeightTarget(blockHeight, keepAlive, cancellation);
        }

        #endregion

        #region Functions about wallet informations from the sync.

        /// <summary>
        /// Get the wallet balance from transactions synced.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="maxBlockHeightTarget"></param>
        /// <param name="buildCheckpoint"></param>
        /// <param name="useCheckpoint"></param>
        /// <param name="isWallet"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        public static async Task<ClassBlockchainWalletBalanceCalculatedObject> GetWalletBalanceFromTransactionAsync(string walletAddress, long maxBlockHeightTarget, bool useCheckpoint, bool buildCheckpoint, bool isWallet, bool useSemaphore, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetWalletBalanceFromTransaction(walletAddress, maxBlockHeightTarget, useCheckpoint, buildCheckpoint, isWallet, useSemaphore, null, cancellation);
        }

        #endregion

    }
}
