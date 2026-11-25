using System;

namespace TopWar.Core.Runner
{
    public sealed class SupplyCrate
    {
        public SupplyCrate(float distance, bool isLeft, float maxHp, int rewardCount)
        {
            if (!(maxHp > 0f)) throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (rewardCount < 1) throw new ArgumentOutOfRangeException(nameof(rewardCount));

            Distance = distance;
            IsLeft = isLeft;
            MaxHp = maxHp;
            Hp = maxHp;
            RewardCount = rewardCount;
        }

        public float Distance { get; }
        public bool IsLeft { get; }
        public float MaxHp { get; }
        public float Hp { get; private set; }
        public int RewardCount { get; }
        public bool IsOpen => Hp == 0f;
        public bool IsMissed { get; private set; }
        public bool IsActive => !IsOpen && !IsMissed;

        internal void TakeDamage(float damage) => Hp = Math.Max(0f, Hp - damage);

        internal void Miss() => IsMissed = true;
    }
}
