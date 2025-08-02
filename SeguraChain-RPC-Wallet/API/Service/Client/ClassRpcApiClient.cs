using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client.Enum;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Packet.Enum;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response.GET;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response.POST;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Database.Wallet;
using SeguraChain_RPC_Wallet.Node.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassRpcApiGetWalletStats = SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response.GET.ClassRpcApiGetWalletStats;


namespace SeguraChain_RPC_Wallet.API.Service.Client
{
    public class ClassRpcApiClient
    {
        private ClassRpcConfig _apiRpcConfig;
        private bool _apiClientStatus;
        private TcpClient _apiTcpClient;
        private ClassNodeApiClient _nodeApiClient;
        private ClassWalletDatabase _walletDatabase;
        private CancellationTokenSource _apiCancellationToken;
        private CancellationTokenSource _apiCheckerCancellationToken;
        private long _apiClientLastPacketTimestamp;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassRpcApiClient(
            ClassRpcConfig apiRpcConfig, 
            TcpClient apiTcpClient,
            ClassNodeApiClient nodeApiClient,
            ClassWalletDatabase walletDatabase, 
            CancellationTokenSource apiServerCancellationToken)
        {
            _apiRpcConfig = apiRpcConfig;
            _apiClientStatus = true;
            _apiTcpClient = apiTcpClient;
            _nodeApiClient = nodeApiClient;
            _walletDatabase = walletDatabase;
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
        public async System.Threading.Tasks.Task HandleApiClient()
        {
            try
            {
                new System.Threading.Tasks.Task(async() => await CheckApiClientAsync(), _apiCheckerCancellationToken.Token).Start();

            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
            
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
                            bool isGetRequest = false;

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

                                    await networkStream.FlushAsync();

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
                                    }

#if DEBUG
                                    Debug.WriteLine(packetReceived);
#endif

                                    #region From Node ? 

                                    if (packetReceived.Contains("PacketContentObjectSerialized"))
                                    {
                                        _apiClientStatus = false;
                                        break;
                                    }
                                    #endregion

                                    // Control the post request content length, break the reading if the content length is reach.
                                    if (packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.HttpPostRequestType) &&
                                        packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1))
                                    {
                                        isPostRequest = true;

                                        int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1, 0, StringComparison.Ordinal);

