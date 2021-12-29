namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum
{
    public enum CacheEnumName
    {
        IO_CACHE = 0,
    }

    public enum CacheBlockMemoryInsertEnumType
    {
        INSERT_IN_ACTIVE_MEMORY_OBJECT = 1,
        INSERT_IN_PERSISTENT_CACHE_OBJECT = 2,
    }

    public enum CacheBlockMemoryEnumState
    {
        IN_ACTIVE_MEMORY = 0,
        IN_CACHE = 1,
        IN_PERSISTENT_CACHE = 2
    }

    public enum CacheBlockMemoryEnumGetType
    {
        GET_OBJECT_FROM_ACTIVE_MEMORY = 0,
        GET_OBJECT_FROM_CACHE = 1,
        GET_OBJECT_FROM_BOTH = 2,
    }

    public enum CacheBlockMemoryEnumInsertPolicy
    {
        INSERT_PROBABLY_NOT_REALLY_USED = 0,
        INSERT_MOSTLY_USED = 1,
        INSERT_IN_AND_CACHE = 2,
        INSERT_FROZEN = 3,
    }
}
