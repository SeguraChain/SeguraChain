using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Other.Object.List;
using System;
using System.Numerics;

namespace SeguraChain_RPC_Wallet.Database.Object
{
    public class ClassSendTransactionFeeCostCalculationObject : IDisposable
    {
        public long BlockHeight;
        public long BlockHeightTarget;
        public BigInteger AmountCalculed;
        public BigInteger FeeCalculated;
        public bool CalculationStatus;
        public DisposableDictionary<string, ClassTransactionHashSourceObject> ListTransactionHashToSpend;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassSendTransactionFeeCostCalculationObject()
        {
            ListTransactionHashToSpend = new DisposableDictionary<string, ClassTransactionHashSourceObject>();
        }

        #region Disposable part.

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    ListTransactionHashToSpend.Clear();
                    CalculationStatus = false;
                }
            }

            _disposed = true;
        }

        #endregion
    }
}
