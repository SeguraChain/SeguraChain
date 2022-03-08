using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using SeguraChain_Lib.Instance.Node.Network.Enum.Manage;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Database
{
    public class ClassPeerDataSetting
    {
        public const string PeerDataDirectoryName = "\\Peer\\";
        public const string PeerDataFileName = "peers.list";
        public const string PeerDataUncompressedFileName = "peers-uncompressed.list";
    }

    public class ClassPeerDatabase
    {
        public static Dictionary<string, ConcurrentDictionary<string, ClassPeerObject>> DictionaryPeerDataObject;
        private static string _peerDataDirectoryPath;
        private static string _peerDataFilePath;
        private static byte[] _peerDataStandardEncryptionKey;
        private static byte[] _peerDataStandardEncryptionKeyIv;

        /// <summary>
        /// Load peers saved.
        /// </summary>
        /// <returns></returns>
        public static bool LoadPeerDatabase(ClassPeerNetworkSettingObject peerNetworkSetting)
        {
            DictionaryPeerDataObject = new Dictionary<string, ConcurrentDictionary<string, ClassPeerObject>>();

            _peerDataDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName);
            _peerDataFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName + ClassPeerDataSetting.PeerDataFileName);

            if (_peerDataStandardEncryptionKey == null || _peerDataStandardEncryptionKeyIv == null)
            {
                if (!ClassAes.GenerateKey(BlockchainSetting.BlockchainMarkKey, true, out _peerDataStandardEncryptionKey))
                {
                    ClassLog.WriteLine("Can't generate standard encryption key for decrypt peer list.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    return false;
                }

                _peerDataStandardEncryptionKeyIv = ClassAes.GenerateIv(_peerDataStandardEncryptionKey);
            }

            if (!Directory.Exists(_peerDataDirectoryPath))
                Directory.CreateDirectory(_peerDataDirectoryPath);

            if (File.Exists(_peerDataFilePath))
            {
                int totalPeerRead = 0;
                using (StreamReader peerReader = new StreamReader(_peerDataFilePath))
                {
                    string line;
                    int lineIndex = 1;
                    while((line = peerReader.ReadLine()) != null)
                    {
                        try
                        {
                            if (ClassUtility.CheckBase64String(line))
                            {
                                // Base 64 string format decrypted to byte array -> decompress LZ4 -> string serialized -> deserialize.
                                if (ClassAes.DecryptionProcess(Convert.FromBase64String(line), _peerDataStandardEncryptionKey, _peerDataStandardEncryptionKeyIv, out var result))
                                {
                                    ClassPeerObject peerObject = JsonConvert.DeserializeObject<ClassPeerObject>(LZ4.LZ4Codec.Unwrap(result).GetStringFromByteArrayAscii());

                                    if (!peerObject.PeerIp.IsNullOrEmpty(false, out _))
                                    {
                                        if (IPAddress.TryParse(peerObject.PeerIp, out _))
                                        {
                                            // Ignore dead peers.
                                            bool insert = !(peerObject.PeerStatus == ClassPeerEnumStatus.PEER_DEAD ||
                                                            peerObject.PeerLastDeadTimestamp + peerNetworkSetting.PeerDeadDelay > ClassUtility.GetCurrentTimestampInSecond() ||
                                                            peerObject.PeerLastPacketReceivedTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer < ClassUtility.GetCurrentTimestampInSecond() ||
                                                            peerObject.PeerLastDeadTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer >= ClassUtility.GetCurrentTimestampInSecond());


                                            if (insert)
                                            {
                                                peerObject.PeerTimestampSignatureWhitelist = 0;

                                                if (DictionaryPeerDataObject.ContainsKey(peerObject.PeerIp))
                                                {
                                                    if (!DictionaryPeerDataObject[peerObject.PeerIp].ContainsKey(peerObject.PeerUniqueId))
                                                        DictionaryPeerDataObject[peerObject.PeerIp].TryAdd(peerObject.PeerUniqueId, peerObject);
                                                    else
                                                        DictionaryPeerDataObject[peerObject.PeerIp][peerObject.PeerUniqueId] = peerObject;
                                                   
                                                }
                                                else
                                                {
                                                    DictionaryPeerDataObject.Add(peerObject.PeerIp, new ConcurrentDictionary<string, ClassPeerObject>());
                                                    DictionaryPeerDataObject[peerObject.PeerIp].TryAdd(peerObject.PeerUniqueId, peerObject);
                                                }

                                                #region Peer encryption streams.
                                                if (peerObject.PeerClientPacketEncryptionKey != null && peerObject.PeerClientPacketEncryptionKeyIv != null)
                                                    DictionaryPeerDataObject[peerObject.PeerIp][peerObject.PeerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerObject.PeerClientPacketEncryptionKey, peerObject.PeerClientPacketEncryptionKeyIv, peerObject.PeerClientPublicKey, peerObject.PeerInternPrivateKey, new CancellationTokenSource());

                                                if (peerObject.PeerInternPacketEncryptionKey != null && peerObject.PeerInternPacketEncryptionKeyIv != null)
                                                    DictionaryPeerDataObject[peerObject.PeerIp][peerObject.PeerUniqueId].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerObject.PeerInternPacketEncryptionKey, peerObject.PeerInternPacketEncryptionKeyIv, peerObject.PeerInternPublicKey, peerObject.PeerInternPrivateKey, new CancellationTokenSource());
                                                
                                                #endregion

                                                totalPeerRead++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch(Exception error)
                        {
                            ClassLog.WriteLine("Error on reading peer line at index: "+lineIndex+" | Exception: "+error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                        }
                        lineIndex++;
                    }
                }

                ClassLog.WriteLine(totalPeerRead + " peers read successfully.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
            }
            else
                File.Create(_peerDataFilePath).Close();

            return true;
        }

        /// <summary>
        /// Save a peer or all peers.
        /// </summary>
        /// <param name="peerKey"></param>
        /// <param name="fullSave"></param>
        /// <returns></returns>
        public static bool SavePeers(string peerKey, bool fullSave = false)
        {
            if (_peerDataDirectoryPath.IsNullOrEmpty(false, out _) || _peerDataFilePath.IsNullOrEmpty(false, out _))
            {
                _peerDataDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName);
                _peerDataFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName + ClassPeerDataSetting.PeerDataFileName);
            }

            if (_peerDataStandardEncryptionKey == null || _peerDataStandardEncryptionKeyIv == null)
            {
                if (!ClassAes.GenerateKey(BlockchainSetting.BlockchainMarkKey, true, out _peerDataStandardEncryptionKey))
                {
                    ClassLog.WriteLine("Can't generate standard encryption key for decrypt peer list.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    return false;
                }
                _peerDataStandardEncryptionKeyIv = ClassAes.GenerateIv(_peerDataStandardEncryptionKey);
            }


            try
            {
                if (!Directory.Exists(_peerDataDirectoryPath))
                    Directory.CreateDirectory(_peerDataDirectoryPath);

                if (fullSave)
                {
                    using (StreamWriter peerWriter = new StreamWriter(_peerDataFilePath))
                    {
                        using (StreamWriter peerUncompressedWriter = new StreamWriter(ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName + ClassPeerDataSetting.PeerDataUncompressedFileName)))
                        {
                            foreach (var peerIp in DictionaryPeerDataObject.Keys)
                            {
                                if (!peerIp.IsNullOrEmpty(false, out _))
                                {
                                    if (DictionaryPeerDataObject[peerIp].Count > 0)
                                    {
                                        foreach (string peerUniqueId in DictionaryPeerDataObject[peerIp].Keys)
                                        {
                                            // Serialize -> byte array -> compress LZ4 -> encryption -> base 64 string format.
                                            if (ClassAes.EncryptionProcess(LZ4.LZ4Codec.Wrap(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.SerializeData(DictionaryPeerDataObject[peerIp][peerUniqueId], Formatting.None))), _peerDataStandardEncryptionKey, _peerDataStandardEncryptionKeyIv, out var result))
                                            {
                                                peerWriter.WriteLine(Convert.ToBase64String(result));

                                                // Uncompressed peer data.
                                                peerUncompressedWriter.WriteLine(ClassUtility.SerializeData(DictionaryPeerDataObject[peerIp][peerUniqueId], Formatting.Indented));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (DictionaryPeerDataObject.ContainsKey(peerKey))
                    {
                        using (StreamWriter peerWriter = new StreamWriter(_peerDataFilePath, true))
                        {
                            using (StreamWriter peerUncompressedWriter = new StreamWriter(ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassPeerDataSetting.PeerDataDirectoryName + ClassPeerDataSetting.PeerDataUncompressedFileName)))
                            {
                                if (!peerKey.IsNullOrEmpty(false, out _))
                                {
                                    if (DictionaryPeerDataObject[peerKey].Count > 0)
                                    {
                                        foreach (string peerUniqueId in DictionaryPeerDataObject[peerKey].Keys)
                                        {
                                            // Serialize -> byte array -> compress LZ4 -> encryption -> base 64 string format.
                                            if (ClassAes.EncryptionProcess(LZ4.LZ4Codec.Wrap(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.SerializeData(DictionaryPeerDataObject[peerKey][peerUniqueId], Formatting.None))), _peerDataStandardEncryptionKey, _peerDataStandardEncryptionKeyIv, out var result))
                                            {
                                                peerWriter.WriteLine(Convert.ToBase64String(result));
                                                // Uncompressed peer data.
                                                peerUncompressedWriter.WriteLine(ClassUtility.SerializeData(DictionaryPeerDataObject[peerKey][peerUniqueId], Formatting.Indented));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch(Exception error)
            {
                ClassLog.WriteLine("Error on saving peer(s). | Exception: "+error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                return false;
            }

            return true;
        }

        #region Manage Peers functions.

        /// <summary>
        /// Return the port of a peer registered.
        /// </summary>
        /// <param name="peerKey"></param>
        /// <param name="peerUniqueId"></param>
        /// <returns></returns>
        public static int GetPeerPort(string peerKey, string peerUniqueId)
        {
            if (!peerKey.IsNullOrEmpty(false, out _))
                return BlockchainSetting.PeerDefaultPort;

            if (!DictionaryPeerDataObject.ContainsKey(peerKey))
                return BlockchainSetting.PeerDefaultPort;

            if (!DictionaryPeerDataObject[peerKey].ContainsKey(peerUniqueId))
                return BlockchainSetting.PeerDefaultPort;

            if (DictionaryPeerDataObject[peerKey][peerUniqueId].PeerPort < BlockchainSetting.PeerMinPort || DictionaryPeerDataObject[peerKey][peerUniqueId].PeerPort > BlockchainSetting.PeerMaxPort)
                return BlockchainSetting.PeerDefaultPort;

            return DictionaryPeerDataObject[peerKey][peerUniqueId].PeerPort;
        }

        /// <summary>
        /// Input a peer to the database of peer if possible.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// <returns></returns>
        public static ClassPeerEnumInsertStatus InputPeer(string peerIp, int peerPort, string peerUniqueId)
        {
            if (!peerIp.IsNullOrEmpty(false, out _))
            {
                if (peerPort >= BlockchainSetting.PeerMinPort && peerPort <= BlockchainSetting.PeerMaxPort)
                {
                    if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                    {
                        if (IPAddress.TryParse(peerIp, out _))
                        {
                            try
                            {
                                DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());
                                if (DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject()
                                {
                                    PeerPort = peerPort,
                                    PeerIp = peerIp,
                                    PeerUniqueId = peerUniqueId,
                                    PeerStatus = ClassPeerEnumStatus.PEER_ALIVE
                                }))
                                {

                                    ClassLog.WriteLine("Peer: " + peerIp + ":" + peerPort + " has been inserted successfully into the database list.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                                    return ClassPeerEnumInsertStatus.PEER_INSERT_SUCCESS;
                                }

                                return ClassPeerEnumInsertStatus.PEER_ALREADY_EXIST;
                            }
                            catch
                            {
                                return ClassPeerEnumInsertStatus.EXCEPTION_INSERT_PEER;
                            }
                        }
                        return ClassPeerEnumInsertStatus.INVALID_PEER_IP;
                    }

                    if (!DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                    {
                        if (DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject()
                        {
                            PeerPort = peerPort,
                            PeerIp = peerIp,
                            PeerUniqueId = peerUniqueId,
                            PeerStatus = ClassPeerEnumStatus.PEER_ALIVE
                        }))
                        {

                            ClassLog.WriteLine("Peer: " + peerIp + ":" + peerPort + " has been inserted successfully into the database list.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                            return ClassPeerEnumInsertStatus.PEER_INSERT_SUCCESS;
                        }
                        return ClassPeerEnumInsertStatus.PEER_ALREADY_EXIST;

                    }

                    return ClassPeerEnumInsertStatus.PEER_ALREADY_EXIST;
                }
                return ClassPeerEnumInsertStatus.INVALID_PEER_PORT;
            }

            return ClassPeerEnumInsertStatus.EMPTY_PEER_IP;
        }

        /// <summary>
        /// Generate a peer list info.
        /// </summary>
        /// <param name="peerIpIgnored"></param>
        /// <returns></returns>
        public static Dictionary<string, Tuple<int, string>> GetPeerListInfo(string peerIpIgnored)
        {
            Dictionary<string, Tuple<int, string>> listPeerInfo = new Dictionary<string, Tuple<int, string>>();

            using (DisposableList<string> listIpPeer = new DisposableList<string>(false, 0, DictionaryPeerDataObject.Keys.ToList()))
            {

                if (listIpPeer.Contains(peerIpIgnored))
                    listIpPeer.Remove(peerIpIgnored);

                foreach (var peerIp in listIpPeer.GetList)
                {
                    if (!listPeerInfo.ContainsKey(peerIp))
                    {
                        if (DictionaryPeerDataObject[peerIp].Count > 0)
                        {
                            foreach (var peerUniqueId in DictionaryPeerDataObject[peerIp].Keys.ToArray())
                            {
                                if (DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                                {
                                    if (DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_ALIVE)
                                    {
                                        if (!listPeerInfo.ContainsKey(peerIp))
                                            listPeerInfo.Add(peerIp, new Tuple<int, string>(DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort, peerUniqueId));
                                    }
                                }
                            }
                        }
                    }
                }

                // Clean up.
                listIpPeer.Clear();
            }

            return listPeerInfo;
        }

        /// <summary>
        /// Retrieve the numeric public key of a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="numericPublicKey"></param>
        /// <returns></returns>
        public static bool GetPeerNumericPublicKey(string peerIp, string peerUniqueId, out string numericPublicKey)
        {
            numericPublicKey = null;
            if (DictionaryPeerDataObject.ContainsKey(peerIp))
            {
                numericPublicKey = DictionaryPeerDataObject[peerIp][peerUniqueId].PeerNumericPublicKey;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the database of peer contains a peer ip followed by his peer unique id.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <returns></returns>
        public static bool ContainsPeer(string peerIp, string peerUniqueId)
        {
            if (peerUniqueId.IsNullOrEmpty(false, out _))
                return false;

            if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                return false;

            return DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId);
        }

        #endregion
    }
}
