using SeguraChain_IO_Cache_Network_System.Client;
using SeguraChain_IO_Cache_Network_System.Config;
using SeguraChain_IO_Cache_Network_System.Server.Object;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Other.Object.List;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_IO_Cache_Network_System.Server
{
    public class ClassIoCacheServerObject
    {
        private bool _enableIoNetworkCache;
        private ClassConfigIoNetworkCache _configIoNetworkCache;
        private CancellationTokenSource _cancellationIoNetworkCache;
        private TcpListener _tcpListenerIoCacheServer;
        private ConcurrentDictionary<string, List<ClassIoCacheClientObject>> _dictionaryIoCacheClient;
        private ClassCacheIoSystem _cacheIoSystem;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configIoNetworkCache"></param>
        /// <param name="cacheIoSystem"></param>
        public ClassIoCacheServerObject(ClassConfigIoNetworkCache configIoNetworkCache, ClassCacheIoSystem cacheIoSystem)
        {
            _configIoNetworkCache = configIoNetworkCache;
            _dictionaryIoCacheClient = new ConcurrentDictionary<string, List<ClassIoCacheClientObject>>();
            _cacheIoSystem = cacheIoSystem;
        }

        /// <summary>
        /// Start the IO Cache Server.
        /// </summary>
        /// <returns></returns>
        public bool StartIoCacheServer()
        {
            _enableIoNetworkCache = true;
            _cancellationIoNetworkCache = new CancellationTokenSource();

            try
            {
                _tcpListenerIoCacheServer = new TcpListener(IPAddress.Parse(_configIoNetworkCache.BlockchainConfigIoNetworkCacheServer.ip), _configIoNetworkCache.BlockchainConfigIoNetworkCacheServer.port);
                _tcpListenerIoCacheServer.Start();
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error - Can't start the IO Cache Server. Exception: " + error.Message);
#endif
                return false;
            }

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (_enableIoNetworkCache)
                    {

                        try
                        {
                            while (!_tcpListenerIoCacheServer.Pending())
                                await Task.Delay(1, _cancellationIoNetworkCache.Token);

                            TcpClient tcpClient = await _tcpListenerIoCacheServer.AcceptTcpClientAsync();

                            if (tcpClient != null)
                            {
                                string clientIp = ((IPEndPoint)(tcpClient.Client.RemoteEndPoint)).Address.ToString();

                                bool inserted = _dictionaryIoCacheClient.ContainsKey(clientIp);

                                if (!inserted)
                                    inserted = _dictionaryIoCacheClient.TryAdd(clientIp, new List<ClassIoCacheClientObject>());

                                if (inserted)
                                {
                                    int count = _dictionaryIoCacheClient[clientIp].Count;

                                    _dictionaryIoCacheClient[clientIp].Add(new ClassIoCacheClientObject(tcpClient, _cacheIoSystem, _configIoNetworkCache, _cancellationIoNetworkCache));
                                    _dictionaryIoCacheClient[clientIp][count].ListenIoCacheClient();
                                }
                            }
                        }
                        // Ignored.
                        catch (Exception error)
                        {
#if DEBUG
                            Debug.WriteLine("Error, can't handle incoming network connexion from the IO Cache server. Details: " + error.Message);
#endif
                        }
                    }

                }, _cancellationIoNetworkCache.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            return false;
        }

        /// <summary>
        /// Stop the IO Cache Server.
        /// </summary>
        /// <returns></returns>
        public bool StopIoCacheServer()
        {
            _enableIoNetworkCache = false;

            if (!_cancellationIoNetworkCache.IsCancellationRequested)
                _cancellationIoNetworkCache.Cancel();

            try
            {
                _tcpListenerIoCacheServer.Stop();
            }
            catch
            {
                // Ignored, catch the exception once the TCP Listener is completed.
            }


            foreach (string ip in _dictionaryIoCacheClient.Keys)
            {
                try
                {
                    if (_dictionaryIoCacheClient[ip].Count > 0)
                    {
                        for(int i = 0; i < _dictionaryIoCacheClient.Count; i ++)
                            _dictionaryIoCacheClient[ip][i].CloseIoCacheClient();
                    }
                }
                catch
                {
                    // Ignore the exception on each connections closed.
                }
            }

            // Clean up.
            _dictionaryIoCacheClient.Clear();

            return false;
        }


        /// <summary>
        /// Return the amount of IO Cache client.
        /// </summary>
        /// <returns></returns>
        public ClassIoCacheServerStatsObject GetIoCacheServerStats()
        {
            ClassIoCacheServerStatsObject ioCacheServerStatsObject = new ClassIoCacheServerStatsObject();

            using (DisposableList<string> ipList = new DisposableList<string>(false, 0, _dictionaryIoCacheClient.Keys.ToArray()))
            {
                ioCacheServerStatsObject.CountIoCacheClientIp += ipList.Count;

                foreach (string ip in _dictionaryIoCacheClient.Keys.ToArray())
                {
                    ioCacheServerStatsObject.CountIoCacheClient += _dictionaryIoCacheClient.Count;

                    for (int i = 0; i < _dictionaryIoCacheClient[ip].Count; i++)
                    {
                        if (_dictionaryIoCacheClient[ip][i].IoCacheClientStatus)
                            ioCacheServerStatsObject.CountIoCacheClientAlive++;
                    }
                }
            }

            return ioCacheServerStatsObject;
        }
    }
}
