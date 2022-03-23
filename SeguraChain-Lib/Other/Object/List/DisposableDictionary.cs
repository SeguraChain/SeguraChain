using System;
using System.Collections.Generic;
using System.Linq;

namespace SeguraChain_Lib.Other.Object.List
{
    public class DisposableDictionary<V, T> : IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="sourceDictionary"></param>
        public DisposableDictionary(int capacity = 0, Dictionary<V, T> sourceDictionary = null)
        {
            if (sourceDictionary == null)
                GetList = capacity > 0 ? new Dictionary<V, T>(capacity) : new Dictionary<V, T>();
            else
                GetList = new Dictionary<V, T>(sourceDictionary);
        }

        #region Dispose functions

        public bool Disposed;

        ~DisposableDictionary()
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

        public void Add(V key, T data)
        {
            if (GetList != null)
                GetList.Add(key, data);
        }
        public bool ContainsKey(V key) => GetList != null ? GetList.ContainsKey(key) : false;


        public bool Remove(V key)
        {
            try
            {
                return GetList != null ? GetList.Remove(key) : false;
            }
            catch
            {
                return false;
            }
        }


        public T this[V key]
        {
            get
            {
               return GetList != null ? GetList[key] : default;
            }
            set
            {
                if (GetList != null)
                    GetList[key] = value;
            }
        }

        public void Clear()
        {
            try
            {
                if (GetList?.Count > 0)
                {
                    foreach (V key in GetList.Keys)
                    {
                        try
                        {
                            GetList[key] = default(T);
                            if(!GetList.Remove(key))
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
            }
            catch
            {
                // Ignored, collection can changed or has been cleaned.
            }

            GetList?.Clear();

#if NET5_0_OR_GREATER
            GetList?.TrimExcess();
#endif

        }

        public ICollection<KeyValuePair<V,T>> GetAll => GetList;

        public Dictionary<V, T> GetList { get; set; }

    }

}
