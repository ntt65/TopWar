using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class EnemyGroup
    {
        private readonly int _initialCount;

        public EnemyGroup(float distance, UnitType type, int count, float hpPerEnemy)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (!(hpPerEnemy > 0f)) throw new ArgumentOutOfRangeException(nameof(hpPerEnemy));

            Distance = distance;
            Type = type;
            HpPerEnemy = hpPerEnemy;
            _initialCount = count;
            MaxHp = count * hpPerEnemy;
            Hp = MaxHp;
        }

        public float Distance { get; }
        public UnitType Type { get; }
        public float HpPerEnemy { get; }
        public float MaxHp { get; }
        public float Hp { get; private set; }
        public int RemainingCount => Hp == MaxHp
            ? _initialCount
            : (int)Math.Min(_initialCount, Math.Ceiling(Hp / HpPerEnemy));
        public bool IsAlive => Hp > 0f;

        internal void TakeDamage(float damage) => Hp = Math.Max(0f, Hp - damage);

        internal void Clear() => Hp = 0f;
    }
}
