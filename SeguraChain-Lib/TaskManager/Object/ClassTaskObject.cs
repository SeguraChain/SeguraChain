using System.Net.Sockets;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager.Object
{
    public class ClassTaskObject
    {
        public bool Disposed;
        public Task Task;
        public long TimestampEnd;
        public Socket Socket;
    }
}
