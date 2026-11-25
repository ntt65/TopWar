using System;
using TopWar.Core.Units;

namespace TopWar.Core.Progression
{
    public sealed class PlayerProgress
    {
        private const int UpgradeCount = (int)UpgradeType.GridSize + 1;
        private readonly int[] _upgradeLevels = new int[UpgradeCount];

        public int Gems { get; private set; }
        public int HighestClearedStage { get; private set; }
        public int NextStage => HighestClearedStage + 1;

        public event Action Changed;

        public int GetLevel(UpgradeType type)
        {
            int index = (int)type;
            if ((uint)index >= UpgradeCount) throw new ArgumentOutOfRangeException(nameof(type));

            return _upgradeLevels[index];
        }

        public int GetMaxLevel(UpgradeType type) => type switch
        {
            UpgradeType.ArmyAttack or UpgradeType.AirForceAttack or UpgradeType.NavyAttack => 20,
            UpgradeType.BaseHealth => 10,
            UpgradeType.GridSize => GameRules.MaxGridSize - GameRules.DefaultGridSize,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        public bool IsMaxed(UpgradeType type) => GetLevel(type) == GetMaxLevel(type);

        public int GetCost(UpgradeType type)
        {
            int level = GetLevel(type);
            if (level == GetMaxLevel(type)) return 0;

            return (type == UpgradeType.GridSize ? 30 : 5) * (level + 1);
        }

        public bool CanUpgrade(UpgradeType type)
        {
            int cost = GetCost(type);
            return cost > 0 && Gems >= cost;
        }

        public bool TryUpgrade(UpgradeType type)
        {
            int cost = GetCost(type);
            if (cost == 0 || Gems < cost) return false;

            Gems -= cost;
            _upgradeLevels[(int)type]++;
            Changed?.Invoke();
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;

            Gems = checked(Gems + amount);
            Changed?.Invoke();
        }

        public void RecordStageCleared(int stage)
        {
            if (stage < 1) throw new ArgumentOutOfRangeException(nameof(stage));
            if (stage <= HighestClearedStage) return;

            HighestClearedStage = stage;
            Changed?.Invoke();
        }

        public float GetAttackMultiplier(UnitType type)
        {
            var upgrade = type switch
            {
                UnitType.Army => UpgradeType.ArmyAttack,
                UnitType.AirForce => UpgradeType.AirForceAttack,
                UnitType.Navy => UpgradeType.NavyAttack,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };

            return 1f + 0.1f * GetLevel(upgrade);
        }

        public int GetBaseMaxHealth(int baseValue) => baseValue + 20 * GetLevel(UpgradeType.BaseHealth);

        public int GetGridSize() => GameRules.DefaultGridSize + GetLevel(UpgradeType.GridSize);

        public PlayerProgressData ToData() => new PlayerProgressData
        {
            gems = Gems,
            highestClearedStage = HighestClearedStage,
            upgradeLevels = (int[])_upgradeLevels.Clone(),
        };

        public static PlayerProgress FromData(PlayerProgressData data)
        {
            var progress = new PlayerProgress();
            if (data == null) return progress;

            progress.Gems = Math.Max(0, data.gems);
            progress.HighestClearedStage = Math.Max(0, data.highestClearedStage);
            if (data.upgradeLevels == null) return progress;

            int count = Math.Min(UpgradeCount, data.upgradeLevels.Length);
            for (int i = 0; i < count; i++)
            {
                int maxLevel = progress.GetMaxLevel((UpgradeType)i);
                progress._upgradeLevels[i] = Math.Clamp(data.upgradeLevels[i], 0, maxLevel);
            }

            return progress;
        }
    }
}
