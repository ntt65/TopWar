using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class RunnerSettings
    {
        public float ForwardSpeed { get; set; } = 6f;
        public float HalfWidth { get; set; } = 2f;
        public float FireRange { get; set; } = 12f;
        public float FireInterval { get; set; } = 0.25f;
        public float BossStopGap { get; set; } = 8f;
        public float BossTimeLimit { get; set; } = 20f;
        public float BossAttackInterval { get; set; } = 2f;
        public int StartCount { get; set; } = 5;
        public UnitType StartType { get; set; } = UnitType.Army;
        public float UnitBaseAttack { get; set; } = 10f;
        public float ArmyAttackMultiplier { get; set; } = 1f;
        public float AirForceAttackMultiplier { get; set; } = 1f;
        public float NavyAttackMultiplier { get; set; } = 1f;

        public float GetUnitAttackMultiplier(UnitType type) => type switch
        {
            UnitType.Army => ArmyAttackMultiplier,
            UnitType.AirForce => AirForceAttackMultiplier,
            UnitType.Navy => NavyAttackMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }
}
