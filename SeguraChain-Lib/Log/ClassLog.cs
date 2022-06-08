using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
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
    }

    public enum ClassEnumLogWriteLevel
    {
        LOG_WRITE_LEVEL_MANDATORY_PRIORITY = 0,
        LOG_WRITE_LEVEL_HIGH_PRIORITY = 1,
        LOG_WRITE_LEVEL_MEDIUM_PRIORITY = 2,
        LOG_WRITE_LEVEL_LOWEST_PRIORITY = 3,
    }

    public class ClassLog
    {
        /// <summary>
        /// Log object.
        /// </summary>
        internal class ClassLogObject
        {
            public string LogContent;
            public bool Written;
            public long Timestamp;
        }

        #region Log settings and status.

        public static bool LogWriterInitialized;
        public static ClassEnumLogLevelType CurrentLogLevelType = ClassEnumLogLevelType.LOG_LEVEL_GENERAL;
        public static ClassEnumLogWriteLevel CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY;
        private const int MinLogToWrite = 100;
        private const int TaskWriteLogInterval = 5000;
        private const int LogCapacityList = MinLogToWrite * TaskWriteLogInterval;

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
        private static readonly string LogFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + LogDirectoryName);

        #endregion

        #region Streams.

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

        #endregion

        #region Logs storage.

        private static Dictionary<ClassEnumLogLevelType, List<ClassLogObject>> _logListOnCollect;
        
        #endregion

        #region Log tasks cancellation.

        private static CancellationTokenSource _cancellationTokenSourceLogWriter;

        #endregion

        #region Log Writer functions.

        /// <summary>
        /// Initialize log writer.
        /// </summary>
        /// <returns></returns>
        public static bool InitializeWriteLog(string customLogFilePath = null)
        {
            _logListOnCollect = new Dictionary<ClassEnumLogLevelType, List<ClassLogObject>>();

            if (customLogFilePath.IsNullOrEmpty(false, out _))
                customLogFilePath = LogFilePath;

            if (!Directory.Exists(customLogFilePath))
                Directory.CreateDirectory(customLogFilePath);

            #region Initialize Filestream of logs.

            try
            {
                _logGeneralStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogGeneralFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logGeneralStreamWriter = new StreamWriter(_logGeneralStream) { AutoFlush = true };

                _logPeerTaskSyncStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerTaskSyncFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerTaskSyncStreamWriter = new StreamWriter(_logPeerTaskSyncStream) { AutoFlush = true };

                _logPeerServerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerServerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerServerStreamWriter = new StreamWriter(_logPeerServerStream) { AutoFlush = true };

                _logApiServerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogApiServerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logApiServerStreamWriter = new StreamWriter(_logApiServerStream) { AutoFlush = true };

                _logFirewallStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogFirewallFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logFirewallStreamWriter = new StreamWriter(_logFirewallStream) { AutoFlush = true };

                _logWalletStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogWalletFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logWalletStreamWriter = new StreamWriter(_logWalletStream) { AutoFlush = true };

                _logMiningStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogMiningFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logMiningStreamWriter = new StreamWriter(_logMiningStream) { AutoFlush = true };

                _logPeerManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerManagerFilename), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerManagerStreamWriter = new StreamWriter(_logPeerManagerStream) { AutoFlush = true };

                _logPeerTaskTransactionConfirmationStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogPeerTaskTransactionConfirmationFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPeerTaskTransactionConfirmationStreamWriter = new StreamWriter(_logPeerTaskTransactionConfirmationStream) { AutoFlush = true };

                _logCacheManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogMemoryManagerFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logCacheManagerStreamWriter = new StreamWriter(_logCacheManagerStream) { AutoFlush = true };

                _logTaskManagerStream = new FileStream(ClassUtility.ConvertPath(customLogFilePath + LogTaskManagerFileName), FileMode.Append, FileAccess.Write, FileShare.Read);
                _logTaskManagerStreamWriter = new StreamWriter(_logTaskManagerStream) { AutoFlush = true };
            }
            catch (Exception error)
            {
                Console.WriteLine("Can't initialize stream writer of log file. Exception: " + error.Message);
                return false;
            }

            #endregion

            #region Initialize list(s) of type of logs.


            foreach (var enumType in Enum.GetValues(typeof(ClassEnumLogLevelType)).Cast<ClassEnumLogLevelType>())
                _logListOnCollect.Add(enumType, new List<ClassLogObject>(LogCapacityList));



            #endregion

            LogWriterInitialized = true;
            return true;
        }

        /// <summary>
        /// Enable Write log Task.
        /// </summary>
        public static void EnableWriteLogTask()
        {
            _cancellationTokenSourceLogWriter = new CancellationTokenSource();

            foreach (ClassEnumLogLevelType logLevelType in _logListOnCollect.Keys)
                WriteLogTask(logLevelType);
        }

        /// <summary>
        /// Enable the write log task depending the log level type scheduled.
        /// </summary>
        /// <param name="logLevelType"></param>
        private static void WriteLogTask(ClassEnumLogLevelType logLevelType)
        {

            Task.Factory.StartNew(new Action(async () =>
            {
                int writtenLog = 0;
                while (!_cancellationTokenSourceLogWriter.IsCancellationRequested)
                {
                    bool enterList = false;
                    using (DisposableList<ClassLogObject> logListToWrite = new DisposableList<ClassLogObject>())
                    {

                        try
                        {
                            try
                            {
                                // Clean safely the log list.
                                enterList = Monitor.TryEnter(_logListOnCollect[logLevelType]);
                                if (enterList)
                                {
                                    if (_logListOnCollect[logLevelType].Count >= MinLogToWrite)
                                    {

                                        // Get all log content retrieved to the concurrent bag of logs.
                                        foreach (ClassLogObject logObject in _logListOnCollect[logLevelType].ToArray())
                                        {
                                            if (_cancellationTokenSourceLogWriter.IsCancellationRequested)
                                                break;

                                            logListToWrite.Add(logObject);

                                            if (_logListOnCollect[logLevelType].Remove(logObject))
                                                writtenLog++;
                                        }

                                        if (writtenLog >= LogCapacityList || _logListOnCollect[logLevelType].Count == 0)
                                        {
                                            _logListOnCollect[logLevelType].TrimExcess();
                                            writtenLog = 0;
                                        }

                                        Monitor.PulseAll(_logListOnCollect[logLevelType]);
                                    }
                                }
                            }
                            catch (Exception error)
                            {
                                // The task is cancelled.
                                if (error is OperationCanceledException)
                                {
                                    logListToWrite.Clear();
                                    break;
                                }
#if DEBUG
                                Debug.WriteLine("Error on saving log(s) of type: " + logLevelType + " into the log file target. Exception: " + error.Message);
#endif
                                WriteLine("Error on saving log(s) of type: " + logLevelType + " into the log file target. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                            }
                        }
                        finally
                        {
                            if (enterList)
                                Monitor.Exit(_logListOnCollect[logLevelType]);
                        }

                        try
                        {
                            if (logListToWrite.Count > 0)
                            {
                                // Join all log strings content has array of lines and write them directly by a single call.
                                foreach (ClassLogObject logLine in logListToWrite.GetList.OrderBy(x => x.Timestamp))
                                {
                                    switch (logLevelType)
                                    {
                                        case ClassEnumLogLevelType.LOG_LEVEL_GENERAL:
                                            _logGeneralStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC:
                                            _logPeerTaskSyncStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER:
                                            _logPeerServerStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_API_SERVER:
                                            _logApiServerStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_FIREWALL:
                                            _logFirewallStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_WALLET:
                                            _logWalletStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_MINING:
                                            _logMiningStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER:
                                            _logPeerManagerStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION:
                                            _logPeerTaskTransactionConfirmationStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER:
                                            _logCacheManagerStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                        case ClassEnumLogLevelType.LOG_LEVEL_TASK_MANAGER:
                                            _logPeerTaskTransactionConfirmationStreamWriter.WriteLine(logLine.LogContent);
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            // The task is cancelled.
                            if (error is OperationCanceledException)
                                break;

                            WriteLine("Error on saving log(s) of type: " + logLevelType + " into the log file target. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                        }
                    }

                    try
                    {
                        await Task.Delay(TaskWriteLogInterval, _cancellationTokenSourceLogWriter.Token);
                    }
                    catch
                    {
                        break;
                    }
                }
            }), _cancellationTokenSourceLogWriter.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

        }

        /// <summary>
        /// Close log writer.
        /// </summary>
        public static void CloseLogStreams()
        {
            if (LogWriterInitialized)
            {
                try
                {
                    if (_cancellationTokenSourceLogWriter != null)
                    {
                        if (!_cancellationTokenSourceLogWriter.IsCancellationRequested)
                            _cancellationTokenSourceLogWriter.Cancel();
                    }

                    // General streams.
                    _logGeneralStreamWriter.Close();
                    _logGeneralStream.Close();

                    // Peer Task Sync streams.
                    _logPeerTaskSyncStreamWriter.Close();
                    _logPeerTaskSyncStream.Close();

                    // Peer Server streams.
                    _logPeerServerStreamWriter.Close();
                    _logPeerServerStream.Close();

                    // Api Server streams.
                    _logApiServerStreamWriter.Close();
                    _logApiServerStream.Close();

                    // Firewall streams.
                    _logFirewallStreamWriter.Close();
                    _logFirewallStream.Close();

                    // Wallet streams.
                    _logWalletStreamWriter.Close();
                    _logWalletStream.Close();

                    // Mining streams.
                    _logMiningStreamWriter.Close();
                    _logMiningStream.Close();

                    // Peer Manager streams.
                    _logPeerManagerStreamWriter.Close();
                    _logPeerManagerStream.Close();

                    // Peer Task Transaction confirmation stream.
                    _logPeerTaskTransactionConfirmationStreamWriter.Close();
                    _logPeerTaskTransactionConfirmationStream.Close();

                    // Cache manager streams.
                    _logCacheManagerStreamWriter.Close();
                    _logCacheManagerStream.Close();
                }
                catch
                {
                    // Ignored.
                }

                LogWriterInitialized = false;
            }
            _logListOnCollect?.Clear();
        }

        #endregion

        #region Console functions & Manage log level(s) functions.

        /// <summary>
        /// Show log depending of the log level.
        /// </summary>
        /// <param name="logLine"></param>
        /// <param name="logLevelType"></param>
        /// <param name="writeLogLevel"></param>
        /// <param name="hideText"></param>
        /// <param name="color"></param>
        public static void WriteLine(string logLine, ClassEnumLogLevelType logLevelType, ClassEnumLogWriteLevel writeLogLevel, bool hideText = false, ConsoleColor color = ConsoleColor.White)
        {
            logLine = DateTime.Now + " - " + logLine;

            if (!hideText)
            {
                if (logLevelType == CurrentLogLevelType || logLevelType == ClassEnumLogLevelType.LOG_LEVEL_GENERAL)
                    SimpleWriteLine(logLine, color, true);
            }

            if ((int)writeLogLevel <= (int)CurrentWriteLogLevel)
            {
                if (LogWriterInitialized)
                {
                    TaskManager.TaskManager.InsertTask(() =>
                    {

                        bool locked = false;

                        try
                        {
                            try
                            {
                                locked = Monitor.TryEnter(_logListOnCollect[logLevelType]);
                                if (locked)
                                {
                                    if (_logListOnCollect.ContainsKey(logLevelType))
                                    {

                                        _logListOnCollect[logLevelType].Add(new ClassLogObject()
                                        {
                                            LogContent = logLine,
                                            Written = false,
                                            Timestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                        });

                                        Monitor.PulseAll(_logListOnCollect[logLevelType]);

                                    }
                                }
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                        finally
                        {
                            if (locked)
                                Monitor.Exit(_logListOnCollect[logLevelType]);
                        }

                    }, 0, null, null);
                }
            }
        }

        /// <summary>
        /// Simply write console line without to push lines into the log system. Permit to change the ForegroundColor.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="color"></param>
        /// <param name="haveDatetime"></param>
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

            SimpleWriteLine("Current log level: " + (int)CurrentLogLevelType, ConsoleColor.Magenta);
        }

        /// <summary>
        /// Change the current log level.
        /// </summary>
        /// <param name="logLevel"></param>
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
            else return true;
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
        /// <param name="logWriteLevel"></param>
        public static bool ChangeLogWriteLevel(int logWriteLevel)
        {
            bool logLevelChanged = false;
            switch (logWriteLevel)
            {
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY:
                    {
                        CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY;
                        logLevelChanged = true;
                    }
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY:
                    {
                        CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY;
                        logLevelChanged = true;
                    }
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY:
                    {
                        CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY;
                        logLevelChanged = true;
                    }
                    break;
                case (int)ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY:
                    {
                        CurrentWriteLogLevel = ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY;
                        logLevelChanged = true;
                    }
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
