using System;

namespace TopWar.Core.Progression
{
    [Serializable]
    public sealed class PlayerProgressData
    {
        public int version = 1;
        public int gems;
        public int highestClearedStage;
        public int[] upgradeLevels;
    }
}
