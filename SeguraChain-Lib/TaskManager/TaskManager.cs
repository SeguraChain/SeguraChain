using SeguraChain_Lib.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public class TaskManager
    {
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();


        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation)
        {

            CancellationTokenSource cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cancelTaskManager.Token,
                cancellation != null ?
                cancellation.Token : new CancellationToken(),
                timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - ClassUtility.GetCurrentTimestampInMillisecond())).Token : new CancellationToken());

            Task.Factory.StartNew(action, cancellationToken.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
        }

        public static void StopTaskManager()
        {
            _cancelTaskManager.Cancel();
        }
    }
}
