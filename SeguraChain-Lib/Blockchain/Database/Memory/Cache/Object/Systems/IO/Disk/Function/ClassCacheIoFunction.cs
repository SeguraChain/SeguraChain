using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Function
{
    public class ClassCacheIoFunction
    {
        private const string IoDataCharacterSeperator = "¤";
        public const string IoDataBeginBlockString = ">BLOCK-INDEX-START=";
        public const string IoDataEndBlockString = "BLOCK-DATA-END<";
        private const string IoDataBeginBlockStringClose = "~";
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false);

        /// <summary>
        /// Convert a io data string from a io cache file into a block object.
        /// </summary>
        /// <param name="ioData"></param>
        /// <param name="blockchainDatabaseSetting"></param>
        /// <param name="blockInformationsOnly"></param>
        /// <param name="cancellation"></param>
        /// <param name="blockObject"></param>
        /// <returns></returns>
        public bool IoStringDataLineToBlockObject(string ioData, ClassBlockchainDatabaseSetting blockchainDatabaseSetting, bool blockInformationsOnly, CancellationTokenSource cancellation, out ClassBlockObject blockObject)
        {
            blockObject = null; // Default.

            if (ioData.IsNullOrEmpty(false, out _))
                return false;



            bool blockMetadataFound = false;


            using (DisposableList<string> dataList = ioData.DisposableSplit(Environment.NewLine))
            {
                foreach (var ioDataLine in dataList.GetList)
                {
                    if (cancellation != null)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;
                    }

                    if (!ioDataLine.StartsWith(IoDataBeginBlockString) && !ioDataLine.StartsWith(IoDataEndBlockString))
                    {
                        if (!blockMetadataFound)
                        {
                            if (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData)
                            {
                                if (!ClassBlockUtility.StringToBlockObject(Utf8Encoding.GetString(ClassUtility.DecompressDataLz4(Convert.FromBase64String(ioDataLine))), out blockObject))
                                    return false;
                            }
                            else
                            {
                                if (!ClassBlockUtility.StringToBlockObject(ioDataLine, out blockObject))
                                    return false;
                            }

                            if (blockObject == null)
                                return false;

                            if (blockObject.BlockTransactions == null)
                                blockObject.BlockTransactions = new SortedList<string, ClassBlockTransaction>();

                            blockMetadataFound = true;

                            if (blockInformationsOnly)
                                break;
                        }
                        else
                        {
                            if (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData)
                            {
                                using (DisposableList<string> transactionList = Utf8Encoding.GetString(ClassUtility.DecompressDataLz4(Convert.FromBase64String(ioDataLine))).DisposableSplit(IoDataCharacterSeperator))
                                {
                                    foreach (var transaction in transactionList.GetList)
                                    {
                                        if (cancellation.IsCancellationRequested)
                                            break;

                                        if (!ClassTransactionUtility.StringToBlockTransaction(transaction, out ClassBlockTransaction blockTransaction))
                                            return false;

                                        if (blockTransaction?.TransactionObject == null)
                                            return false;

                                        blockObject.BlockTransactions.Add(blockTransaction.TransactionObject.TransactionHash, blockTransaction);
                                    }
                                }
                            }
                            else
                            {
                                using (DisposableList<string> transactionList = ioDataLine.DisposableSplit(IoDataCharacterSeperator))
                                {
                                    foreach (var transaction in transactionList.GetList)
                                    {
                                        if (!ClassTransactionUtility.StringToBlockTransaction(transaction, out ClassBlockTransaction blockTransaction))
                                            return false;

                                        if (blockTransaction?.TransactionObject == null)
                                            return false;


                                        blockObject.BlockTransactions.Add(blockTransaction.TransactionObject.TransactionHash, blockTransaction);
                                    }
                                }
                            }
                        }
                    }


                }
            }


            return blockObject != null ? true : false;
        }


        /// <summary>
        /// Convert a io data string from a io cache file into a block object.
        /// </summary>
        /// <param name="ioData"></param>
        /// <param name="transactionHash"></param>
        /// <param name="blockchainDatabaseSetting"></param>
        /// <param name="cancellation"></param>
        /// <param name="blockTransaction"></param>
        /// <returns></returns>
        public bool IoGetBlockTransactionFromIoStringData(string ioData, string transactionHash, ClassBlockchainDatabaseSetting blockchainDatabaseSetting, CancellationTokenSource cancellation, out ClassBlockTransaction blockTransaction)
        {
            blockTransaction = null; // Default.

            bool blockMetadataFound = false;

            try
            {
                using (DisposableList<string> dataList = ioData.DisposableSplit(Environment.NewLine))
                {
                    foreach (var ioDataLine in dataList.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        if (!ioDataLine.StartsWith(IoDataBeginBlockString) && !ioDataLine.StartsWith(IoDataEndBlockString))
                        {
                            if (!blockMetadataFound)
                            {
                                if (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData)
                                {
                                    if (!ClassBlockUtility.StringToBlockObject(Utf8Encoding.GetString(ClassUtility.DecompressDataLz4(Convert.FromBase64String(ioDataLine))), out _))
                                        return false;
                                }
                                else
                                {
                                    if (!ClassBlockUtility.StringToBlockObject(ioDataLine, out _))
                                        return false;
                                }

                                blockMetadataFound = true;
                            }
                            else
                            {
                                if (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData)
                                {
                                    using (DisposableList<string> transactionList = Utf8Encoding.GetString(ClassUtility.DecompressDataLz4(Convert.FromBase64String(ioDataLine))).DisposableSplit(IoDataCharacterSeperator))
                                    {
                                        foreach (var transaction in transactionList.GetList)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                                break;

                                            if (!ClassTransactionUtility.StringToBlockTransaction(transaction, out blockTransaction))
                                                return false;

                                            if (blockTransaction?.TransactionObject == null)
                                                return false;

                                            if (blockTransaction.TransactionObject.TransactionHash == transactionHash)
                                                return true;
                                        }
                                    }
                                }
                                else
                                {
                                    using (DisposableList<string> transactionList = ioDataLine.DisposableSplit(IoDataCharacterSeperator))
                                    {
                                        foreach (var transaction in transactionList.GetList)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                                break;

                                            if (!ClassTransactionUtility.StringToBlockTransaction(transaction, out blockTransaction))
                                                return false;

                                            if (blockTransaction == null)
                                                return false;

                                            if (blockTransaction.TransactionObject.TransactionHash == transactionHash)
                                                return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                blockTransaction = null;
            }

            return blockTransaction != null ? true : false;
        }

        /// <summary>
        /// Format a block object into a io string data line;
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="blockchainDatabaseSetting"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public IEnumerable<string> BlockObjectToIoStringData(ClassBlockObject blockObject, ClassBlockchainDatabaseSetting blockchainDatabaseSetting, CancellationTokenSource cancellation)
        {
            yield return IoDataBeginBlockString + blockObject.BlockHeight + IoDataBeginBlockStringClose + Environment.NewLine;

            if (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData)
                yield return Convert.ToBase64String(ClassUtility.CompressDataLz4(Utf8Encoding.GetBytes(ClassBlockUtility.SplitBlockObject(blockObject)))) + Environment.NewLine;
            else
                yield return ClassBlockUtility.SplitBlockObject(blockObject) + Environment.NewLine;


            if (blockObject.BlockTransactions.Count > 0)
            {
                string ioDataLineTransaction = string.Empty;
                int totalIoTransactionDataInLine = 0;

                foreach (KeyValuePair<string, ClassBlockTransaction> blockObjectBlockTransaction in blockObject.BlockTransactions)
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    ioDataLineTransaction += ClassTransactionUtility.SplitBlockTransactionObject(blockObjectBlockTransaction.Value) + IoDataCharacterSeperator;

                    totalIoTransactionDataInLine++;
                    if (totalIoTransactionDataInLine >= blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxTransactionPerLineOnBlockStringToWrite || 
                        ioDataLineTransaction.Length >= blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxTransactionSizePerLine)
                    {
                        yield return (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData ?
                            Convert.ToBase64String(ClassUtility.CompressDataLz4(Utf8Encoding.GetBytes(ioDataLineTransaction))) : ioDataLineTransaction) + Environment.NewLine;

                        totalIoTransactionDataInLine = 0;
                    }
                }

                yield return (blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableCompressBlockData ?
                    Convert.ToBase64String(ClassUtility.CompressDataLz4(Utf8Encoding.GetBytes(ioDataLineTransaction))) : ioDataLineTransaction) + Environment.NewLine;
            }

            yield return IoDataEndBlockString;
        }


        /// <summary>
        /// Extract block height from a io data line.
        /// </summary>
        /// <param name="ioDataLine"></param>
        /// <returns></returns>
        public long ExtractBlockHeight(string ioDataLine)
        {
            try
            {
                if (!ioDataLine.IsNullOrEmpty(false, out _))
                {
                    if (ioDataLine.StartsWith(IoDataBeginBlockString))
                    {
                        if (long.TryParse(ioDataLine.GetStringBetweenTwoStrings(IoDataBeginBlockString, IoDataBeginBlockStringClose), out long blockHeight))
                            return blockHeight;
                    }
                }
            }
            catch
            {
                // Ignored.
            }
            return 0;
        }

        /// <summary>
        /// Check if the io data line is match to the block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="ioDataLine"></param>
        /// <returns></returns>
        public bool BlockHeightMatchToIoDataLine(long blockHeight, string ioDataLine)
        {
            try
            {
                if (ExtractBlockHeight(ioDataLine) == blockHeight)
                    return true;
            }
            catch
            {
                // Ignored.
            }

            return false;
        }

    }
}
