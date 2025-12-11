using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
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
        private static bool _enabled;
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();

        // Thread-safe collection
        private static readonly ConcurrentDictionary<long, ClassTaskObject> _taskCollection =
            new ConcurrentDictionary<long, ClassTaskObject>();

        // Queue of completed tasks
        private static readonly ConcurrentQueue<long> _completedTaskIds =
            new ConcurrentQueue<long>();

        private const int MaxTaskClean = 10000;

        // Timestamps
        public static long CurrentTimestampMillisecond { get; private set; }
        public static long CurrentTimestampSecond { get; private set; }

        // Timers (NET 4.8)
        private static Timer _timestampTimer;
        private static Timer _cleanupTimer;

        public static int CountTask => _taskCollection.Count;

        public static int CountTaskCompleted
        {
            get
            {
                try
                {
                    return _taskCollection.Values.Count(x => x != null && x.Disposed);
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Enable the task manager.
        /// </summary>
        public static void EnableTaskManager(ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            if (_enabled)
                return;

            _enabled = true;

            // Reset CTS if needed
            if (_cancelTaskManager.IsCancellationRequested)
            {
                _cancelTaskManager.Dispose();
                _cancelTaskManager = new CancellationTokenSource();
            }

            // --------------------------------------------
            // Timestamp timer (update every 50ms)
            // --------------------------------------------
            _timestampTimer = new Timer(state =>
            {
                if (!_enabled) return;

                try
                {
                    CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();
                    CurrentTimestampSecond = ClassUtility.GetCurrentTimestampInSecond();
                }
                catch { }

            }, null, 0, 50);

            // --------------------------------------------
            // Cleanup timer (every 60 sec)
            // --------------------------------------------
            _cleanupTimer = new Timer(state =>
            {
                if (!_enabled) return;

                try
                {
                    ManageTask(false);
                    PerformCompletedIdsCleanup();
                }
                catch (Exception ex)
                {
                    ClassLog.WriteLine("Cleanup error: " + ex.Message,
                        ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER,
                        ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                        false, ConsoleColor.Red);
                }

            }, null, 60000, 60000);
        }

        /// <summary>
        /// Manage tasks: mark completed ones as disposed.
        /// </summary>
        private static void ManageTask(bool force)
        {
            var snapshot = _taskCollection.ToArray();

            foreach (var kvp in snapshot)
            {
                var taskObj = kvp.Value;
                if (taskObj == null)
                    continue;

                if (taskObj.Disposed || !taskObj.Started)
                    continue;

                bool doDispose = false;

                if (!force)
                {
                    var t = taskObj.Task;

                    if (t != null)
                    {
                        bool isCompleted = false;
#if NET5_0_OR_GREATER
                        isCompleted = t.IsCanceled || t.IsCompletedSuccessfully || t.IsFaulted;
#else
                        isCompleted = t.IsCanceled || t.IsCompleted || t.IsFaulted;
#endif

                        if (isCompleted)
                            doDispose = true;
                    }

                    if (taskObj.TimestampEnd > 0 &&
                        taskObj.TimestampEnd < CurrentTimestampMillisecond)
                        doDispose = true;

                    try
                    {
                        if (taskObj.Cancellation != null &&
                            taskObj.Cancellation.IsCancellationRequested)
                            doDispose = true;
                    }
                    catch { }

                    if (!doDispose)
                        continue;
                }

                // Dispose
                taskObj.Disposed = true;

                try
                {
                    taskObj.Socket?.Kill(SocketShutdown.Both);
                }
                catch { }

                try
                {
                    if (taskObj.Cancellation != null &&
                        !taskObj.Cancellation.IsCancellationRequested)
                        taskObj.Cancellation.Cancel();
                }
                catch { }

                try
                {
                    var tr = taskObj.Task;
                    if (tr != null)
                    {
#if NET5_0_OR_GREATER
                        if (tr.IsCanceled || tr.IsCompletedSuccessfully || tr.IsFaulted)
                            tr.Dispose();
#else
                        if (tr.IsCanceled || tr.IsCompleted || tr.IsFaulted)
                            tr.Dispose();
#endif
                    }
                }
                catch { }

                _completedTaskIds.Enqueue(taskObj.Id);
            }

            if (force)
                PerformCompletedIdsCleanup();
        }

        /// <summary>
        /// Cleanup completed tasks.
        /// </summary>
        private static void PerformCompletedIdsCleanup()
        {
            int toProcess = Math.Min(_completedTaskIds.Count, MaxTaskClean);
            if (toProcess <= 0)
                return;

            int processed = 0;

            while (processed < toProcess &&
                   _completedTaskIds.TryDequeue(out long taskId))
            {
                try
                {
                    _taskCollection.TryRemove(taskId, out _);
                    processed++;
                }
                catch { }
            }

            // Extra safety pass
            foreach (var kvp in _taskCollection.ToArray())
            {
                if (kvp.Value == null || kvp.Value.Disposed)
                {
                    _taskCollection.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>
        /// Insert a task.
        /// </summary>
        public static async Task InsertTask(Action action, long timestampEnd,
            CancellationTokenSource cancellation, ClassCustomSocket socket = null,
            bool useFactory = false)
        {
            if (!_enabled)
                return;

            long endDelta = timestampEnd - CurrentTimestampMillisecond;

            CancellationTokenSource cancellationTask = null;
            bool exceptionDuringCreation = false;

            try
            {
                if (cancellation != null && endDelta > 0)
                {
                    cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellation.Token,
                        new CancellationTokenSource((int)endDelta).Token
                    );
                }
                else
                {
                    cancellationTask = endDelta > 0
                        ? new CancellationTokenSource((int)endDelta)
                        : new CancellationTokenSource();
                }
            }
            catch
            {
                exceptionDuringCreation = true;
            }

            if (exceptionDuringCreation || cancellationTask == null)
                return;

            if (cancellationTask.IsCancellationRequested)
                return;

            if (useFactory)
            {
                try
                {
                    await Task.Factory.StartNew(action, cancellationTask.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ConfigureAwait(false);
                }
                catch { }
                return;
            }

            ClassTaskObject taskObj;
            try
            {
                taskObj = new ClassTaskObject(action, cancellationTask, timestampEnd, socket);
            }
            catch (Exception ex)
            {
                ClassLog.WriteLine("Failed to create ClassTaskObject: " + ex.Message,
                    ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER,
                    ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                    false, ConsoleColor.Red);
                return;
            }

            // Insert task — collisions are extremely unlikely
            if (!_taskCollection.TryAdd(taskObj.Id, taskObj))
            {
                ClassLog.WriteLine("Task ID collision: " + taskObj.Id,
                    ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER,
                    ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                    false, ConsoleColor.Yellow);
                return;
            }

            try
            {
                if (!taskObj.Started)
                    taskObj.Run();
            }
            catch (Exception ex)
            {
                ClassLog.WriteLine("Failed to start task: " + ex.Message,
                    ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER,
                    ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                    false, ConsoleColor.Red);

                taskObj.Disposed = true;
                _completedTaskIds.Enqueue(taskObj.Id);
            }
        }

        /// <summary>
        /// Stop task manager.
        /// </summary>
        public static void StopTaskManager()
        {
            _enabled = false;

            try { _timestampTimer?.Dispose(); } catch { }
            try { _cleanupTimer?.Dispose(); } catch { }

            if (!_cancelTaskManager.IsCancellationRequested)
                _cancelTaskManager.Cancel();

            ManageTask(true);
        }
    }
}
