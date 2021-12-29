using SeguraChain_IO_Cache_Network_System.Command.Enum;
using SeguraChain_IO_Cache_Network_System.Server;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Utility;
using System;
using System.Threading;

namespace SeguraChain_IO_Cache_Network_System.Command
{
    public class ClassCommandLine
    {
        private ClassIoCacheServerObject _ioCacheServerObject;
        private ClassCacheIoSystem _ioCacheSystemObject;
        private bool _serverStatus;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ioCacheServerObject"></param>
        /// <param name="ioCacheSystemObject"></param>
        public ClassCommandLine(ClassIoCacheServerObject ioCacheServerObject, ClassCacheIoSystem ioCacheSystemObject)
        {
            _ioCacheServerObject = ioCacheServerObject;
            _ioCacheSystemObject = ioCacheSystemObject;
        }

        /// <summary>
        /// Enable command line system.
        /// </summary>
        public void EnableCommandLine()
        {
            new Thread(async () =>
            {
                while(_serverStatus)
                {
                    string commandLine = Console.ReadLine();

                    switch(commandLine)
                    {
                        case ClassCommandLineEnum.helpCommand:
                            {

                            }
                            break;
                        case ClassCommandLineEnum.statsCommand:
                            {
                                Console.WriteLine("Generating IO Cache Server stats..");

                                long memoryUsage = _ioCacheSystemObject.GetIoCacheSystemMemoryConsumption(new CancellationTokenSource(), out int totalBlockKeepAlive);
                                _ioCacheServerObject.StartIoCacheServer();

                                var ioCacheServerStats = _ioCacheServerObject.GetIoCacheServerStats();

                                Console.WriteLine("[IO Cache Server Stats]");
                                Console.WriteLine("Memory Usage: " + ClassUtility.ConvertBytesToMegabytes(memoryUsage) + " MB");
                                Console.WriteLine("Total Block Keep Alive: " + totalBlockKeepAlive);
                                Console.WriteLine("IO Cache Client(s): " + ioCacheServerStats.CountIoCacheClient);
                                Console.WriteLine("Count Client(s) IP: " + ioCacheServerStats.CountIoCacheClientIp);
                                Console.WriteLine("Client(s) Alive: " + ioCacheServerStats.CountIoCacheClientAlive);
                            }
                            break;
                        case ClassCommandLineEnum.exitCommand:
                            {

                            }
                            break;
                    }
                }
            });
        }
    }
}
