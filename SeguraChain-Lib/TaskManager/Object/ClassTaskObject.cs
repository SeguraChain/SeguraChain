using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager.Object
{
    public class ClassTaskObject
    {
        public long Id;
        public bool Started;
        public bool Disposed;
        public Action Action;
        public Task Task;
        public CancellationTokenSource Cancellation;
        public long TimestampEnd;
        public ClassCustomSocket Socket;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassTaskObject(Action action, CancellationTokenSource cancellation, long timestampEnd, ClassCustomSocket socket)
        {
            Id = ClassUtility.GetRandomBetweenLong(0, long.MaxValue - 1);
            Cancellation = cancellation;
            TimestampEnd = timestampEnd;
            Socket = socket;
            Action = action;
        }
    }
}
