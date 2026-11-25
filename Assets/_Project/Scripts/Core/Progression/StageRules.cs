using System;
using TopWar.Core.Battle;
using TopWar.Core.Units;

namespace TopWar.Core.Progression
{
    public static class StageRules
    {
        public const int WavesPerStage = 10;
        public const float StageHpGrowth = 1.6f;
        public const int BaseMaxHealth = 100;

        public static float EnemyHpScale(int stage)
        {
            if (stage < 1) throw new ArgumentOutOfRangeException(nameof(stage));

            return (float)Math.Pow(StageHpGrowth, stage - 1);
        }

        public static int GemReward(int stage, int clearedWaves, bool victory)
        {
            if (stage < 1) throw new ArgumentOutOfRangeException(nameof(stage));
            if (clearedWaves < 0) throw new ArgumentOutOfRangeException(nameof(clearedWaves));

            return clearedWaves + (victory ? 10 + 5 * (stage - 1) : 0);
        }

        public static BattleSettings CreateSettings(int stage, PlayerProgress progress, float unitBaseAttack)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            return new BattleSettings
            {
                WavesPerStage = WavesPerStage,
                EnemyHpScale = EnemyHpScale(stage),
                UnitBaseAttack = unitBaseAttack,
                ArmyAttackMultiplier = progress.GetAttackMultiplier(UnitType.Army),
                AirForceAttackMultiplier = progress.GetAttackMultiplier(UnitType.AirForce),
                NavyAttackMultiplier = progress.GetAttackMultiplier(UnitType.Navy),
                BaseMaxHealth = progress.GetBaseMaxHealth(BaseMaxHealth),
            };
        }
    }
}
