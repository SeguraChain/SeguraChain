using SeguraChain_Lib.Blockchain.Sovereign.Enum;

namespace SeguraChain_Lib.Blockchain.Sovereign.Object
{
    /// <summary>
    /// The content can differ depending the type of the update.
    /// </summary>
    public class ClassSovereignUpdateContentObject
    {
        public string PossibleContent1; // Depend of the type of the update.
        public string PossibleContent2; // Depend of the type of the update.
        public string Description; // Description of the sovereign update.
    }

    public class ClassSovereignUpdateObject
    {
        public ClassSovereignEnumUpdateType SovereignUpdateType;
        public ClassSovereignUpdateContentObject SovereignUpdateContent;
        public string SovereignUpdateDevWalletAddress;
        public string SovereignUpdateSignature;
        public string SovereignUpdateHash;
        public long SovereignUpdateTimestamp;
    }
}
