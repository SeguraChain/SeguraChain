using SeguraChain_IO_Cache_Network_System.Command;
using SeguraChain_IO_Cache_Network_System.Config;
using SeguraChain_IO_Cache_Network_System.Server;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SeguraChain_IO_Cache_Network_System
{
    /// <summary>
    /// Note: The IO Cache server, is a TCP Server, this one have a IO Cache instance.
    /// This one permit to store the blockchain database, outside of a node.
    /// This one can be splitted too, across multiple dedicated servers.
    /// </summary>
    class Program
    {
        private static ClassCacheIoSystem _cacheIoSystem;
        private static ClassIoCacheServerObject _ioCacheServerObject;
        private static ClassConfigIoNetworkCache _configIoNetworkCache;
        private static ClassCommandLine _commandLine;

        static void Main(string[] args)
        {
            if (InitCacheIoSystem().Result)
                _commandLine.EnableCommandLine();
            else
            {
                Console.WriteLine("Failed to initialize the IO Cache Network System.");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Init the cache io system.
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> InitCacheIoSystem()
        {
            try
            {
                if (!File.Exists(AppContext.BaseDirectory + "\\" + ClassConfigIoNetworkDefaultSetting.ConfigIoNetworkConfigFile))
                {
                    _configIoNetworkCache = new ClassConfigIoNetworkCache();

                    using (StreamWriter writer = new StreamWriter(AppContext.BaseDirectory + "\\" + ClassConfigIoNetworkDefaultSetting.ConfigIoNetworkConfigFile))
                    {
                        writer.WriteLine(_configIoNetworkCache);
                        writer.Flush();
                    }
                }
                else
                {
                    using (StreamReader reader = new StreamReader(AppContext.BaseDirectory + "\\" + ClassConfigIoNetworkDefaultSetting.ConfigIoNetworkConfigFile))
                    {
                        if (!ClassUtility.TryDeserialize(reader.ReadToEnd(), out _configIoNetworkCache))
                            return false;
                    }
                }

                _cacheIoSystem = new ClassCacheIoSystem(_configIoNetworkCache.BlockchainDatabaseSetting);

                var initCache = await _cacheIoSystem.InitializeCacheIoSystem();

                if (initCache.Item1)
                {
#if DEBUG
                    Debug.WriteLine("The amount of the IO Cache System count " + initCache.Item2.Count + ".");
#endif
                    Console.WriteLine("The IO Cache system has been initialized.");

                    _ioCacheServerObject = new ClassIoCacheServerObject(_configIoNetworkCache, _cacheIoSystem);

                    if (_ioCacheServerObject.StartIoCacheServer())
                        Console.WriteLine("The IO Cache Server has been started.");
                }
                else
                    Console.WriteLine("Failed to initialize the cache IO Cache System.");
            }
            catch (Exception error)
            {
                Console.WriteLine("On the initialization of the IO Network Cache system " + error.Message + ".");
                return false;
            }

            _commandLine = new ClassCommandLine(_ioCacheServerObject, _cacheIoSystem);

            return true;
        }
    }
}
