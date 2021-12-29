using SeguraChain_Lib.Utility;
using System.Collections.Generic;
using System.Threading;

namespace SeguraChain_RPC_Wallet.API.Service.Client.Sub
{
    public class ClassRpcClientObject
    {
        public SemaphoreSlim _semaphoreHandleRpcClient;
        public List<ClassRpcApiClient> _listRpcClientObject;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassRpcClientObject()
        {
            _semaphoreHandleRpcClient = new SemaphoreSlim(1, ClassUtility.GetMaxAvailableProcessorCount());
            _listRpcClientObject = new List<ClassRpcApiClient>();
        }
    }
}
