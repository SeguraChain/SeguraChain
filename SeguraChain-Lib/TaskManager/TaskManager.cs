using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
using System.Linq;
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

        /// <summary>
        /// Store tasks, task id's to delete.
        /// </summary>
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private static DisposableList<int> _listTaskIdCompleted = new DisposableList<int>();
        private const int MaxTaskClean = 10000;

        /// <summary>
        /// Current timestamp 
        /// </summary>
        public static long CurrentTimestampMillisecond { get; private set; }
        public static long CurrentTimestampSecond { get; private set; }

        public static int CountTask => _taskCollection.Count;
        public static int CountTaskCompleted
        {
            get
            {
                int count = 0;
                bool isLocked = false;
                try
                {
                    isLocked = Monitor.TryEnter(_taskCollection);
                    if (isLocked)
                        count = _taskCollection.Count(x => x.Disposed == true);
                }
                finally
                {
                    if (isLocked)
                        Monitor.Exit(_taskCollection);
                }
                return count;
            }
        }

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager(ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            TaskManagerEnabled = true;

            SetThreadPoolValue(peerNetworkSettingObject);

            #region Auto run task stored.

            
            InsertTask(new Action(async () =>
            {


                while (TaskManagerEnabled)
                {
                    RunTask();
                    await Task.Delay(1);
                }

            }), 0, _cancelTaskManager, null, true);

            #endregion

            #region Auto clean up dead tasks.

            InsertTask(new Action(async () =>
            {

                while (TaskManagerEnabled)
                {
                    ManageTask(false);
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
                    if (_listTaskIdCompleted.Count >= MaxTaskClean)
                    {
                        bool isLocked = false;
                        bool cleaned = false;

                        try
                        {
                            isLocked = Monitor.TryEnter(_taskCollection);

                            if (isLocked)
                            {

                                using (DisposableList<int> listTaskToDelete = new DisposableList<int>(false, 0, _listTaskIdCompleted.GetList.ToArray()))
                                {
                                    for(int i = 0; i < listTaskToDelete.Count; i++)
                                    {
                                        if (i >= _taskCollection.Count)
                                            break;

                                        try
                                        {
                                            _taskCollection.RemoveAt(listTaskToDelete[i]);
                                            cleaned = true;

                                            _listTaskIdCompleted.Remove(i);
                                        }
                                        catch
                                        {
                                            continue;
                                        }
                                    }
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
        /// Set thread pools min values and max values.
        /// </summary>
        /// <param name="peerNetworkSettingObject"></param>
        private static void SetThreadPoolValue(ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            ThreadPool.SetMinThreads(peerNetworkSettingObject.PeerMinThreadsPool, peerNetworkSettingObject.PeerMinThreadsPoolCompletionPort);
            ThreadPool.SetMaxThreads(peerNetworkSettingObject.PeerMaxThreadsPool, peerNetworkSettingObject.PeerMaxThreadsPoolCompletionPort);
        }

        /// <summary>
        /// Run every task registered into the task manager.
        /// </summary>
        /// <returns></returns>
        private static void RunTask()
        {
            bool isLocked = false;
            try
            {
                isLocked = Monitor.TryEnter(_taskCollection);

                if (isLocked)
                {
                    int count = _taskCollection.Count;
                    for (int i = 0; i < count; i++)
                    {

                        try
                        {
                            if (_taskCollection[i] == null || _taskCollection[i].Started)
                                continue;

                            if (_taskCollection[i].Disposed ||
                                (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < CurrentTimestampMillisecond))
                            {
                                _taskCollection[i].Started = true;
                                continue;
                            }
                            _taskCollection[i].Run();

                        }
                        catch
                        {
                            // If the amount change..
                            if (i > _taskCollection.Count || count > _taskCollection.Count)
                                break;
                        }
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

        /// <summary>
        /// Manage every tasks stored into the TaskManager.
        /// </summary>
        /// <returns></returns>
        private static void ManageTask(bool force)
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



                        try
                        {
                            bool doDispose = false;

                            if (!force)
                            {
                                if (_taskCollection[i].Task != null && (_taskCollection[i].Task.IsCanceled ||
#if NET5_0_OR_GREATER
                                        _taskCollection[i].Task.IsCompletedSuccessfully ||
#endif

                                    _taskCollection[i].Task.IsFaulted))
                                    doDispose = true;

                                if (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < CurrentTimestampMillisecond)
                                    doDispose = true;

                                try
                                {
                                    if (_taskCollection[i].Cancellation != null)
                                    {
                                        if (_taskCollection[i].Cancellation.IsCancellationRequested)
                                            doDispose = true;
                                    }
                                }
                                catch
                                {
                                    // Ignored.
                                }

                                if (!doDispose)
                                    continue;
                            }

                            changeDone = true;
                            _taskCollection[i].Disposed = true;
                            _taskCollection[i].Socket?.Kill(SocketShutdown.Both);

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


                            try
                            {


                                if (_taskCollection[i].Task != null && (_taskCollection[i].Task.IsCanceled ||
#if NET5_0_OR_GREATER
                                        _taskCollection[i].Task.IsCompletedSuccessfully
#else
                                        _taskCollection[i].Task.IsCompleted
#endif
                                        ||
                                    _taskCollection[i].Task.IsFaulted))
                                    _taskCollection[i].Task?.Dispose();

                            }
                            catch
                            {
                                // Ignored, the task dispose can failed.
                            }

                            _listTaskIdCompleted.Add(i);
                        }
                        catch (Exception error)
                        {
                            ClassLog.WriteLine("Error on cleaning the task ID: " + i + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
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
        }

        /// <summary>
        /// Insert task to manager.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="timestampEnd"></param>
        /// <param name="cancellation"></param>
        /// <param name="socket"></param>
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, ClassCustomSocket socket = null, bool useFactory = false)
        {
            if (TaskManagerEnabled)
            {

                long end = timestampEnd - CurrentTimestampMillisecond;

                CancellationTokenSource cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                cancellation != null ?
                cancellation.Token : new CancellationToken(),
                end > 0 ? new CancellationTokenSource((int)(end)).Token : new CancellationToken());

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
                    bool isLocked = false;

                    try
                    {

                        while (!cancellationTask.IsCancellationRequested && TaskManagerEnabled && !_cancelTaskManager.IsCancellationRequested)
                        {
                            if (timestampEnd > 0 && timestampEnd < CurrentTimestampMillisecond)
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
                                ClassLog.WriteLine("Error on insert a new task to the TaskManager | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true, ConsoleColor.Red);
                                Thread.Sleep(1);
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

            ManageTask(true);
        }
    }
}
