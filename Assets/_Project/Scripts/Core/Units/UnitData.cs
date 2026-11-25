using System;

namespace TopWar.Core.Units
{
    /// <summary>그리드 한 칸을 차지하는 유닛. 병종과 레벨만으로 정의된다.</summary>
    public readonly struct UnitData : IEquatable<UnitData>
    {
        public UnitType Type { get; }
        public int Level { get; }

        public UnitData(UnitType type, int level)
        {
            if (level < GameRules.MinLevel || level > GameRules.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level), level,
                    $"Level must be between {GameRules.MinLevel} and {GameRules.MaxLevel}.");

            Type = type;
            Level = level;
        }

        public bool IsMaxLevel => Level >= GameRules.MaxLevel;

        /// <summary>같은 병종·레벨이고 최대 레벨이 아니면 합성할 수 있다.</summary>
        public bool CanMergeWith(UnitData other) =>
            Type == other.Type && Level == other.Level && !IsMaxLevel;

        public UnitData Merged()
        {
            if (IsMaxLevel)
                throw new InvalidOperationException("Max level unit cannot be merged.");

            return new UnitData(Type, Level + 1);
        }

        public bool Equals(UnitData other) => Type == other.Type && Level == other.Level;
        public override bool Equals(object obj) => obj is UnitData other && Equals(other);
        public override int GetHashCode() => ((int)Type * 397) ^ Level;
        public override string ToString() => $"{Type} Lv.{Level}";

        public static bool operator ==(UnitData a, UnitData b) => a.Equals(b);
        public static bool operator !=(UnitData a, UnitData b) => !a.Equals(b);
    }
}
