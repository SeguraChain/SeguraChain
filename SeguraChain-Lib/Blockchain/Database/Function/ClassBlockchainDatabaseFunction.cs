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

namespace SeguraChain_Lib.Blockchain.Database.Function
{
    public class ClassBlockchainDatabaseFunction
    {

        public IEnumerable<ClassBlockObject> LoadBlockchainDatabaseEnumerable(ClassBlockchainDatabaseSetting blockchainDatabaseSetting, string encryptionDatabaseKey = null, bool resetBlockchain = false, bool fromWallet = false)
        {
            List<string> listBlockFile = Directory.GetFiles(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath, "*" + ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileExtension).ToList();

            listBlockFile.Sort();

            int countBlockLoaded = 0;

            foreach (string blockFilePath in listBlockFile)
            {

                string blockFilename = blockFilePath.Replace(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath, "");

                ClassLog.WriteLine("Load block file: " + blockFilename + "..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                string blockIndexName = blockFilename.Replace(ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileName, "").Replace(ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileExtension, "");

                if (int.TryParse(blockIndexName, out int blockIndex))
                {
                    using (FileStream fileStreamBlock = new FileStream(blockFilePath, FileMode.Open))
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
                                                foreach (string txLine in line.Split(new  [] { ClassBlockUtility.StringBlockDataCharacterSeperator }, StringSplitOptions.RemoveEmptyEntries))
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
