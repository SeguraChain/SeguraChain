using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager.Object
{
    public class ClassTaskObject
    {
        public bool Disposed;
        public Task Task;
        public CancellationTokenSource Cancellation;
        public long TimestampEnd;
        public Socket Socket;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassTaskObject(Action action, CancellationTokenSource cancellation, long timestampEnd, Socket socket)
        {
            Cancellation = cancellation;
            TimestampEnd = timestampEnd;
            Socket = socket;
            Task = Task.Factory.StartNew(action, Cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);
            Task.ConfigureAwait(false);
        }
    }
}
