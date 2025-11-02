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
        private Action _action;
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
            _action = action;
        }

        /// <summary>
        /// Run the action into a task.
        /// </summary>
        public void Run()
        {
            try
            {
                Task = Task.Factory.StartNew(_action, Cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);
            }
            catch
            {
                // Catch the exception, once the task is canelled.
            }
            Started = true;
        }
    }
}
