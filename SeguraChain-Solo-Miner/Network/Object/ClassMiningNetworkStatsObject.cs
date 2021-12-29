using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Mining.Object;

namespace SeguraChain_Solo_Miner.Network.Object
{
    public class ClassMiningNetworkStatsObject
    {
        private ClassMiningPoWaCSettingObject _miningPoWaCSettingObject;

        #region Update functions.

        /// Update the current blocktemplate.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockTemplateObject"></param>
        public void UpdateBlocktemplate(ClassBlockTemplateObject blockTemplateObject)
        {
            GetBlockTemplateObject = blockTemplateObject;
        }

        /// <summary>
        /// Update the current mining poc setting.
        /// </summary>
        /// <param name="miningPoWaCSettingObject"></param>
        public void UpdateMiningPoWacSetting(ClassMiningPoWaCSettingObject miningPoWaCSettingObject)
        {
            if (miningPoWaCSettingObject != null)
                _miningPoWaCSettingObject = miningPoWaCSettingObject;
        }

        /// <summary>
        /// Update the amount of block unlocked.
        /// </summary>
        public void IncrementTotalUnlockedBlock()
        {
            GetTotalUnlockedBlock++;
        }

        /// <summary>
        /// Update the amount of orphaned block.
        /// </summary>
        public void IncrementTotalOrphanedBlock()
        {
            GetTotalOrphanedBlock++;
        }

        /// <summary>
        /// Update the amount of refused share.
        /// </summary>
        public void IncrementTotalRefusedShare()
        {
            GetTotalRefusedShare++;
        }

        /// <summary>
        /// Update the amount of invalid share.
        /// </summary>
        public void IncrementTotalInvalidShare()
        {
            GetTotalInvalidShare++;
        }

        #endregion

        #region Getter functions.

        /// <summary>
        /// Return the last blocktemplate.
        /// </summary>
        public ClassBlockTemplateObject GetBlockTemplateObject { get; private set; }

        /// <summary>
        /// Return the current mining setting.
        /// </summary>
        /// <returns></returns>
        public ClassMiningPoWaCSettingObject GetCurrentMiningPoWacSetting() => _miningPoWaCSettingObject;

        /// <summary>
        /// Return the amount of block unlocked.
        /// </summary>
        public int GetTotalUnlockedBlock { get; private set; }

        /// <summary>
        /// Return the amount of block orphaned.
        /// </summary>
        public int GetTotalOrphanedBlock { get; private set; }

        /// <summary>
        /// Return the amount of refused share.
        /// </summary>
        public int GetTotalRefusedShare { get; private set; }

        /// <summary>
        /// Return the amount of invalid share.
        /// </summary>
        public int GetTotalInvalidShare { get; private set; }

        #endregion
    }
}
