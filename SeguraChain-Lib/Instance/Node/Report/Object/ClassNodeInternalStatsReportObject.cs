
namespace SeguraChain_Lib.Instance.Node.Report.Object
{
    public class ClassNodeInternalStatsReportObject
    {
        public long NodeCacheMemoryUsage { get; set; }
        public long NodeCacheMaxMemoryAllocation { get; set; }
        public long NodeCacheTransactionMemoryUsage { get; set; }
        public long NodeCacheTransactionMaxMemoryAllocation { get; set; }
        public long NodeCacheWalletIndexMemoryUsage { get; set; }
        public long NodeCacheWalletIndexMaxMemoryAllocation { get; set; }
        public long NodeTotalMemoryUsage { get; set; }
    }
}
