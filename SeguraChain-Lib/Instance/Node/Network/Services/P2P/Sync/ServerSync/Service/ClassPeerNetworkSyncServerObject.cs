using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Client;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Service.Enum;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Service
{
    public class ClassPeerNetworkSyncServerObject : IDisposable
    {
        public bool NetworkPeerServerStatus;
        private TcpListener _tcpListenerPeer;
        private TcpListener _tcpListenerPeerIpv6;
        private CancellationTokenSource _cancellationTokenSourcePeerServer;
        private ConcurrentDictionary<string, ClassPeerIncomingConnectionObject> _listPeerIncomingConnectionObject;
        public string PeerIpOpenNatServer;
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _firewallSettingObject;

        #region Dispose functions

        private bool _disposed;

        ~ClassPeerNetworkSyncServerObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                StopPeerServer();

            _disposed = true;
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerIpOpenNatServer">The Public IP of the node, permit to ignore it on sync.</param>
        /// <param name="peerNetworkSettingObject">The network setting object.</param>
        /// <param name="firewallSettingObject">The firewall setting object.</param>
        public ClassPeerNetworkSyncServerObject(string peerIpOpenNatServer, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject firewallSettingObject)
        {
            PeerIpOpenNatServer = peerIpOpenNatServer;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _firewallSettingObject = firewallSettingObject;
        }

        #region Peer Server management functions.

        /// <summary>
        /// Start the peer server listening task.
        /// </summary>
        /// <returns>Return if the binding and the execution of listening incoming connection work.</returns>
        public bool StartPeerServer()
        {

            if (_listPeerIncomingConnectionObject == null)
                _listPeerIncomingConnectionObject = new ConcurrentDictionary<string, ClassPeerIncomingConnectionObject>();
            else
                CleanUpAllIncomingConnection();

            try
            {
                _tcpListenerPeer = new TcpListener(IPAddress.Parse(_peerNetworkSettingObject.ListenIp), _peerNetworkSettingObject.ListenPort);
                _tcpListenerPeer.Start();
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on initialize TCP Listener of the Peer. | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            NetworkPeerServerStatus = true;
            _cancellationTokenSourcePeerServer = new CancellationTokenSource();


            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (NetworkPeerServerStatus)
                {
                    try
                    {
                        while (!_tcpListenerPeer.Pending())
                        {
                            if (!_cancellationTokenSourcePeerServer.IsCancellationRequested)
                                break;

                            await Task.Delay(1);
                        }

                        await _tcpListenerPeer.AcceptSocketAsync().ContinueWith(async clientTask =>
                        {

                            var clientPeerTcp = await clientTask;

                            string clientIp = string.Empty;
                            bool exception = false;

                            try
                            {
                                clientIp = ((IPEndPoint)(clientPeerTcp.RemoteEndPoint)).Address.ToString();
                            }
                            catch
                            {
                                exception = true;
                            }

                            if (!exception)
                            {
                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
#if DEBUG
                                    ClassLog.WriteLine("Start handle incoming connection from client IP: " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif
                                    switch (await HandleIncomingConnection(clientIp, clientPeerTcp, PeerIpOpenNatServer))
                                    {
                                        case ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT:
                                        case ClassPeerNetworkServerHandleConnectionEnum.BAD_CLIENT_STATUS:
                                        case ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION:
                                        case ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION:
                                            {
                                                if (_firewallSettingObject.PeerEnableFirewallLink)
                                                    ClassPeerFirewallManager.InsertInvalidPacket(clientIp);
                                                ClassUtility.CloseSocket(clientPeerTcp);
                                            }
                                            break;
                                    }


                                }), 0, null, null);
                            }
                            else
                                ClassUtility.CloseSocket(clientPeerTcp);

                        }, _cancellationTokenSourcePeerServer.Token);
                    }
                    catch
                    {
                        // Ignored, catch the exception once the task is cancelled.
                    }
                }
            }), 0, _cancellationTokenSourcePeerServer, null);

            return true;
        }


        /// <summary>
        /// Stop the peer server listening.
        /// </summary>
        public void StopPeerServer()
        {
            if (NetworkPeerServerStatus)
            {
                NetworkPeerServerStatus = false;
                try
                {
                    if (_cancellationTokenSourcePeerServer != null)
                    {
                        if (!_cancellationTokenSourcePeerServer.IsCancellationRequested)
                            _cancellationTokenSourcePeerServer.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
                try
                {
                    _tcpListenerPeer.Stop();
                }
                catch
                {
                    // Ignored.
                }

                CleanUpAllIncomingConnection();
            }
        }

        /// <summary>
        /// Handle incoming connection.
        /// </summary>
        /// <param name="clientIp">The ip of the incoming connection.</param>
        /// <param name="clientPeerTcp">The tcp client object of the incoming connection.</param>
        /// <param name="peerIpOpenNatServer">The public ip of the server.</param>
        /// <returns>Return the handling status of the incoming connection.</returns>
        private async Task<ClassPeerNetworkServerHandleConnectionEnum> HandleIncomingConnection(string clientIp, Socket clientPeerTcp, string peerIpOpenNatServer)
        {
            try
            {

                // Do not check the client ip if this is the same connection.
                if (_peerNetworkSettingObject.ListenIp != clientIp && _firewallSettingObject.PeerEnableFirewallLink)
                {
                    if (!ClassPeerFirewallManager.CheckClientIpStatus(clientIp))
                        return ClassPeerNetworkServerHandleConnectionEnum.BAD_CLIENT_STATUS;
                }

                CancellationTokenSource cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSourcePeerServer.Token, new CancellationTokenSource(_peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay).Token);

                if (GetTotalActiveConnection(clientIp, cancellationToken) > _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                    return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;

                if (_listPeerIncomingConnectionObject.Count >= int.MaxValue - 1)
                    return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;


                // If it's a new incoming connection, create a new index of incoming connection.
                if (!_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (!_listPeerIncomingConnectionObject.TryAdd(clientIp, new ClassPeerIncomingConnectionObject(_peerNetworkSettingObject)))
                        return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;
                }

                #region Ensure to not have too much incoming connection from the same ip.


                if (!GeneratePeerUniqueId(clientIp, cancellationToken, out long randomId))
                    return ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION;


                if (GetTotalActiveConnection(clientIp, cancellationToken) < _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                {
                    if (!_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryAdd(randomId, 
                        new ClassPeerNetworkClientServerObject(clientPeerTcp, cancellationToken, clientIp, peerIpOpenNatServer, _peerNetworkSettingObject, _firewallSettingObject)))
                        return ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION;
                }
                else
                {
                    if (CleanUpInactiveConnectionFromClientIpTarget(clientIp) > 0)
                    {
                        if (!_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryAdd(randomId, 
                            new ClassPeerNetworkClientServerObject(clientPeerTcp, cancellationToken, clientIp, peerIpOpenNatServer, _peerNetworkSettingObject, _firewallSettingObject)))
                            return ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION;
                    }
                    else
                    {
                        if (GetTotalActiveConnection(clientIp, cancellationToken) > _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                            return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
                    }
                }

                #endregion


                bool failed = false;
                bool semaphoreUsed = false;

#if DEBUG
                ClassLog.WriteLine("Start to handle the peer client IP: " + clientIp + " | " + randomId, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
#endif


                try
                {
                    try
                    {
                        long timestampEnd = ClassUtility.GetCurrentTimestampInMillisecond() + _peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay;

                        while (ClassUtility.GetCurrentTimestampInMillisecond() < timestampEnd)
                        {
                            if (await _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.WaitAsync(10, _cancellationTokenSourcePeerServer.Token))
                            {
                                semaphoreUsed = true;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        failed = true;
                        semaphoreUsed = false;
                    }

                    #region Handle the incoming connection to the P2P server.

                    if (semaphoreUsed)
                    {
                        semaphoreUsed = false;
#if DEBUG
                        ClassLog.WriteLine("Start the handle task of the peer client IP: " + clientIp + " | " + randomId + " in " + stopwatch.ElapsedMilliseconds + " ms.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif
                        _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();

                        _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].HandlePeerClient();

                    }
                    else failed = true;

                    #endregion
                }

                finally
                {
                    if (semaphoreUsed)
                        _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();
                }


                if (failed)
                {
                    _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].Dispose();
                    _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryRemove(randomId, out _);
                    return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
                }
            }
            catch
            {
                return ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION;
            }
            return ClassPeerNetworkServerHandleConnectionEnum.VALID_HANDLE;
        }

        #endregion

        #region Stats connections functions.

        /// <summary>
        /// Generate peer unique id.
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool GeneratePeerUniqueId(string clientIp, CancellationTokenSource cancellation, out long randomId)
        {
            bool result = true;

#if DEBUG
            ClassLog.WriteLine("Start to generate a peer unique id to the client IP: " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

            while (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(randomId))
            {
                if (cancellation.IsCancellationRequested)
                {
                    result = false;
                    break;
                }

                 randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);
            }

#if DEBUG
            stopwatch.Stop();
            ClassLog.WriteLine("The peer unique id "+randomId+" has been generated for the client: " + clientIp + " into "+stopwatch.ElapsedMilliseconds+" ms.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif

            return result;
        }

        /// <summary>
        /// Get the total active connection amount of a client ip.
        /// </summary>
        /// <param name="clientIp">The client IP.</param>
        /// <returns>Return the amount of total connection of a client IP.</returns>
        private int GetTotalActiveConnection(string clientIp, CancellationTokenSource cancellation)
        {
#if DEBUG
            ClassLog.WriteLine("Start to get the total active connection from client IP: " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            int totalActiveConnection = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {
                if (_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Count > 0)
                    {
                        foreach (long key in _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Keys.ToArray())
                        {
                            if (cancellation.IsCancellationRequested)
                                break;

                            try
                            {
                                if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(key))
                                {
                                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].ClientPeerConnectionStatus)
                                        totalActiveConnection++;
                                }
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                    }
                }
            }

#if DEBUG
            stopwatch.Stop();
            ClassLog.WriteLine("Total active connection generated: " + stopwatch.ElapsedMilliseconds + " ms. | " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif

            return totalActiveConnection;
        }

        /// <summary>
        /// Get the total of all active connections of all client ip's.
        /// </summary>
        /// <returns>Return the amount of total active connections.</returns>
        public long GetAllTotalActiveConnection()
        {
#if DEBUG
            ClassLog.WriteLine("Start to get the total active connection.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            long totalActiveConnection = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {
                using (DisposableList<string> listClientIpKeys = new DisposableList<string>(false, 0, _listPeerIncomingConnectionObject.Keys.ToList()))
                {
                    foreach (var clientIp in listClientIpKeys.GetList)
                    {
                        if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Count > 0)
                        {
                            foreach (long key in _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Keys.ToArray())
                            {

                                try
                                {
                                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(key)
                                        && _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].ClientPeerConnectionStatus)
                                        totalActiveConnection++;

                                }
                                catch
                                {
                                    // Ignored.
                                }
                            }
                        }
                    }
                }
            }

#if DEBUG
            stopwatch.Stop();
            ClassLog.WriteLine("Total active connection " + totalActiveConnection + " generated into: " + stopwatch.ElapsedMilliseconds + " ms.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif

            return totalActiveConnection;
        }

        /// <summary>
        /// Clean up inactive connection from client ip target.
        /// </summary>
        /// <param name="clientIp">The client IP.</param>
        public int CleanUpInactiveConnectionFromClientIpTarget(string clientIp)
        {
#if DEBUG
            ClassLog.WriteLine("Start to get the total active connection. Client IP: " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            int totalRemoved = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {
                if (_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Count > 0)
                    {
                        _listPeerIncomingConnectionObject[clientIp].OnCleanUp = true;

                        using (DisposableList<long> listKey = new DisposableList<long>(false, 0, _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Keys.ToList()))
                        {
                            foreach (var key in listKey.GetList)
                            {
                                bool remove = false;

                                if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key] == null)
                                    remove = true;
                                else
                                {
                                    try
                                    {
                                        if (!_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].ClientPeerConnectionStatus ||
                                            _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].ClientPeerLastPacketReceived + _peerNetworkSettingObject.PeerMaxDelayConnection < TaskManager.TaskManager.CurrentTimestampSecond)
                                        {
                                            _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].Dispose();
                                            remove = true;
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }
                                }

                                if (remove)
                                {
                                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryRemove(key, out _))
                                        totalRemoved++;
                                }
                            }
                        }

                        _listPeerIncomingConnectionObject[clientIp].OnCleanUp = false;
                    }
                }
            }

#if DEBUG
            stopwatch.Stop();
            ClassLog.WriteLine("Total inactive connection " + totalRemoved + " cleaned up into: " + stopwatch.ElapsedMilliseconds + " ms.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif

            return totalRemoved;
        }

        /// <summary>
        /// Clean up all incoming closed connection.
        /// </summary>
        /// <param name="totalIp">The number of ip's returned from the amount of closed connections cleaned.</param>
        /// <returns>Return the amount of total connection closed.</returns>
        public long CleanUpAllIncomingClosedConnection(out int totalIp)
        {
            long totalConnectionClosed = 0;
            totalIp = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {

                using (DisposableList<string> peerIncomingConnectionKeyList = new DisposableList<string>(false, 0, _listPeerIncomingConnectionObject.Keys.ToList()))
                {
                    foreach (var peerIncomingConnectionKey in peerIncomingConnectionKeyList.GetList)
                    {

                        if (_listPeerIncomingConnectionObject[peerIncomingConnectionKey] != null && _listPeerIncomingConnectionObject[peerIncomingConnectionKey].ListPeerClientObject?.Count > 0)
                        {
                            long totalClosed = CleanUpInactiveConnectionFromClientIpTarget(peerIncomingConnectionKey);

                            if (totalClosed > 0)
                                totalIp++;

                            totalConnectionClosed += totalClosed;
                        }
                    }
                }
            }

            return totalConnectionClosed;
        }

        /// <summary>
        /// Clean up all incoming connection.
        /// </summary>
        /// <returns>Return the amount of incoming connection closed.</returns>
        public long CleanUpAllIncomingConnection()
        {
            long totalConnectionClosed = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {

                using (DisposableList<string> peerIncomingConnectionKeyList = new DisposableList<string>(false, 0, _listPeerIncomingConnectionObject.Keys.ToList()))
                {
                    foreach (var peerIncomingConnectionKey in peerIncomingConnectionKeyList.GetList)
                    {
                        if (_listPeerIncomingConnectionObject[peerIncomingConnectionKey] != null && _listPeerIncomingConnectionObject[peerIncomingConnectionKey]?.ListPeerClientObject?.Count > 0)
                        {
                            _listPeerIncomingConnectionObject[peerIncomingConnectionKey].OnCleanUp = true;

                            using (DisposableList<long> listKey = new DisposableList<long>(false, 0, _listPeerIncomingConnectionObject[peerIncomingConnectionKey].ListPeerClientObject.Keys.ToList()))
                            {
                                foreach (var key in listKey.GetList)
                                {
                                    try
                                    {

                                        if (_listPeerIncomingConnectionObject[peerIncomingConnectionKey].ListPeerClientObject[key].ClientPeerConnectionStatus)
                                            totalConnectionClosed++;

                                        _listPeerIncomingConnectionObject[peerIncomingConnectionKey].ListPeerClientObject[key].Dispose();
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }
                                }
                            }

                            _listPeerIncomingConnectionObject[peerIncomingConnectionKey].OnCleanUp = false;
                        }
                    }
                }

                _listPeerIncomingConnectionObject.Clear();

            }

            return totalConnectionClosed;
        }

        #endregion
    }
}
