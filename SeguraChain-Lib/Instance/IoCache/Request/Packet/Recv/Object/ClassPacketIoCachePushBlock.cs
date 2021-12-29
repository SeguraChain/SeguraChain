using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.IoCache.Request.Packet.Recv.Object
{
    public class ClassPacketIoCachePushBlock
    {
        public SortedList<long, ClassBlockObject> ListBlockObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassPacketIoCachePushBlock()
        {
            ListBlockObject = new SortedList<long, ClassBlockObject>();
        }
    }
}
