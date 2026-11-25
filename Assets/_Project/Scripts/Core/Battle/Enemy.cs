using System;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public sealed class Enemy
    {
        public Enemy(int id, UnitType type, EnemyStats stats, int lane, float y)
        {
            Id = id;
            Type = type;
            Stats = stats;
            Lane = lane;
            Y = y;
            Hp = stats.MaxHp;
        }

        public int Id { get; }
        public UnitType Type { get; }
        public EnemyStats Stats { get; }
        public int Lane { get; }
        public float Y { get; set; }
        public float Hp { get; private set; }
        public bool IsAlive => Hp > 0f;
        public float HpRatio => Hp / Stats.MaxHp;

        public bool TakeDamage(float amount)
        {
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!IsAlive) return false;

            Hp = Math.Max(0f, Hp - amount);
            return Hp == 0f;
        }
    }
}
