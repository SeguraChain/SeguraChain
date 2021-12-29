using System;
using System.Numerics;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Stats.Object
{
    public class ClassBlockchainNetworkStatsObject
    {
        /// <summary>
        /// Stats provided from data synced.
        /// </summary>
        public long LastBlockHeightTransactionConfirmationDone;
        public long LastBlockHeightUnlocked;
        public long LastBlockHeight;
        public long LastAverageMiningTimespendDone;
        public long LastAverageMiningTimespendExpected;
        public BigInteger LastBlockDifficulty;
        public string LastBlockHash;
        public ClassBlockEnumStatus LastBlockStatus;
        public DateTime LastUpdateStatsDateTime;

        /// <summary>
        /// Calculated results.
        /// </summary>
        public long TotalTransactions;
        public long TotalTransactionsConfirmed;
        public BigInteger TotalCoinCirculating;
        public BigInteger TotalCoinPending;
        public BigInteger TotalFee;

        /// <summary>
        /// Formatted stats and current stats.
        /// </summary>
        public string TotalCoinCirculatingFormatted;
        public string TotalCoinPendingFormatted;
        public string TotalCoinFeeFormatted;
        public BigInteger NetworkHashrateEstimated;
        public BigInteger TotalCoinsSpread;
        public decimal TotalCoinsSpreadFormatted;
        public decimal TotalSupply;
        public float TotalTaskConfirmationsDoneProgress;
        public string NetworkHashrateEstimatedFormatted;
        public ClassBlockchainMiningStatsEnum BlockchainMiningStats;
        public double BlockMiningLuckPercent;
        public long BlockchainStatsTimestampToGenerate;

        /// <summary>
        /// Network stats provided from the network sync process.
        /// </summary>
        public long LastNetworkBlockHeight;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassBlockchainNetworkStatsObject()
        {
            TotalTransactionsConfirmed = 0;
            TotalCoinCirculating = 0;
            TotalCoinPending = 0;
            TotalTaskConfirmationsDoneProgress = 0;
            TotalCoinsSpread = 0;
            TotalTransactions = 0;
            NetworkHashrateEstimated = 0;
            BlockchainMiningStats = ClassBlockchainMiningStatsEnum.NORMAL;
        }

        /// <summary>
        /// Generate blockchain stats.
        /// </summary>
        public void FormatBlockchainStats()
        {
            TotalCoinCirculatingFormatted = ClassTransactionUtility.GetFormattedAmountFromBigInteger(TotalCoinCirculating);
            TotalCoinPendingFormatted = ClassTransactionUtility.GetFormattedAmountFromBigInteger(TotalCoinPending);
            TotalCoinFeeFormatted = ClassTransactionUtility.GetFormattedAmountFromBigInteger(TotalFee);

            BigInteger totalCoinsSpread = TotalFee + TotalCoinCirculating + TotalCoinPending;

            TotalCoinsSpreadFormatted = (decimal)(totalCoinsSpread / BlockchainSetting.CoinDecimal);
            TotalSupply = (decimal)(BlockchainSetting.MaxSupply / BlockchainSetting.CoinDecimal);

            TotalTaskConfirmationsDoneProgress = ((float)LastBlockHeightTransactionConfirmationDone / LastBlockHeightUnlocked) * 100f;
            TotalTaskConfirmationsDoneProgress = (float)Math.Round(TotalTaskConfirmationsDoneProgress, 2);
            if (float.IsNaN(TotalTaskConfirmationsDoneProgress) || float.IsInfinity(TotalTaskConfirmationsDoneProgress) || float.IsNegativeInfinity(TotalTaskConfirmationsDoneProgress) || float.IsPositiveInfinity(TotalTaskConfirmationsDoneProgress))
            {
                TotalTaskConfirmationsDoneProgress = 0;
            }

            NetworkHashrateEstimatedFormatted = ClassUtility.GetFormattedHashrate(NetworkHashrateEstimated);

            double averageLuckEstimated = (double) LastAverageMiningTimespendExpected / LastAverageMiningTimespendDone;
            BlockMiningLuckPercent = Math.Round(averageLuckEstimated * 100d, 2);
            if (averageLuckEstimated < BlockchainSetting.BlockMiningStatsAvgPoorLuck)
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.VERY_POOR_LUCK;
            }
            else if (averageLuckEstimated >= BlockchainSetting.BlockMiningStatsAvgPoorLuck && averageLuckEstimated < BlockchainSetting.BlockMiningStatsAvgNormalLuck)
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.POOR_LUCK;
            }
            else if (averageLuckEstimated >= BlockchainSetting.BlockMiningStatsAvgNormalLuck && averageLuckEstimated < BlockchainSetting.BlockMiningStatsAvgLucky)
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.NORMAL;
            }
            else if (averageLuckEstimated >= BlockchainSetting.BlockMiningStatsAvgLucky && averageLuckEstimated < BlockchainSetting.BlockMiningStatsAvgVeryLucky)
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.LUCKY;
            }
            else if (averageLuckEstimated >= BlockchainSetting.BlockMiningStatsAvgVeryLucky && averageLuckEstimated < BlockchainSetting.BlockMiningStatsAvgWarningLuck)
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.VERY_LUCKY;
            }
            else
            {
                BlockchainMiningStats = ClassBlockchainMiningStatsEnum.WARNING;
            }

        }
    }
}
