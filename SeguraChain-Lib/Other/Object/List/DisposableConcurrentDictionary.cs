using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableConcurrentDictionary<V, T> : IDisposable
    {
        public DisposableConcurrentDictionary(ConcurrentDictionary<V, T> sourceDictionary = null)
        {
            if (sourceDictionary == null)
                GetList = new ConcurrentDictionary<V, T>();
            else
                GetList = new ConcurrentDictionary<V, T>(sourceDictionary);
        }

        #region Dispose functions

        public bool Disposed;

        ~DisposableConcurrentDictionary()
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

        public bool TryAdd(V key, T data) => GetList.TryAdd(key, data);

        public bool ContainsKey(V key) => GetList.ContainsKey(key);


        public bool TryRemove(V key)
        {
            return GetList.TryRemove(key, out _);
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
                            GetList.TryRemove(key, out _);
                        }
                    }
                }
            }
            catch
            {
                // Ignored.
            }

            GetList?.Clear();
        }

        public ICollection<KeyValuePair<V, T>> GetAll => GetList;

        public ConcurrentDictionary<V, T> GetList { get; set; }

    }

}
