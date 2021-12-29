using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.InternalForm.Startup;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialize app.
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            Thread.CurrentThread.Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

            if (ClassLog.InitializeWriteLog())
            {
                ClassLog.EnableWriteLogTask();

#if NET5_0_OR_GREATER
                Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ClassWalletStartupInternalForm());
            }
            else
                MessageBox.Show("Error, can't initialize the log system, the desktop wallet can't be started", "Log system error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        #region Other functions.

        /// <summary>
        /// Catch unexpected exception not handled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var filePath = ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + "\\desktop-wallet-crash.log");
            var exception = (Exception)e.ExceptionObject;

            using (var writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Message: " + exception.Message + "<br/>" +
                                 Environment.NewLine +
                                 "StackTrace: " +
                                 exception.StackTrace +
                                 "" + Environment.NewLine + "Date: " + DateTime.Now);
                writer.WriteLine(Environment.NewLine +
                                 "-----------------------------------------------------------------------------" +
                                 Environment.NewLine);
            }

            Trace.TraceError(exception.StackTrace);
            Environment.Exit(1);
        }

        #endregion

    }
}
