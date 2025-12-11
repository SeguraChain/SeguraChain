using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Other.Object.Network
{
    /// <summary>
    /// Optimized custom socket class for .NET Framework 4.8.
    /// - Sets TCP options (NoDelay, buffer sizes, keepalive) for throughput/latency tuning.
    /// - Uses Begin/End async pattern wrapped in Task to stay compatible with .NET 4.8.
    /// - Avoids Span/Memory/ValueTask APIs not available in .NET 4.8.
    /// </summary>
    public class ClassCustomSocket
    {
        private readonly Socket _socket;
        private NetworkStream _networkStream;

        public bool Closed { get; private set; }

        public ClassCustomSocket(Socket socket, bool isServer)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            // TCP tuning: reduce latency (NoDelay) and increase buffers for throughput.
            try
            {
                _socket.NoDelay = true; // disable Nagle for low-latency writes
                _socket.SendBufferSize = 65536; // 64 KB send buffer
                _socket.ReceiveBufferSize = 65536; // 64 KB receive buffer

                // Enable KeepAlive and set timings (Windows SIO_KEEPALIVE_VALS)
                try
                {
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    // KeepAlive values: on (uint 1), keepalive time (ms), keepalive interval (ms)
                    var keepAliveTime = (uint)60000; // 60s idle before probes
                    var keepAliveInterval = (uint)1000; // 1s between probes
                    var inOptionValues = new byte[12];
                    BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                    BitConverter.GetBytes(keepAliveTime).CopyTo(inOptionValues, 4);
                    BitConverter.GetBytes(keepAliveInterval).CopyTo(inOptionValues, 8);

                    // IOControlCode.KeepAliveValues is available in .NET Framework on Windows
                    _socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                }
                catch
                {
                    // Ignore if platform doesn't support KeepAlive tuning
                }
            }
            catch
            {
                // ignore socket option failures
            }

            if (isServer)
            {
                try
                {
                    // NetworkStream(Socket, ownsSocket) exists in .NET 4.8
                    _networkStream = new NetworkStream(_socket, false);
                }
                catch
                {
                    Close(SocketShutdown.Both);
                }
            }
        }

        /// <summary>
        /// Connect using BeginConnect/EndConnect wrapped into a Task for .NET 4.8 compatibility.
        /// delay is timeout in seconds for the attempt.
        /// </summary>
        public async Task<bool> Connect(string ip, int port, int delay, CancellationTokenSource cancellation)
        {
            if (string.IsNullOrWhiteSpace(ip) || cancellation == null) return false;

            var success = false;

            // Wrap BeginConnect/EndConnect in a Task
            Task connectTask = Task.Factory.StartNew(() =>
            {
                var tcs = new TaskCompletionSource<bool>();
                try
                {
                    _socket.BeginConnect(ip, port, ar =>
                    {
                        try
                        {
                            _socket.EndConnect(ar);
                            tcs.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }

                // Wait synchronously on the tcs task inside this Task to keep the lexical scope simple.
                try
                {
                    tcs.Task.Wait();
                }
                catch
                {
                    // swallow
                }
            }, cancellation.Token);

            // Wait for either connect, cancellation, or timeout
            var timeoutTask = Task.Delay(delay * 1000, cancellation.Token);
            var finished = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

            if (finished == connectTask && !cancellation.IsCancellationRequested)
            {
                // check socket connected
                if (_socket != null && _socket.Connected)
                {
                    success = true;
                }
            }

            if (!success)
                return false;

            // Create network stream after successful connect
            try
            {
                _networkStream = new NetworkStream(_socket, false);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public string GetIp
        {
            get
            {
                try
                {
                    if (_socket != null && _socket.RemoteEndPoint != null)
                        return ((IPEndPoint)_socket.RemoteEndPoint).Address.ToString();

                    return string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Lightweight connected check using Poll and Available.
        /// Poll timeout reduced to 1ms to avoid blocking.
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                if (Closed || _socket == null || !_socket.Connected || _networkStream == null)
                    return false;

                // A small poll (1ms) — if readable and no data available, socket is closed
                return !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TrySendSplittedPacket(byte[] packetData, CancellationTokenSource cancellation, int packetPeerSplitSeperator, bool singleWrite)
        {
            if (packetData == null || _networkStream == null)
                return false;

            try
            {   
                // Ensure write timeout does not block indefinitely
                using (var writeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
                {
                    // If your NetworkStream extension honors cancellation token, pass it along.
                    return await _networkStream.TrySendSplittedPacket(packetData, writeCts, packetPeerSplitSeperator, singleWrite).ConfigureAwait(false);
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine("Sending packet exception to " + GetIp + " | Exception: " + error.Message);
            }
            return false;
        }

        /// <summary>
        /// Read packet data compatible with .NET 4.8 ReadAsync(byte[],int,int,CancellationToken).
        /// delayReading is a timeout in milliseconds.
        /// </summary>
        public async Task<ReadPacketData> TryReadPacketData(int packetLength, int delayReading, bool isHttp, CancellationTokenSource cancellation)
        {
            var readPacketData = new ReadPacketData { Data = new byte[packetLength] };

            try
            {
                using (var delayCts = new CancellationTokenSource(delayReading))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, delayCts.Token))
                {
                    // ReadAsync(byte[],int,int,CancellationToken) is available on .NET 4.8
                    await _networkStream.ReadAsync(readPacketData.Data, 0, packetLength, linkedCts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Reading packet exception from " + GetIp + " | Exception: " + error.Message);
#endif
            }

            using (var listOfData = new DisposableList<byte>())
            {
                for (int i = 0; i < readPacketData.Data.Length; i++)
                {
                    byte data = readPacketData.Data[i];
                    if (data == 0) continue;

                    char c = (char)data;

                    if (!isHttp)
                    {
                        if (ClassUtility.CharIsABase64Character(c) || ClassPeerPacketSetting.PacketPeerSplitSeperator == c)
                            listOfData.Add(data);
                    }
                    else
                    {
                        listOfData.Add(data);
                    }
                }

                readPacketData.Data = listOfData.GetList.ToArray();
            }

            readPacketData.Status = readPacketData.Data.Length > 0;
            return readPacketData;
        }

        public void Kill(SocketShutdown shutdownType)
        {
            Close(shutdownType);
        }

        private void Close(SocketShutdown shutdownType)
        {
            if (Closed) return;

            Closed = true;

            try
            {
                if (_socket != null && _socket.Connected)
                {
                    try { _socket.Shutdown(shutdownType); } catch { }
                    try { _socket.Close(); } catch { }
                }

                try { if (_networkStream != null) _networkStream.Close(); } catch { }
            }
            catch
            {
                // ignore
            }
        }

        public class ReadPacketData : IDisposable
        {
            public bool Status;
            public byte[] Data;

            private bool _disposed;

            public void Dispose()
            {
                Dispose(true);
            }

            private void Dispose(bool dispose)
            {
                if (_disposed || !dispose) return;
                Data = null;
                Status = false;
                _disposed = true;
            }
        }
    }
}