using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
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


                        int count = _taskCollection.Count;
                        for (int i = 0; i < count; i++)
                        {

                            try
                            {
                                if (_taskCollection[i] == null || _taskCollection[i].Started || _taskCollection[i].Disposed)
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
                                if (i > _taskCollection.Count || count > _taskCollection.Count)
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
                    bool isLocked = false;
                    bool changeDone = false;
                    try
                    {
                        isLocked = Monitor.TryEnter(_taskCollection);
                        if (isLocked)
                        {

                            for (int i = 0; i < _taskCollection.Count; i++)
                            {

                                if (_taskCollection[i] == null || _taskCollection[i].Task == null || _taskCollection[i].Disposed || !_taskCollection[i].Started)
                                    continue;

                                if (!_taskCollection[i].Disposed && _taskCollection[i].Started && _taskCollection[i].Task != null)
                                {
                                    try
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

                                        changeDone = true;
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
                                    catch(Exception error)
                                    {
                                        ClassLog.WriteLine("Error on cleaning the task ID: " + i + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (isLocked)
                        {
                            if (changeDone)
                                Monitor.PulseAll(_taskCollection);
                            Monitor.Exit(_taskCollection);
                        }
                    }
                    await Task.Delay(1);
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

                    await Task.Delay(1);
                }
            }), 0, _cancelTaskManager);

            #endregion

            #region Auto clean up dead tasks.

            InsertTask(new Action(async () =>
            {

                while (TaskManagerEnabled)
                {

                    if (listTaskToRemove.Count >= MaxTaskClean)
                    {
                        bool isLocked = false;
                        bool cleaned = false;

                        try
                        {
                            isLocked = Monitor.TryEnter(_taskCollection);

                            if (!isLocked)
                                continue;

                                try
                                {
                                    foreach (int taskId in listTaskToRemove.GetList.ToArray())
                                    {
                                        if (taskId >= _taskCollection.Count)
                                            continue;

                                        try
                                        {
                                            _taskCollection.RemoveAt(taskId);
                                            cleaned = true;

                                            listTaskToRemove.Remove(taskId);
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
                            
                        }
                        finally
                        {
                            if (isLocked)
                            {
                                if (cleaned)
                                    Monitor.PulseAll(_taskCollection);

                                Monitor.Exit(_taskCollection);
                            }
                        }
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
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, Socket socket = null, bool useFactory = false)
        {
            if (TaskManagerEnabled)
            {

                bool isLocked = false;

                CancellationTokenSource cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                cancellation != null ?
                cancellation.Token : new CancellationToken(),
                timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - CurrentTimestampMillisecond)).Token : new CancellationToken());

                if (useFactory)
                {
                    try
                    {
                        Task.Factory.StartNew(action, cancellationTask.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignored, catch the exception once the task is cancelled.
                    }
                }
                else
                {
                    try
                    {

                        while (!cancellationTask.IsCancellationRequested && TaskManagerEnabled && !_cancelTaskManager.IsCancellationRequested)
                        {
                            if (!TaskManagerEnabled || (timestampEnd > 0 && timestampEnd < CurrentTimestampMillisecond))
                                break;

                            try
                            {
                                isLocked = Monitor.TryEnter(_taskCollection);

                                if (isLocked)
                                {
                                    _taskCollection.Add(new ClassTaskObject(action, cancellationTask, timestampEnd, socket));
                                    break;
                                }
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Error on insert a new task to the TaskManager | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            }
                        }

                    }
                    finally
                    {
                        if (isLocked)
                        {
                            Monitor.PulseAll(_taskCollection);
                            Monitor.Exit(_taskCollection);
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
