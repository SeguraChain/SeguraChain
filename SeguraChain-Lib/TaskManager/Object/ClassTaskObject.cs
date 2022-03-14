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
            Task = new Task(action, Cancellation.Token);
            Task.ConfigureAwait(false);
            Task.Start();
        }
    }
}
