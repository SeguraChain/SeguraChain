using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public class TaskManager
    {
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        while(!_cancelTaskManager.IsCancellationRequested)
                        {
                            bool useSemaphore = false;
                            try
                            {
                                await _semaphore.WaitAsync(_cancelTaskManager.Token);
                                useSemaphore = true;


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
                                                if (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < ClassUtility.GetCurrentTimestampInMillisecond())
                                                    doDispose = true;
                                            }

                                            if (doDispose)
                                            {
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
                                        }
                                    }

                                }


                            }
                            finally
                            {
                                if (useSemaphore)
                                    _semaphore.Release();
                            }
                            await Task.Delay(1000);
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
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
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                try
                {

                    _semaphore.Wait(new CancellationTokenSource(5000).Token);
                    useSemaphore = true;

                    _taskCollection.Add(new ClassTaskObject()
                    {
                        TimestampEnd = timestampEnd,
                        Task = Task.Factory.StartNew(action, CancellationTokenSource.CreateLinkedTokenSource(_cancelTaskManager.Token,
                        cancellation != null ?
                        cancellation.Token : new CancellationToken(),
                        timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - ClassUtility.GetCurrentTimestampInMillisecond())).Token : new CancellationToken()).Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current)
                    });

                }
                catch
                {
                    // Ignored, catch the exception if the cancellation token of the semaphore is dead.
                }
            }
            finally
            {
                if (useSemaphore)
                    _semaphore.Release();
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
