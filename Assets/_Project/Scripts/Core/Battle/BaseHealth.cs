using System;

namespace TopWar.Core.Battle
{
    public sealed class BaseHealth
    {
        public BaseHealth(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));

            Max = max;
            Current = max;
        }

        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDestroyed => Current <= 0;

        public event Action Changed;

        public void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            int remaining = Math.Max(0, Current - amount);
            if (remaining == Current) return;

            Current = remaining;
            Changed?.Invoke();
        }

        public void TakePercentOfMax(float ratio)
        {
            if (!(ratio >= 0f && ratio <= 1f))
                throw new ArgumentOutOfRangeException(nameof(ratio));

            double damage = Math.Ceiling(Max * ratio);
            TakeDamage((int)Math.Min(Max, damage));
        }
    }
}
