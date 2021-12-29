using System;
using System.Numerics;

namespace SeguraChain_Lib.Blockchain.Setting.Function
{
    public class ClassBlockRewardFunction
    {
        /// <summary>
        /// Calculate halving from the current block height with the block reward initial amount and the block reward halving range.
        /// </summary>
        /// <param name="currentBlockHeight"></param>
        /// <returns></returns>
        public static BigInteger GetBlockRewardWithHalving(long currentBlockHeight)
        {
            int halvingFactor = GetBlockRewardHalvingFactor(currentBlockHeight);

            if (halvingFactor > 1)
            {
                // Not enough coins to provide the max supply is reach. Block fee mining should start.
                if (halvingFactor > MaxBlockRewardHalving())
                    return 0;

                decimal blockReward = ((decimal)BlockchainSetting.BlockRewardStatic) / BlockchainSetting.CoinDecimal;
                blockReward /= halvingFactor;

                BigInteger blockRewardHalved = (BigInteger)(blockReward * BlockchainSetting.CoinDecimal);

                if(blockRewardHalved < BlockchainSetting.CoinDecimal)
                    return 0;
            }

            return BlockchainSetting.BlockRewardStatic;
        }

        /// <summary>
        /// Calculate the dev fee amount from the current block height who permit to calculate the block reward halving.
        /// </summary>
        /// <param name="currentBlockHeight"></param>
        /// <returns></returns>
        public static BigInteger GetDevFeeWithHalving(long currentBlockHeight)
        {
            BigInteger blockRewardHalved = GetBlockRewardWithHalving(currentBlockHeight);

            if (blockRewardHalved > BlockchainSetting.CoinDecimal)
                return (BigInteger)((decimal)blockRewardHalved * BlockchainSetting.BlockDevFeePercent);

            // Not enough coins to provide the max supply is reach. Block fee mining should start.
            return 0;
        }

        /// <summary>
        /// Return the maximum of halving possible of the block reward, from the max supply - genesis block reward.
        /// </summary>
        /// <returns></returns>
        public static int MaxBlockRewardHalving()
        {
            return (int)((BlockchainSetting.MaxSupply - BlockchainSetting.GenesisBlockAmount) / BlockchainSetting.CoinDecimal) / BlockchainSetting.BlockRewardHalvingRange;
        }

        /// <summary>
        /// Return the block reward halving factor.
        /// </summary>
        /// <param name="currentBlockHeight"></param>
        /// <returns></returns>
        public static int GetBlockRewardHalvingFactor(long currentBlockHeight)
        {
            return (int)Math.Ceiling((double)currentBlockHeight / BlockchainSetting.BlockRewardHalvingRange);
        }
    }
}
