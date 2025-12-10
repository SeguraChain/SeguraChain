using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Other.Object.List;
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

    /// <summary>
    /// Hybrid optimized logger for .NET 4.8.
    /// Mode B: keep external behavior but reduce locks/allocations and write in batches.
    /// Uses per-log-type ConcurrentQueue and a long-running writer Task per type.
    /// </summary>
    public static class ClassLog
    {
        internal class ClassLogObject
        {
            public string LogContent;
            public long Timestamp;
        }

        // Settings
        public static bool LogWriterInitialized;
        public static ClassEnumLogLevelType CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_GENERAL;
        public static ClassEnumLogWriteLevel CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY;

        private const int TaskWriteLogInterval = 5000; // ms
        private const int BatchWriteLimit = 512; // max lines pulled at once
        private const int MaxQueuePerType = 100000; // safety cap to avoid OOM

        // File names and paths
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

        // Writers
        private static FileStream _logGeneralStream;
        private static FileStream _logPeerTaskSyncStream;
        private static FileStream _logPeerServerStream;
        private static FileStream _logApiServerStream;
        private static FileStream _logFirewallStream;
        private static FileStream _logWalletStream;
        private static FileStream _logMiningStream;
        private static FileStream _logPeerManagerStream;
        private static FileStream _logPeerTaskTransactionConfirmationStream;
        private static FileStream _logCacheManagerStream;
        private static FileStream _logTaskManagerStream;
        private static FileStream _logMemPoolBroadcastStream;

        private static StreamWriter _logGeneralStreamWriter;
        private static StreamWriter _logPeerTaskSyncStreamWriter;
        private static StreamWriter _logPeerServerStreamWriter;
        private static StreamWriter _logApiServerStreamWriter;
        private static StreamWriter _logFirewallStreamWriter;
        private static StreamWriter _logWalletStreamWriter;
        private static StreamWriter _logMiningStreamWriter;
        private static StreamWriter _logPeerManagerStreamWriter;
        private static StreamWriter _logPeerTaskTransactionConfirmationStreamWriter;
        private static StreamWriter _logCacheManagerStreamWriter;
        private static StreamWriter _logTaskManagerStreamWriter;
        private static StreamWriter _logMemPoolBroadcastStreamWriter;

        // Queues per log type
        private static readonly Dictionary<ClassEnumLogLevelType, ConcurrentQueue<ClassLogObject>> _queues = new Dictionary<ClassEnumLogLevelType, ConcurrentQueue<ClassLogObject>>();

        // Writers tasks and cancellation
        private static CancellationTokenSource _cts;
        private static readonly Dictionary<ClassEnumLogLevelType, Task> _writerTasks = new Dictionary<ClassEnumLogLevelType, Task>();

        // Map log level to writer info
        private class WriterInfo
        {
            public Func<StreamWriter> StreamWriterGetter;
            public StreamWriter StreamWriter;
        }

        private static readonly Dictionary<ClassEnumLogLevelType, WriterInfo> WriterMap = new Dictionary<ClassEnumLogLevelType, WriterInfo>();

        // Initialization
        public static bool InitializeWriteLog(string customLogFilePath = null)
        {
            if (customLogFilePath.IsNullOrEmpty(false, out _))
                customLogFilePath = LogFilePath;

            if (!Directory.Exists(customLogFilePath))
                Directory.CreateDirectory(customLogFilePath);

            try
            {
                _logGeneralStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogGeneralFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logGeneralStreamWriter = new StreamWriter(_logGeneralStream, Encoding.UTF8) { AutoFlush = false };

                _logPeerTaskSyncStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerTaskSyncFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerTaskSyncStreamWriter = new StreamWriter(_logPeerTaskSyncStream, Encoding.UTF8) { AutoFlush = false };

                _logPeerServerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerServerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerServerStreamWriter = new StreamWriter(_logPeerServerStream, Encoding.UTF8) { AutoFlush = false };

                _logApiServerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogApiServerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logApiServerStreamWriter = new StreamWriter(_logApiServerStream, Encoding.UTF8) { AutoFlush = false };

                _logFirewallStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogFirewallFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logFirewallStreamWriter = new StreamWriter(_logFirewallStream, Encoding.UTF8) { AutoFlush = false };

                _logWalletStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogWalletFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logWalletStreamWriter = new StreamWriter(_logWalletStream, Encoding.UTF8) { AutoFlush = false };

                _logMiningStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogMiningFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logMiningStreamWriter = new StreamWriter(_logMiningStream, Encoding.UTF8) { AutoFlush = false };

                _logPeerManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerManagerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerManagerStreamWriter = new StreamWriter(_logPeerManagerStream, Encoding.UTF8) { AutoFlush = false };

                _logPeerTaskTransactionConfirmationStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerTaskTransactionConfirmationFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerTaskTransactionConfirmationStreamWriter = new StreamWriter(_logPeerTaskTransactionConfirmationStream, Encoding.UTF8) { AutoFlush = false };

                _logCacheManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogMemoryManagerFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logCacheManagerStreamWriter = new StreamWriter(_logCacheManagerStream, Encoding.UTF8) { AutoFlush = false };

                _logTaskManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogTaskManagerFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logTaskManagerStreamWriter = new StreamWriter(_logTaskManagerStream, Encoding.UTF8) { AutoFlush = false };

                _logMemPoolBroadcastStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogMemPoolBroadcastFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logMemPoolBroadcastStreamWriter = new StreamWriter(_logMemPoolBroadcastStream, Encoding.UTF8) { AutoFlush = false };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't initialize stream writer of log file. Exception: " + ex.Message);
                return false;
            }

            // Initialize queues and writer map
            foreach (ClassEnumLogLevelType level in Enum.GetValues(typeof(ClassEnumLogLevelType)))
            {
                _queues[level] = new ConcurrentQueue<ClassLogObject>();
            }

            WriterMap.Clear();
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_GENERAL] = new WriterInfo { StreamWriter = _logGeneralStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC] = new WriterInfo { StreamWriter = _logPeerTaskSyncStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER] = new WriterInfo { StreamWriter = _logPeerServerStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_API_SERVER] = new WriterInfo { StreamWriter = _logApiServerStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_FIREWALL] = new WriterInfo { StreamWriter = _logFirewallStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_WALLET] = new WriterInfo { StreamWriter = _logWalletStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_MINING] = new WriterInfo { StreamWriter = _logMiningStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER] = new WriterInfo { StreamWriter = _logPeerManagerStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION] = new WriterInfo { StreamWriter = _logPeerTaskTransactionConfirmationStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER] = new WriterInfo { StreamWriter = _logCacheManagerStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER] = new WriterInfo { StreamWriter = _logTaskManagerStreamWriter };
            WriterMap[ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST] = new WriterInfo { StreamWriter = _logMemPoolBroadcastStreamWriter };

            LogWriterInitialized = true;
            return true;
        }

        // Enable writer tasks; call after InitializeWriteLog
        public static void EnableWriteLogTask()
        {
            if (!LogWriterInitialized)
                throw new InvalidOperationException("Log writer not initialized");

            _cts = new CancellationTokenSource();

            foreach (var kv in _queues)
            {
                var level = kv.Key;
                // create and store task
                var task = Task.Factory.StartNew(() => WriterLoop(level, _cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                _writerTasks[level] = task;
            }
        }

        private static void WriterLoop(ClassEnumLogLevelType level, CancellationToken token)
        {
            var queue = _queues[level];
            StreamWriter writer = WriterMap.ContainsKey(level) ? WriterMap[level].StreamWriter : null;

            var sb = new StringBuilder();
            var localBatch = new List<ClassLogObject>(BatchWriteLimit);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Dequeue a batch or wait a bit
                    ClassLogObject logObj;
                    localBatch.Clear();

                    // Fast path: try dequeue up to BatchWriteLimit
                    for (int i = 0; i < BatchWriteLimit; i++)
                    {
                        if (queue.TryDequeue(out logObj))
                        {
                            localBatch.Add(logObj);
                        }
                        else
                            break;
                    }

                    if (localBatch.Count == 0)
                    {
                        // Nothing to write, sleep a bit
                        try { Task.Delay(TaskWriteLogInterval, token).Wait(token); } catch { }
                        continue;
                    }

                    // Order by timestamp to keep chronological order across producers
                    localBatch.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                    sb.Clear();
                    for (int i = 0; i < localBatch.Count; i++)
                    {
                        sb.AppendLine(localBatch[i].LogContent);
                    }

                    if (writer != null)
                    {
                        writer.Write(sb.ToString());
                        writer.Flush(); // ensure durability per batch
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // If writer failed, fallback to console to avoid losing logs
                    try { SimpleWriteLine("Error while writing logs: " + ex.Message, ConsoleColor.Yellow, true); } catch { }
                }
            }

            // Drain remaining entries on shutdown
            try
            {
                if (!queue.IsEmpty && WriterMap.ContainsKey(level))
                {
                    var writer2 = WriterMap[level].StreamWriter;
                    var remaining = new List<ClassLogObject>();
                    ClassLogObject itm;
                    while (queue.TryDequeue(out itm))
                        remaining.Add(itm);

                    if (remaining.Count > 0)
                    {
                        remaining.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                        var sb2 = new StringBuilder();
                        for (int i = 0; i < remaining.Count; i++)
                            sb2.AppendLine(remaining[i].LogContent);

                        writer2.Write(sb2.ToString());
                        writer2.Flush();
                    }
                }
            }
            catch { }
        }

        public static void CloseLogStreams()
        {
            if (!LogWriterInitialized)
                return;

            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                    _cts.Cancel();

                // Wait for tasks to finish (bounded wait)
                foreach (var t in _writerTasks.Values)
                {
                    try { t.Wait(3000); } catch { }
                }

                // Flush and close writers safely
                SafeCloseWriter(_logGeneralStreamWriter, _logGeneralStream);
                SafeCloseWriter(_logPeerTaskSyncStreamWriter, _logPeerTaskSyncStream);
                SafeCloseWriter(_logPeerServerStreamWriter, _logPeerServerStream);
                SafeCloseWriter(_logApiServerStreamWriter, _logApiServerStream);
                SafeCloseWriter(_logFirewallStreamWriter, _logFirewallStream);
                SafeCloseWriter(_logWalletStreamWriter, _logWalletStream);
                SafeCloseWriter(_logMiningStreamWriter, _logMiningStream);
                SafeCloseWriter(_logPeerManagerStreamWriter, _logPeerManagerStream);
                SafeCloseWriter(_logPeerTaskTransactionConfirmationStreamWriter, _logPeerTaskTransactionConfirmationStream);
                SafeCloseWriter(_logCacheManagerStreamWriter, _logCacheManagerStream);
                SafeCloseWriter(_logTaskManagerStreamWriter, _logTaskManagerStream);
                SafeCloseWriter(_logMemPoolBroadcastStreamWriter, _logMemPoolBroadcastStream);
            }
            catch { }
            finally
            {
                LogWriterInitialized = false;
                _queues.Clear();
                _writerTasks.Clear();
                WriterMap.Clear();
            }
        }

        private static void SafeCloseWriter(StreamWriter writer, FileStream fs)
        {
            try
            {
                if (writer != null)
                {
                    try { writer.Flush(); } catch { }
                    try { writer.Close(); } catch { }
                }
            }
            catch { }

            try
            {
                if (fs != null)
                    fs.Close();
            }
            catch { }
        }

        // Public logging API: unchanged signature/semantics
        public static void WriteLine(string logLine, ClassEnumLogLevelType logLevelType, ClassEnumLogWriteLevel writeLogLevel, bool hideText = false, ConsoleColor color = ConsoleColor.White)
        {
            // Prepend datetime once
            string line = DateTime.Now + " - " + logLine;

            if (!hideText)
            {
                if (logLevelType == CurrentLogLevelType || logLevelType == ClassEnumLogLevelType.LOG_LEVEL_GENERAL)
                    SimpleWriteLine(line, color, true);
            }

            if ((int)writeLogLevel <= (int)CurrentWriteLogLevel)
            {
                if (!LogWriterInitialized)
                    return; // nothing to enqueue

                var queue = _queues.ContainsKey(logLevelType) ? _queues[logLevelType] : null;
                if (queue == null)
                    return;

                // Safety: drop if queue becomes too large (prevent OOM)
                if (queue.Count > MaxQueuePerType)
                {
                    // drop oldest by dequeuing a few
                    ClassLogObject tmp;
                    for (int i = 0; i < 16 && queue.TryDequeue(out tmp); i++) { }
                }

                var obj = new ClassLogObject
                {
                    LogContent = line,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                queue.Enqueue(obj);
            }
        }

        public static void SimpleWriteLine(string line, ConsoleColor color = ConsoleColor.White, bool haveDatetime = false)
        {
            try
            {
                Console.ForegroundColor = color;
                if (!haveDatetime)
                    Console.WriteLine(DateTime.Now + " - " + line);
                else
                    Console.WriteLine(line);
            }
            catch { }
            finally
            {
                try { Console.ForegroundColor = ConsoleColor.White; } catch { }
            }
        }

        public static void ShowLogLevels()
        {
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_GENERAL) + " - No log showed.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC) + " - Show logs from task of sync.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER) + " - Show logs of the peer network server.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_API_SERVER) + " - Show logs of the peer API server.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_FIREWALL) + " - Show logs of the API Firewall.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_WALLET) + " - Show logs of the Wallet. Only used on Desktop/RPC Wallet tool.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_MINING) + " - Show logs of Mining.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER) + " - Show logs of the Peer Manager. Only used by Node Tool.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION) + " - Show logs of the task who confirm transactions.");
            SimpleWriteLine(((int)ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER) + " - Show logs of the cache manager.");

            SimpleWriteLine("Current log level: " + (int)CurrentLogLevelType, ConsoleColor.Magenta);
        }

        public static bool ChangeLogLevel(bool init, int logLevel)
        {
            var previous = CurrentLogLevelType;
            switch (logLevel)
            {
                case (int)ClassEnumLogLevelType.LOG_LEVEL_NONE:
                case (int)ClassEnumLogLevelType.LOG_LEVEL_GENERAL:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_GENERAL; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_API_SERVER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_API_SERVER; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_FIREWALL:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_FIREWALL; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_WALLET:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_WALLET; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_MINING:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_MINING; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION; break;
                case (int)ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER:
                    CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER; break;
                default:
                    SimpleWriteLine("Log level: " + logLevel + " not exist.", ConsoleColor.Yellow);
                    return false;
            }

            if (!init)
            {
                if (previous != CurrentLogLevelType)
                {
                    SimpleWriteLine(CurrentLogLevelType + " enabled.", ConsoleColor.Cyan);
                    return true;
                }
                return false;
            }

            return true;
        }

        public static void ShowLogWriteLevels()
        {
            Console.WriteLine(((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY) + " - this level write every mandatory logs to write.");
            Console.WriteLine(((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY) + " - this level write every logs in high priority.");
            Console.WriteLine(((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY) + " - this level write every logs higher or medium priority.");
            Console.WriteLine(((int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY) + " - this level write every logs.");

            SimpleWriteLine("Current log write level: " + (int)CurrentWriteLogLevel, ConsoleColor.Cyan);
        }

        public static bool ChangeLogWriteLevel(int logWriteLevel)
        {
            bool changed = false;
            switch (logWriteLevel)
            {
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY; changed = true; break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY; changed = true; break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY; changed = true; break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY:
                    CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY; changed = true; break;
                default:
                    SimpleWriteLine("Log write level: " + logWriteLevel + " not exist.", ConsoleColor.DarkYellow); break;
            }

            if (changed)
            {
                SimpleWriteLine(CurrentWriteLogLevel + " enabled.", ConsoleColor.Magenta);
                return true;
            }

            return false;
        }
    }
}
