using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableConcurrentDictionary<V, T> : IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sourceDictionary"></param>
        public DisposableConcurrentDictionary(ConcurrentDictionary<V, T> sourceDictionary = null)
        {
            GetList = sourceDictionary == null ? new ConcurrentDictionary<V, T>() : new ConcurrentDictionary<V, T>(sourceDictionary);
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

        public int Count => GetList != null ? GetList.Count : 0;

        public bool TryAdd(V key, T data)
        {
            return GetList != null ? GetList.TryAdd(key, data) : false;
        }

        public bool ContainsKey(V key){

            return GetList != null ? GetList.ContainsKey(key) : false;
        }

        public bool TryRemove(V key)
        {
            return GetList != null ? GetList.TryRemove(key, out _) : false;
        }


        public T this[V key]
        {
            get
            {
                return GetList != null ? GetList[key] : default;
            }
            set
            {
                if (GetList != null && value != null)
                    GetList[key] = value;
            }
        }

        public void Clear()
        {

            if (GetList?.Count > 0)
            {
                try
                {
                    foreach (V key in GetList.Keys)
                    {
                        try
                        {
                            GetList[key] = default;
                            if (!GetList.TryRemove(key, out _))
                            {
                                if (GetList == null || GetList.Count == 0)
                                    break;
                            }
                        }
                        catch
                        {
                            if (GetList == null || GetList.Count == 0)
                                break;
                        }
                    }
                }
                catch
                {
                    // Keys can are removed or the collection has changed.
                }
            }


            GetList?.Clear();
        }

        public ICollection<KeyValuePair<V, T>> GetAll => GetList;

        public ConcurrentDictionary<V, T> GetList { get; set; }

    }

}
