using System;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public readonly struct CombatProfile
    {
        public CombatProfile(float range, float attackInterval, float attackMultiplier, float splashRadius)
        {
            Range = range;
            AttackInterval = attackInterval;
            AttackMultiplier = attackMultiplier;
            SplashRadius = splashRadius;
        }

        public float Range { get; }
        public float AttackInterval { get; }
        public float AttackMultiplier { get; }
        public float SplashRadius { get; }

        public static CombatProfile For(UnitType type) => type switch
        {
            UnitType.Army => new CombatProfile(3f, 1f, 1f, 0f),
            UnitType.AirForce => new CombatProfile(2.5f, 0.5f, 0.45f, 0.8f),
            UnitType.Navy => new CombatProfile(5f, 2f, 2.6f, 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        public float AttackPower(float unitBaseAttack, int level) =>
            UnitStats.AttackAtLevel(unitBaseAttack * AttackMultiplier, level);
    }
}
