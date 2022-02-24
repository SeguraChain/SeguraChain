using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;

namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableList<V> : IDisposable
    {
        public DisposableList(bool enableSort = false, int capacity = 0, IList<V> listCopy = null)
        {
            bool fromCopy = false;
            if (listCopy != null)
            {
                if (listCopy.Count > 0)
                {
                    GetList = new List<V>(listCopy);
                    if (enableSort)
                    {
                        var typeOfData = typeof(V);
                        if (typeof(ClassBlockObject) != typeOfData && typeof(byte) != typeOfData && typeof(byte[]) != typeOfData && typeof(string[]) != typeOfData)
                            Sort();
                    }
                    fromCopy = true;
                }
            }
            if(!fromCopy)
                GetList = capacity > 0 ? new List<V>(capacity) : new List<V>();

            if (GetList == null)
                GetList = new List<V>();
        }

        public DisposableList(bool enableSort, int capacity, List<int> listCopy)
        {
            this.enableSort = enableSort;
            this.capacity = capacity;
            this.listCopy = listCopy;
        }

        #region Dispose functions

        public bool Disposed;
        private bool enableSort;
        private int capacity;
        private List<int> listCopy;

        ~DisposableList()
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
            if (Disposed && GetList?.Count == 0)
                return;

            if (disposing)
            {
                if (typeof(V) == typeof(Task))
                    ClearTask();
                else
                    Clear();
                GetList = null;
            }

            Disposed = true;
        }


        #endregion

        public int Count => GetList.Count;

        public void Add(V data) => GetList.Add(data);

        public bool Contains(V data) => GetList.Contains(data);

        public bool Remove(V data)
        {
            try
            {
                return GetList.Remove(data);
            }
            catch
            {
                return false;
            }
        }

        public V ElementAt(int index) => GetList.ElementAt(index);

        public V this[int i]
        {
            get => GetList[i];
            set => GetList[i] = value;
        }

        public void Clear()
        {
            try
            {
                if (GetList != null && GetList?.Count > 0)
                {
                    for (int i = 0; i < GetList.Count; i++)
                        GetList[i] = default(V);
                }
            }
            catch
            {
                // Ignored.
            }
            GetList?.Clear();
            GetList?.TrimExcess();
        }

        public void ClearTask()
        {

            if (GetList != null && GetList?.Count > 0)
            {
                for (int i = 0; i < GetList.Count; i++)
                {
                    try
                    {
                        if ((GetList[i] as Task)?.Status == TaskStatus.RanToCompletion || 
                            (GetList[i] as Task)?.Status == TaskStatus.Faulted || 
                            (GetList[i] as Task)?.Status == TaskStatus.Canceled)
                        {
                            (GetList[i] as Task).Dispose();

                            GetList[i] = default(V);
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }

            GetList?.Clear();
            GetList?.TrimExcess();
        }

        public ICollection<V> GetAll => GetList;

        public List<V> GetList { get; set; }

        public void Sort() => GetList.Sort();

    }
}
