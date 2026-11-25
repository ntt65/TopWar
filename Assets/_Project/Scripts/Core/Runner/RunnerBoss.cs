using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class RunnerBoss
    {
        public RunnerBoss(float distance, UnitType type, float maxHp, int killsPerAttack)
        {
            if (!(distance > 0f) || float.IsInfinity(distance))
                throw new ArgumentOutOfRangeException(nameof(distance));
            if (!(maxHp > 0f)) throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (killsPerAttack < 1) throw new ArgumentOutOfRangeException(nameof(killsPerAttack));

            Distance = distance;
            Type = type;
            MaxHp = maxHp;
            Hp = maxHp;
            KillsPerAttack = killsPerAttack;
        }

        public float Distance { get; }
        public UnitType Type { get; }
        public float MaxHp { get; }
        public float Hp { get; private set; }
        public int KillsPerAttack { get; }
        public bool IsAlive => Hp > 0f;

        internal void TakeDamage(float damage) => Hp = Math.Max(0f, Hp - damage);
    }
}
