using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;


namespace SeguraChain_Lib.Blockchain.Transaction.Utility
{
    public class ClassTransactionUtility
    {
        #region Build transaction functions.

        /// <summary>
        /// Build a transaction.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="walletAddressSender"></param>
        /// <param name="publicKeySender"></param>
        /// <param name="publicKeyReceiver"></param>
        /// <param name="amount"></param>
        /// <param name="fee"></param>
        /// <param name="walletAddressReceiver"></param>
        /// <param name="timestampSend"></param>
        /// <param name="transactionType"></param>
        /// <param name="paymentId"></param>
        /// <param name="blockHash"></param>
        /// <param name="transactionHashBlockReward"></param>
        /// <param name="walletPrivateKeySender"></param>
        /// <param name="walletPrivateKeyReceiver"></param>
        /// <param name="amountSourceList"></param>
        /// <param name="timestampBlockHeightCreate"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static ClassTransactionObject BuildTransaction(long blockHeight, long blockHeightTarget, string walletAddressSender, string publicKeySender, string publicKeyReceiver, BigInteger amount, BigInteger fee, string walletAddressReceiver, long timestampSend, ClassTransactionEnumType transactionType, long paymentId, string blockHash, string transactionHashBlockReward, string walletPrivateKeySender, string walletPrivateKeyReceiver, Dictionary<string, ClassTransactionHashSourceObject> amountSourceList, long timestampBlockHeightCreate,  CancellationTokenSource cancellation)
        {
            var transactionObject = new ClassTransactionObject()
            {
                BlockHeightTransaction = blockHeight,
                BlockHeightTransactionConfirmationTarget = blockHeightTarget,
                WalletAddressSender = walletAddressSender,
                WalletPublicKeySender = publicKeySender,
                Amount = amount,
                Fee = fee,
                WalletAddressReceiver = walletAddressReceiver,
                WalletPublicKeyReceiver = publicKeyReceiver,
                TransactionType = transactionType,
                PaymentId = paymentId,
                TransactionVersion = BlockchainSetting.TransactionVersion,
                BlockHash = blockHash,
                TransactionHashBlockReward = transactionHashBlockReward,
                AmountTransactionSource = amountSourceList,
                TimestampSend = timestampSend,
                TimestampBlockHeightCreateSend = timestampBlockHeightCreate
            };
            BuildTransactionHash(transactionObject, out transactionObject.TransactionHash);

            switch (transactionType)
            {
                case ClassTransactionEnumType.NORMAL_TRANSACTION:
                case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                    if (!FinishBuildTransactionObject(transactionObject, walletPrivateKeySender, walletPrivateKeyReceiver, cancellation, out transactionObject))
                        return null;
                    break;
                case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                    if (transactionObject.BlockHeightTransaction == BlockchainSetting.GenesisBlockHeight)
                    {
                        if (!FinishBuildTransactionObject(transactionObject, walletPrivateKeySender, walletPrivateKeyReceiver, cancellation, out transactionObject))
                            return null;
                    }
                    else
                        return transactionObject;
                    break;
                case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                    return transactionObject;
                default:
                    return null;
            }

            return transactionObject;
        }

