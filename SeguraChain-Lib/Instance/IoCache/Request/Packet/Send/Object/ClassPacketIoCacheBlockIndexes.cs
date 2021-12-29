using SeguraChain_Lib.Utility;
using System;

namespace SeguraChain_Lib.Instance.IoCache.Request.Packet.Send.Object
{
    public class ClassPacketIoCacheBlockIndexes
    {
        public long BlockHeightStart;
        public long BlockHeightEnd;
        public long PacketTimestamp;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockTupleHeight"></param>
        public ClassPacketIoCacheBlockIndexes(Tuple<long, long> blockTupleHeight)
        {
            BlockHeightStart = blockTupleHeight.Item1;
            BlockHeightEnd = blockTupleHeight.Item2;
            PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond();
        }
    }
}
