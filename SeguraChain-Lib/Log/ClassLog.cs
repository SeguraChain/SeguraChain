using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Log
{
    public enum ClassEnumLogLevelType
    {
        LOG_LEVEL_NONE = 0,
        LOG_LEVEL_GENERAL = 1,
        LOG_LEVEL_PEER_TASK_SYNC = 2,
        LOG_LEVEL_PEER_SERVER = 3,
        LOG_LEVEL_API_SERVER = 4,
        LOG_LEVEL_FIREWALL = 5,
        LOG_LEVEL_WALLET = 6,
        LOG_LEVEL_MINING = 7,
        LOG_LEVEL_PEER_MANAGER = 8,
        LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION = 9,
        LOG_LEVEL_MEMORY_MANAGER = 10,
        LOG_LEVEL_TASK_MANAGER = 11,
        LOG_LEVEL_MEMPOOL_BROADCAST = 12
    }

    public enum ClassEnumLogWriteLevel
    {
        LOG_WRITE_LEVEL_MANDATORY_PRIORITY = 0,
        LOG_WRITE_LEVEL_HIGH_PRIORITY = 1,
        LOG_WRITE_LEVEL_MEDIUM_PRIORITY = 2,
        LOG_WRITE_LEVEL_LOWEST_PRIORITY = 3,
    }

    public static class ClassLog
    {
        private sealed class LogItem
        {
            public ClassEnumLogLevelType Level;
            public string Content;
        }

        #region Log settings and status.

        public static bool LogWriterInitialized;
        public static ClassEnumLogLevelType CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_GENERAL;
        public static ClassEnumLogWriteLevel CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY;

        // Tuning (NET4.8 safe)
        private const int FlushIntervalMs = 3000;
        private const int MaxBatchSize = 2000;

        #endregion

        #region Log files informations.

        private const string LogDirectoryName = "\\Log\\";
        private const string LogGeneralFilename = "general.log";
        private const string LogPeerTaskSyncFilename = "peer-task-sync.log";
        private const string LogPeerServerFilename = "peer-server.log";
        private const string LogApiServerFilename = "api-server.log";
        private const string LogFirewallFilename = "firewall.log";
        private const string LogWalletFilename = "wallet.log";
        private const string LogMiningFilename = "mining.log";
        private const string LogPeerManagerFilename = "peer-manager.log";
        private const string LogPeerTaskTransactionConfirmationFileName = "peer-task-transaction-confirmation.log";
        private const string LogMemoryManagerFileName = "memory-manager.log";
        private const string LogTaskManagerFileName = "task-manager.log";
        private const string LogMemPoolBroadcastFileName = "mempool-broadcast.log";

        private static readonly string LogFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + LogDirectoryName);

        #endregion

        #region Internal storage / writers

        private static readonly ConcurrentQueue<LogItem> _queue = new ConcurrentQueue<LogItem>();
        private static readonly Dictionary<ClassEnumLogLevelType, StreamWriter> _writers = new Dictionary<ClassEnumLogLevelType, StreamWriter>();
        private static CancellationTokenSource _cts;
        private static Task _writerTask;

        #endregion

        #region Log Writer functions.

        /// <summary>
        /// Initialize log writer.
        /// </summary>
        public static bool InitializeWriteLog(string customLogFilePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(customLogFilePath))
                    customLogFilePath = LogFilePath;

                if (!Directory.Exists(customLogFilePath))
                    Directory.CreateDirectory(customLogFilePath);

                // Ensure idempotent init if called twice.
                CloseLogStreams();

                CreateWriter(customLogFilePath, LogGeneralFilename, ClassEnumLogLevelType.LOG_LEVEL_GENERAL);
                CreateWriter(customLogFilePath, LogPeerTaskSyncFilename, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC);
                CreateWriter(customLogFilePath, LogPeerServerFilename, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER);
                CreateWriter(customLogFilePath, LogApiServerFilename, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER);
                CreateWriter(customLogFilePath, LogFirewallFilename, ClassEnumLogLevelType.LOG_LEVEL_FIREWALL);
                CreateWriter(customLogFilePath, LogWalletFilename, ClassEnumLogLevelType.LOG_LEVEL_WALLET);
                CreateWriter(customLogFilePath, LogMiningFilename, ClassEnumLogLevelType.LOG_LEVEL_MINING);
                CreateWriter(customLogFilePath, LogPeerManagerFilename, ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER);
                CreateWriter(customLogFilePath, LogPeerTaskTransactionConfirmationFileName, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION);
                CreateWriter(customLogFilePath, LogMemoryManagerFileName, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER);
                CreateWriter(customLogFilePath, LogTaskManagerFileName, ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER); // ✅ fix: correct file/level
                CreateWriter(customLogFilePath, LogMemPoolBroadcastFileName, ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST);

                LogWriterInitialized = true;
                return true;
            }
            catch (Exception error)
            {
                Console.WriteLine("Can't initialize stream writer of log file. Exception: " + error.Message);
                return false;
            }
        }

        private static void CreateWriter(string basePath, string filename, ClassEnumLogLevelType level)
        {
            var fullPath = ClassUtility.ConvertPath(basePath + filename);

            // SequentialScan helps for append-heavy workloads; buffer size avoids tiny writes.
            var fs = new FileStream(
                fullPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan);

            // Important: AutoFlush false => huge perf gain (we flush periodically + on close)
            _writers[level] = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = false };
        }

        /// <summary>
        /// Enable Write log Task.
        /// </summary>
        public static void EnableWriteLogTask()
        {
            if (!LogWriterInitialized)
                return;

            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            _writerTask = Task.Factory.StartNew(
                () => WriterLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static void WriterLoop(CancellationToken token)
        {
            // One buffer per log file to reduce allocations and IO calls.
            var buffers = new Dictionary<ClassEnumLogLevelType, StringBuilder>(_writers.Count);
            foreach (var kv in _writers)
                buffers[kv.Key] = new StringBuilder(16 * 1024);

            while (!token.IsCancellationRequested)
            {
                int processed = 0;

                while (processed < MaxBatchSize && _queue.TryDequeue(out var item))
                {
                    if (item != null && item.Content != null && buffers.TryGetValue(item.Level, out var sb))
                        sb.AppendLine(item.Content);

                    processed++;
                }

                // Write buffers
                foreach (var kv in buffers)
                {
                    if (kv.Value.Length == 0)
                        continue;

                    // If a writer is missing (shouldn't), skip.
                    if (_writers.TryGetValue(kv.Key, out var writer))
                    {
                        writer.Write(kv.Value.ToString());
                    }
                    kv.Value.Clear();
                }

                // Flush once per loop (controlled)
                foreach (var writer in _writers.Values)
                    writer.Flush();

                // Sleep
                try
                {
                    token.WaitHandle.WaitOne(FlushIntervalMs);
                }
                catch
                {
                    // ignored
                }
            }

            // Final drain + flush on cancellation
            while (_queue.TryDequeue(out var item2))
            {
                if (item2 != null && item2.Content != null && buffers.TryGetValue(item2.Level, out var sb2))
                    sb2.AppendLine(item2.Content);
            }

            foreach (var kv in buffers)
            {
                if (kv.Value.Length == 0) continue;
                if (_writers.TryGetValue(kv.Key, out var writer))
                    writer.Write(kv.Value.ToString());
                kv.Value.Clear();
            }

            foreach (var writer in _writers.Values)
                writer.Flush();
        }

        /// <summary>
        /// Close log writer.
        /// </summary>
        public static void CloseLogStreams()
        {
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                    _cts.Cancel();

                // Best-effort wait (no deadlock)
                try { _writerTask?.Wait(1000); } catch { /* ignored */ }

                foreach (var writer in _writers.Values)
                {
                    try
                    {
                        writer.Flush();
                        writer.Close(); // closes underlying stream too
                    }
                    catch { /* ignored */ }
                }
                _writers.Clear();
            }
            catch
            {
                // ignored
            }
            finally
            {
                LogWriterInitialized = false;
            }
        }

        #endregion

        #region Console functions & Manage log level(s) functions.

        /// <summary>
        /// Show log depending of the log level.
        /// </summary>
        public static void WriteLine(string logLine, ClassEnumLogLevelType logLevelType, ClassEnumLogWriteLevel writeLogLevel,
            bool hideText = false, ConsoleColor color = ConsoleColor.White)
        {
            // Keep the exact behavior: prefix datetime
            string line = DateTime.Now + " - " + logLine;

            if (!hideText)
            {
                if (logLevelType == CurrentLogLevelType || logLevelType == ClassEnumLogLevelType.LOG_LEVEL_GENERAL)
                    SimpleWriteLine(line, color, true);
            }

            if ((int)writeLogLevel <= (int)CurrentWriteLogLevel && LogWriterInitialized)
            {
                // Lock-free enqueue
                _queue.Enqueue(new LogItem
                {
                    Level = logLevelType,
                    Content = line
                });
            }
        }

        /// <summary>
        /// Simply write console line without to push lines into the log system.
        /// </summary>
        public static void SimpleWriteLine(string line, ConsoleColor color = ConsoleColor.White, bool haveDatetime = false)
        {
            Console.ForegroundColor = color;

            if (!haveDatetime)
                Console.WriteLine(DateTime.Now + " - " + line);
            else
                Console.WriteLine(line);

            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Show every log levels available.
        /// </summary>
        public static void ShowLogLevels()
        {
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_GENERAL + " - No log showed.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC + " - Show logs from task of sync.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER + " - Show logs of the peer network server.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_API_SERVER + " - Show logs of the peer API server.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_FIREWALL + " - Show logs of the API Firewall.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_WALLET + " - Show logs of the Wallet. Only used on Desktop/RPC Wallet tool.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_MINING + " - Show logs of Mining.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER + " - Show logs of the Peer Manager. Only used by Node Tool.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION + " - Show logs of the task who confirm transactions.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER + " - Show logs of the cache manager.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER + " - Show logs of the task manager.");
            SimpleWriteLine((int)ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST + " - Show logs of mempool broadcast.");

            SimpleWriteLine("Current log level: " + (int)CurrentLogLevelType, ConsoleColor.Magenta);
        }

        /// <summary>
        /// Change the current log level.
        /// </summary>
        public static bool ChangeLogLevel(bool init, int logLevel)
        {
            ClassEnumLogLevelType previousLogLevelType = CurrentLogLevelType;

            switch (logLevel)
            {
                case (int)ClassEnumLogLevelType.LOG_LEVEL_NONE:
                case (int)ClassEnumLogLevelType.LOG_LEVEL_GENERAL:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_GENERAL;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_API_SERVER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_API_SERVER;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_FIREWALL:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_FIREWALL;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_WALLET:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_WALLET;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_MINING:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_MINING;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER;
                    break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST;
                    break;
                default:
                    SimpleWriteLine("Log level: " + logLevel + " not exist.", ConsoleColor.Yellow);
                    return false;
            }

            if (!init)
            {
                if (previousLogLevelType != CurrentLogLevelType)
                {
                    SimpleWriteLine(CurrentLogLevelType + " enabled.", ConsoleColor.Cyan);
                    return true;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Show every log write levels available.
        /// </summary>
        public static void ShowLogWriteLevels()
        {
            Console.WriteLine((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY + " - this level write every mandatory logs to write.");
            Console.WriteLine((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY + " - this level write every logs in high priority.");
            Console.WriteLine((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY + " - this level write every logs higher or medium priority.");
            Console.WriteLine((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY + " - this level write every logs.");

            SimpleWriteLine("Current log write level: " + (int)CurrentWriteLogLevel, ConsoleColor.Cyan);
        }

        /// <summary>
        /// Change the current log write level.
        /// </summary>
        public static bool ChangeLogWriteLevel(int logWriteLevel)
        {
            bool logLevelChanged = false;
            switch (logWriteLevel)
            {
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY;
                    logLevelChanged = true;
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY;
                    logLevelChanged = true;
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY;
                    logLevelChanged = true;
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY;
                    logLevelChanged = true;
                    break;
                default:
                    SimpleWriteLine("Log write level: " + logWriteLevel + " not exist.", ConsoleColor.DarkYellow);
                    break;
            }

            if (logLevelChanged)
            {
                SimpleWriteLine(CurrentWriteLogLevel + " enabled.", ConsoleColor.Magenta);
                return true;
            }

            return false;
        }

        #endregion
    }
}
