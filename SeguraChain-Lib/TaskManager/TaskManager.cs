using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public class TaskManager
    {
        private static bool TaskManagerEnabled;
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private const int MaxTaskClean = 10000;
        public static long CurrentTimestampMillisecond { get; private set; }
        private static DisposableList<int> listTaskToRemove = new DisposableList<int>();

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager()
        {
            TaskManagerEnabled = true;

            try
            {
                #region Auto clean up dead tasks.

                Task.Factory.StartNew(async () =>
                {

                    while (TaskManagerEnabled)
                    {

                        for (int i = 0; i < _taskCollection.Count; i++)
                        {
                            try
                            {
                                if (!_taskCollection[i].Disposed && _taskCollection[i].Started && _taskCollection[i].Task != null)
                                {
                                    bool doDispose = false;

                                    if (_taskCollection[i].Task != null && (

                                            _taskCollection[i].Task.IsCanceled || _taskCollection[i].Task.IsFaulted))
                                        doDispose = true;
                                    else
                                    {
                                        if (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < CurrentTimestampMillisecond)

                                        {
                                            doDispose = true;
                                            try
                                            {
                                                if (_taskCollection[i].Cancellation != null)
                                                {
                                                    if (!_taskCollection[i].Cancellation.IsCancellationRequested)
                                                        _taskCollection[i].Cancellation.Cancel();
                                                }
                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }
                                        }
                                    }

                                    if (doDispose)
                                    {
                                        _taskCollection[i].Disposed = true;
                                        if (_taskCollection[i].Socket != null)
                                            ClassUtility.CloseSocket(_taskCollection[i].Socket);
                                        try
                                        {

                                            _taskCollection[i].Task?.Dispose();
                                        }
                                        catch
                                        {
                                            // Ignored, the task dispose can failed.
                                        }
                                        listTaskToRemove.Add(i);
                                    }
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
                        await Task.Delay(10);
                    }

                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                #endregion

                #region Auto update current timestamp in millisecond.

                Task.Factory.StartNew(async () =>
                {
                    while(TaskManagerEnabled)
                    {
                        CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();

                        await Task.Delay(10);
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                #endregion

                #region Auto run task stored.

                Task.Factory.StartNew(async () =>
                {
                    while (TaskManagerEnabled)
                    {
                        for (int i = 0; i < _taskCollection.Count; i++)
                        {
                            if (i > _taskCollection.Count)
                                break;

                            try
                            {
                                if (!_taskCollection[i].Started)
                                {

                                    await Task.Factory.StartNew(() =>
                                    {

                                        try
                                        {
                                            _taskCollection[i].Task = Task.Run(_taskCollection[i].Action, _taskCollection[i].Cancellation.Token);
                                            _taskCollection[i].Started = true;
                                        }
                                        catch
                                        {
                                            // Catch the exception if the task cannot start.
                                        }
                                    }, _cancelTaskManager.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);

                                }
                            }
                            catch
                            {
                                // If the amount change..
                                break;
                            }

                        }
                        await Task.Delay(1);
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                #endregion

                #region Auto clean up dead tasks.

                Task.Factory.StartNew(async () =>
                {
                    while(TaskManagerEnabled)
                    {

                        if (listTaskToRemove.Count >= MaxTaskClean)
                        {
                            foreach (int taskId in listTaskToRemove.GetList)
                                _taskCollection.RemoveAt(taskId);

                            _taskCollection.TrimExcess();
                            listTaskToRemove.Clear();
                        }

                        await Task.Delay(60 * 1000);
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                #endregion
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


            if (TaskManagerEnabled)
            {
                try
                {
                    CancellationTokenSource cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                           cancellation != null ?
                           cancellation.Token : new CancellationToken(),
                           timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - CurrentTimestampMillisecond)).Token : new CancellationToken());

                    _taskCollection.Add(new ClassTaskObject(action, cancellationTask, timestampEnd, socket));
                }
                catch
                {
                    // If the insert of the task failed.
                }
            }


        }

        /// <summary>
        /// Stop task manager.
        /// </summary>
        public static void StopTaskManager()
        {
            TaskManagerEnabled = false;
            if (!_cancelTaskManager.IsCancellationRequested)
                _cancelTaskManager.Cancel();
        }
    }
}
