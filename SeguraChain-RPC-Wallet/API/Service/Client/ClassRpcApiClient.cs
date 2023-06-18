using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client.Enum;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Packet.Enum;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Node.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SeguraChain_RPC_Wallet.API.Service.Client
{
    public class ClassRpcApiClient
    {
        private ClassRpcConfig _apiRpcConfig;
        private bool _apiClientStatus;
        private TcpClient _apiTcpClient;
        private ClassNodeApiClient _nodeApiClient;
        private CancellationTokenSource _apiCancellationToken;
        private CancellationTokenSource _apiCheckerCancellationToken;
        private long _apiClientLastPacketTimestamp;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassRpcApiClient(ClassRpcConfig apiRpcConfig, TcpClient apiTcpClient, ClassNodeApiClient nodeApiClient, CancellationTokenSource apiServerCancellationToken)
        {
            _apiRpcConfig = apiRpcConfig;
            _apiClientStatus = true;
            _apiTcpClient = apiTcpClient;
            _nodeApiClient = nodeApiClient;
            _apiCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(apiServerCancellationToken.Token);
            _apiCheckerCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(apiServerCancellationToken.Token);
            _apiClientLastPacketTimestamp = ClassUtility.GetCurrentTimestampInSecond();
        }

        /// <summary>
        /// Check the API Client connection.
        /// </summary>
        private async System.Threading.Tasks.Task CheckApiClientAsync()
        {
            try
            {
                while (_apiClientStatus)
                {
                    // Status offline.
                    if (!_apiClientStatus)
                        break;

                    // Socket dead.
                    if (!ClassUtility.SocketIsConnected(_apiTcpClient.Client))
                        break;

                    // Timeout
                    if (_apiClientLastPacketTimestamp + 30 < ClassUtility.GetCurrentTimestampInSecond())
                        break;

                    await System.Threading.Tasks.Task.Delay(1000);
                }
            }
            catch
            {
                // Ignored.
            }

            CloseApiClient(true);
        }

        /// <summary>
        /// Handle the incoming api client connection.
        /// </summary>
        public void HandleApiClient()
        {
            try
            {
                new TaskFactory().StartNew(CheckApiClientAsync, _apiCheckerCancellationToken.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            try
            {
                new TaskFactory().StartNew(async () =>
                {
                    try
                    {
                        long packetSizeCount = 0;

                        using (DisposableList<byte[]> listPacket = new DisposableList<byte[]>())
                        {
                            if (_apiClientStatus)
                            {
                                try
                                {
                                    bool continueReading = true;
                                    bool isPostRequest = false;

                                    string packetReceived = string.Empty;

                                    using (NetworkStream networkStream = new NetworkStream(_apiTcpClient.Client))
                                    {
                                        while (continueReading && _apiClientStatus)
                                        {
                                            if (_apiCancellationToken.IsCancellationRequested)
                                                break;

                                            byte[] packetBuffer = new byte[BlockchainSetting.PeerMaxPacketBufferSize];

                                            int packetLength = await networkStream.ReadAsync(packetBuffer, 0, packetBuffer.Length, _apiCancellationToken.Token);

                                            if (packetLength > 0)
                                                listPacket.Add(packetBuffer);
                                            else break;

                                            packetSizeCount += packetLength;

                                            _apiClientLastPacketTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                            if (listPacket.Count > 0)
                                            {

                                                foreach (byte dataByte in listPacket.GetList.SelectMany(x => x).ToArray())
                                                {
                                                    if (_apiCancellationToken.IsCancellationRequested)
                                                        break;

                                                    char character = (char)dataByte;

                                                    if (character != '\0')
                                                        packetReceived += character;
                                                }

                                                // Control the post request content length, break the reading if the content length is reach.
                                                if (packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.HttpPostRequestType) &&
                                                    packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1))
                                                {
                                                    isPostRequest = true;

                                                    int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1, 0, StringComparison.Ordinal);

                                                    string[] packetInfoSplitted = packetReceived.Substring(indexPacket).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                                    if (packetInfoSplitted.Length == 2)
                                                    {
                                                        int packetContentLength = 0;

                                                        string contentLength = packetInfoSplitted[0].Replace(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1 + " ", "");

                                                        // Compare Content-length with content.
                                                        if (int.TryParse(contentLength, out packetContentLength))
                                                        {
                                                            if (packetContentLength == packetInfoSplitted[1].Length)
                                                                continueReading = false;
                                                        }
                                                    }
                                                }
                                                else if (packetReceived.Contains("GET /"))
                                                    continueReading = false;

                                                if (continueReading)
                                                    packetReceived.Clear();
                                            }
                                        }
                                    }

                                    if (listPacket.Count > 0 && _apiClientStatus)
                                    {
                                        #region Take in count the common POST HTTP request syntax of data.

                                        if (isPostRequest)
                                        {
                                            int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataTargetIndexOf, 0, StringComparison.Ordinal);

                                            packetReceived = packetReceived.Substring(indexPacket);

                                            await HandlePostPacket(packetReceived);
                                        }

                                        #endregion
                                    }

                                    // Close the connection after to have receive the packet of the incoming connection.
                                    _apiClientStatus = false;
                                }
                                catch
                                {
                                    _apiClientStatus = false;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Catch the exception pending to reading packet data received from the api client.
                    }

                    CloseApiClient(false);

                }, _apiCancellationToken.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

        }

        /// <summary>
        /// Close the api client connection.
        /// </summary>
        private void CloseApiClient(bool fromChecker)
        {
            _apiClientStatus = false;

            if (!fromChecker)
            {
                try
                {
                    if (_apiCheckerCancellationToken != null)
                    {
                        if (!_apiCheckerCancellationToken.IsCancellationRequested)
                            _apiCheckerCancellationToken.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
            }

            try
            {
                if (_apiCancellationToken != null)
                {
                    if (!_apiCancellationToken.IsCancellationRequested)
                        _apiCancellationToken.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }

            try
            {
                try
                {
                    _apiTcpClient?.Client?.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    _apiTcpClient?.Close();
                    _apiTcpClient?.Dispose();
                }
            }
            catch
            {
                // Ignored.
            }
        }

        #region Handle the packet received, deserialize, check the timestamp, check the content, check the content format, check if the content is encrypted.

        /// <summary>
        /// Handle the packet data and his content.
        /// </summary>
        /// <param name="packetData"></param>
        /// <returns></returns>
        private async Task<string> HandlePacketContent(string packetData)
        {

            if (packetData.IsNullOrEmpty(false, out _))
                return string.Empty;

            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiPostPacket rpcApiPostPacket))
            {
                await SendApiResponse("Failed to deserialize the transaction data received. " + packetData);
                return string.Empty;
            }

            if (rpcApiPostPacket.packet_timestamp + _apiRpcConfig.RpcApiSetting.RpcApiMaxConnectDelay >= ClassUtility.GetCurrentTimestampInSecond())
            {
                await SendApiResponse("The packet content sent has expired.");
                return string.Empty;
            }

            if (rpcApiPostPacket.packet_encrypted != _apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey)
            {
                // Different response result.
                await SendApiResponse("The packet content data received format is invalid. It seems to be " + (_apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey ? "unencrypted" : "encrypted"));
                return string.Empty;
            }

            if (rpcApiPostPacket.packet_content.IsNullOrEmpty(false, out _))
            {
                await SendApiResponse("The packet content data received is empty.");
                return string.Empty;
            }

            byte[] packetContentDecrypted = null;

            if (_apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey)
            {
                if (!ClassUtility.CheckBase64String(rpcApiPostPacket.packet_content))
                {
                    await SendApiResponse("The packet content data received format is invalid. Please use the Base64 format.");
                    return string.Empty;
                }

                if (!ClassAes.DecryptionProcess(Convert.FromBase64String(rpcApiPostPacket.packet_content), _apiRpcConfig.RpcApiSetting.RpcApiSecretKeyArray, _apiRpcConfig.RpcApiSetting.RpcApiSecretIvArray, out packetContentDecrypted))
                {
                    await SendApiResponse("Can't decrypt the packet content.");
                    return string.Empty;
                }

                if (packetContentDecrypted == null || packetContentDecrypted?.Length == 0)
                {
                    await SendApiResponse("The packet content decrypted is empty.");
                    return string.Empty;
                }
            }

            return _apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey ? packetContentDecrypted.GetStringFromByteArrayUtf8() : rpcApiPostPacket.packet_content;
        }

        #endregion

        /// <summary>
        /// Handle the api post packet.
        /// </summary>
        /// <param name="packetData"></param>
        /// <returns></returns>
        private async Task<bool> HandlePostPacket(string packetData)
        {
            try
            {
                packetData = await HandlePacketContent(packetData);

                if (packetData.IsNullOrEmpty(false, out _))
                    return false;

                switch (packetData)
                {
                    case ClassRpcApiPostPacketEnum.RpcApiPostTransaction:
                        {
                            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiPostTransactionObject rpcApiPostTransactionObject))
                            {
                                await SendApiResponse("Can't deserialize the packet transaction content.");
                                return false;
                            }

                            if (rpcApiPostTransactionObject.wallet_address_src.IsNullOrEmpty(false, out _) ||
                                rpcApiPostTransactionObject.wallet_address_target.IsNullOrEmpty(false, out _))
                            {
                                await SendApiResponse("The transaction packet content is invalid, check wallet addresses.");
                                return false;
                            }

                            if (rpcApiPostTransactionObject.amount == 0 || rpcApiPostTransactionObject.fee == 0)
                            {
                                await SendApiResponse("The transaction packet content is invalid, amount or fee argument are invalid.");
                                return false;
                            }

                            if (!await SendApiResponse(await _nodeApiClient.SendTransaction(rpcApiPostTransactionObject, _apiCancellationToken)))
                                return false;
                        }
                        break;
                    case ClassRpcApiPostPacketEnum.RpcApiGetWalletStats:
                        {
                            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiGetWalletStats rpcApiGetWalletStats))
                            {
                                await SendApiResponse("Can't deserialize the packet transaction content.");
                                return false;
                            }

                            if (!await SendApiResponse(_nodeApiClient.SendRpcWalletStats(rpcApiGetWalletStats, _apiCancellationToken)))
                                return false;
                        }
                        break;
                    case ClassRpcApiPostPacketEnum.RpcApiGetWalletInformation:
                        {
                            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiGetWalletInformation rpcApiGetWalletInformation))
                            {
                                await SendApiResponse("Can't deserialize the packet transaction content.");
                                return false;
                            }

                            if (!await SendApiResponse(_nodeApiClient.SendRpcWalletInformation(rpcApiGetWalletInformation)))
                                return false;
                        }
                        break;
                    case ClassRpcApiPostPacketEnum.RpcApiGetWalletTransaction:
                        {
                            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiGetWalletTransaction rpcApiGetWalletTransaction))
                            {
                                await SendApiResponse("Can't deserialize the packet transaction content.");
                                return false;
                            }

                            if (!await SendApiResponse(_nodeApiClient.SendRpcWalletTransactionObject(rpcApiGetWalletTransaction, _apiCancellationToken)))
                                return false;
                        }
                        break;

                    default:
                        {
#if DEBUG
                            Debug.WriteLine("Invalid post packet order: " + packetData);
#endif
                        }
                        break;
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error on handling the packet data: " + packetData + " | Exception: " + error.Message);
#endif
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send an API Response to the client.
        /// </summary>
        /// <param name="packetContent">The packet content to send.</param>
        /// <param name="htmlContentType">Html packet content, default json.</param>
        /// <returns></returns>
        private async Task<bool> SendApiResponse<T>(T packetContent, string htmlContentType = "text/json")
        {
            StringBuilder builder = new StringBuilder();

            bool sendResult = true;

            try
            {
                string packetContentSerialized = JsonConvert.SerializeObject(packetContent);

                if (_apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey)
                {
                    if (ClassAes.EncryptionProcess(packetContentSerialized.GetByteArray(), _apiRpcConfig.RpcApiSetting.RpcApiSecretKeyArray, _apiRpcConfig.RpcApiSetting.RpcApiSecretIvArray, out byte[] packetContentEncrypted))
                        packetContentSerialized = Convert.ToBase64String(packetContentEncrypted);
                }

                string packetResponse = JsonConvert.SerializeObject(new ClassRpcApiResponsePacket
                {
                    packet_content = packetContentSerialized,
                    packet_encrypted = _apiRpcConfig.RpcApiSetting.RpcApiEnableSecretKey,
                    packet_timestamp = ClassUtility.GetCurrentTimestampInSecond()
                });

                builder.AppendLine(@"HTTP/1.1 200 OK");
                builder.AppendLine(@"Content-Type: " + htmlContentType);
                builder.AppendLine(@"Content-Length: " + packetResponse.Length);
                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                builder.AppendLine(@"");
                builder.AppendLine(@"" + packetResponse);

                using (NetworkStream networkStream = new NetworkStream(_apiTcpClient.Client))
                {
                    if (!await networkStream.TrySendSplittedPacket(builder.ToString().GetByteArray(), _apiCheckerCancellationToken, BlockchainSetting.PeerMaxPacketSplitedSendSize))
                        sendResult = false;
                }
            }
            catch
            {
                sendResult = false;
            }

            // Clean up.
            builder.Clear();

            return sendResult;
        }

    }
}
