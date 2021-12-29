using System;
using System.Security.Cryptography;
using System.Threading;
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


        private ECPrivateKeyParameters _ecPrivateKeyParameters;
        private ECPublicKeyParameters _ecPublicKeyParameters;

        private bool _initialized;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        /// 
        public ClassPeerCryptoStreamObject(byte[] key, byte[] iv, string publicKey, string privateKey, CancellationTokenSource cancellation)
        {
            InitializeAesAndEcdsaSign(key, iv, publicKey, privateKey, true, cancellation);
        }

        /// <summary>
        /// Update the crypto stream informations.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public void UpdateEncryptionStream(byte[] key, byte[] iv, string publicKey, string privateKey, CancellationTokenSource cancellation)
        {
            InitializeAesAndEcdsaSign(key, iv, publicKey, privateKey, false, cancellation);
        }

        /// <summary>
        /// Initialize AES.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        private void InitializeAesAndEcdsaSign(byte[] key, byte[] iv, string publicKey, string privateKey, bool fromInitialization, CancellationTokenSource cancellation)
        {

            _initialized = false;

            try
            {
                if (fromInitialization || _aesManaged == null)
                    _aesManaged = new RijndaelManaged()
                    {
                        KeySize = ClassAes.EncryptionKeySize,
                        BlockSize = ClassAes.EncryptionBlockSize,
                        Key = key,
                        IV = iv,
                        Mode = CipherMode.CFB,
                        Padding = PaddingMode.None
                    };
                else
                {

                    try
                    {
                        _aesManaged?.Dispose();
                    }
                    catch
                    {
                        // Ignored.
                    }

                    _aesManaged = new RijndaelManaged()
                    {
                        KeySize = ClassAes.EncryptionKeySize,
                        BlockSize = ClassAes.EncryptionBlockSize,
                        Key = key,
                        IV = iv,
                        Mode = CipherMode.CFB,
                        Padding = PaddingMode.None
                    };
                }

                if (fromInitialization || _encryptCryptoTransform == null)
                    _encryptCryptoTransform = _aesManaged.CreateEncryptor(key, iv);
                else
                {

                    try
                    {
                        _encryptCryptoTransform?.Dispose();
                    }
                    catch
                    {
                        // Ignored.
                    }

                    _encryptCryptoTransform = _aesManaged.CreateEncryptor(key, iv);
                }

                if (fromInitialization || _decryptCryptoTransform == null)
                    _decryptCryptoTransform = _aesManaged.CreateDecryptor(key, iv);
                else
                {

                    try
                    {
                        _decryptCryptoTransform?.Dispose();
                    }
                    catch
                    {
                        // Ignored.
                    }
                    _decryptCryptoTransform = _aesManaged.CreateDecryptor(key, iv);

                }

                if (!publicKey.IsNullOrEmpty(out _) && !privateKey.IsNullOrEmpty(out _))
                {
                    _ecPrivateKeyParameters = new ECPrivateKeyParameters(new BigInteger(ClassBase58.DecodeWithCheckSum(privateKey, true)), ClassWalletUtility.ECDomain);
                    _ecPublicKeyParameters = new ECPublicKeyParameters(ClassWalletUtility.ECParameters.Curve.DecodePoint(ClassBase58.DecodeWithCheckSum(publicKey, false)), ClassWalletUtility.ECDomain);
                }
            }
            catch
            {
                // Ignored.
            }

            _initialized = true;


        }

        /// <summary>
        /// Encrypt data.
        /// </summary>
        /// <param name="content"></param>
        /// 
        /// <returns></returns>
        public byte[] EncryptDataProcess(byte[] content)
        {
            if (!_initialized)
                return null;

            if (content == null)
                return null;

            if (content.Length == 0)
                return null;

            try
            {
                byte[] packetPadded = ClassUtility.DoPacketPadding(content);
    
                return _encryptCryptoTransform.TransformFinalBlock(packetPadded, 0, packetPadded.Length);
            }
            catch
            {
               // Ignored.
            }


            return null;
        }

        /// <summary>
        /// Decrypt data.
        /// </summary>
        /// <param name="content"></param>
        /// 
        /// <returns></returns>
        public Tuple<byte[], bool> DecryptDataProcess(byte[] content)
        {
            if (!_initialized)
                return null;

            if (content == null)
                return null;

            if (content.Length == 0)
                return null;
            try
            {

                byte[] decryptedPaddedPacket = _decryptCryptoTransform.TransformFinalBlock(content, 0, content.Length);
                byte[] result = ClassUtility.UndoPacketPadding(decryptedPaddedPacket);

                return new Tuple<byte[], bool>(result, result != null ? result.Length > 0 : false);

            }
            catch
            {
                // Ignored.
            }

            return null;
        }

        /// <summary>
        /// Generate a signature.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public string DoSignatureProcess(string hash, string privateKey)
        {
            try
            {
                if (!_initialized || privateKey.IsNullOrEmpty(out _))
                    return string.Empty;

                var _signerDoSignature = SignerUtilities.GetSigner(BlockchainSetting.SignerName);

                if (_ecPrivateKeyParameters == null)
                    _ecPrivateKeyParameters = new ECPrivateKeyParameters(new BigInteger(ClassBase58.DecodeWithCheckSum(privateKey, true)), ClassWalletUtility.ECDomain);

                _signerDoSignature.Init(true, _ecPrivateKeyParameters);

                _signerDoSignature.BlockUpdate(ClassUtility.GetByteArrayFromHexString(hash), 0, hash.Length / 2);

                string signature = Convert.ToBase64String(_signerDoSignature.GenerateSignature());

                // Reset.
                _signerDoSignature.Reset();


                return signature;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check a signature.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public bool CheckSignatureProcess(string hash, string signature, string publicKey)
        {
            try
            {
                if (!_initialized || publicKey.IsNullOrEmpty(out _) || signature.IsNullOrEmpty(out signature))
                    return false;

                if (publicKey == "empty" && signature == "empty")
                    return false;

                if (!ClassUtility.CheckBase64String(signature))
                    return false;

                byte[] decodedPublicKey = ClassBase58.DecodeWithCheckSum(publicKey, false);

                if (decodedPublicKey == null)
                    return false;

                var _signerCheckSignature = SignerUtilities.GetSigner(BlockchainSetting.SignerName);

                if (_ecPublicKeyParameters == null)
                    _ecPublicKeyParameters = new ECPublicKeyParameters(ClassWalletUtility.ECParameters.Curve.DecodePoint(decodedPublicKey), ClassWalletUtility.ECDomain);

                _signerCheckSignature.Init(false, _ecPublicKeyParameters);

                _signerCheckSignature.BlockUpdate(ClassUtility.GetByteArrayFromHexString(hash), 0, hash.Length / 2);

                bool result = _signerCheckSignature.VerifySignature(Convert.FromBase64String(signature));

                // Reset.
                _signerCheckSignature.Reset();


                return result;
            }
            catch
            {
                return false;
            }
        }
    }
}