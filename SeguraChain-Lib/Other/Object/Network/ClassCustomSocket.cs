using SeguraChain_Lib.Utility;
using System.Net.Sockets;
using System.Threading;

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

        public ClassCustomSocket(Socket socket)
        {
            _socket = socket;
        }

        public bool IsConnected => ClassUtility.SocketIsConnected(Socket);


        public void Shutdown(SocketShutdown shutdownType)
        {
            bool isLocked = false;

            try
            {
                isLocked = Monitor.TryEnter(_socket);

                if (!Disposed && !Closed && isLocked)
                {
                    _socket?.Shutdown(shutdownType);
                    Close();
                    Dispose();
                }
            }
            finally
            {
                if (isLocked)
                    Monitor.Exit(_socket);
            }
        }

        private void Close()
        {
            if (!Disposed && !Closed)
            {
                Closed = true;
                _socket?.Close();
            }
        }

        private void Dispose()
        {
            if (!Disposed)
            {
                if (!Closed)
                    Close();

                Disposed = true;
                _socket?.Dispose();
            }
        }
    }
}
