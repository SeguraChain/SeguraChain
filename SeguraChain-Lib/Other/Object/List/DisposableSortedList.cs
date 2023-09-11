using System;
using System.Collections.Generic;
using System.Linq;


namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableSortedList<V, T> : IDisposable
    {
        public DisposableSortedList(int capacity = 0, SortedList<V, T> sourceDictionary = null)
        {
            if (sourceDictionary == null)
                GetList = capacity > 0 ? new SortedList<V, T>(capacity) : new SortedList<V, T>();
            else
                GetList = new SortedList<V, T>(sourceDictionary);
        }

        #region Dispose functions

        public bool Disposed;

        ~DisposableSortedList()
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

        public void Add(V key, T data) => GetList.Add(key, data);

        public bool ContainsKey(V key) => GetList.ContainsKey(key);


        public bool Remove(V key)
        {
            try
            {
                return GetList.Remove(key);
            }
            catch
            {
                return false;
            }
        }


        public T this[V key]
        {
            get => GetList[key];
            set => GetList[key] = value;
        }

        public void Clear()
        {
            try
            {
                if (GetList != null)
                {
                    if (GetList.Count > 0)
                    {
                        foreach (V key in GetList.Keys.ToArray())
                        {
                            GetList[key] = default(T);
                            GetList.Remove(key);
                        }
                    }
                }

                GetList?.Clear();

                GetList?.TrimExcess();
            }
            catch
            {
                // Ignored.
            }


        }

        public ICollection<KeyValuePair<V, T>> GetAll => GetList;

        public SortedList<V, T> GetList { get; set; }

    }

}
