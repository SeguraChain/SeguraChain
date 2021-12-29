using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet.Object
{
    public class BlockchainWalletMemoryObject
    {
        public SortedList<long, ClassBlockchainWalletBalanceCheckpointObject> ListBlockchainWalletBalanceCheckpoints;
        public long LastTimestampCallOrUpdate;
        public long MemorySize;
        public bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockchainWalletMemoryObject()
        {
            ListBlockchainWalletBalanceCheckpoints = new SortedList<long, ClassBlockchainWalletBalanceCheckpointObject>();
            LastTimestampCallOrUpdate = ClassUtility.GetCurrentTimestampInMillisecond();
        }

        #region Manage Wallet Checkpoint Object.


        /// <summary>
        /// Check if the block height is contained on the list of wallet balance checkpoint.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool ContainsBlockHeightCheckpoint(long blockHeight)
        {
            return ListBlockchainWalletBalanceCheckpoints.ContainsKey(blockHeight);
        }


        /// <summary>
        /// Get wallet total tx.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public long GetWalletTotalTxCheckpoint(long blockHeight)
        {
            long totalTx = 0;
            if (ListBlockchainWalletBalanceCheckpoints.ContainsKey(blockHeight))
            {
                foreach (var blockHeightKey in ListBlockchainWalletBalanceCheckpoints.Keys.ToArray())
                    totalTx += ListBlockchainWalletBalanceCheckpoints[blockHeightKey].TotalTx;
            }
            return totalTx;
        }

        /// <summary>
        /// Get the wallet balance of a checkpoint from a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public BigInteger GetWalletBalanceCheckpoint(long blockHeight)
        {
            if (ListBlockchainWalletBalanceCheckpoints.ContainsKey(blockHeight))
                return ListBlockchainWalletBalanceCheckpoints[blockHeight].LastWalletBalance;

            return 0;
        }

        /// <summary>
        /// Add/Update wallet balance checkpoint.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="walletBalance"></param>
        /// <param name="walletPendingBalance"></param>
        /// <param name="totalTx"></param>
        public void InsertWalletBalanceCheckpoint(long blockHeight, BigInteger walletBalance, BigInteger walletPendingBalance, int totalTx, string walletAddress)
        {

            if (!ListBlockchainWalletBalanceCheckpoints.ContainsKey(blockHeight))
            {
                if (blockHeight > GetLastWalletBlockHeightCheckpoint())
                {

                    try
                    {
                        ListBlockchainWalletBalanceCheckpoints.Add(blockHeight, new ClassBlockchainWalletBalanceCheckpointObject()
                        {
                            BlockHeight = blockHeight,
                            LastWalletBalance = walletBalance,
                            LastWalletPendingBalance = walletPendingBalance,
                            TotalTx = totalTx
                        });
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
        }

        /// <summary>
        /// Get the amount of wallet balance checkpoint stored.
        /// </summary>
        /// <returns></returns>
        public int GetCountWalletBalanceCheckpoint()
        {
            return ListBlockchainWalletBalanceCheckpoints.Count;
        }

        public void ClearWalletBalanceCheckpoint()
        {
            ListBlockchainWalletBalanceCheckpoints.Clear();
        }

        /// <summary>
        /// Return every wallet balance block height checkpoint.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> GetListWalletBalanceBlockHeightCheckPoint()
        {
            foreach (var blockHeight in ListBlockchainWalletBalanceCheckpoints.Keys)
                yield return blockHeight;
        }

        public long GetLastWalletBlockHeightCheckpoint()
        {
            if (ListBlockchainWalletBalanceCheckpoints.Count > 0)
                return ListBlockchainWalletBalanceCheckpoints.Keys.ToArray().Last();

            return 0;
        }

        #endregion


    }
}
