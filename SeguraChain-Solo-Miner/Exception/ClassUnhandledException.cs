using System;
using System.Diagnostics;
using System.IO;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Solo_Miner.Exception
{
    public class ClassUnhandledException
    {

        /// <summary>
        /// Catch unexpected exception not handled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var filePath = ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + "\\error_solo-miner.txt");
            var exception = (System.Exception)e.ExceptionObject;
            using (var writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Message :" + exception.Message + "<br/>" + Environment.NewLine +
                                 "StackTrace :" +
                                 exception.StackTrace +
                                 "" + Environment.NewLine + "Date :" + DateTime.Now);
                writer.WriteLine(Environment.NewLine +
                                 "-----------------------------------------------------------------------------" +
                                 Environment.NewLine);
            }

            Trace.TraceError(exception.StackTrace);
            Environment.Exit(1);
        }

    }
}
