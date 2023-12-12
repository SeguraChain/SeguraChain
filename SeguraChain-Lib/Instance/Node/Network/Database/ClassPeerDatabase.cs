using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.IO.Pem;
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
        private Dictionary<string, ConcurrentDictionary<string, ClassPeerObject>> DictionaryPeerDataObject;
        private SemaphoreSlim _semaphoreAccess = new SemaphoreSlim(1, 1);
        private string _peerDataDirectoryPath;
        private string _peerDataFilePath;
        private byte[] _peerDataStandardEncryptionKey;
        private byte[] _peerDataStandardEncryptionKeyIv;

        /// <summary>
        /// Load peers saved.
        /// </summary>
        /// <returns></returns>
        public bool LoadPeerDatabase(ClassPeerNetworkSettingObject peerNetworkSetting)
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
                    while ((line = peerReader.ReadLine()) != null)
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
                                            totalPeerRead++;

                                            // Ignore dead peers.
                                            bool insert = !(peerObject.PeerStatus == ClassPeerEnumStatus.PEER_DEAD ||
                                                            peerObject.PeerLastDeadTimestamp + peerNetworkSetting.PeerDeadDelay > TaskManager.TaskManager.CurrentTimestampSecond ||
                                                            peerObject.PeerLastPacketReceivedTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer < TaskManager.TaskManager.CurrentTimestampSecond ||
                                                            peerObject.PeerLastDeadTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer >= TaskManager.TaskManager.CurrentTimestampSecond) ||
                                                            peerObject.PeerClientPacketEncryptionKey == null || peerObject.PeerClientPacketEncryptionKeyIv == null ||
                                                    peerObject.PeerInternPacketEncryptionKey == null || peerObject.PeerInternPacketEncryptionKeyIv == null ||
                                                    peerObject.PeerInternPrivateKey.IsNullOrEmpty(false, out _) || peerObject.PeerInternPublicKey.IsNullOrEmpty(false, out _);


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

                                                DictionaryPeerDataObject[peerObject.PeerIp][peerObject.PeerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerObject.PeerIp, peerObject.PeerUniqueId, peerObject.PeerClientPacketEncryptionKey, peerObject.PeerClientPacketEncryptionKeyIv, peerObject.PeerClientPublicKey, peerObject.PeerInternPrivateKey, new CancellationTokenSource());

                                                DictionaryPeerDataObject[peerObject.PeerIp][peerObject.PeerUniqueId].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerObject.PeerIp, peerObject.PeerUniqueId, peerObject.PeerInternPacketEncryptionKey, peerObject.PeerInternPacketEncryptionKeyIv, peerObject.PeerInternPublicKey, peerObject.PeerInternPrivateKey, new CancellationTokenSource());

                                                #endregion

                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            ClassLog.WriteLine("Error on reading peer line at index: " + lineIndex + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
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
        public bool SavePeers(string peerKey, bool fullSave = false)
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
                            using (DisposableList<string> listPeerData = new DisposableList<string>(false, 0, DictionaryPeerDataObject.Keys.ToList()))
                            {
                                foreach (var peerIp in listPeerData.GetList)
                                {
                                    if (!peerIp.IsNullOrEmpty(false, out _))
                                    {
                                        if (DictionaryPeerDataObject[peerIp].Count > 0)
                                        {
                                            foreach (string peerUniqueId in DictionaryPeerDataObject[peerIp].Keys)
                                            {
                                                // Serialize -> byte array -> compress LZ4 -> encryption -> base 64 string format.
                                                if (ClassAes.EncryptionProcess(LZ4.LZ4Codec.Wrap((ClassUtility.SerializeData(DictionaryPeerDataObject[peerIp][peerUniqueId], Formatting.None)).GetByteArray(true)), _peerDataStandardEncryptionKey, _peerDataStandardEncryptionKeyIv, out var result))
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
                                            if (ClassAes.EncryptionProcess(LZ4.LZ4Codec.Wrap((ClassUtility.SerializeData(DictionaryPeerDataObject[peerKey][peerUniqueId], Formatting.None)).GetByteArray(true)), _peerDataStandardEncryptionKey, _peerDataStandardEncryptionKeyIv, out var result))
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
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on saving peer(s). | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get peer list from peer ip.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, ClassPeerObject> this[string peerIp, CancellationTokenSource cancellation]
        {
            get
            {
                bool semaphore = false;
                try
                {
                    semaphore = _semaphoreAccess.TryWait(cancellation);

                    if (!semaphore)
                        return null;

                    return DictionaryPeerDataObject[peerIp];
                }
                finally
                {
                    if (semaphore)
                        _semaphoreAccess.Release();
                }
            }
        }

        /// <summary>
        /// Get peer object from peer ip and peer unique id.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassPeerObject this[string peerIp, string peerUniqueId, CancellationTokenSource cancellation]
        {
            get
            {
                bool semaphore = false;
                try
                {
                    semaphore = _semaphoreAccess.TryWait(cancellation);

                    if (!semaphore)
                        return null;

                    if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                        return null;

                    return DictionaryPeerDataObject[peerIp][peerUniqueId];
                }
                finally
                {
                    if (semaphore)
                        _semaphoreAccess.Release();
                }
            }
        }

        /// <summary>
        /// Check if the peer ip exist.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool ContainsPeerIp(string peerIp, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                return DictionaryPeerDataObject.ContainsKey(peerIp);
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Check if the peer unique id exist.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool ContainsPeerUniqueId(string peerIp, string peerUniqueId, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                return DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId);
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Return the amount of peer ip.
        /// </summary>
        public int Count => DictionaryPeerDataObject.Count;

        /// <summary>
        /// Return the list of peer ip.
        /// </summary>
        public IEnumerable<string> Keys => DictionaryPeerDataObject.Keys.ToArray();

        /// <summary>
        /// Try to insert a new peer ip.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool TryAddPeerIp(string peerIp, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                    DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

                return true;
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Try to insert a new peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool TryAddPeer(string peerIp, int peerPort, string peerUniqueId, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                    DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

                if (DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                    return false;

                return DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject()
                {
                    PeerIp = peerIp,
                    PeerPort = peerPort,
                    PeerUniqueId = peerUniqueId
                });
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Remove a peer from ip.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool RemovePeer(string peerIp, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                if (!DictionaryPeerDataObject.ContainsKey(peerIp))
                    return false;

                DictionaryPeerDataObject[peerIp].Clear(); // Clear peers registered.

                return DictionaryPeerDataObject.Remove(peerIp);
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Update a peer object.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool UpdatePeer(string peerIp, string peerUniqueId, ClassPeerObject peerObject, CancellationTokenSource cancellation)
        {
            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return false;

                if (!DictionaryPeerDataObject.ContainsKey(peerIp) || !DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                    return false;

                DictionaryPeerDataObject[peerIp][peerUniqueId] = peerObject;

                return true;
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Return a list of peer informations (peer ip's, peer ports, peer unique id's)
        /// </summary>
        /// <param name="peerClientIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public Dictionary<string, Tuple<int, string>> GetPeerListInfo(string peerClientIp,  CancellationTokenSource cancellation)
        {
            Dictionary<string, Tuple<int, string>> peerClientListInfo = new Dictionary<string, Tuple<int, string>>();

            bool semaphore = false;
            try
            {
                semaphore = _semaphoreAccess.TryWait(cancellation);

                if (!semaphore)
                    return peerClientListInfo;

                foreach (string peerIp in DictionaryPeerDataObject.Keys)
                {
                    if (peerIp == peerClientIp)
                        continue;

                    foreach (ClassPeerObject peerObject in DictionaryPeerDataObject[peerIp].Values)
                    {
                        if (!peerClientListInfo.ContainsKey(peerIp))
                            peerClientListInfo.Add(peerIp, new Tuple<int, string>(peerObject.PeerPort, peerObject.PeerUniqueId));
                    }
                }

                return peerClientListInfo;
            }
            finally
            {
                if (semaphore)
                    _semaphoreAccess.Release();
            }
        }
    }
}