                                        string[] packetInfoSplitted = packetReceived.Substring(indexPacket).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                        if (packetInfoSplitted.Length >= 2)
                                        {
                                            int packetContentLength = 0;

                                            string contentLength = packetInfoSplitted[0].Replace(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1 + " ", "");

                                            // Compare Content-length with content.
                                            if (int.TryParse(contentLength, out packetContentLength))
                                            {
                                                string packetInfo = string.Empty;
                                                foreach(var stringPostData in packetInfoSplitted)
                                                {
                                                    if (stringPostData.Contains(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1))
                                                        continue;

                                                    packetInfo += stringPostData;
                                                }
                                                if (packetContentLength >= packetInfo.Length)
                                                {
                                                    continueReading = false;
                                                    packetReceived = packetInfo;
                                                }
                                            }
                                        }
                                    }
                                    else if (packetReceived.Contains("GET /") && !isPostRequest)
                                    {
                                        isGetRequest = true;
                                        continueReading = false;
                                    }
                                    if (continueReading)
                                        packetReceived.Clear();

                                }
                            }

                            #region Take in count the common POST HTTP request syntax of data.

                            if (isPostRequest)
                            {
                                int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataTargetIndexOf, 0, StringComparison.Ordinal);

                                if (indexPacket >= 0)
                                    packetReceived = packetReceived.Substring(indexPacket);

                                await HandlePostPacket(packetReceived);
                            }

                            #endregion
                            
                            if (isGetRequest)
                            {
                                packetReceived = packetReceived.GetStringBetweenTwoStrings("GET /", "HTTP");
                                packetReceived = packetReceived.Replace("%7C", "|"); // Translate special character | 
                                packetReceived = packetReceived.Replace(" ", ""); // Remove empty,space characters
                                await HandleGetPacket(packetReceived);
                            }
                            // Close the connection after to have receive the packet of the incoming connection.
                        }
                        catch
                        {
                        }

                        _apiClientStatus = false;
                    }
                }



            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            CloseApiClient(false);

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
        private async Task<ClassRpcApiPostPacket> HandlePacketContent(string packetData)
        {



            if (packetData.IsNullOrEmpty(false, out _))
                return null;

            if (!ClassUtility.TryDeserialize(packetData, out ClassRpcApiPostPacket rpcApiPostPacket))
            {
                await SendApiResponse("Failed to deserialize the transaction data received. " + packetData);
                return null;
            }

            if (rpcApiPostPacket.packet_timestamp + _apiRpcConfig.RpcApiSetting.RpcApiMaxConnectDelay < ClassUtility.GetCurrentTimestampInSecond())
            {
                await SendApiResponse("The packet content sent has expired.");
                return null;
            }

            

            if (rpcApiPostPacket.packet_content.ToString().IsNullOrEmpty(false, out _))
            {
                await SendApiResponse("The packet content data received is empty.");
                return null;
            }

            return rpcApiPostPacket;
        }

        #endregion

        /// <summary>
        /// Handle GET packet data.
        /// </summary>
        /// <param name="packetRequest"></param>
        /// <returns></returns>
        private async Task<bool> HandleGetPacket(string packetRequest)
        {
#if DEBUG
            Debug.WriteLine("Packet request received: " + packetRequest);
#endif
            try 
            {
                switch(packetRequest)
                {
                    case ClassRpcApiGetPacketEnum.GetRpcWalletStats:
                        {
                            return await SendApiResponse(new ClassRpcApiGetWalletStats()
                            {
                                wallet_count = _walletDatabase.GetWalletCount,
                                wallet_total_amount = _walletDatabase.GetWalletTotalBalance(),
                                wallet_total_pending_amount = _walletDatabase.GetWalletTotalPendingBalance(),
                                wallet_total_fee_amount = _walletDatabase.GetWalletTotalFeeBalance(),
                                wallet_total_transactions = _walletDatabase.GetWalletTotalTransactions()
                            });
                        }
                    case ClassRpcApiGetPacketEnum.GetRpcCreateWallet:
                        {
                            return await SendApiResponse(new ClassRpcApiSendWalletInformation()
                            {
                                wallet_data = _walletDatabase.CreateWallet(string.Empty, true)
                            });
                        }
                }
            }
            catch(Exception error)
            {
#if DEBUG
                Debug.WriteLine("Exception on handle GET packet. Details: " + error.Message);
#endif
            }

            return false;
        }

        /// <summary>
        /// Handle the api post packet.
        /// </summary>
        /// <param name="packetData"></param>
        /// <returns></returns>
        private async Task<bool> HandlePostPacket(string packetData)
        {
            try
            {
                ClassRpcApiPostPacket packetDataObject = await HandlePacketContent(packetData);

                if (packetDataObject == null) 
                    return false;

                switch (packetDataObject.packet_type)
                {
                    case ClassRpcApiPostPacketEnum.RpcApiPostTransaction:
                        {
                            if (!ClassUtility.TryDeserialize(packetDataObject.packet_content.ToString(), out ClassRpcApiPostTransactionObject rpcApiPostTransactionObject))
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
                    case ClassRpcApiPostPacketEnum.RpcApiGetWallet:
                        {
                            if (!ClassUtility.TryDeserialize(packetDataObject.packet_content.ToString(), out ClassRpcApiGetWalletInformation rpcApiGetWallet))
                            {
                                await SendApiResponse("Can't deserialize the packet wallmet content.");
                                return false;
                            }

                            return await SendApiResponse(new ClassRpcApiSendWalletInformation()
                            {
                                wallet_data = _walletDatabase.GetWalletDataFromWalletAddress(rpcApiGetWallet.wallet_address)
                            });
                        }
                    case ClassRpcApiPostPacketEnum.RpcApiGetWalletTransaction:
                        {
                            if (!ClassUtility.TryDeserialize(packetDataObject.packet_content.ToString(), out ClassRpcApiGetWalletTransaction rpcApiGetWalletTransaction))
                            {
                                await SendApiResponse("Can't deserialize the packet transaction content.");
                                return false;
                            }

                            return await SendApiResponse(_nodeApiClient.SendRpcWalletTransactionObject(rpcApiGetWalletTransaction, _apiCancellationToken));
                        }
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

                string packetResponse = JsonConvert.SerializeObject(new ClassRpcApiResponsePacket
                {
                    packet_content = packetContent,
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
