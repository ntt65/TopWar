using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class Squad
    {
        public const int MaxCount = 500;

        private readonly int[] _counts = new int[GameRules.MaxLevel + 1];

        public Squad(UnitType type, int initialCount)
        {
            if (initialCount < 1) throw new ArgumentOutOfRangeException(nameof(initialCount));

            Type = type;
            Count = Math.Min(initialCount, MaxCount);
            _counts[GameRules.MinLevel] = Count;
        }

        public UnitType Type { get; private set; }
        public int Count { get; private set; }
        public bool IsDefeated => Count == 0;

        public int HighestLevel
        {
            get
            {
                for (int level = GameRules.MaxLevel; level >= GameRules.MinLevel; level--)
                {
                    if (_counts[level] > 0) return level;
                }

                return 0;
            }
        }

        public event Action Changed;

        public int GetCount(int level)
        {
            if (level < GameRules.MinLevel || level > GameRules.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));

            return _counts[level];
        }

        public void Add(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            int added = Math.Min(count, MaxCount - Count);
            if (added == 0) return;

            _counts[GameRules.MinLevel] += added;
            Count += added;
            Changed?.Invoke();
        }

        public void Multiply(int factor)
        {
            if (factor < 2) throw new ArgumentOutOfRangeException(nameof(factor));

            // 높은 레벨부터 채우면 상한 초과분을 낮은 레벨부터 제거한 결과와 같다.
            int remaining = MaxCount;
            bool changed = false;
            for (int level = GameRules.MaxLevel; level >= GameRules.MinLevel; level--)
            {
                int count = (int)Math.Min((long)_counts[level] * factor, remaining);
                changed |= _counts[level] != count;
                _counts[level] = count;
                remaining -= count;
            }

            Count = MaxCount - remaining;
            if (changed) Changed?.Invoke();
        }

        public void Remove(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            int removed = Math.Min(count, Count);
            if (removed == 0) return;

            int remaining = removed;
            for (int level = GameRules.MinLevel; remaining > 0; level++)
            {
                int fromLevel = Math.Min(_counts[level], remaining);
                _counts[level] -= fromLevel;
                remaining -= fromLevel;
            }

            Count -= removed;
            Changed?.Invoke();
        }

        public void Merge()
        {
            int previousCount = Count;
            for (int level = GameRules.MaxLevel - 1; level >= GameRules.MinLevel; level--)
            {
                int pairs = _counts[level] / 2;
                _counts[level] -= 2 * pairs;
                _counts[level + 1] += pairs;
                Count -= pairs;
            }

            if (Count != previousCount) Changed?.Invoke();
        }

        public void ChangeType(UnitType type)
        {
            if (Type == type) return;

            Type = type;
            Changed?.Invoke();
        }

        public float Firepower(float baseAttack, float attackMultiplier)
        {
            float attack = 0f;
            for (int level = GameRules.MinLevel; level <= GameRules.MaxLevel; level++)
            {
                if (_counts[level] > 0)
                    attack += _counts[level] * UnitStats.AttackAtLevel(baseAttack, level);
            }

            return attack * attackMultiplier;
        }
    }
}
