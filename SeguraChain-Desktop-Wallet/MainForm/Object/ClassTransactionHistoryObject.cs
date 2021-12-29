using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using SeguraChain_Desktop_Wallet.Enum;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;

namespace SeguraChain_Desktop_Wallet.MainForm.Object
{
    /// <summary>
    /// Transaction history object constantly updated to paint.
    /// </summary>
    public class ClassTransactionHistoryObject
    {
        /// <summary>
        /// Listing progress from sync.
        /// </summary>
        public long LastBlockHeight;
        public long LastTransactionCount;
        public long LastTransactionCountOnRead;
        public Dictionary<string, TransactionHistoryInformationObject> DictionaryTransactionHistoryHashListed;
        public ConcurrentDictionary<string, TransactionHistoryInformationShowedObject> DictionaryTransactionHistoryHashListedShowed;
        public long LastBlockTransactionShowTimestampUpdate;

        /// <summary>
        /// Transaction history graphics content.
        /// </summary>
        public Graphics GraphicsTransactionHistory;
        public Bitmap BitmapTransactionHistory;

        /// <summary>
        /// Transaction history draw & pages progress.
        /// </summary>
        public ClassEnumTransactionHistoryColumnType TransactionHistoryColumnOrdering;
        public bool EnableEventDrawPage;
        public bool OnLoad;
        public int TotalTransactionShowed;
        public int CurrentTransactionHistoryPage;
        public int Width;
        public int Height;
        public string TransactionInformationSelectedByClick;
        public string TransactionInformationSelectedByPosition;

        /// <summary>
        /// Columns properties calculated on initilization of the graphics content.
        /// </summary>
        public int ColumnDateMaxWidth { get; }
        public int ColumnTypeMaxWidth { get; }
        public int ColumnWalletAddressMaxWidth { get; }
        public int ColumnHashMaxWidth { get;  }
        public int ColumnAmountMaxWidth { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ClassTransactionHistoryObject(int width, int height)
        {
            Width = width;
            Height = height;
            ColumnDateMaxWidth = Width / 5;
            ColumnTypeMaxWidth = ColumnDateMaxWidth * 2;
            ColumnWalletAddressMaxWidth = ColumnDateMaxWidth * 3;
            ColumnHashMaxWidth = ColumnDateMaxWidth * 4;
            ColumnAmountMaxWidth = ColumnDateMaxWidth * 5;
            OnLoad = true;
            DictionaryTransactionHistoryHashListed = new Dictionary<string ,TransactionHistoryInformationObject>();
            DictionaryTransactionHistoryHashListedShowed = new ConcurrentDictionary<string, TransactionHistoryInformationShowedObject>();
            CurrentTransactionHistoryPage = 1;
            TransactionHistoryColumnOrdering = ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;
            TransactionInformationSelectedByClick = string.Empty;
            TransactionInformationSelectedByPosition = string.Empty;
        }

        /// <summary>
        /// Clear/Initialize the panel transaction history graphics content.
        /// </summary>
        public void InitializeOrClearPanelTransactionHistoryGraphicsContent()
        {
            if (GraphicsTransactionHistory == null)
            {
                BitmapTransactionHistory = new Bitmap(Width, Height);
                GraphicsTransactionHistory = Graphics.FromImage(BitmapTransactionHistory);
            }

            lock (GraphicsTransactionHistory)
            {
                GraphicsTransactionHistory.Clear(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorOnClear);

                GraphicsTransactionHistory.SmoothingMode = SmoothingMode.HighQuality;
                GraphicsTransactionHistory.CompositingQuality = CompositingQuality.HighQuality;

                GraphicsTransactionHistory.FillRectangle(new SolidBrush(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorOnClear), new Rectangle(0, 0, BitmapTransactionHistory.Width, BitmapTransactionHistory.Height));


                // Column date.
                GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, ColumnDateMaxWidth, Height, ColumnDateMaxWidth, 0);
                // Column type.
                GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, ColumnTypeMaxWidth, Height, ColumnTypeMaxWidth, 0);
                // Column wallet address.
                GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, ColumnWalletAddressMaxWidth, Height, ColumnWalletAddressMaxWidth, 0);
                // Column hash.
                GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, ColumnHashMaxWidth, Height, ColumnHashMaxWidth, 0);
                // Column amount.
                GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, ColumnAmountMaxWidth, Height, ColumnAmountMaxWidth, 0);

                Rectangle rectangleBorderTransaction;
                int height = Height / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                for (int i = 0; i < ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage; i++)
                {
                    int positionY = height * i;

                    rectangleBorderTransaction = new Rectangle(0, positionY, Width - 1, Height);

                    GraphicsTransactionHistory.DrawRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryCellLinesPen, rectangleBorderTransaction);
                }

                rectangleBorderTransaction = new Rectangle(0, Height, Width - 1, Height);

                GraphicsTransactionHistory.DrawRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryCellLinesPen, rectangleBorderTransaction);
            }
        }

        /// <summary>
        /// Clear the transaction history content.
        /// </summary>
        public void ClearTransactionHistoryContent()
        {
            TransactionHistoryColumnOrdering = ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;
            DictionaryTransactionHistoryHashListed.Clear();

#if NET5_0_OR_GREATER
            DictionaryTransactionHistoryHashListed.TrimExcess();
#endif
            DictionaryTransactionHistoryHashListedShowed.Clear();

            TotalTransactionShowed = 0;
            LastTransactionCount = 0;
            LastTransactionCountOnRead = 0;
            LastBlockHeight = 0;
            CurrentTransactionHistoryPage = 1;
            EnableEventDrawPage = true;
        }
    }

    /// <summary>
    /// Transaction history information object.
    /// </summary>
    public class TransactionHistoryInformationObject
    {
        public bool IsMemPool;
        public DateTime DateSent;
        public string TransactionType;
        public string WalletAddress;
        public string TransactionHash;
        public BigInteger Amount;
        public BigInteger Fee;
    }

    public class TransactionHistoryInformationShowedObject
    {
        public bool IsMemPool;
        public ClassBlockTransaction BlockTransaction;
        public TransactionHistoryInformationObject TransactionHistoryInformationObject;
        public Rectangle RectangleTransaction;
    }
}
