using SeguraChain_Lib.Utility;
using System;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Network.Request.Packet.Send.Object
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
            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
        }
    }
}
