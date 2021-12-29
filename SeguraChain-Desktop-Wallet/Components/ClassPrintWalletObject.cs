using SeguraChain_Desktop_Wallet.Properties;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Components
{
    public class ClassPrintWalletObject : IDisposable
    {
        private Bitmap _bitmapQrCodePrivateKey;
        private Bitmap _bitmapQrCodeWalletAddress;

        #region Dispose functions

        public bool Disposed;

        ~ClassPrintWalletObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            Disposed = true;

            if (disposing)
            {
                _bitmapQrCodePrivateKey.Dispose();
                _bitmapQrCodeWalletAddress.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitmapQrCodePrivateKey"></param>
        /// <param name="bitmapQrCodeWalletAddress"></param>
        public ClassPrintWalletObject(Bitmap bitmapQrCodePrivateKey, Bitmap bitmapQrCodeWalletAddress)
        {
            _bitmapQrCodePrivateKey = bitmapQrCodePrivateKey;
            _bitmapQrCodeWalletAddress = bitmapQrCodeWalletAddress;
        }

        /// <summary>
        /// Execute the print dialog of the wallet qr codes.
        /// </summary>
        /// <param name="parentForm"></param>
        public void DoPrintWallet(Form parentForm)
        {
            using (PrintPreviewDialog printPreviewQrCodeDialog = new PrintPreviewDialog())
            {
                using (PrintDocument printDocument = new PrintDocument())
                {
                    printDocument.PrintPage += PrintDocumentWalletQrCode_Event;
                    printPreviewQrCodeDialog.Document = printDocument;
                    printPreviewQrCodeDialog.ShowDialog(parentForm);
                }
            }
        }


        /// <summary>
        /// Print document wallet created qr codes paint event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDocumentWalletQrCode_Event(object sender, PrintPageEventArgs e)
        {
            using (Bitmap bitmapWallet = new Bitmap(Resources.Wallet_Picture_Print.Width, Resources.Wallet_Picture_Print.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmapWallet))
                {
                    graphics.DrawImage(Resources.Wallet_Picture_Print, new RectangleF(0, 0, Resources.Wallet_Picture_Print.Width, Resources.Wallet_Picture_Print.Height));

                    graphics.DrawImage(Resources.logo_web_profil, new Rectangle(400, 150, 200, 200));

                    graphics.DrawImage(new Bitmap(_bitmapQrCodePrivateKey), new Rectangle(90, 71, 250, 250));
                    graphics.DrawImage(new Bitmap(_bitmapQrCodeWalletAddress), new Rectangle(675, 200, 250, 250));

                    float newHeight = (Resources.Wallet_Picture_Print.Height * 90) / 100f;

                    RectangleF bounds = e.PageSettings.PrintableArea;

                    e.Graphics.DrawImage(bitmapWallet, new RectangleF(0, 0, bounds.Width, newHeight));
                }
            }
        }
    }
}
