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
        private static DisposableList<long> _listTaskIdCompleted = new DisposableList<long>();
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

            #region Auto clean up dead tasks.

            InsertTask(new Action(async () =>
            {

                while (TaskManagerEnabled)
                {
                    ManageTask(false);
                    await Task.Delay(1000);
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

                                using (DisposableList<long> listTaskToDelete = new DisposableList<long>(false, 0, _listTaskIdCompleted.GetList.ToArray()))
                                {
                                    foreach(long taskId in listTaskToDelete.GetList)
                                    {

                                        try
                                        {
                                            _taskCollection.RemoveAll(x => x != null && x.Id == taskId);
                                            cleaned = true;

                                            _listTaskIdCompleted.Remove(taskId);
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
        /// Manage every tasks stored into the TaskManager.
        /// </summary>
        /// <returns></returns>
        private static void ManageTask(bool force)
        {
            bool isLocked = false;
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

                            _listTaskIdCompleted.Add(_taskCollection[i].Id);
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
                    Monitor.Exit(_taskCollection);
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
                //useFactory = true;

                long end = timestampEnd - CurrentTimestampMillisecond;

                CancellationTokenSource cancellationTask = null;

                bool exception = false;

                try
                {

                    cancellationTask =
                        cancellation != null && end > 0 ?
                        CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, new CancellationTokenSource((int)end).Token)
                        : (end > 0 ? new CancellationTokenSource((int)end) : new CancellationTokenSource());

                }
                catch
                {
                    exception = true;
                }

                if (!exception)
                {
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
                            isLocked = Monitor.TryEnter(_taskCollection);

                            if (isLocked)
                            {
                                _taskCollection.Add(new ClassTaskObject(action, cancellationTask, timestampEnd, socket));
                                if (!_taskCollection[_taskCollection.Count - 1].Started)
                                {
                                    _taskCollection[_taskCollection.Count - 1].Started = true;
                                    _taskCollection[_taskCollection.Count - 1].Run();
                                }
                            }
                        }
                        finally
                        {
                            if (isLocked)
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
