using System;
using System.Collections.Concurrent;
#if DEBUG
using System.Diagnostics;
#endif
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
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Service
{
    public class ClassPeerNetworkSyncServerObject :  IDisposable
    {
        public bool NetworkPeerServerStatus;
        private TcpListener _tcpListenerPeer;
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
                        ClassCustomSocket clientPeerTcp = new ClassCustomSocket(await _tcpListenerPeer.AcceptTcpClientAsync(), true);

                        string clientIp = clientPeerTcp.GetIp;

                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                        {
                            ClassLog.WriteLine("Handle incoming connection from: " + clientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                            var handleResult = await HandleIncomingConnection(clientIp, clientPeerTcp, PeerIpOpenNatServer);

                            switch (handleResult)
                            {


#if NET5_0_OR_GREATER
                                case not ClassPeerNetworkServerHandleConnectionEnum.VALID_HANDLE:

#else
                                    case ClassPeerNetworkServerHandleConnectionEnum.BAD_CLIENT_STATUS:
                                    case ClassPeerNetworkServerHandleConnectionEnum.HANDLE_CLIENT_EXCEPTION:
                                    case ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION:
                                    case ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT:
#endif

                                    {
                                        ClassLog.WriteLine("Cannot handle incoming connection from: " + clientIp + " | Result: " + System.Enum.GetName(typeof(ClassPeerNetworkServerHandleConnectionEnum), handleResult), ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                        if (_firewallSettingObject.PeerEnableFirewallLink)
                                            ClassPeerFirewallManager.InsertInvalidPacket(clientIp);
                                        clientPeerTcp.Kill(SocketShutdown.Both);
                                    }
                                    break;
                            }
                        }), 0, _cancellationTokenSourcePeerServer, null, true);
                    }
                    catch
                    {
                        // Ignored, catch the exception once the task is cancelled.
                    }
                }
            }), 0, _cancellationTokenSourcePeerServer);

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
        private async Task<ClassPeerNetworkServerHandleConnectionEnum> HandleIncomingConnection(string clientIp, ClassCustomSocket clientPeerTcp, string peerIpOpenNatServer)
        {
            try
            {

                // Do not check the client ip if this is the same connection.
                if (_peerNetworkSettingObject.ListenIp != clientIp && _firewallSettingObject.PeerEnableFirewallLink)
                {
                    if (!ClassPeerFirewallManager.CheckClientIpStatus(clientIp))
                        return ClassPeerNetworkServerHandleConnectionEnum.BAD_CLIENT_STATUS;
                }

                long timestampEnd = TaskManager.TaskManager.CurrentTimestampMillisecond + _peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay;


                if (GetTotalActiveConnection(clientIp, _cancellationTokenSourcePeerServer, true, timestampEnd) > _peerNetworkSettingObject.PeerMaxNodeConnectionPerIp)
                    return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;

                if (_listPeerIncomingConnectionObject.Count >= int.MaxValue - 1)
                {
                    CleanUpAllIncomingClosedConnection(out _);
                    return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;
                }

                // If it's a new incoming connection, create a new index of incoming connection.
                if (!_listPeerIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (!_listPeerIncomingConnectionObject.TryAdd(clientIp, new ClassPeerIncomingConnectionObject(_peerNetworkSettingObject)))
                        return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;
                }

                #region Ensure to not have too much incoming connection from the same ip.

                long randomId = await GeneratePeerUniqueIdAsync(clientIp, _cancellationTokenSourcePeerServer, timestampEnd);

                if (randomId == -1)
                    return ClassPeerNetworkServerHandleConnectionEnum.BAD_RANDOM_ID;

                if (!_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.TryAdd(randomId,
                    new ClassPeerNetworkClientServerObject(clientPeerTcp, _cancellationTokenSourcePeerServer, clientIp, peerIpOpenNatServer, _peerNetworkSettingObject, _firewallSettingObject)))
                    return ClassPeerNetworkServerHandleConnectionEnum.INSERT_CLIENT_EXCEPTION;


                #endregion

                bool handlePeerClientStatus = false;
                try
                {
                    // Against flood.
                    handlePeerClientStatus = await _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.TryWaitAsync(_peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSourcePeerServer.Token, new CancellationTokenSource(_peerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay).Token));

                    if (handlePeerClientStatus)
                        _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].HandlePeerClient();
                    else
                    {
                        _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[randomId].Dispose();
                        return ClassPeerNetworkServerHandleConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
                    }

                }
                finally
                {
                    if (handlePeerClientStatus)
                        _listPeerIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();
                }

            }
            catch(Exception error)
            {
                ClassLog.WriteLine("Cannot handle incoming connection from: " + clientIp + " | Exception: "+error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

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
        public async Task<long> GeneratePeerUniqueIdAsync(string clientIp, CancellationTokenSource cancellation, long timestampEnd)
        {

            long randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);
            try
            {
                while (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(randomId))
                {

                    if (cancellation.IsCancellationRequested || TaskManager.TaskManager.CurrentTimestampMillisecond >= timestampEnd)
                        return -1;

                    randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

                    await Task.Delay(1);
                }

            }
            catch
            {
                return -1;
            }

            return randomId;
        }

        /// <summary>
        /// Get the total active connection amount of a client ip.
        /// </summary>
        /// <param name="clientIp">The client IP.</param>
        /// <returns>Return the amount of total connection of a client IP.</returns>
        private int GetTotalActiveConnection(string clientIp, CancellationTokenSource cancellation, bool useTimestamp, long timestampEnd)
        {
            if (_listPeerIncomingConnectionObject.Count == 0 ||
                !_listPeerIncomingConnectionObject.ContainsKey(clientIp) ||
                _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Count == 0)
                return 0;


            int totalActiveConnection = 0;


            foreach (long key in _listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.Keys.ToArray())
            {
                if (cancellation.IsCancellationRequested || (useTimestamp && timestampEnd < TaskManager.TaskManager.CurrentTimestampMillisecond))
                    break;

                if (!_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject.ContainsKey(key))
                    continue;

                try
                {
                    if (_listPeerIncomingConnectionObject[clientIp].ListPeerClientObject[key].ClientPeerConnectionStatus)
                        totalActiveConnection++;
                }
                catch
                {
                    // Ignored.
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
                        //_listPeerIncomingConnectionObject[clientIp].OnCleanUp = true;

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

                        //_listPeerIncomingConnectionObject[clientIp].OnCleanUp = false;
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

                        if (_listPeerIncomingConnectionObject[peerIncomingConnectionKey].ListPeerClientObject.Count == 0)
                            _listPeerIncomingConnectionObject.TryRemove(peerIncomingConnectionKey, out _);
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
                            //_listPeerIncomingConnectionObject[peerIncomingConnectionKey].OnCleanUp = true;

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

                            //_listPeerIncomingConnectionObject[peerIncomingConnectionKey].OnCleanUp = false;
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
