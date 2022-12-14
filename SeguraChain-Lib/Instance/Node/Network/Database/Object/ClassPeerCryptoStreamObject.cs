using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Wallet.Function;

using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Database.Object
{
    public class ClassPeerCryptoStreamObject
    {
        /// <summary>
        /// Encryption/Decryption streams.
        /// </summary>
        private RijndaelManaged _aesManaged;
        private ICryptoTransform _encryptCryptoTransform;
        private ICryptoTransform _decryptCryptoTransform;
        private SemaphoreSlim _semaphoreCryptoObject;


        private ECPrivateKeyParameters _ecPrivateKeyParameters;
        private ECPublicKeyParameters _ecPublicKeyParameters;

        private string PeerIp;
        private string PeerUniqueId;
        private bool _initialized;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassPeerCryptoStreamObject(string peerIp, string peerUniqueId, byte[] key, byte[] iv, string publicKey, string privateKey, CancellationTokenSource cancellation)
        {
            PeerIp = peerIp;
            PeerUniqueId = peerUniqueId;
            _semaphoreCryptoObject = new SemaphoreSlim(1, 1);
            UpdateEncryptionStream(key, iv, publicKey, privateKey, cancellation).Wait();
        }

        /// <summary>
        /// Update the crypto stream informations.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> UpdateEncryptionStream(byte[] key, byte[] iv, string publicKey, string privateKey, CancellationTokenSource cancellation)
        {
            return await _semaphoreCryptoObject.TryWaitExecuteActionAsync(() =>
            {
                InitializeAesAndEcdsaSign(key, iv, publicKey, privateKey);
            }, cancellation);
        }

        /// <summary>
        /// Initialize AES.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        private void InitializeAesAndEcdsaSign(byte[] key, byte[] iv, string publicKey, string privateKey)
        {

            _initialized = false;

            try
            {
                if (publicKey.IsNullOrEmpty(false, out _) || privateKey.IsNullOrEmpty(false, out _))
                    return;

                _aesManaged?.Dispose();
                _encryptCryptoTransform?.Dispose();
                _decryptCryptoTransform?.Dispose();

                _aesManaged = new RijndaelManaged()
                {
                    KeySize = ClassAes.EncryptionKeySize,
                    BlockSize = ClassAes.EncryptionBlockSize,
                    Key = key,
                    IV = iv,
                    Mode = CipherMode.CFB,
                    Padding = PaddingMode.None
                };

                _encryptCryptoTransform = _aesManaged.CreateEncryptor(key, iv);

                _decryptCryptoTransform = _aesManaged.CreateDecryptor(key, iv);

                _ecPrivateKeyParameters = new ECPrivateKeyParameters(new BigInteger(ClassBase58.DecodeWithCheckSum(privateKey, true)), ClassWalletUtility.ECDomain);
                _ecPublicKeyParameters = new ECPublicKeyParameters(ClassWalletUtility.ECParameters.Curve.DecodePoint(ClassBase58.DecodeWithCheckSum(publicKey, false)), ClassWalletUtility.ECDomain);

                _initialized = true;

            }
#if DEBUG
            catch (Exception error)
            {
                Debug.WriteLine("Failed to initialize crypto object of : " + error.Message);
#else
            catch // Ignored.

            {
            
#endif
                _initialized = false;
            }

        }

        /// <summary>
        /// Encrypt data.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<byte[]> EncryptDataProcess(byte[] content, CancellationTokenSource cancellation)
        {
            byte[] result = null;

            if (!await _semaphoreCryptoObject.TryWaitExecuteActionAsync(() =>
            {
                if (_initialized && content != null && content?.Length > 0)
                {
                    try
                    {
                        byte[] packetPadded = ClassUtility.DoPacketPadding(content);

                        result = _encryptCryptoTransform.TransformFinalBlock(packetPadded, 0, packetPadded.Length);
                    }
                    catch
                    {
                        // Ignored.
                    }
                }

            }, cancellation))
            {
#if DEBUG
                Debug.WriteLine("Failed to encrypt data.");
#endif
            }


            return result;
        }

        /// <summary>
        /// Decrypt data.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<Tuple<byte[], bool>> DecryptDataProcess(byte[] content, CancellationTokenSource cancellation)
        {
            Tuple<byte[], bool> decryptResult = null;

            await _semaphoreCryptoObject.TryWaitExecuteActionAsync(() =>
            {
                if (!_initialized || content == null || content?.Length == 0)
                    return;

                try
                {

                    byte[] decryptedPaddedPacket = _decryptCryptoTransform.TransformFinalBlock(content, 0, content.Length);

                    if (decryptedPaddedPacket == null)
                        return;

                    byte[] result = ClassUtility.UndoPacketPadding(decryptedPaddedPacket);

                    decryptResult = new Tuple<byte[], bool>(result, result != null ? result.Length > 0 : false);

                }
                catch (Exception error)
                {
                    Debug.WriteLine("Error to decrypt packet from " + PeerIp + " | Exception: " + error.Message);
                }
                
            }, cancellation);

            return decryptResult;
        }

        /// <summary>
        /// Generate a signature.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public async Task<string> DoSignatureProcess(string hash, string privateKey, CancellationTokenSource cancellation)
        {
            string result = string.Empty;


            return await _semaphoreCryptoObject.TryWaitExecuteActionAsync(() =>
            {
                try
                {
                    var _signerDoSignature = SignerUtilities.GetSigner(BlockchainSetting.SignerNameNetwork);

                    if (_ecPrivateKeyParameters == null)
                        _ecPrivateKeyParameters = new ECPrivateKeyParameters(new BigInteger(ClassBase58.DecodeWithCheckSum(privateKey, true)), ClassWalletUtility.ECDomain);

                    _signerDoSignature.Init(true, _ecPrivateKeyParameters);

                    _signerDoSignature.BlockUpdate(ClassUtility.GetByteArrayFromHexString(hash), 0, hash.Length / 2);

                    result = Convert.ToBase64String(_signerDoSignature.GenerateSignature());

                    // Reset.
                    _signerDoSignature.Reset();
                }
                catch
                {
                }
            }, cancellation) ? result : string.Empty;
        }

        /// <summary>
        /// Check a signature.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public async Task<bool> CheckSignatureProcess(string hash, string signature, string publicKey, CancellationTokenSource cancellation)
        {

            bool result = false;

            return await _semaphoreCryptoObject.TryWaitExecuteActionAsync(() =>
            {
                try
                {

                    double byteSize = hash.Length / 2;

                    // Slow.
                    if (!_initialized || publicKey.IsNullOrEmpty(false, out _) || signature.IsNullOrEmpty(false, out _) ||
                    byteSize.ToString().Contains(",") || publicKey == "empty" && signature == "empty" || !ClassUtility.CheckBase64String(signature)) // If the size is not an integer, return false immediatly.
                        return;

                    byte[] decodedPublicKey = ClassBase58.DecodeWithCheckSum(publicKey, false);

                    if (decodedPublicKey == null)
                        return;

                    var _signerCheckSignature = SignerUtilities.GetSigner(BlockchainSetting.SignerNameNetwork);

                    if (_ecPublicKeyParameters == null)
                        _ecPublicKeyParameters = new ECPublicKeyParameters(ClassWalletUtility.ECParameters.Curve.DecodePoint(decodedPublicKey), ClassWalletUtility.ECDomain);


                    _signerCheckSignature.Init(false, _ecPublicKeyParameters);



                    // Do not contain the hash converted into a byte array inside of the memory.
                    _signerCheckSignature.BlockUpdate(ClassUtility.GetByteArrayFromHexString(hash), 0, (int)byteSize);

                    result = _signerCheckSignature.VerifySignature(Convert.FromBase64String(signature));

                    // Reset.
                    _signerCheckSignature.Reset();

                }
                catch
                {
#if DEBUG
                    Debug.WriteLine("hash size: " + hash.Length);
#endif
                    result = false;
                }
            }, cancellation) && result ? true : false;
        }
    }
}