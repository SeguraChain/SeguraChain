﻿using SeguraChain_Lib.Instance.Node.Network.Services.API.Client;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Service
{
    public class ClassPeerApiServerServiceObject : IDisposable
    {
        public bool NetworkPeerApiServerStatus;
        private TcpListener _tcpListenerPeerApi;
        private CancellationTokenSource _cancellationTokenSourcePeerApiServer;
        private ConcurrentDictionary<string, ClassPeerApiIncomingConnectionObject> _listApiIncomingConnectionObject;
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _firewallSettingObject;
        private string _peerIpOpenNatServer;


        #region Dispose functions

        private bool _disposed;

        ~ClassPeerApiServerServiceObject()
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
                StopPeerApiServer();

            _disposed = true;
        }

        #endregion


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerIpOpenNatServer">The OpenNAT IP Server.</param>
        /// <param name="peerNetworkSettingObject">The network setting object.</param>
        /// <param name="firewallSettingObject">The firewall setting object.</param>
        public ClassPeerApiServerServiceObject(string peerIpOpenNatServer, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject firewallSettingObject)
        {
            _peerIpOpenNatServer = peerIpOpenNatServer;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _firewallSettingObject = firewallSettingObject;
        }

        #region Peer API Server management functions.

        /// <summary>
        /// Start the API Server of the Peer.
        /// </summary>
        /// <returns></returns>
        public bool StartPeerApiServer()
        {

            if (_listApiIncomingConnectionObject == null)
                _listApiIncomingConnectionObject = new ConcurrentDictionary<string, ClassPeerApiIncomingConnectionObject>();
            else
                CleanUpAllIncomingConnection();

            try
            {
                _tcpListenerPeerApi = new TcpListener(IPAddress.Parse(_peerNetworkSettingObject.ListenApiIp), _peerNetworkSettingObject.ListenApiPort);
                _tcpListenerPeerApi.Start();
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on initialize TCP Listener of the Peer. | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            NetworkPeerApiServerStatus = true;
            _cancellationTokenSourcePeerApiServer = new CancellationTokenSource();


            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {

                while (NetworkPeerApiServerStatus)
                {
                    try
                    {
                        while (!_tcpListenerPeerApi.Pending())
                        {
                            if (!_cancellationTokenSourcePeerApiServer.IsCancellationRequested)
                                break;

                            await Task.Delay(1);
                        }

                        await _tcpListenerPeerApi.AcceptSocketAsync().ContinueWith(async clientTask =>
                        {
                            ClassCustomSocket clientApiTcp = null;

                            try
                            {
                                clientApiTcp = new ClassCustomSocket(await clientTask, true);
                            }
                            catch
                            {
                                // Ignored catch the exception once the task is cancelled.
                            }

                            if (clientApiTcp != null)
                            {
                                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    string clientIp = clientApiTcp.GetIp;

                                    ClassLog.WriteLine("Handle incoming connection from: " + clientIp + " to the API.", ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                    switch (await HandleIncomingConnection(clientIp, clientApiTcp))
                                    {

                                        case ClassPeerApiHandleIncomingConnectionEnum.INSERT_CLIENT_IP_EXCEPTION:
                                        case ClassPeerApiHandleIncomingConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT:
                                            {
                                                if (_firewallSettingObject.PeerEnableFirewallLink)
                                                    ClassPeerFirewallManager.InsertInvalidPacket(clientIp);
                                                clientApiTcp?.Kill(SocketShutdown.Both);
                                            }
                                            break;
                                    }


                                }), 0, _cancellationTokenSourcePeerApiServer, clientApiTcp);
                            }

                        }, _cancellationTokenSourcePeerApiServer.Token);
                    }
                    catch
                    {
                        if (!NetworkPeerApiServerStatus)
                            break;
                    }
                }

            }), 0, _cancellationTokenSourcePeerApiServer, null).Wait();
       
            return true;
        }

        /// <summary>
        /// Stop the API Server of the Peer.
        /// </summary>
        public void StopPeerApiServer()
        {
            if (NetworkPeerApiServerStatus)
            {
                NetworkPeerApiServerStatus = false;
                try
                {
                    if (_cancellationTokenSourcePeerApiServer != null)
                    {
                        if (!_cancellationTokenSourcePeerApiServer.IsCancellationRequested)
                            _cancellationTokenSourcePeerApiServer.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
                try
                {
                    _tcpListenerPeerApi.Stop();
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
        /// <param name="clientIp"></param>
        /// <param name="clientApiTcp"></param>
        private async Task<ClassPeerApiHandleIncomingConnectionEnum> HandleIncomingConnection(string clientIp, ClassCustomSocket clientApiTcp)
        {
            try
            {
                if (GetTotalActiveConnectionFromClientIp(clientIp) < _peerNetworkSettingObject.PeerMaxApiConnectionPerIp)
                {
                    // Do not check the client ip if this is the same connection.
                    if (_peerNetworkSettingObject.ListenApiIp != clientIp)
                    {
                        if (_firewallSettingObject.PeerEnableFirewallLink)
                        {
                            if (!ClassPeerFirewallManager.CheckClientIpStatus(clientIp))
                                return ClassPeerApiHandleIncomingConnectionEnum.BAD_CLIENT_STATUS;
                        }
                    }

                    if (_listApiIncomingConnectionObject.Count < int.MaxValue - 1)
                    {
                        // If it's a new incoming connection, create a new index of incoming connection.
                        if (!_listApiIncomingConnectionObject.ContainsKey(clientIp))
                        {
                            if (!_listApiIncomingConnectionObject.TryAdd(clientIp, new ClassPeerApiIncomingConnectionObject()))
                                return ClassPeerApiHandleIncomingConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;

                        }

                        #region Ensure to not have too much incoming connection from the same ip.

                        long randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

                        while (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.ContainsKey(randomId))
                            randomId = ClassUtility.GetRandomBetweenInt(0, int.MaxValue - 1);

                        if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count < _peerNetworkSettingObject.PeerMaxApiConnectionPerIp)
                        {
                            if (!_listApiIncomingConnectionObject[clientIp].ListApiClientObject.TryAdd(randomId, new ClassPeerApiClientObject(clientApiTcp, clientIp, _peerNetworkSettingObject, _firewallSettingObject, _peerIpOpenNatServer, _cancellationTokenSourcePeerApiServer)))
                                return ClassPeerApiHandleIncomingConnectionEnum.HANDLE_CLIENT_EXCEPTION;
                        }
                        else
                        {
                            if (CleanUpInactiveConnectionFromClientIp(clientIp) > 0 || _listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count < _peerNetworkSettingObject.PeerMaxApiConnectionPerIp)
                            {
                                if (!_listApiIncomingConnectionObject[clientIp].ListApiClientObject.TryAdd(randomId, new ClassPeerApiClientObject(clientApiTcp, clientIp, _peerNetworkSettingObject, _firewallSettingObject, _peerIpOpenNatServer, _cancellationTokenSourcePeerApiServer)))
                                    return ClassPeerApiHandleIncomingConnectionEnum.HANDLE_CLIENT_EXCEPTION;
                            }
                            else
                            {
                                ClassLog.WriteLine("Too much listed connection IP registered. Incoming connection from IP: " + clientIp + " closed.", ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                return ClassPeerApiHandleIncomingConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
                            }
                        }

                        #endregion


                        // Handle the incoming connection.
                        bool failed = true;
                        bool semaphoreUsed = false;

                        bool resultHandleRequest = false;

#if DEBUG
                        ClassLog.WriteLine("Start to handle the peer client IP: " + clientIp + " | " + randomId, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
#endif

                        try
                        {
                            try
                            {
                                semaphoreUsed = await _listApiIncomingConnectionObject[clientIp].SemaphoreHandleConnection.WaitAsync(_peerNetworkSettingObject.PeerApiSemaphoreDelay, _cancellationTokenSourcePeerApiServer.Token);

                                #region Handle the incoming connection to the API.

                                if (semaphoreUsed)
                                {
#if DEBUG
                                    ClassLog.WriteLine("Complete to handle the peer client IP: " + clientIp + " | " + randomId + " into " + stopwatch.ElapsedMilliseconds + " ms.", ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif
                                    failed = false;
                                    resultHandleRequest = await _listApiIncomingConnectionObject[clientIp].ListApiClientObject[randomId].HandleApiClientConnection();
                                }


                                #endregion
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Failed to handle the client IP: " + clientIp + " | " + randomId + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }
                        }
                        finally
                        {
                            if (semaphoreUsed)
                                _listApiIncomingConnectionObject[clientIp].SemaphoreHandleConnection.Release();
                        }

#if DEBUG
                        stopwatch.Stop();
                        ClassLog.WriteLine("Complete the task who handle the peer client IP: " + clientIp + " | " + randomId + " into " + stopwatch.ElapsedMilliseconds + " ms.", ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#endif



                        if (failed)
                            return ClassPeerApiHandleIncomingConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;

                        if (!resultHandleRequest)
                            return ClassPeerApiHandleIncomingConnectionEnum.HANDLE_CLIENT_EXCEPTION;

                    }
                    else
                        return ClassPeerApiHandleIncomingConnectionEnum.INSERT_CLIENT_IP_EXCEPTION;
                }
                else
                    return ClassPeerApiHandleIncomingConnectionEnum.TOO_MUCH_ACTIVE_CONNECTION_CLIENT;
            }
            catch
            {
                // Ignored, catch the exception once cancelled.
                return ClassPeerApiHandleIncomingConnectionEnum.HANDLE_CLIENT_EXCEPTION;
            }

            return ClassPeerApiHandleIncomingConnectionEnum.VALID_HANDLE;
        }

        #endregion

        #region Stats connections functions.

        /// <summary>
        /// Get the total active connection amount.
        /// </summary>
        /// <returns></returns>
        private int GetTotalActiveConnectionFromClientIp(string clientIp)
        {
            int totalActiveConnection = 0;

            try
            {
                if (_listApiIncomingConnectionObject.Count > 0)
                {
                    if (_listApiIncomingConnectionObject.ContainsKey(clientIp))
                    {
                        if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count > 0)
                        {
                            foreach (long id in _listApiIncomingConnectionObject[clientIp].ListApiClientObject.Keys.ToArray())
                            {
                                try
                                {
                                    if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.ContainsKey(id))
                                    {
                                        if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].ClientConnectionStatus)
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
            }
            catch
            {
                // Ignored.
            }

            return totalActiveConnection;
        }

        /// <summary>
        /// Get the total of all active connections of all client ip's.
        /// </summary>
        /// <returns></returns>
        public long GetAllTotalActiveConnection()
        {
            long totalActiveConnection = 0;

            if (_listApiIncomingConnectionObject.Count > 0)
            {
                using (DisposableList<string> listIp = new DisposableList<string>(false, 0, _listApiIncomingConnectionObject.Keys.ToList()))
                {
                    foreach (var clientIp in listIp.GetAll)
                    {
                        try
                        {
                            if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count > 0)
                                totalActiveConnection += GetTotalActiveConnectionFromClientIp(clientIp);
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                }
            }

            return totalActiveConnection;
        }

        /// <summary>
        /// Clean up inactive connection from client ip target.
        /// </summary>
        /// <param name="clientIp"></param>
        private long CleanUpInactiveConnectionFromClientIp(string clientIp)
        {
            int totalClosed = 0;

            if (_listApiIncomingConnectionObject.Count > 0)
            {
                if (_listApiIncomingConnectionObject.ContainsKey(clientIp))
                {
                    if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count > 0)
                    {
                        long currentTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;


                        using (DisposableList<long> listIds = new DisposableList<long>(false, 0, _listApiIncomingConnectionObject[clientIp].ListApiClientObject.Keys.ToList()))
                        {
                            foreach (var id in listIds.GetList)
                            {
                                try
                                {
                                    if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.ContainsKey(id))
                                    {
                                        bool remove = false;

                                        if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id] == null)
                                            remove = true;
                                        else
                                        {
                                            if (!_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].ClientConnectionStatus ||
                                                _listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].PacketResponseSent ||
                                                 (_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].ClientConnectionStatus &&
                                                        !_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].OnHandlePacket &&
                                                         _listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].ClientConnectTimestamp + _peerNetworkSettingObject.PeerApiMaxConnectionDelay < currentTimestamp))
                                            {
                                                remove = true;
                                            }

                                            if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject[id]._cancellationTokenApiClient.IsCancellationRequested)
                                                remove = true;
                                        }

                                        if (remove)
                                        {
                                            _listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].Dispose();
                                            totalClosed++;
                                        }
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }

                    }
                }
            }

            return totalClosed;
        }

        /// <summary>
        /// Clean up all incoming closed connection.
        /// </summary>
        /// <returns></returns>
        public long CleanUpAllIncomingClosedConnection(out int totalIp)
        {
            long totalConnectionClosed = 0;
            totalIp = 0;
            if (_listApiIncomingConnectionObject.Count > 0)
            {

                using (DisposableList<string> clientIpList = new DisposableList<string>(false, 0, _listApiIncomingConnectionObject.Keys.ToList()))
                {
                    foreach (string clientIp in clientIpList.GetList)
                    {
                        if (_listApiIncomingConnectionObject.ContainsKey(clientIp))
                        {
                            if (_listApiIncomingConnectionObject[clientIp] != null)
                            {
                                if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count > 0)
                                {
                                    long totalClosed = CleanUpInactiveConnectionFromClientIp(clientIp);

                                    if (totalClosed > 0)
                                        totalIp++;

                                    totalConnectionClosed += totalClosed;
                                }
                            }
                        }
                    }
                }
            }
            return totalConnectionClosed;
        }

        /// <summary>
        /// Clean up all incoming connection.
        /// </summary>
        public long CleanUpAllIncomingConnection()
        {
            long totalConnectionClosed = 0;

            if (_listApiIncomingConnectionObject.Count > 0)
            {
                using (DisposableList<string> peerIncomingConnectionKeyList = new DisposableList<string>(false, 0, _listApiIncomingConnectionObject.Keys.ToList()))
                {
                    long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;

                    foreach (string clientIp in peerIncomingConnectionKeyList.GetList)
                    {
                        if (_listApiIncomingConnectionObject.ContainsKey(clientIp))
                        {
                            if (_listApiIncomingConnectionObject[clientIp] != null)
                            {
                                if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count > 0)
                                {
                                    using (DisposableList<long> idsList = new DisposableList<long>(false, 0, _listApiIncomingConnectionObject[clientIp].ListApiClientObject.Keys.ToList()))
                                    {
                                        foreach (var id in idsList.GetList)
                                        {
                                            try
                                            {

                                                _listApiIncomingConnectionObject[clientIp].ListApiClientObject[id].Dispose();
                                                if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.TryRemove(id, out _))
                                                {
                                                    if (_listApiIncomingConnectionObject[clientIp].ListApiClientObject.Count == 0)
                                                        _listApiIncomingConnectionObject.TryRemove(clientIp, out _);
                                                }

                                                totalConnectionClosed++;
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
                    }
                }
            }

            return totalConnectionClosed;
        }

        #endregion

    }
}
