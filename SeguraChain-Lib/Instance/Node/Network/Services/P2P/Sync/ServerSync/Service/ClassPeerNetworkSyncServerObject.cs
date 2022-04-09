using System;
using System.Collections.Concurrent;
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
        private CancellationTokenSource _cancellationTokenSourcePeerServer;
        private ConcurrentDictionary<string, ClassPeerIncomingConnectionObject> _listPeerIncomingConnectionObject;
        public string PeerIpOpenNatServer;
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _firewallSettingObject;
        private SemaphoreSlim _semaphoreHandleIncomingConnection = new SemaphoreSlim(1, Environment.ProcessorCount);

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

            try
            {
                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    while (NetworkPeerServerStatus)
                    {
                        try
                        {
                            await _tcpListenerPeer.AcceptSocketAsync().ContinueWith(async clientTask =>
                            {

                                var clientPeerTcp = await clientTask;

                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    try
                                    {
                                        string clientIp = ((IPEndPoint)(clientPeerTcp.RemoteEndPoint)).Address.ToString();

                                        switch (await HandleIncomingConnection(clientIp, clientPeerTcp, PeerIpOpenNatServer))
                                        {
                                            case ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT:
                                            case ClassPeerNetworkServerHandleConnectionEnum.BAD_CLIENT_STATUS:
                                                if (_firewallSettingObject.PeerEnableFirewallLink)
                                                    ClassPeerFirewallManager.InsertInvalidPacket(clientIp);
                                                break;
                                        }
                                    }
                                    catch
                                    {
                                        // The socket can be dead.
                                    }
                                    ClassUtility.CloseSocket(clientPeerTcp);

                                }), 0, null, clientPeerTcp);

                            }, _cancellationTokenSourcePeerServer.Token);
                        }
                        catch
                        {
                            // Ignored, catch the exception once the task is cancelled.
                        }
                    }
                }), 0, _cancellationTokenSourcePeerServer, null);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
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

                if (GetTotalActiveConnection(clientIp) > _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                    return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;

                if (_listPeerIncomingConnectionObject.Count >= int.MaxValue - 1)
                    return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;


                // If it's a new incoming connection, create a new index of incoming connection.
                if (!_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (!_listPeerIncomingConnectionObject.TryAdd(clientIp, new ClassPeerIncomingConnectionObject()))
                        return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;
                }

                #region Ensure to not have too much incoming connection from the same ip.

                long randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

                while (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(randomId))
                    randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

                CancellationTokenSource cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSourcePeerServer.Token);

                if (GetTotalActiveConnection(clientIp) < _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
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
                        if (GetTotalActiveConnection(clientIp) > _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                            return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
                    }
                }

                #endregion


                bool failed = false;
                bool semaphoreUsed = false;

                try
                {
                    try
                    {
                        long timestampEnd = TaskManager.TaskManager.CurrentTimestampMillisecond + _peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay;

                        while (TaskManager.TaskManager.CurrentTimestampMillisecond < timestampEnd)
                        {
                            if (await _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.WaitAsync(1000, _cancellationTokenSourcePeerServer.Token))
                            {
                                if (!ClassUtility.SocketIsConnected(clientPeerTcp))
                                    break;

                                _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();
                                semaphoreUsed = true;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }

                    #region Handle the incoming connection to the P2P server.

                    if (semaphoreUsed)
                    {
                        semaphoreUsed = false;
                        await _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].HandlePeerClient();
                    }

                    #endregion
                }

                finally
                {
                    if (semaphoreUsed)
                        _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();
                }
                _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].Dispose();
                _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryRemove(randomId, out _);

                if (failed)
                    return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
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
        /// Get the total active connection amount of a client ip.
        /// </summary>
        /// <param name="clientIp">The client IP.</param>
        /// <returns>Return the amount of total connection of a client IP.</returns>
        private int GetTotalActiveConnection(string clientIp)
        {
            int totalActiveConnection = 0;

            if (_listPeerIncomingConnectionObject.Count > 0)
            {
                if (_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Count > 0)
                    {
                        foreach (long key in _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Keys.ToArray())
                        {

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
 
            return totalActiveConnection;
        }

        /// <summary>
        /// Get the total of all active connections of all client ip's.
        /// </summary>
        /// <returns>Return the amount of total active connections.</returns>
        public long GetAllTotalActiveConnection()
        {
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

            return totalActiveConnection;
        }

        /// <summary>
        /// Clean up inactive connection from client ip target.
        /// </summary>
        /// <param name="clientIp">The client IP.</param>
        public int CleanUpInactiveConnectionFromClientIpTarget(string clientIp)
        {
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
