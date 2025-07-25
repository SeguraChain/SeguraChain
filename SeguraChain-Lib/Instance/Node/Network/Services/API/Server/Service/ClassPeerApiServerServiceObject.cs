using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Service
{
    public class ClassPeerApiServerServiceObject : IDisposable
    {
        private readonly string _peerWebDirectory = AppDomain.CurrentDomain.BaseDirectory + "website";
        public bool NetworkPeerApiServerStatus;
        private TcpListener _tcpListenerPeerApi;
        private CancellationTokenSource _cancellationTokenSourcePeerApiServer;
        private ClassPeerDatabase _peerDatabase;
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
        public ClassPeerApiServerServiceObject(ClassPeerDatabase peerDatabase, string peerIpOpenNatServer, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject firewallSettingObject)
        {
            _peerDatabase = peerDatabase;
            _peerIpOpenNatServer = peerIpOpenNatServer;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _firewallSettingObject = firewallSettingObject;
        }

        /// <summary>
        /// Start the API Server of the Peer.
        /// </summary>
        /// <returns></returns>
        public bool StartPeerApiServer()
        {
            try
            {
                if (!Directory.Exists(_peerWebDirectory))
                    Directory.CreateDirectory(_peerWebDirectory);
            }
            catch
            {
                return false;
            }

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

                        await _tcpListenerPeerApi.AcceptSocketAsync().ContinueWith(async clientTask =>
                        {
                            ClassCustomSocket clientApiTcp = new ClassCustomSocket(await clientTask, true);

                            await TaskManager.TaskManager.InsertTask(new Action(async () =>
                            {
                                string clientIp = clientApiTcp.GetIp;

                                var client = new ClassPeerApiClientObject(_peerDatabase, clientApiTcp, clientIp, _peerNetworkSettingObject, _firewallSettingObject, _peerIpOpenNatServer, _cancellationTokenSourcePeerApiServer);

                                client.HandleApiClientConnection();

                            }), 0, _cancellationTokenSourcePeerApiServer, clientApiTcp);


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

            }
        }
    }
}
