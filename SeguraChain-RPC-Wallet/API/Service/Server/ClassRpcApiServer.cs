using SeguraChain_RPC_Wallet.API.Service.Client;
using SeguraChain_RPC_Wallet.API.Service.Client.Sub;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Node.Client;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_RPC_Wallet.API.Service.Server
{
    public class ClassRpcApiServer
    {
        private bool _enableApiServer;
        private TcpListener _rpcListener;
        private ClassRpcConfig _rpcConfig;
        private ClassNodeApiClient _nodeApiClient;
        private CancellationTokenSource _cancellationApiServer;
        private ConcurrentDictionary<string, ClassRpcClientObject> _concurrentRpcApiClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rpcConfig"></param>
        /// <param name="nodeApiClient"></param>
        public ClassRpcApiServer(ClassRpcConfig rpcConfig, ClassNodeApiClient nodeApiClient)
        {
            _rpcConfig = rpcConfig;
            _concurrentRpcApiClient = new ConcurrentDictionary<string, ClassRpcClientObject>();
            _nodeApiClient = nodeApiClient;
        }

        /// <summary>
        /// Launch the RPC API Server.
        /// </summary>
        public void StartApiServer()
        {
            _enableApiServer = true;
            _cancellationApiServer = new CancellationTokenSource();
            _rpcListener = new TcpListener(IPAddress.Parse(_rpcConfig.RpcApiSetting.RpcApiIp), _rpcConfig.RpcApiSetting.RpcApiPort);
            _rpcListener.Start();

            try
            {
                new TaskFactory().StartNew(async () =>
                {
                    while(_enableApiServer)
                    {
                        try
                        {
                            TcpClient tcpApiClient = await _rpcListener.AcceptTcpClientAsync();

                            if (tcpApiClient != null)
                            {
                                string clientIp = ((IPEndPoint)(tcpApiClient.Client.RemoteEndPoint)).Address.ToString();

                                bool inserted = _concurrentRpcApiClient.ContainsKey(clientIp);

                                if (!inserted)
                                    inserted = _concurrentRpcApiClient.TryAdd(clientIp, new ClassRpcClientObject());

                                if (inserted)
                                {
                                    await new TaskFactory().StartNew(async () => 
                                    {
                                        if (!await HandleApiClientConnection(clientIp, tcpApiClient))
                                            CloseApiClientConnection(tcpApiClient);

                                    }, _cancellationApiServer.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                }
                                else
                                    CloseApiClientConnection(tcpApiClient);
                            }
                        }
                        catch
                        {
                            // Ignored, catch the exception once the incoming socket received has not been handled propertly.
                        }
                    }
                }, _cancellationApiServer.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Stop the RPC API Server.
        /// </summary>
        public void StopApiServer()
        {
            _enableApiServer = false;

            try
            {
                if (_cancellationApiServer != null)
                {
                    if (!_cancellationApiServer.IsCancellationRequested)
                        _cancellationApiServer.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }

            _rpcListener.Stop();
        }

        /// <summary>
        /// Handle the incoming API client connection with his own semaphore.
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="apiTcpClient"></param>
        /// <returns></returns>
        private async Task<bool> HandleApiClientConnection(string clientIp, TcpClient apiTcpClient)
        {
            if (!await _concurrentRpcApiClient[clientIp]._semaphoreHandleRpcClient.WaitAsync(_rpcConfig.RpcApiSetting.RpcApiSemaphoreTimeout))
                return false; // Semaphore is dead.

            int countApiClient = _concurrentRpcApiClient[clientIp]._listRpcClientObject.Count;

            _concurrentRpcApiClient[clientIp]._listRpcClientObject.Add(new ClassRpcApiClient(_rpcConfig, apiTcpClient, _nodeApiClient, _cancellationApiServer));

            _concurrentRpcApiClient[clientIp]._listRpcClientObject[countApiClient].HandleApiClient();

            return true;
        }

        /// <summary>
        /// Close the API Client connection.
        /// </summary>
        private void CloseApiClientConnection(TcpClient apiTcpClient)
        {
            try
            {
                try
                {
                    apiTcpClient?.Client?.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    apiTcpClient?.Close();
                    apiTcpClient?.Dispose();
                }
            }
            catch
            {
                // Ignored.
            }
        }
    }
}
