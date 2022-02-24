using System;
using System.Collections.Generic;
using System.Linq;


namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableHashset<V> : IDisposable
    {
        public DisposableHashset(bool enableSort = false, int capacity = 0, IList<V> listCopy = null)
        {
            bool fromCopy = false;
            if (listCopy != null)
            {
                if (listCopy.Count > 0)
                {
                    GetList = new HashSet<V>(listCopy);
                    if (enableSort)
                    {
                        var typeOfData = typeof(V);
                        if (typeof(byte) != typeOfData && typeof(byte[]) != typeOfData && typeof(string[]) != typeOfData)
                            GetList = Sort(false).ToHashSet();
                    }
                    fromCopy = true;
                }
            }
            if (!fromCopy)
                GetList = capacity > 0 ? new HashSet<V>(capacity) : new HashSet<V>();

            if (GetList == null)
                GetList = new HashSet<V>();
        }

        #region Dispose functions

        public bool Disposed;

        ~DisposableHashset()
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
                Clear();
                GetList = null;
            }

            Disposed = true;
        }


        #endregion

        public int Count => GetList.Count;

        public bool Add(V data)
        {
            return GetList.Add(data);
        }

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


        public void Clear()
        {
            GetList?.Clear();
            GetList?.TrimExcess();
        }

        public ICollection<V> GetAll => GetList;

        public HashSet<V> GetList { get; set; }

        public HashSet<V> Sort(bool descending)
        {

            if (descending)
                return GetList.OrderBy(x => x).ToHashSet();
            else
                return GetList.OrderByDescending(x => x).ToHashSet();

        }
    }

}
