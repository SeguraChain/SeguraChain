﻿using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Utility;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Object
{
    public class ClassCacheIoStructureObject
    {

        /// <summary>
        /// Get/Set the block data. Synchronization forced, the monitor help to lock access from multithreading changes and notify changes done.
        /// </summary>
        public ClassBlockObject BlockObject
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (!IsNull)
                {

                    LastGetTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                    return _blockObject;
                }

                return null;
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (value != null)
                {
                    bool lockedBlock = false;

                    try
                    {
                        lockedBlock = Monitor.TryEnter(value);

                        if (lockedBlock)
                        {
                            if (value.BlockIsUpdated)
                                IsUpdated = true;

                            if (!IsNull)
                            {

                                lock(_blockObject)
                                {
                                    if (value.BlockFromMemory || value.BlockCloned || !value.BlockFromCache)
                                    {
                                        bool isUpdated = _blockObject.BlockLastChangeTimestamp <= value.BlockLastChangeTimestamp;
                                        _blockObject = value.DirectCloneBlockObject();
                                        _blockObject.BlockIsUpdated = isUpdated;
                                    }
                                    else
                                    {
                                        if (_blockObject.BlockLastChangeTimestamp <= value.BlockLastChangeTimestamp)
                                        {
                                            _blockObject = value;
                                            _blockObject.BlockIsUpdated = true;
                                        }
                                    }
                                    _blockObject.BlockFromMemory = false;
                                    _blockObject.BlockFromCache = true;
                                    _blockObject.BlockCloned = false;
                                    _blockObject.Disposed = false;
                                    LastUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;
                                }
                            }
                            else
                            {
                                _blockObject = value.BlockFromMemory || value.BlockCloned || !value.BlockFromCache ? value.DirectCloneBlockObject() : value;
                                _blockObject.BlockFromMemory = false;
                                _blockObject.BlockFromCache = true;
                                _blockObject.BlockCloned = false;
                                _blockObject.BlockIsUpdated = false;
                                _blockObject.Disposed = false;
                                LastUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;
                            }

                            if (_ioDataSizeOnMemory == 0)
                                _ioDataSizeOnMemory = ClassBlockUtility.GetIoBlockSizeOnMemory(_blockObject);
                        }
                    }
                    finally
                    {
                        if (lockedBlock)
                            Monitor.Exit(value);

                    }

                }
                else
                {
                    if (!IsNull)
                    {
                        _blockObject?.Dispose();
                        _blockObject = null;
                        IsUpdated = false;
                        LastUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;
                    }
                }
            }
        }


        /// <summary>
        /// Store the block data.
        /// </summary>
        private ClassBlockObject _blockObject;

        /// <summary>
        /// The last position of the block data on the io cache file.
        /// </summary>
        public long IoDataPosition { get; set; }

        /// <summary>
        /// The last size of the block data on the io cache file.
        /// </summary>
        private long _ioDataSizeOnMemory;

        /// <summary>
        /// The last amount of memory of the block data.
        /// </summary>
        public long IoDataSizeOnMemory
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_ioDataSizeOnMemory == 0)
                {
                    if (!IsNull)
                    {
                        bool locked = false;

                        try
                        {
                            Monitor.TryEnter(_blockObject, ref locked);
                            if (locked)
                                _ioDataSizeOnMemory = ClassBlockUtility.GetIoBlockSizeOnMemory(_blockObject);
                        }
                        finally
                        {
                            if (locked)
                                Monitor.Exit(_blockObject);
                        }
                    }
                }
                return _ioDataSizeOnMemory;
            }
            set => _ioDataSizeOnMemory = value;
        }

        /// <summary>
        /// The last size of the block data on the io cache file.
        /// </summary>
        public long IoDataSizeOnFile { get; set; }

        /// <summary>
        /// Indicate if the block data is written on the io cache file.
        /// </summary>
        public bool IsWritten { get; set; }

        /// <summary>
        /// Provide the last update timestamp.
        /// </summary>
        public long LastUpdateTimestamp { get; private set; }

        /// <summary>
        /// Provide the last get timestamp.
        /// </summary>
        public long LastGetTimestamp { get; private set; }

        /// <summary>
        /// Indicate if the block has been deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Indicate if the block has been updated.
        /// </summary>
        public bool IsUpdated { get; private set; }

        /// <summary>
        /// Indicate if the block is empty.
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_blockObject == null)
                    return true;

                try
                {
                    if (Monitor.IsEntered(_blockObject))
                    {
                        try
                        {
                            if (_blockObject.Disposed)
                                return true;

                            if (_blockObject.BlockTransactions == null)
                                return true;

                            if (_blockObject.BlockTransactions.Count != _blockObject.TotalTransaction)
                                return true;

                            if (IsDeleted)
                                return true;
                        }
                        catch
                        {
                            return true;
                        }
                    }
                    else
                    {
                        bool locked = false;
                        bool exception = false;

                        try
                        {
                            try
                            {
                                Monitor.TryEnter(_blockObject, ref locked);

                                if (locked)
                                {
                                    if (_blockObject.Disposed)
                                        return true;

                                    if (_blockObject.BlockTransactions == null)
                                        return true;

                                    if (_blockObject.BlockTransactions.Count != _blockObject.TotalTransaction)
                                        return true;

                                    if (IsDeleted)
                                        return true;
                                }
                            }
                            finally
                            {
                                if (locked)
                                    Monitor.Exit(_blockObject);
                            }
                        }
                        catch
                        {
                            exception = true;
                        }


                        if (exception) return true;
                    }
                }
                catch
                {
                    return true;
                }

                return false;
            }
        }
    }
}
