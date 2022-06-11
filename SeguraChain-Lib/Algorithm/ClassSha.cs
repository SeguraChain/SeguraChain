using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;
using System;
using System.Threading;

namespace SeguraChain_Lib.Algorithm
{
    public class ClassSha
    {
        private const int ShaSplitDataSizeLimit = 1024; // Max length supported by SHA3-512.

        /// <summary>
        /// Make a big sha3-512 hash representation depending of the size of the data.
        /// Attempt to protect against extension length attacks.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string MakeBigShaHashFromBigData(byte[] data, CancellationTokenSource cancellation)
        {
            string hash = string.Empty;

            using (ClassSha3512DigestDisposable shaObject = new ClassSha3512DigestDisposable())
            {
                if (data.Length > ShaSplitDataSizeLimit)
                {
                    long lengthProceed = 0;

                    while (lengthProceed < data.Length)
                    {
                        if (cancellation != null)
                        {
                            if (cancellation.IsCancellationRequested)
                                break;
                        }

                        long lengthToProceed = ShaSplitDataSizeLimit;

                        if (lengthToProceed + lengthProceed > data.Length)
                            lengthToProceed = data.Length - lengthProceed;

                        byte[] dataToProceed = new byte[lengthToProceed];

                        Array.Copy(data, lengthProceed, dataToProceed, 0, lengthToProceed);

                        hash += ClassUtility.GetHexStringFromByteArray(shaObject.Compute(dataToProceed));

                        lengthProceed += lengthToProceed;
                    }
                }
                else
                    hash = ClassUtility.GetHexStringFromByteArray(shaObject.Compute(data));

                shaObject.Reset();
            }

            return hash;
        }

        /*public static byte[] DoCustomSha(byte[] data, CancellationTokenSource cancellation)
        {

            long dataLength = data.Length;
            //long dataSplitLength = dataLength > dataLength / ShaSplitDataSizeLimit : 1024;

        }*/
    }
}
