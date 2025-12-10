﻿using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        // Utilisation de ConcurrentDictionary pour éviter les locks explicites
        private static readonly ConcurrentDictionary<long, ClassTaskObject> _taskCollection = new ConcurrentDictionary<long, ClassTaskObject>();
        private static readonly ConcurrentQueue<long> _taskIdsToRemove = new ConcurrentQueue<long>();
        private const int MaxTaskClean = 10000;
        private const int CleanupBatchSize = 1000;

        // Cache des timestamps pour éviter les appels répétés
        private static long _currentTimestampMillisecond;
        private static long _currentTimestampSecond;

        public static long CurrentTimestampMillisecond => _currentTimestampMillisecond;
        public static long CurrentTimestampSecond => _currentTimestampSecond;

        public static int CountTask => _taskCollection.Count;
        public static int CountTaskCompleted => _taskCollection.Count(x => x.Value.Disposed);

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager(ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            TaskManagerEnabled = true;
            SetThreadPoolValue(peerNetworkSettingObject);

            // Utilisation de Task.Run au lieu de InsertTask pour les tâches système
            Task.Run(async () =>
            {
                while (TaskManagerEnabled)
                {
                    ManageTask(false);
                    await Task.Delay(60000, _cancelTaskManager.Token).ConfigureAwait(false);
                }
            }, _cancelTaskManager.Token);

            Task.Run(async () =>
            {
                while (TaskManagerEnabled)
                {
                    _currentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();
                    _currentTimestampSecond = ClassUtility.GetCurrentTimestampInSecond();
                    await Task.Delay(1, _cancelTaskManager.Token).ConfigureAwait(false);
                }
            }, _cancelTaskManager.Token);

            Task.Run(async () =>
            {
                while (TaskManagerEnabled)
                {
                    if (_taskIdsToRemove.Count >= MaxTaskClean)
                    {
                        CleanupCompletedTasks();
                    }
                    await Task.Delay(60000, _cancelTaskManager.Token).ConfigureAwait(false);
                }
            }, _cancelTaskManager.Token);
        }

        private static void CleanupCompletedTasks()
        {
            int cleaned = 0;
            var idsToProcess = new List<long>(CleanupBatchSize);

            // Traiter par lots pour améliorer les performances
            while (_taskIdsToRemove.TryDequeue(out long taskId) && cleaned < CleanupBatchSize)
            {
                if (_taskCollection.TryRemove(taskId, out _))
                {
                    cleaned++;
                }
            }

#if DEBUG
            if (cleaned > 0)
                Debug.WriteLine($"Total dead tasks cleaned: {cleaned}");
#endif
        }

        private static void SetThreadPoolValue(ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            ThreadPool.SetMinThreads(peerNetworkSettingObject.PeerMinThreadsPool, peerNetworkSettingObject.PeerMinThreadsPoolCompletionPort);
            ThreadPool.SetMaxThreads(peerNetworkSettingObject.PeerMaxThreadsPool, peerNetworkSettingObject.PeerMaxThreadsPoolCompletionPort);
        }

        /// <summary>
        /// Manage every tasks stored into the TaskManager.
        /// </summary>
        private static void ManageTask(bool force)
        {
            // Traitement parallèle des tâches avec Parallel.ForEach pour de meilleures performances
            var tasksToCheck = _taskCollection.Values.Where(t => t != null && !t.Disposed && t.Started).ToList();

            foreach (var taskObj in tasksToCheck)
            {
                if (taskObj == null || taskObj.Task == null)
                    continue;

                try
                {
                    bool doDispose = force;

                    if (!force)
                    {
                        // Vérifications optimisées
                        if (taskObj.Task.IsCanceled || taskObj.Task.IsFaulted ||
#if NET5_0_OR_GREATER
                            taskObj.Task.IsCompletedSuccessfully)
#else
                            taskObj.Task.IsCompleted)
#endif
                        {
                            doDispose = true;
                        }
                        else if (taskObj.TimestampEnd > 0 && taskObj.TimestampEnd < _currentTimestampMillisecond)
                        {
                            doDispose = true;
                        }
                        else if (taskObj.Cancellation?.IsCancellationRequested == true)
                        {
                            doDispose = true;
                        }

                        if (!doDispose)
                            continue;
                    }

                    DisposeTask(taskObj);
                }
                catch (Exception error)
                {
                    ClassLog.WriteLine($"Error on cleaning task ID: {taskObj.Id} | Exception: {error.Message}",
                        ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER,
                        ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                        false, ConsoleColor.Red);
                }
            }
        }

        private static void DisposeTask(ClassTaskObject taskObj)
        {
            taskObj.Disposed = true;
           
            try
            {
                taskObj.Socket?.Kill(SocketShutdown.Both);
            }
            catch { }

            try
            {
                if (taskObj.Cancellation != null && !taskObj.Cancellation.IsCancellationRequested)
                {
                    taskObj.Cancellation.Cancel();
                }
            }
            catch { }
            try
            {
                if (taskObj.Task != null && (taskObj.Task.IsCanceled || taskObj.Task.IsFaulted ||
#if NET5_0_OR_GREATER
                    taskObj.Task.IsCompletedSuccessfully))
#else
                    taskObj.Task.IsCompleted))
#endif
                {
                    taskObj.Task.Dispose();
                }
            }
            catch { }

            _taskIdsToRemove.Enqueue(taskObj.Id);
        }

        /// <summary>
        /// Insert task to manager.
        /// </summary>
        public static async Task InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, ClassCustomSocket socket = null, bool useFactory = false)
        {
            if (!TaskManagerEnabled)
                return;

            long end = timestampEnd > 0 ? timestampEnd - _currentTimestampMillisecond : 0;
            CancellationTokenSource cancellationTask = null;

            try
            {
                if (cancellation != null && end > 0)
                {
                    cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellation.Token,
                        new CancellationTokenSource((int)end).Token);
                }
                else if (end > 0)
                {
                    cancellationTask = new CancellationTokenSource((int)end);
                }
                else
                {
                    cancellationTask = cancellation ?? new CancellationTokenSource();
                }
            }
            catch
            {
                return;
            }

            if (cancellationTask.IsCancellationRequested)
                return;

            if (useFactory)
            {
                try
                {
                    await Task.Factory.StartNew(action, cancellationTask.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ConfigureAwait(false);
                }
                catch { }
            }
            else
            {
                var taskObject = new ClassTaskObject(action, cancellationTask, timestampEnd, socket);

                // Ajout non-bloquant avec ConcurrentDictionary
                if (_taskCollection.TryAdd(taskObject.Id, taskObject))
                {
                    taskObject.Run();
                }
#if DEBUG
                else
                {
                    Debug.WriteLine($"Failed to add task ID: {taskObject.Id}");
                }
#endif
            }
        }

        /// <summary>
        /// Stop task manager.
        /// </summary>
        public static void StopTaskManager()
        {
            TaskManagerEnabled = false;

            try
            {
                if (!_cancelTaskManager.IsCancellationRequested)
                    _cancelTaskManager.Cancel();
            }
            catch { }

            ManageTask(true);

            // Nettoyage final
            _taskCollection.Clear();

            while (_taskIdsToRemove.TryDequeue(out _)) { }
        }
    }

}