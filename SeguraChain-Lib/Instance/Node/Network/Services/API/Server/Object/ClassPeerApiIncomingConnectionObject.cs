using System.Collections.Concurrent;
using System.Threading;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Object
{
    public class ClassPeerApiIncomingConnectionObject
    {
        public ConcurrentDictionary<long, ClassPeerApiClientObject> ListApiClientObject;
        public SemaphoreSlim SemaphoreHandleConnection;

        public ClassPeerApiIncomingConnectionObject()
        {
            SemaphoreHandleConnection = new SemaphoreSlim(1, ClassUtility.GetMaxAvailableProcessorCount() * ClassUtility.GetMaxAvailableProcessorCount());
            ListApiClientObject = new ConcurrentDictionary<long, ClassPeerApiClientObject>();
        }
    }
}
