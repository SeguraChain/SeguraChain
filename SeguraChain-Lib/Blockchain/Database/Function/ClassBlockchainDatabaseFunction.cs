using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using LZ4;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using System.Linq;
using SeguraChain_Lib.Other.Object.List;

namespace SeguraChain_Lib.Blockchain.Database.Function
{
    public class ClassBlockchainDatabaseFunction
    {

        public IEnumerable<ClassBlockObject> LoadBlockchainDatabaseEnumerable(ClassBlockchainDatabaseSetting blockchainDatabaseSetting, string encryptionDatabaseKey = null, bool resetBlockchain = false, bool fromWallet = false)
        {
            List<string> listBlockFile = Directory.GetFiles(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath, "*" + ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileExtension).ToList();

            

            int countBlockLoaded = 0;

            using (DisposableSortedList<int, string> dictionaryBlockFiles = new DisposableSortedList<int, string>())
            {
                // Build the sorted list of block files.
                foreach (string blockFilePath in listBlockFile)
                {
                    string blockFileIndex = blockFilePath.Replace(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath, "").
                        Replace(blockchainDatabaseSetting.BlockchainSetting.BlockchainBlockDatabaseFilename, "").Replace(ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileExtension, "");

                    int blockHeight = 0;

                    if (int.TryParse(blockFileIndex, out blockHeight))
                        dictionaryBlockFiles.Add(blockHeight, blockFilePath);
                }

                Debug.WriteLine("block files count: "+dictionaryBlockFiles.Count);

                foreach (var element in dictionaryBlockFiles.GetList)
                {

                    string blockFilename = element.Value.Replace(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath, "");

                    ClassLog.WriteLine("Load block file: " + blockFilename + "..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                    using (FileStream fileStreamBlock = new FileStream(element.Value, FileMode.Open))
                    {
                        using (StreamReader readerBlock = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ? new StreamReader(new LZ4Stream(fileStreamBlock, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)) : new StreamReader(fileStreamBlock))
                        {
                            string line;

                            ClassBlockObject blockObject = null;

                            while ((line = readerBlock.ReadLine()) != null)
                            {
                                if (!line.StartsWith(ClassBlockUtility.BlockDataBegin))
                                {
                                    if (!line.StartsWith(ClassBlockUtility.BlockDataEnd))
                                    {
                                        if (blockObject == null)
                                        {
                                            if (blockchainDatabaseSetting.DataSetting.DataFormatIsJson)
                                            {
                                                if (ClassUtility.TryDeserialize(line, out blockObject, ObjectCreationHandling.Reuse))
#if DEBUG
                                                    Debug.WriteLine("Load block file: " + blockFilename + " information(s) successfully done. Block Hash: " + blockObject.BlockHash);
#endif
                                            }
                                            else
                                            {
                                                if (ClassBlockUtility.StringToBlockObject(line, out blockObject))
#if DEBUG
                                                    Debug.WriteLine("Load block file: " + blockFilename + " information(s) successfully done. Block Hash: " + blockObject.BlockHash);
#endif
                                            }
                                        }
                                        else
                                        {
                                            if (blockObject != null)
                                            {
                                                foreach (string txLine in line.Split(new[] { ClassBlockUtility.StringBlockDataCharacterSeperator }, StringSplitOptions.RemoveEmptyEntries))
                                                {
                                                    if (ClassTransactionUtility.StringToBlockTransaction(txLine, out ClassBlockTransaction blockTransactionObject))
                                                        blockObject.BlockTransactions.Add(blockTransactionObject.TransactionObject.TransactionHash, blockTransactionObject);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (blockObject == null)
                                        {
#if DEBUG
                                            Debug.WriteLine("Load block file: " + blockFilename + " failed.");
#endif
                                            ClassLog.WriteLine("Load block file: " + blockFilename + " failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                            yield return null;
                                        }
#if DEBUG
                                        Debug.WriteLine("Load block file: " + blockFilename + " successfully done. Total TX: " + blockObject.BlockTransactions.Count + " | Hash: " + blockObject.BlockHash);
#endif
                                        ClassLog.WriteLine("Load block file: " + blockFilename + " successfully done. Total TX: " + blockObject.BlockTransactions.Count + " | Hash: " + blockObject.BlockHash, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                        yield return blockObject;
                                    }
                                }
                            }
                        }
                    }

                }
            }

            ClassLog.WriteLine("Total block(s) loaded: " + countBlockLoaded, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
        }


    }
}
