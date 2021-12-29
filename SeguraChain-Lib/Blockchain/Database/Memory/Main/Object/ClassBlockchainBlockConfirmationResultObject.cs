using SeguraChain_Lib.Other.Object.List;
using System.Collections.Generic;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Object
{
    public class ClassBlockchainBlockConfirmationResultObject
    {
        public bool Status;
        public long LastBlockHeightConfirmationDone;
        public DisposableList<long> ListBlockHeightConfirmed;

        public ClassBlockchainBlockConfirmationResultObject()
        {
            ListBlockHeightConfirmed = new DisposableList<long>();
        }
    }
}
