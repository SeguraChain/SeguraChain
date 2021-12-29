using System;
using Org.BouncyCastle.Crypto.Digests;

namespace SeguraChain_Lib.Other.Object.SHA3
{
    public class ClassSha3512DigestDisposable : IDisposable
    {
        private Sha3Digest _sha3Digest;
        private const int ShaBitLength = 512;
        private const int ShaByteArrayLength = 64;
        private int _shaByteArrayLengthSelected;

        public ClassSha3512DigestDisposable(int shaByteArrayLengthSelected = ShaByteArrayLength)
        {
            _shaByteArrayLengthSelected = shaByteArrayLengthSelected;
            _sha3Digest = new Sha3Digest(ShaBitLength);
        }

        #region Dispose functions

        public bool Disposed;

        ~ClassSha3512DigestDisposable()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                _sha3Digest.Reset();
                _sha3Digest = null;
            }
            Disposed = true;
        }

        #endregion

        /// <summary>
        /// Compute the data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="result"></param>
        public void Compute(byte[] data, out byte[] result)
        {
            result = new byte[_shaByteArrayLengthSelected];
            _sha3Digest.BlockUpdate(data, 0, data.Length);
            _sha3Digest.DoFinal(result, 0);
        }

        /// <summary>
        /// Compute the data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Compute(byte[] data)
        {
            byte[] result = new byte[_shaByteArrayLengthSelected];
            _sha3Digest.BlockUpdate(data, 0, data.Length);
            _sha3Digest.DoFinal(result, 0);
            return result;
        }

        /// <summary>
        /// Reset digest.
        /// </summary>
        public void Reset()
        {
            _sha3Digest.Reset();
        }
    }
}
