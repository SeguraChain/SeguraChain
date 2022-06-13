using SeguraChain_Lib.Utility;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Other.Object.Network
{
    /// <summary>
    /// Custom socket class.
    /// </summary>
    public class ClassCustomSocket
    {
        private Socket _socket;

        public Socket Socket 
        {
            get
            {
                bool isLocked = false;
                try
                {
                    isLocked = Monitor.TryEnter(_socket);
                    if (isLocked)
                        return _socket;
                }
                finally
                {
                    if (isLocked)
                        Monitor.Exit(_socket);
                }
                return null;
            }
        }

        public bool Closed { private set; get; }
        public bool Disposed { private set; get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="socket"></param>
        public ClassCustomSocket(Socket socket)
        {
            _socket = socket;
        }

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                await _socket.ConnectAsync(ip, port);
            }
            catch
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Socket is connected.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (Disposed || Closed || _socket == null || !_socket.Connected)
                return false;

            try
            {
                return !((_socket.Poll(10, SelectMode.SelectRead) && (_socket.Available == 0)));
            }
            catch
            {
                return false;
            }

        }


        public void Kill(SocketShutdown shutdownType)
        {
            Close(shutdownType);
            Dispose();
        }

        private void Close(SocketShutdown shutdownType)
        {
            if (Closed)
                return;

            Closed = true;

            if (IsConnected())
            {
                _socket?.Shutdown(shutdownType);
                _socket?.Close();
            }
        }

        private void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            _socket?.Dispose();
        }
    }
}
