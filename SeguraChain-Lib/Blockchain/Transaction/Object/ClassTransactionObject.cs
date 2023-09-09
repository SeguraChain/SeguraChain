using System.Collections.Generic;
using System.Numerics;
using SeguraChain_Lib.Blockchain.Transaction.Enum;

namespace SeguraChain_Lib.Blockchain.Transaction.Object
{
    public class ClassTransactionObject
    {
        #region General informations of the transaction.

        public ClassTransactionEnumType TransactionType;
        public long BlockHeightTransaction;
        public long BlockHeightTransactionConfirmationTarget;
        public string TransactionHash; // Transaction hash generated from informations of the transaction.
        public BigInteger Amount;
        public BigInteger Fee;
        public long PaymentId; // Usefull for exchanges.
        public int TransactionVersion;
        public long TimestampSend;
        public long TimestampBlockHeightCreateSend;
        public Dictionary<string, ClassTransactionHashSourceObject> AmountTransactionSource;

        #endregion

        #region Informations of the wallet who send the transaction.

        public string WalletAddressSender;
        public string WalletPublicKeySender;
        public string TransactionSignatureSender; // Signature of the sender encoded into Base64 String.
        public string TransactionBigSignatureSender; // Big signature of the sender encoded into Base64 String.

        #endregion

        #region Informations of the wallet receiver, some informations are used for a transfer transaction type only.

        public string WalletAddressReceiver;
        public string WalletPublicKeyReceiver;
        public string TransactionSignatureReceiver; // Signature of the sender encoded into Base64 String.
        public string TransactionBigSignatureReceiver; // Big signature of the receiver encoded into Base64 String.

        #endregion

        #region Informations about the block reward.

        public string BlockHash;

        #endregion

        #region Informations about dev block reward fee.

        public string TransactionHashBlockReward;

        #endregion


        /// <summary>
        /// Constructor. Default values.
        /// </summary>
        public ClassTransactionObject()
        {
            WalletAddressReceiver = string.Empty;
            WalletPublicKeyReceiver = string.Empty;
            TransactionSignatureReceiver = string.Empty;
            TransactionBigSignatureReceiver = string.Empty;
            TransactionBigSignatureSender = string.Empty;
            BlockHash = string.Empty;
            TransactionHashBlockReward = string.Empty;
            WalletAddressSender = string.Empty;
            WalletPublicKeySender = string.Empty;
            TransactionSignatureSender = string.Empty; // Signature of the sender encoded into Base64 String.
            TransactionBigSignatureSender = string.Empty; // Big signature of the sender encoded into Base64 String.
            TransactionHash = string.Empty;
        }
    }

}
