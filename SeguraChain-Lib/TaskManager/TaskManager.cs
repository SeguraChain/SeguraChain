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
        private static bool TaskManagerEnabled;
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private const int MaxTaskClean = 10000;
        public static long CurrentTimestampMillisecond { get; private set; }
        public static long CurrentTimestampSecond { get; private set; }
        private static DisposableList<int> listTaskToRemove = new DisposableList<int>();

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager()
        {
            TaskManagerEnabled = true;

            try
            {
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
                                if (_taskCollection[i] == null || _taskCollection[i].Started)
                                    continue;


                                await Task.Factory.StartNew(() =>
                                {

                                    try
                                    {
                                        _taskCollection[i].Started = true;
                                        _taskCollection[i].Task = Task.Run(_taskCollection[i].Action, _taskCollection[i].Cancellation.Token);
                                    }
                                    catch
                                    {
                                        // Catch the exception if the task cannot start.
                                    }
                                }, _cancelTaskManager.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);


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
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            #region Auto clean up dead tasks.

            InsertTask(new Action(async () =>
            {

                while (TaskManagerEnabled)
                {

                    for (int i = 0; i < _taskCollection.Count; i++)
                    {
                        try
                        {
                            if (_taskCollection[i] == null || _taskCollection[i].Task == null || _taskCollection[i].Disposed || !_taskCollection[i].Started)
                                continue;

                            if (!_taskCollection[i].Disposed && _taskCollection[i].Started && _taskCollection[i].Task != null)
                            {
                                bool doDispose = false;

                                if (_taskCollection[i].Task != null && (_taskCollection[i].Task.IsCanceled ||
                                _taskCollection[i].Task.IsFaulted))
                                    doDispose = true;
                                
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


                                if (!doDispose)
                                    continue;

                                _taskCollection[i].Disposed = true;

                                ClassUtility.CloseSocket(_taskCollection[i].Socket);

                                try
                                {
                                    if (_taskCollection[i].Cancellation != null)
                                    {
                                        if (_taskCollection[i].Cancellation.IsCancellationRequested)
                                            _taskCollection[i].Task?.Dispose();
                                    }
                                    else
                                    {
                                        if ((_taskCollection[i].Task.IsCanceled ||
                                            _taskCollection[i].Task.IsFaulted || _taskCollection[i].Task.Status == TaskStatus.RanToCompletion))
                                            _taskCollection[i].Task?.Dispose();
                                    }
                                }
                                catch
                                {
                                    // Ignored, the task dispose can failed.
                                }

                                listTaskToRemove.Add(i);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                    await Task.Delay(10);
                }

            }), 0, _cancelTaskManager);

            #endregion

            #region Auto update current timestamp in millisecond.

            InsertTask(new Action(async () =>
            {
                while (TaskManagerEnabled)
                {
                    CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();
                    CurrentTimestampSecond = ClassUtility.GetCurrentTimestampInSecond();

                    await Task.Delay(10);
                }
            }), 0, _cancelTaskManager);

            #endregion

            #region Auto clean up dead tasks.

            InsertTask(new Action(async () =>
            {

                while (TaskManagerEnabled)
                {

                    if (listTaskToRemove.Count < MaxTaskClean)
                        continue;

                    bool cleaned = false;
                    try
                    {
                        foreach (int taskId in listTaskToRemove.GetList.ToArray())
                        {
                            if (taskId >= _taskCollection.Count)
                                break;

                            try
                            {
                                _taskCollection.RemoveAt(taskId);

                                if (listTaskToRemove.Remove(taskId))
                                    cleaned = true;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // Collection generic list changed exception.
                    }

                    try
                    {

                        if (cleaned)
                        {
                            _taskCollection.RemoveAll(x => x == null || x.Disposed);
                            _taskCollection.TrimExcess();
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }


                    await Task.Delay(60 * 1000);
                }

            }), 0, _cancelTaskManager);

            #endregion
        }

        /// <summary>
        /// Insert task to manager.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="timestampEnd"></param>
        /// <param name="cancellation"></param>
        /// <param name="socket"></param>
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, Socket socket = null)
        {
            if (TaskManagerEnabled)
            {
                CancellationTokenSource cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                               cancellation != null ?
                               cancellation.Token : new CancellationToken(),
                               timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - CurrentTimestampMillisecond)).Token : new CancellationToken());

                while (!cancellationTask.IsCancellationRequested)
                {
                    if (!TaskManagerEnabled)
                        break;

                    try
                    {
                        _taskCollection.Add(new ClassTaskObject(action, cancellationTask, timestampEnd, socket));
                        break;
                    }
                    catch
                    {
                        try
                        {
                            Task.Factory.StartNew(() => action, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignored, catch the exception once the task is completed.
                        }
                    }
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
