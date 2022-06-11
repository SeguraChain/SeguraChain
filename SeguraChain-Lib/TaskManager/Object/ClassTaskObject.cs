using SeguraChain_Lib.Other.Object.Network;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager.Object
{
    public class ClassTaskObject
    {
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
            Cancellation = cancellation;
            TimestampEnd = timestampEnd;
            Socket = socket;
            Action = action;
        }
    }
}
