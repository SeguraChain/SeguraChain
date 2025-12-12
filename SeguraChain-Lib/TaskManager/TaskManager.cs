using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public static class TaskManager
    {
        private static volatile bool _enabled;
        private static readonly CancellationTokenSource _globalCts = new CancellationTokenSource();

        private static readonly List<ClassTaskObject> _tasks = new List<ClassTaskObject>(1024);
        private static readonly DisposableList<long> _completedTaskIds = new DisposableList<long>();

        private const int MaxCompletedBeforeCleanup = 5000;

        public static long CurrentTimestampMillisecond { get; private set; }
        public static long CurrentTimestampSecond { get; private set; }

        public static int CountTask
        {
            get
            {
                lock (_tasks)
                    return _tasks.Count;
            }
        }

        public static int CountTaskCompleted
        {
            get
            {
                int count = 0;
                lock (_tasks)
                {
                    for (int i = 0; i < _tasks.Count; i++)
                        if (_tasks[i]?.Disposed == true)
                            count++;
                }
                return count;
            }
        }

        #region ENABLE / STOP

        public static void EnableTaskManager(ClassPeerNetworkSettingObject settings)
        {
            if (_enabled)
                return;

            _enabled = true;

            StartTimestampUpdater();
            StartTaskCleaner();
            StartCompletedTaskCleanup();
        }

        public static void StopTaskManager()
        {
            _enabled = false;

            if (!_globalCts.IsCancellationRequested)
                _globalCts.Cancel();

            ForceCleanup();
        }

        #endregion

        #region BACKGROUND WORKERS

        private static void StartTimestampUpdater()
        {
            Task.Run(async () =>
            {
                while (_enabled)
                {
                    CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();
                    CurrentTimestampSecond = ClassUtility.GetCurrentTimestampInSecond();
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }, _globalCts.Token);
        }

        private static void StartTaskCleaner()
        {
            Task.Run(async () =>
            {
                while (_enabled)
                {
                    ManageTasks(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }, _globalCts.Token);
        }

        private static void StartCompletedTaskCleanup()
        {
            Task.Run(async () =>
            {
                while (_enabled)
                {
                    CleanupCompletedTasks();
                    await Task.Delay(60000).ConfigureAwait(false);
                }
            }, _globalCts.Token);
        }

        #endregion

        #region CORE LOGIC

        private static void ManageTasks(bool force)
        {
            lock (_tasks)
            {
                for (int i = 0; i < _tasks.Count; i++)
                {
                    ClassTaskObject task = _tasks[i];
                    if (task == null || task.Disposed || !task.Started)
                        continue;

                    bool dispose = force ||
                                   task.Task == null ||
                                   task.Task.IsCompleted ||
                                   task.Task.IsCanceled ||
                                   task.Task.IsFaulted ||
                                   (task.TimestampEnd > 0 && task.TimestampEnd < CurrentTimestampMillisecond);

                    if (!dispose)
                        continue;

                    task.Disposed = true;
                    task.Socket?.Kill(SocketShutdown.Both);

                    try { task.Cancellation?.Cancel(); } catch { }
                    try { task.Task?.Dispose(); } catch { }

                    _completedTaskIds.Add(task.Id);
                }
            }
        }

        private static void CleanupCompletedTasks()
        {
            if (_completedTaskIds.Count < MaxCompletedBeforeCleanup)
                return;

            lock (_tasks)
            {
                for (int i = _tasks.Count - 1; i >= 0; i--)
                {
                    if (_tasks[i] == null || _tasks[i].Disposed)
                        _tasks.RemoveAt(i);
                }
            }

            _completedTaskIds.Clear();
        }

        private static void ForceCleanup()
        {
            ManageTasks(true);
            CleanupCompletedTasks();
        }

        #endregion

        #region INSERT TASK

        public static async Task InsertTask(
            Action action,
            long timestampEnd,
            CancellationTokenSource cancellation,
            ClassCustomSocket socket = null,
            bool useFactory = false)
        {
            if (!_enabled)
                return;

            long remaining = timestampEnd > 0
                ? Math.Max(0, timestampEnd - CurrentTimestampMillisecond)
                : 0;

            CancellationTokenSource localCts;

            try
            {
                localCts = cancellation != null && remaining > 0
                    ? CancellationTokenSource.CreateLinkedTokenSource(
                        cancellation.Token,
                        new CancellationTokenSource((int)remaining).Token)
                    : new CancellationTokenSource();
            }
            catch
            {
                return;
            }

            if (useFactory)
            {
                try
                {
                    await Task.Factory.StartNew(
                        action,
                        localCts.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default).ConfigureAwait(false);
                }
                catch { }
                return;
            }

            lock (_tasks)
            {
                var taskObj = new ClassTaskObject(action, localCts, timestampEnd, socket);
                _tasks.Add(taskObj);
                if (!taskObj.Started)
                    taskObj.Run();
            }
        }

        #endregion
    }
}
