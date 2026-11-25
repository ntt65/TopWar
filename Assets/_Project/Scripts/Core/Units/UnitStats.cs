using System;

namespace TopWar.Core.Units
{
    public static class UnitStats
    {
        /// <summary>레벨이 하나 오를 때마다 공격력 2배: base × 2^(level-1).</summary>
        public static float AttackAtLevel(float baseAttack, int level)
        {
            if (level < GameRules.MinLevel || level > GameRules.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));

            return baseAttack * (1 << (level - 1));
        }
    }
}
