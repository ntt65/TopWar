using System;

namespace TopWar.Core.Battle
{
    public readonly struct EnemyStats
    {
        public EnemyStats(float maxHp, float speed, int baseDamage, int reward, bool isBoss)
        {
            if (maxHp <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (speed < 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(reward));

            MaxHp = maxHp;
            Speed = speed;
            BaseDamage = baseDamage;
            Reward = reward;
            IsBoss = isBoss;
        }

        public float MaxHp { get; }
        public float Speed { get; }
        public int BaseDamage { get; }
        public int Reward { get; }
        public bool IsBoss { get; }

        public EnemyStats WithHpMultiplier(float multiplier) =>
            new EnemyStats(MaxHp * multiplier, Speed, BaseDamage, Reward, IsBoss);
    }
}