        /// <summary>
        /// Finish to build the transaction object.
        /// </summary>
        /// <param name="prebuildTransactionObject"></param>
        /// <param name="privateKeySender">Used for sign the transaction by the sender.</param>
        /// <param name="privateKeyReceiver">Used for sign the transfer part with the receiver private key.</param>
        /// <param name="finalBuildTransactionObjet"></param>
        /// <returns></returns>
        private static bool FinishBuildTransactionObject(ClassTransactionObject prebuildTransactionObject, string privateKeySender, string privateKeyReceiver, CancellationTokenSource cancellation, out ClassTransactionObject finalBuildTransactionObjet)
        {
            if (BuildTransactionHash(prebuildTransactionObject, out prebuildTransactionObject.TransactionHash))
            {
                if (!prebuildTransactionObject.TransactionHash.IsNullOrEmpty(false, out _))
                {
                    BuildBigTransactionHash(prebuildTransactionObject, cancellation, out string bigTransactionHash);
                    prebuildTransactionObject.TransactionSignatureSender = ClassWalletUtility.WalletGenerateSignature(privateKeySender, prebuildTransactionObject.TransactionHash);
                    prebuildTransactionObject.TransactionBigSignatureSender = ClassWalletUtility.WalletGenerateSignature(privateKeySender, bigTransactionHash);

                    if (prebuildTransactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                    {
                        prebuildTransactionObject.TransactionSignatureReceiver = ClassWalletUtility.WalletGenerateSignature(privateKeyReceiver, prebuildTransactionObject.TransactionHash);
                        prebuildTransactionObject.TransactionBigSignatureReceiver = ClassWalletUtility.WalletGenerateSignature(privateKeyReceiver, bigTransactionHash);
                    }

                    finalBuildTransactionObjet = prebuildTransactionObject;
                    bigTransactionHash.Clear();
                    return true;
                }
            }
            finalBuildTransactionObjet = null;
            return false;
        }

        /// <summary>
        /// Build a transaction hash.
        /// </summary>
        /// <param name="transactionData"></param>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        private static bool BuildTransactionHash(ClassTransactionObject transactionData, out string transactionHash)
        {
            switch (transactionData.TransactionType)
            {

                case ClassTransactionEnumType.NORMAL_TRANSACTION:
                case ClassTransactionEnumType.TRANSFER_TRANSACTION:

                    transactionHash = ClassUtility.GenerateSha3512FromString(transactionData.BlockHeightTransaction +
                                                                            transactionData.BlockHeightTransactionConfirmationTarget +
                                                                            transactionData.WalletAddressSender +
                                                                            transactionData.WalletPublicKeySender +
                                                                            transactionData.Amount +
                                                                            transactionData.Fee +
                                                                            transactionData.WalletAddressReceiver +
                                                                            transactionData.TimestampSend +
                                                                            transactionData.WalletPublicKeyReceiver +
                                                                            transactionData.PaymentId +
                                                                            transactionData.TransactionVersion +
                                                                            transactionData.TransactionType +
                                                                            transactionData.TimestampBlockHeightCreateSend +
                                                                            ClassUtility.SerializeData(transactionData.AmountTransactionSource));
                    break;
                case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                    transactionHash = ClassUtility.GenerateSha3512FromString(transactionData.BlockHash + transactionData.WalletAddressReceiver + transactionData.TimestampSend + transactionData.TimestampBlockHeightCreateSend);
                    break;
                case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                    transactionHash = ClassUtility.GenerateSha3512FromString(transactionData.TransactionHashBlockReward);
                    break;
                default:
                    transactionHash = null;
                    return false;

            }

            if (!transactionHash.IsNullOrEmpty(false, out _))
                transactionHash = ClassUtility.GetHexStringFromByteArray(BitConverter.GetBytes(transactionData.BlockHeightTransaction)) + transactionHash;

            return true;
        }

        /// <summary>
        /// Build the big transaction hash representation.
        /// </summary>
        /// <param name="transactionData"></param>
        /// <param name="cancellation"></param>
        /// <param name="bigTransactionHash"></param>
        private static void BuildBigTransactionHash(ClassTransactionObject transactionData, CancellationTokenSource cancellation, out string bigTransactionHash)
        {

            bigTransactionHash = ClassSha.MakeBigShaHashFromBigData(ClassUtility.GetByteArrayFromStringUtf8(transactionData.BlockHeightTransaction +
                                                                            transactionData.BlockHeightTransactionConfirmationTarget +
                                                                            transactionData.WalletAddressSender +
                                                                            transactionData.WalletPublicKeySender +
                                                                            transactionData.Amount +
                                                                            transactionData.Fee +
                                                                            transactionData.WalletAddressReceiver +
                                                                            transactionData.TimestampSend +
                                                                            transactionData.WalletPublicKeyReceiver +
                                                                            transactionData.PaymentId +
                                                                            transactionData.TransactionVersion +
                                                                            transactionData.TransactionType +
                                                                            transactionData.TimestampBlockHeightCreateSend +
                                                                            ClassUtility.SerializeData(transactionData.AmountTransactionSource)), cancellation);
        }

        /// <summary>
        /// Retrieve the block height from the transaction hash.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        public static long GetBlockHeightFromTransactionHash(string transactionHash)
        {
            byte[] blockHeightTransactionBytes = new byte[sizeof(long)];

            Array.Copy(ClassUtility.GetByteArrayFromHexString(transactionHash), 0, blockHeightTransactionBytes, 0, sizeof(long));

            return BitConverter.ToInt64(blockHeightTransactionBytes, 0);
        }

        #endregion

        #region Check transaction functions.

        /// <summary>
        /// Check a transaction with data of the blockchain, usually used by Peers.
        /// </summary>
        /// <param name="transactionObject">The transaction object data to check.</param>
        /// <param name="fromOutside">Like API requests or Peer sync.</param>
        /// <param name="checkBalance">Check wallet balance depending of the type of the transaction.</param>
        /// <param name="fromBroadcastInstance">Avoid some checks if the transaction come from a broadcast instance.</param>
        /// <param name="blockObjectSource">The block object source if provided.</param>
        /// <param name="totalConfirmations"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns>Return the check status result of the transaction.</returns>
        public static async Task<ClassTransactionEnumStatus> CheckTransactionWithBlockchainData(ClassTransactionObject transactionObject, bool fromOutside, bool checkBalance, bool fromBroadcastInstance, ClassBlockObject blockObjectSource, long totalConfirmations, DisposableDictionary<string, string> listWalletAndPublicKeysCache, bool useSemaphore, CancellationTokenSource cancellation)
        {
            if (transactionObject == null)
                return ClassTransactionEnumStatus.EMPTY_TRANSACTION;

            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
            ClassTransactionEnumStatus normalCheckTransactionResult;

            // From API Server or Peer Sync. 
            if (fromOutside)
            {
                if (transactionObject.TransactionType == ClassTransactionEnumType.NORMAL_TRANSACTION || transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                {
                    long blockHeightSend = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetCloserBlockHeightFromTimestamp(transactionObject.TimestampBlockHeightCreateSend, cancellation);

                    if (blockHeightSend < BlockchainSetting.GenesisBlockHeight)
                        return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                    ClassBlockObject blockObjectInformations;
                    if (!fromBroadcastInstance)
                    {
                        blockObjectInformations = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeightSend, cancellation);

                        if (blockObjectInformations == null)
                            return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                        if (blockObjectInformations.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                        {
                            if (blockHeightSend + BlockchainSetting.TransactionMandatoryMinBlockHeightStartConfirmation < lastBlockHeight)
                                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;
                        }
                    }

                    if (transactionObject.BlockHeightTransaction <= lastBlockHeight)
                        return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                    if (transactionObject.BlockHeightTransaction > lastBlockHeight + BlockchainSetting.TransactionMandatoryMaxBlockHeightStartConfirmation)
                        return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                     blockObjectInformations = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(transactionObject.BlockHeightTransaction, cancellation);

                    if (blockObjectInformations != null)
                    {
                        if (blockObjectInformations.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                            return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;
                    }

                    if (transactionObject.BlockHeightTransactionConfirmationTarget <= lastBlockHeight)
                        return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;

                    if (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction < BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations)
                        return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;
                    
                    normalCheckTransactionResult = await ClassBlockchainStats.CheckTransaction(transactionObject, blockObjectSource, false, listWalletAndPublicKeysCache, cancellation, true);

                }
                else
                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_TYPE;
            }
            // Internal.
            else
            {
                if (totalConfirmations >= (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction))
                    normalCheckTransactionResult = ClassTransactionEnumStatus.VALID_TRANSACTION;
                else
                    normalCheckTransactionResult = await ClassBlockchainStats.CheckTransaction(transactionObject, blockObjectSource, true, null, cancellation, false);
            }

            if (normalCheckTransactionResult != ClassTransactionEnumStatus.VALID_TRANSACTION)
                return normalCheckTransactionResult;

            if (checkBalance)
            {
                switch (transactionObject.TransactionType)
                {
                    case ClassTransactionEnumType.NORMAL_TRANSACTION:
                    case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                        {
                            var resultBalance = await ClassBlockchainStats.GetWalletBalanceFromTransactionAsync(transactionObject.WalletAddressSender, lastBlockHeight, true, false, false, useSemaphore, cancellation);

                            if (resultBalance.WalletBalance < transactionObject.Amount + transactionObject.Fee)
                                return ClassTransactionEnumStatus.NOT_ENOUGHT_AMOUNT;
                        }
                        break;
                }
            }

            return ClassTransactionEnumStatus.VALID_TRANSACTION;
        }

        /// <summary>
        /// Check the transaction hash inside a transaction object.
        /// </summary>
        /// <param name="transactionData"></param>
        /// <returns></returns>
        public static bool CheckTransactionHash(ClassTransactionObject transactionData)
        {
            if (BuildTransactionHash(transactionData, out var transactionHash))
            {
                if (!transactionHash.IsNullOrEmpty(false, out _))
                {
                    if (transactionData.TransactionHash == transactionHash)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check big transaction signatures.
        /// </summary>
        /// <param name="transactionData"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static bool CheckBigTransactionSignature(ClassTransactionObject transactionData, CancellationTokenSource cancellation)
        {
            BuildBigTransactionHash(transactionData, cancellation, out string bigTransactionHash);

            if (!ClassWalletUtility.WalletCheckSignature(bigTransactionHash, transactionData.TransactionBigSignatureSender, transactionData.WalletPublicKeySender))
            {
                bigTransactionHash.Clear();
                return false;
            }

            if (transactionData.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
            {
                if (!ClassWalletUtility.WalletCheckSignature(bigTransactionHash, transactionData.TransactionBigSignatureReceiver, transactionData.WalletPublicKeyReceiver))
                {
                    bigTransactionHash.Clear();
                    return false;
                }
            }

            bigTransactionHash.Clear();
            return true;
        }

        /// <summary>
        /// Generate the block height target transaction confirmation.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="lastBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<long> GenerateBlockHeightStartTransactionConfirmation(long lastBlockHeightUnlocked, long lastBlockHeight, CancellationTokenSource cancellation)
        {
            bool calculationStatus = true;

            // Do not allow transactions below or equals of the genesis block height.
            if (lastBlockHeightUnlocked > BlockchainSetting.GenesisBlockHeight)
            {
                // List of block heights to travel.
                using (DisposableList<long> listBlockHeightRange = new DisposableList<long>())
                {

                    // Initialize the block to start to travel.
                    long blockHeightToTravel = lastBlockHeightUnlocked - BlockchainSetting.BlockExpectedPerDay;

                    // Ensure the block height of start.
                    if (blockHeightToTravel < BlockchainSetting.GenesisBlockHeight)
                        blockHeightToTravel = BlockchainSetting.GenesisBlockHeight;

                    // Insert each block height to travel.
                    for (long i = 0; i < BlockchainSetting.BlockExpectedPerDay; i++)
                    {
                        if (blockHeightToTravel < lastBlockHeight)
                            listBlockHeightRange.Add(blockHeightToTravel);

                        blockHeightToTravel++;

                        if (blockHeightToTravel > lastBlockHeightUnlocked)
                            break;
                    }

                    // This one store the timespend.
                    double totalBlockTimeSpend = 0;

                    // Calculate the amount of timespend expected.
                    double totalBlockTimeExpected = BlockchainSetting.BlockTime * listBlockHeightRange.Count;

                    // Travel each blocks informations listed.
                    foreach (ClassBlockObject blockInformationObject in await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockInformationDataFromListBlockHeightStrategy(listBlockHeightRange, cancellation))
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            calculationStatus = false;
                            break;
                        }

                        // Do not calculate the fee cost if the block returned is empty.
                        if (blockInformationObject == null)
                        {
                            calculationStatus = false;
                            break;
                        }

                        if (blockInformationObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                        {
                            // Ignore last block, this one was locked on the sending transaction process.
                            if (blockInformationObject.BlockHeight < lastBlockHeight)
                                totalBlockTimeSpend += (blockInformationObject.TimestampFound - blockInformationObject.TimestampCreate);
                        }
                    }

                    if (calculationStatus)
                    {
                        double blockTimespendFactor = (totalBlockTimeSpend / totalBlockTimeExpected) * 100d;

                        double blockTimeFactor = (BlockchainSetting.BlockTime * blockTimespendFactor) / 100d;

                        long blockHeightStartConfirmationsIncrement = BlockchainSetting.TransactionMandatoryMinBlockHeightStartConfirmation + (long)(BlockchainSetting.BlockTime / blockTimeFactor);

                        if (blockHeightStartConfirmationsIncrement < BlockchainSetting.TransactionMandatoryMinBlockHeightStartConfirmation)
                            blockHeightStartConfirmationsIncrement = BlockchainSetting.TransactionMandatoryMinBlockHeightStartConfirmation;

                        return lastBlockHeight + blockHeightStartConfirmationsIncrement;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Compare two transactions objects.
        /// </summary>
        /// <param name="transactionObject1"></param>
        /// <param name="transactionObject2"></param>
        /// <returns></returns>
        public static bool CompareTransactionObject(ClassTransactionObject transactionObject1, ClassTransactionObject transactionObject2)
        {
            if (transactionObject1 == null || transactionObject2 == null)
                return false;

            if (transactionObject1.TransactionType == ClassTransactionEnumType.NORMAL_TRANSACTION ||
                transactionObject1.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
            {
                if (transactionObject1.AmountTransactionSource == null)
                    return false;

                if (transactionObject1.AmountTransactionSource.Count == 0)
                    return false;

                if (transactionObject1.AmountTransactionSource.Keys.Count(x => x.IsNullOrEmpty(false, out _)) > 0)
                    return false;

                if (transactionObject1.AmountTransactionSource.Values.Count(x => x == null) > 0)
                    return false;
            }

            if (transactionObject2.TransactionType == ClassTransactionEnumType.NORMAL_TRANSACTION ||
                transactionObject2.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
            {
                if (transactionObject2.AmountTransactionSource == null)
                    return false;

                if (transactionObject2.AmountTransactionSource.Count == 0)
                    return false;

                if (transactionObject2.AmountTransactionSource.Keys.Count(x => x.IsNullOrEmpty(false, out _)) > 0)
                    return false;

                if (transactionObject2.AmountTransactionSource.Values.Count(x => x == null) > 0)
                    return false;
            }

            if (SplitTransactionObject(transactionObject1) != SplitTransactionObject(transactionObject2))
                return false;

            return true;
        }

        #endregion

        #region Fees transaction cost functions

        /// <summary>
        /// Return the fee cost size of a transaction data.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <returns></returns>
        public static BigInteger GetFeeCostSizeFromTransactionData(ClassTransactionObject transactionObject)
        {
            long transactionSize = GetTransactionMemorySize(transactionObject, true);

            if (transactionSize > 0)
                transactionSize /= 1024;

            return BlockchainSetting.FeeTransactionPerKb * transactionSize;
        }

        /// <summary>
        /// Return the min fee cost of a transaction data.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="blockHeightTransactionTarget">Block height insert target.</param>
        /// <param name="blockHeightTransactionConfirmationTarget">Block height transaction confirmation target.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<Tuple<BigInteger, bool>> GetFeeCostFromWholeBlockchainTransactionActivity(long lastBlockHeightUnlocked, long blockHeightTransactionTarget, long blockHeightTransactionConfirmationTarget, CancellationTokenSource cancellation)
        {
            // Default min fee of a transaction.
            BigInteger feeCost = BlockchainSetting.MinFeeTransaction;

            // The calculation status.
            bool calculationStatus = false;

            // Do not allow transactions below or equals of the genesis block height.
            if (lastBlockHeightUnlocked > BlockchainSetting.GenesisBlockHeight)
            {
                // List of block heights to travel.
                using (DisposableList<long> listBlockHeightRange = new DisposableList<long>())
                {
                    // Initialize the block to start to travel.
                    long blockHeightToTravel = lastBlockHeightUnlocked - BlockchainSetting.BlockExpectedPerDay;

                    // Ensure the block height of start.
                    if (blockHeightToTravel < BlockchainSetting.GenesisBlockHeight)
                        blockHeightToTravel = BlockchainSetting.GenesisBlockHeight;

                    // Insert each block height to travel.
                    for (long i = 0; i < BlockchainSetting.BlockExpectedPerDay; i++)
                    {
                        if (blockHeightToTravel >= BlockchainSetting.GenesisBlockHeight)
                        {
                            listBlockHeightRange.Add(blockHeightToTravel);

                            blockHeightToTravel++;

                            if (blockHeightToTravel > lastBlockHeightUnlocked)
                                break;
                        }
                    }

                    if (listBlockHeightRange.Count > 0)
                    {
                        // This one store the amount of transactions in blocks to travel.
                        double totalTransactionFromBlocks = 0;

                        // This one store the amount of timespend of mining in blocks to travel;
                        double totalTimespendMiningFromBlocks = 0;

                        // Calculate the amount of transactions expected on the range of block heights to travel.
                        double totalMaxTransactionExpected = BlockchainSetting.MaxTransactionPerBlock * listBlockHeightRange.Count;

                        // Calculate the amount of time expected to unlock those blocks.
                        double totalTimespendMiningExpected = BlockchainSetting.BlockTime * listBlockHeightRange.Count;

                        bool failed = false;

                        // Travel each blocks informations listed.
                        foreach (ClassBlockObject blockInformationObject in await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockInformationDataFromListBlockHeightStrategy(listBlockHeightRange, cancellation))
                        {
                            // Do not calculate the fee cost if the block returned is empty.
                            if (blockInformationObject == null)
                            {
                                failed = true;
                                break;
                            }

                            if (blockInformationObject.BlockStatus == ClassBlockEnumStatus.LOCKED)
                            {
                                failed = true;
                                break;
                            }

                            totalTransactionFromBlocks += blockInformationObject.TotalTransaction;

                            totalTimespendMiningFromBlocks += (blockInformationObject.TimestampFound - blockInformationObject.TimestampCreate);
                        }


                        // Continue the calculation if the collect of blocks informations is done propertly.
                        if (!failed)
                        {
                            // Set the calculation status has valid.
                            calculationStatus = true;

                            // First, calculate the transaction factor from the amount of transactions inside those blocks and the amount of transactions expected.
                            double transactionFactor = (totalTransactionFromBlocks / totalMaxTransactionExpected) * 100d;

                            // Secondly, calculate the confirmation factor from the height target and the block height confirmation target.
                            double confirmationFactor = ((double)blockHeightTransactionTarget / blockHeightTransactionConfirmationTarget) * 100d;

                            // Third, calculate the fee cost factor from the amount of confirmations.
                            double confirmationCostFactor = (((double)BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations / (blockHeightTransactionConfirmationTarget - blockHeightTransactionTarget)) * confirmationFactor);

                            // Fourth, calculate the transaction cost factor from the confirmation cost with the transaction factor.
                            double transactionCostFactor = (confirmationCostFactor * transactionFactor) / 100d;

                            // Five, calculate the mining cost factor from the timespend from blocks mined and the timespend of mining expected.
                            // More the timespend is lower than the expected, more the fee cost will increase.
                            double miningCostFactor = ((totalTimespendMiningExpected / totalTimespendMiningFromBlocks) * 100d);

                            // Finally, calculate the fee cost, from the default fee cost scheduled on the chain and the transaction cost factor.
                            feeCost = BlockchainSetting.MinFeeTransaction + (BigInteger)Math.Round(((BlockchainSetting.MinFeeTransaction * transactionCostFactor) * miningCostFactor) / 100d, BlockchainSetting.CoinDecimalNumber);

                            // Just in case, do not allow negative or equal of 0 fee cost.
                            if (feeCost <= 0)
                                feeCost = BlockchainSetting.MinFeeTransaction;
                        }
                    }
                }
            }

#if DEBUG
            if (!calculationStatus)
                Debug.WriteLine("Calculation confirmation fee cost failed.");
#endif

            // Return the fee cost calculated.
            return new Tuple<BigInteger, bool>(feeCost, calculationStatus);
        }

        #endregion

        #region Manage Transaction Data formating for database.

        /// <summary>
        /// Split a block transaction data, for save into a database file, to reduce final size on the database file.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <returns></returns>
        public static string SplitBlockTransactionObject(ClassBlockTransaction blockTransaction)
        {

            return blockTransaction.TransactionTotalConfirmation + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   Convert.ToInt32(blockTransaction.TransactionStatus) + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionInvalidRemoveTimestamp + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionType + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.BlockHeightTransaction + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.BlockHeightTransactionConfirmationTarget + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionHash + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.Amount + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.Fee + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.WalletAddressReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.PaymentId + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionVersion + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TimestampSend + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.WalletAddressSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.WalletPublicKeySender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionSignatureSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.WalletPublicKeyReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionSignatureReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.BlockHash + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionHashBlockReward + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   Convert.ToInt32(blockTransaction.Spent) + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   ClassUtility.SerializeData(blockTransaction.TransactionObject.AmountTransactionSource) + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionBigSignatureSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TransactionBigSignatureReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TotalSpend + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionBlockHeightInsert + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionBlockHeightTarget + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   blockTransaction.TransactionObject.TimestampBlockHeightCreateSend + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   Convert.ToInt32(blockTransaction.TransactionInvalidStatus) + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator;
        }

        /// <summary>
        /// Split a transaction data, usually for compare with another transaction.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <returns></returns>
        public static string SplitTransactionObject(ClassTransactionObject transactionObject)
        {
            return transactionObject.TransactionType + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.BlockHeightTransaction + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.BlockHeightTransactionConfirmationTarget + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionHash + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.Amount + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.Fee + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.WalletAddressReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.PaymentId + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionVersion + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TimestampSend + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.WalletAddressSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.WalletPublicKeySender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionSignatureSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.WalletPublicKeyReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionSignatureReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.BlockHash + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionHashBlockReward + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   ClassUtility.SerializeData(transactionObject.AmountTransactionSource) + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TimestampBlockHeightCreateSend + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionBigSignatureSender + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator +
                   transactionObject.TransactionBigSignatureReceiver + ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator;
        }

        /// <summary>
        /// Convert a string read from a database file into a Block transaction object.
        /// </summary>
        /// <param name="blockTransactionLine"></param>
        /// <param name="blockTransactionObject"></param>
        /// <returns></returns>
        public static bool StringToBlockTransaction(string blockTransactionLine, out ClassBlockTransaction blockTransactionObject)
        {
            try
            {
                string[] blockTransactionLineSplit = blockTransactionLine.Split(new[] { ClassTransactionSplitDataConfig.TransactionSplitDataCharacterSeparator }, StringSplitOptions.None);

                blockTransactionObject = new ClassBlockTransaction(0, new ClassTransactionObject()
                {
                    TransactionType = (ClassTransactionEnumType)System.Enum.Parse(typeof(ClassTransactionEnumType), blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_TYPE]),
                    BlockHeightTransaction = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BLOCK_HEIGHT]),
                    BlockHeightTransactionConfirmationTarget = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BLOCK_HEIGHT_CONFIRMATION_TARGET]),
                    TransactionHash = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_HASH],
                    Amount = BigInteger.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_AMOUNT]),
                    Fee = BigInteger.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_FEE]),
                    WalletAddressReceiver = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_WALLET_ADDRESS_RECEIVER],
                    PaymentId = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_PAYMENT_ID]),
                    TransactionVersion = int.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_VERSION]),
                    TimestampSend = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_TIMESTAMP_SEND]),
                    WalletAddressSender = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_WALLET_ADDRESS_SENDER],
                    WalletPublicKeySender = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_PUBLIC_KEY_SENDER],
                    TransactionSignatureSender = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_SIGNATURE_SENDER],
                    WalletPublicKeyReceiver = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_PUBLIC_KEY_RECEIVER],
                    TransactionSignatureReceiver = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_SIGNATURE_RECEIVER],
                    BlockHash = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BLOCK_HASH],
                    TransactionHashBlockReward = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_HASH_BLOCK_REWARD],
                    TimestampBlockHeightCreateSend = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_TIMESTAMP_BLOCK_HEIGHT_CREATE]),
                    TransactionBigSignatureSender = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BIG_SIGNATURE_SENDER],
                    TransactionBigSignatureReceiver = blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BIG_SIGNATURE_RECEIVER],
                })
                {
                    TransactionTotalConfirmation = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_BLOCK_TRANSACTION_TOTAL_CONFIRMATION]),
                    TransactionStatus = int.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_BLOCK_TRANSACTION_STATUS]) == 1,
                    TransactionInvalidRemoveTimestamp = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_BLOCK_TRANSACTION_INVALID_REMOVE_TIMESTAMP]),
                    TransactionBlockHeightInsert = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BLOCK_HEIGHT_INSERT]),
                    TransactionBlockHeightTarget = long.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_BLOCK_HEIGHT_TARGET]),
                    TotalSpend = BigInteger.Parse(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_TOTAL_SPEND]),
                    TransactionInvalidStatus = (ClassTransactionEnumStatus)System.Enum.Parse(typeof(ClassTransactionEnumStatus), blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_INVALID_TRANSACTION_STATUS]),
                };

                ClassUtility.TryDeserialize(blockTransactionLineSplit[(int)ClassTransactionEnumSplitData.INDEX_TRANSACTION_SOURCE_LIST], out blockTransactionObject.TransactionObject.AmountTransactionSource);

                blockTransactionObject.TransactionSize = GetTransactionMemorySize(blockTransactionObject.TransactionObject, false);

                return true;
            }
            catch
            {
                blockTransactionObject = null;
                return false;
            }
        }

        #endregion

        #region Other functions.

        /// <summary>
        /// Format an amount from a biginteger to a full decimal amount.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string GetFormattedAmountFromBigInteger(BigInteger amount)
        {
            return (((decimal)amount) / BlockchainSetting.CoinDecimal).ToString("N" + BlockchainSetting.CoinDecimalNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get the memory size of a block transaction virtually.
        /// </summary>
        /// <param name="transactionAmountSourceList"></param>
        /// <param name="amountToSend"></param>
        /// <returns></returns>
        public static BigInteger GetBlockTransactionVirtualMemorySizeOnSending(Dictionary<string, ClassTransactionHashSourceObject> transactionAmountSourceList, BigInteger amountToSend)
        {
            long totalMemoryUsage = 0;

            // Block height transaction.
            totalMemoryUsage += sizeof(long);

            // Block height transaction confirmation target.
            totalMemoryUsage += sizeof(long);

            // transaction hash.
            totalMemoryUsage += BlockchainSetting.TransactionHashSize * sizeof(char);

            // Transaction amount.
            if (amountToSend > 0)
                totalMemoryUsage += amountToSend.ToByteArray().Length;

            // Wallet address receiver.
            totalMemoryUsage += BlockchainSetting.WalletAddressWifLengthMax * sizeof(char);

            // Payment id.
            totalMemoryUsage += sizeof(long);

            // Transaction version.
            totalMemoryUsage += sizeof(int);

            // Timestamp send.
            totalMemoryUsage += sizeof(long);

            // List of amount transaction source.
            if (transactionAmountSourceList?.Count > 0)
            {
                // List of amount source transactions hash.
                totalMemoryUsage += transactionAmountSourceList.Count * (BlockchainSetting.TransactionHashSize * sizeof(char));

                // Calculate amount source spend memory usage.
                foreach (KeyValuePair<string, ClassTransactionHashSourceObject> amountTransactionSourceObject in transactionAmountSourceList)
                {
                    if (transactionAmountSourceList[amountTransactionSourceObject.Key].Amount > 0)
                        totalMemoryUsage += transactionAmountSourceList[amountTransactionSourceObject.Key].Amount.ToByteArray().Length;
                }
            }
            

            // Wallet Address Sender.
            totalMemoryUsage += BlockchainSetting.WalletAddressWifLengthMax * sizeof(char);

            // Public Key Sender.
            totalMemoryUsage += BlockchainSetting.WalletPublicKeyWifLength * sizeof(char);

            long feeCostSize = BlockchainSetting.FeeTransactionPerKb * totalMemoryUsage;

            if (feeCostSize > 0)
                feeCostSize /= 1024;

            return feeCostSize;
        }

        /// <summary>
        /// Calculate the amount of memory spend by a block transaction approximatively.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <returns></returns>
        public static long GetBlockTransactionMemorySize(ClassBlockTransaction blockTransaction)
        {
            long totalMemoryUsage = 0;

            if (blockTransaction != null)
            {
                // Block transaction height insert.
                totalMemoryUsage += sizeof(long);

                // Block transaction height target.
                totalMemoryUsage += sizeof(long);

                // Block transaction total confirmations.
                totalMemoryUsage += sizeof(long);

                if (blockTransaction.TransactionObject != null)
                {
                    if (blockTransaction.TransactionSize == 0)
                        totalMemoryUsage += GetTransactionMemorySize(blockTransaction.TransactionObject, false);
                    else
                        totalMemoryUsage += blockTransaction.TransactionSize;
                }

                // Block transaction status.
                totalMemoryUsage += sizeof(bool);

                // Block transaction invalid remove timestamp.
                totalMemoryUsage += sizeof(long);

                // Block transaction index inserted.
                totalMemoryUsage += sizeof(int);

            }

            return totalMemoryUsage;
        }

        /// <summary>
        /// Get the memory size spend by a transaction.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <param name="exceptFee">Used if enable, for calculate the size of the tx, then to calculate the fee cost size</param>
        /// <returns></returns>
        public static long GetTransactionMemorySize(ClassTransactionObject transactionObject, bool exceptFee)
        {
            long totalMemoryUsage = 0;


            // Block height transaction. (Long)
            totalMemoryUsage += sizeof(long);

            // Block height transaction confirmation target. (Long)
            totalMemoryUsage += sizeof(long);

            // transaction hash. Length * size of char.
            totalMemoryUsage += BlockchainSetting.TransactionHashSize * sizeof(char); 

            // Transaction amount.
            if (transactionObject.Amount > 0)
                totalMemoryUsage += transactionObject.Amount.ToByteArray().Length;

            // Transaction fee.
            if (!exceptFee)
            {
                if (transactionObject.Fee > 0)
                    totalMemoryUsage += transactionObject.Fee.ToByteArray().Length;
            }

            // Wallet address receiver. (Length * size of char)
            totalMemoryUsage += BlockchainSetting.WalletAddressWifLengthMax * sizeof(char);

            // Payment id.
            totalMemoryUsage += sizeof(long);

            // Transaction version.
            totalMemoryUsage += sizeof(int);

            // Timestamp send.
            totalMemoryUsage += sizeof(long);

            // List of amount transaction source.
            if (transactionObject.AmountTransactionSource != null)
            {
                if (transactionObject.AmountTransactionSource.Count > 0)
                {
                    // List of amount source transactions hash.
                    totalMemoryUsage += transactionObject.AmountTransactionSource.Count * (BlockchainSetting.TransactionHashSize * sizeof(char));

                    // Calculate amount source spend memory usage.
                    foreach (KeyValuePair<string, ClassTransactionHashSourceObject> amountTransactionSourceObject in transactionObject.AmountTransactionSource)
                    {
                        if (transactionObject.AmountTransactionSource[amountTransactionSourceObject.Key].Amount > 0)
                            totalMemoryUsage += transactionObject.AmountTransactionSource[amountTransactionSourceObject.Key].Amount.ToByteArray().Length;
                    }
                }
            }

            // Wallet Address Sender.
            if (!transactionObject.WalletAddressSender.IsNullOrEmpty(false, out _))
                totalMemoryUsage += transactionObject.WalletAddressSender.Length * sizeof(char);

            // Public Key Sender.
            if (!transactionObject.WalletPublicKeySender.IsNullOrEmpty(false, out _))
                totalMemoryUsage += transactionObject.WalletPublicKeySender.Length * sizeof(char);

            // Signature Sender.
            if (!exceptFee)
            {
                if (!transactionObject.TransactionSignatureSender.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.TransactionSignatureSender.Length * sizeof(char);
            }

            // Big signature sender.
            if (!exceptFee)
            {
                if (!transactionObject.TransactionBigSignatureSender.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.TransactionBigSignatureSender.Length * sizeof(char);
            }

            // Public Key Receiver.
            if (!exceptFee)
            {
                if (!transactionObject.WalletPublicKeyReceiver.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.WalletPublicKeyReceiver.Length * sizeof(char);
            }

            // signature Receiver.
            if (!exceptFee)
            {
                if (!transactionObject.TransactionSignatureReceiver.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.TransactionSignatureReceiver.Length * sizeof(char);
            }

            // Big signature receiver.
            if (!exceptFee)
            {
                if (!transactionObject.TransactionBigSignatureReceiver.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.TransactionBigSignatureReceiver.Length * sizeof(char);
            }

            // Block hash.
            if (!exceptFee)
            {
                if (!transactionObject.BlockHash.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.BlockHash.Length * sizeof(char);

                // Block hash reward.
                if (!transactionObject.TransactionHashBlockReward.IsNullOrEmpty(false, out _))
                    totalMemoryUsage += transactionObject.TransactionHashBlockReward.Length * sizeof(char);
            }

            return totalMemoryUsage;
        }

        #endregion
    }
}
