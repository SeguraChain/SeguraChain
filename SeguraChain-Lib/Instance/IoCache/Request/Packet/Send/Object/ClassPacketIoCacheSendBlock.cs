using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.IoCache.Request.Packet.Send.Object
{
    public class ClassPacketIoCacheSendBlock
    {
        public SortedList<long, ClassBlockObject> ListBlockObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassPacketIoCacheSendBlock()
        {
            ListBlockObject = new SortedList<long, ClassBlockObject>();
        }
    }
}
