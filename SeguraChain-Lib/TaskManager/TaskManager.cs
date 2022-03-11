using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public class TaskManager
    {
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public static long CurrentTimestampMillisecond { get; private set; }

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {

                    while (!_cancelTaskManager.IsCancellationRequested)
                    {
                        bool isLocked = false;
                        bool cleanUpDone = false;

                        try
                        {
                            if (Monitor.TryEnter(_taskCollection))
                            {
                                isLocked = true;

                                using (DisposableList<int> listTaskToRemove = new DisposableList<int>())
                                {
                                    for (int i = 0; i < _taskCollection.Count; i++)
                                    {
                                        if (!_taskCollection[i].Disposed)
                                        {
                                            bool doDispose = false;

                                            if (
#if NET5_0_OR_GREATER
                                                _taskCollection[i].Task.IsCompletedSuccessfully
#else
                                                _taskCollection[i].Task.IsCompleted
#endif
                                                || _taskCollection[i].Task.Status == TaskStatus.Canceled || _taskCollection[i].Task.Status == TaskStatus.Faulted ||
                                                _taskCollection[i].Task.IsCanceled || _taskCollection[i].Task.IsFaulted)
                                            {
                                                doDispose = true;
                                            }
                                            else
                                            {
                                                if (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < CurrentTimestampMillisecond)
                                                    doDispose = true;
                                            }

                                            if (doDispose)
                                            {
                                                ClassUtility.CloseSocket(_taskCollection[i].Socket);
                                                _taskCollection[i].Task.Dispose();
                                                _taskCollection[i].Disposed = true;
                                                listTaskToRemove.Add(i);
                                            }
                                        }

                                        if (listTaskToRemove.Count > 0)
                                        {
                                            foreach (int taskId in listTaskToRemove.GetList)
                                                _taskCollection.RemoveAt(taskId);

                                            _taskCollection.TrimExcess();

                                            cleanUpDone = true;
                                        }
                                    }
                                }

                                if (cleanUpDone)
                                    Monitor.PulseAll(_taskCollection);
                            }

                        }
                        finally
                        {
                            if (isLocked)
                                Monitor.Exit(_taskCollection);
                        }
                        await Task.Delay(1000);
                    }
                    
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                Task.Factory.StartNew(async () =>
                {
                    while(!_cancelTaskManager.IsCancellationRequested)
                    {
                        CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();
                        await Task.Delay(10);
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Insert task to manager.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="timestampEnd"></param>
        /// <param name="cancellation"></param>
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, Socket socket = null)
        {

            try
            {
                _taskCollection.Add(new ClassTaskObject()
                {
                    Socket = socket,
                    TimestampEnd = timestampEnd,
                    Task = Task.Factory.StartNew(action, CancellationTokenSource.CreateLinkedTokenSource(_cancelTaskManager.Token,
                       cancellation != null ?
                       cancellation.Token : new CancellationToken(),
                       timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - CurrentTimestampMillisecond)).Token : new CancellationToken()).Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current)
                });
            }
            catch
            {

                try
                {
                    if (cancellation != null)
                    {
                        if (!cancellation.IsCancellationRequested)
                            cancellation.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
            }

        }

        /// <summary>
        /// Stop task manager.
        /// </summary>
        public static void StopTaskManager()
        {
            _cancelTaskManager.Cancel();
        }
    }
}
