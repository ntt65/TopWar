using System;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public sealed class BattleSettings
    {
        public float SpawnDistance { get; set; } = 6f;
        public float BaseY { get; set; } = -1f;
        public int BaseMaxHealth { get; set; } = 100;
        public float UnitBaseAttack { get; set; } = 10f;
        public float BreakDuration { get; set; } = 5f;
        public int WaveClearBonusBase { get; set; } = 20;
        public int WaveClearBonusPerWave { get; set; } = 5;
        public int EnemyCountBase { get; set; } = 6;
        public int EnemyCountPerWave { get; set; } = 2;
        public float SpawnIntervalStart { get; set; } = 1.2f;
        public float SpawnIntervalStep { get; set; } = 0.05f;
        public float SpawnIntervalMin { get; set; } = 0.4f;
        public float EnemyHpGrowth { get; set; } = 1.18f;
        public int BossEvery { get; set; } = 5;
        public float BossHpMultiplier { get; set; } = 25f;
        public float BossSpeed { get; set; } = 0.3f;
        public int BossReward { get; set; } = 50;
        public float BossTimeLimit { get; set; } = 60f;
        public float BossBaseDamagePercent { get; set; } = 0.5f;

        public EnemyStats GetEnemyBaseStats(UnitType type) => type switch
        {
            UnitType.Army => new EnemyStats(30f, 0.6f, 5, 5, false),
            UnitType.AirForce => new EnemyStats(20f, 0.9f, 5, 5, false),
            UnitType.Navy => new EnemyStats(45f, 0.45f, 8, 7, false),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }
}
