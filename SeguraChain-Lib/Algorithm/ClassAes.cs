using System;
using System.Diagnostics;
using System.Security.Cryptography;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;


namespace SeguraChain_Lib.Algorithm
{
    public class ClassAes
    {
        /// <summary>
        /// AES Settings.
        /// </summary>
        public const int IterationCount = 10240;
        public const int IterationCountKeyGenerator = 1000;
        public const int EncryptionKeySize = 256;
        public const int EncryptionBlockSize = 128;
        public const int EncryptionKeyByteArraySize = 32;
        public const int IvSize = 16;

        /// <summary>
        /// Generate a IV from a key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        public static byte[] GenerateIv(byte[] key, int iteration = IterationCount)
        {
            using (Rfc2898DeriveBytes passwordDeriveBytes = new Rfc2898DeriveBytes(key, BlockchainSetting.BlockchainMarkKey, iteration))
                return passwordDeriveBytes.GetBytes(IvSize);
        }

        /// <summary>
        /// Generate a key from a given data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="useSha"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GenerateKey(byte[] data, bool useSha, out byte[] key)
        {
            key = new byte[EncryptionKeyByteArraySize];

            if (data.Length < EncryptionKeyByteArraySize)
                useSha = true;

            if (useSha)
            {
                data = GenerateIv(data, IterationCountKeyGenerator);
                data = ClassUtility.GenerateSha512ByteArrayFromByteArray(data);
            }

            Array.Copy(data, 0, key, 0, EncryptionKeyByteArraySize);

            return true;
        }

        /// <summary>
        /// AES Encryption process.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool EncryptionProcess(byte[] content, byte[] key, byte[] iv, out byte[] result)
        {
       
                if (content != null)
                {
                    using (RijndaelManaged aesObject = new RijndaelManaged())
                    {
                        aesObject.KeySize = EncryptionKeySize;
                        aesObject.BlockSize = EncryptionBlockSize;
                        aesObject.Key = key;
                        aesObject.IV = iv;
                        aesObject.Mode = CipherMode.CFB;
                        aesObject.Padding = PaddingMode.None;
                        using (ICryptoTransform encryptCryptoTransform = aesObject.CreateEncryptor(key, iv))
                        {
                            byte[] paddedBytes = ClassUtility.DoPadding(content);
                            result = encryptCryptoTransform.TransformFinalBlock(paddedBytes, 0, paddedBytes.Length);
                            return true;
                        }
                    }
                }


            result = null;
            return false;
        }

        /// <summary>
        /// AES Decryption process.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool DecryptionProcess(byte[] content, byte[] key, byte[] iv, out byte[] result)
        {

            try
            {

                if (content != null && key != null && iv != null)
                {
                    using (RijndaelManaged aesObject = new RijndaelManaged())
                    {
                        aesObject.KeySize = EncryptionKeySize;
                        aesObject.BlockSize = EncryptionBlockSize;
                        aesObject.Key = key;
                        aesObject.IV = iv;
                        aesObject.Mode = CipherMode.CFB;
                        aesObject.Padding = PaddingMode.None;
                        using (ICryptoTransform decryptCryptoTransform = aesObject.CreateDecryptor(key, iv))
                        {
                            byte[] decryptedPaddedBytes = decryptCryptoTransform.TransformFinalBlock(content, 0, content.Length);
                            result = ClassUtility.UndoPadding(decryptedPaddedBytes);
                            return true;
                        }
                    }
                }
            }

#if DEBUG
            catch (Exception error)
            {
                Debug.WriteLine("Error on decrypt content. Exception: " + error.Message);
#else
            catch
            {
#endif
            }

            result = null;
            return false;
        }

    }
}
