using System;
using NUnit.Framework;
using TopWar.Core.Battle;
using TopWar.Core.Progression;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public sealed class ProgressionTests
    {
        [TestCase(UpgradeType.ArmyAttack, 20, 5, 10, 100)]
        [TestCase(UpgradeType.AirForceAttack, 20, 5, 10, 100)]
        [TestCase(UpgradeType.NavyAttack, 20, 5, 10, 100)]
        [TestCase(UpgradeType.BaseHealth, 10, 5, 10, 50)]
        [TestCase(UpgradeType.GridSize, 2, 30, 60, 60)]
        public void UpgradeCosts_MatchEachTypeAndStopAtMax(
            UpgradeType type, int maxLevel, int firstCost, int secondCost, int lastCost)
        {
            var progress = new PlayerProgress();
            int changes = 0;
            progress.Changed += () => changes++;

            Assert.That(progress.GetMaxLevel(type), Is.EqualTo(maxLevel));
            Assert.That(progress.GetCost(type), Is.EqualTo(firstCost));
            Assert.That(progress.CanUpgrade(type), Is.False);
            Assert.That(progress.TryUpgrade(type), Is.False);
            Assert.That(changes, Is.Zero);

            progress.AddGems(firstCost);
            Assert.That(progress.CanUpgrade(type), Is.True);
            Assert.That(progress.TryUpgrade(type), Is.True);
            Assert.That(progress.Gems, Is.Zero);
            Assert.That(progress.GetLevel(type), Is.EqualTo(1));
            Assert.That(progress.GetCost(type), Is.EqualTo(secondCost));
            Assert.That(changes, Is.EqualTo(2));

            var levels = new int[5];
            levels[(int)type] = maxLevel - 1;
            var almostMaxed = PlayerProgress.FromData(new PlayerProgressData
            {
                gems = lastCost,
                upgradeLevels = levels,
            });

            Assert.That(almostMaxed.GetCost(type), Is.EqualTo(lastCost));
            Assert.That(almostMaxed.TryUpgrade(type), Is.True);
            Assert.That(almostMaxed.GetLevel(type), Is.EqualTo(maxLevel));
            Assert.That(almostMaxed.IsMaxed(type), Is.True);
            Assert.That(almostMaxed.GetCost(type), Is.Zero);
            almostMaxed.AddGems(100);
            Assert.That(almostMaxed.CanUpgrade(type), Is.False);
            Assert.That(almostMaxed.TryUpgrade(type), Is.False);
            Assert.That(almostMaxed.Gems, Is.EqualTo(100));
        }

        [Test]
        public void GridUpgrade_ExpandsFromFiveToSeven()
        {
            var progress = new PlayerProgress();
            progress.AddGems(90);

            Assert.That(progress.GetGridSize(), Is.EqualTo(5));
            Assert.That(progress.TryUpgrade(UpgradeType.GridSize), Is.True);
            Assert.That(progress.GetGridSize(), Is.EqualTo(6));
            Assert.That(progress.TryUpgrade(UpgradeType.GridSize), Is.True);
            Assert.That(progress.GetGridSize(), Is.EqualTo(7));
            Assert.That(progress.Gems, Is.Zero);
        }

        [Test]
        public void StageRecord_OnlyChangesWhenHigherStageIsCleared()
        {
            var progress = new PlayerProgress();
            int changes = 0;
            progress.Changed += () => changes++;

            Assert.That(progress.NextStage, Is.EqualTo(1));
            progress.AddGems(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddGems(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordStageCleared(0));
            Assert.That(changes, Is.Zero);

            progress.RecordStageCleared(3);
            progress.RecordStageCleared(3);
            progress.RecordStageCleared(2);
            Assert.That(progress.HighestClearedStage, Is.EqualTo(3));
            Assert.That(progress.NextStage, Is.EqualTo(4));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void StateRoundTrip_PreservesValuesAndCopiesUpgradeArrays()
        {
            var progress = new PlayerProgress();
            progress.AddGems(100);
            progress.TryUpgrade(UpgradeType.ArmyAttack);
            progress.TryUpgrade(UpgradeType.NavyAttack);
            progress.TryUpgrade(UpgradeType.BaseHealth);
            progress.TryUpgrade(UpgradeType.GridSize);
            progress.RecordStageCleared(4);

            var data = progress.ToData();
            var restored = PlayerProgress.FromData(data);

            Assert.That(data.version, Is.EqualTo(1));
            Assert.That(data.upgradeLevels, Is.EqualTo(new[] { 1, 0, 1, 1, 1 }));
            Assert.That(restored.ToData().upgradeLevels, Is.EqualTo(data.upgradeLevels));
            Assert.That(restored.Gems, Is.EqualTo(55));
            Assert.That(restored.HighestClearedStage, Is.EqualTo(4));

            data.upgradeLevels[(int)UpgradeType.ArmyAttack] = 0;
            Assert.That(progress.GetLevel(UpgradeType.ArmyAttack), Is.EqualTo(1));
            Assert.That(restored.GetLevel(UpgradeType.ArmyAttack), Is.EqualTo(1));
        }

        [Test]
        public void FromData_DefaultsMissingProgress()
        {
            var progress = PlayerProgress.FromData(null);
            var missingLevels = PlayerProgress.FromData(new PlayerProgressData { gems = 10 });

            Assert.That(progress.Gems, Is.Zero);
            Assert.That(progress.HighestClearedStage, Is.Zero);
            Assert.That(progress.ToData().upgradeLevels, Is.EqualTo(new int[5]));
            Assert.That(missingLevels.Gems, Is.EqualTo(10));
            Assert.That(missingLevels.ToData().upgradeLevels, Is.EqualTo(new int[5]));
        }

        [Test]
        public void FromData_ClampsInvalidValuesAndDefaultsMissingLevels()
        {
            var progress = PlayerProgress.FromData(new PlayerProgressData
            {
                gems = -8,
                highestClearedStage = -3,
                upgradeLevels = new[] { -2, 80, 3 },
            });

            Assert.That(progress.Gems, Is.Zero);
            Assert.That(progress.HighestClearedStage, Is.Zero);
            Assert.That(progress.ToData().upgradeLevels, Is.EqualTo(new[] { 0, 20, 3, 0, 0 }));
        }

        [Test]
        public void FromData_IgnoresExtraLevelsAndClampsEachUpgradeToItsMax()
        {
            var progress = PlayerProgress.FromData(new PlayerProgressData
            {
                upgradeLevels = new[] { 99, 99, 99, 99, 99, 99 },
            });

            Assert.That(progress.ToData().upgradeLevels, Is.EqualTo(new[] { 20, 20, 20, 10, 2 }));
            Assert.That(progress.GetAttackMultiplier(UnitType.Army), Is.EqualTo(3f));
            Assert.That(progress.GetBaseMaxHealth(100), Is.EqualTo(300));
            Assert.That(progress.GetGridSize(), Is.EqualTo(7));
        }

        [TestCase(1, 4, false, 4)]
        [TestCase(3, 10, true, 30)]
        [TestCase(5, 0, false, 0)]
        [TestCase(1, 10, true, 20)]
        public void GemReward_AddsVictoryBonusOnlyOnVictory(int stage, int clearedWaves, bool victory, int reward)
        {
            Assert.That(StageRules.GemReward(stage, clearedWaves, victory), Is.EqualTo(reward));
        }

        [Test]
        public void StageRules_RejectInvalidStageAndWaveCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRules.EnemyHpScale(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRules.GemReward(0, 0, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRules.GemReward(1, -1, false));
        }

        [Test]
        public void CreateSettings_UsesStageGrowthAndPermanentUpgrades()
        {
            var progress = PlayerProgress.FromData(new PlayerProgressData
            {
                upgradeLevels = new[] { 2, 5, 10, 3, 1 },
            });

            var settings = StageRules.CreateSettings(3, progress, 12f);

            Assert.That(StageRules.EnemyHpScale(1), Is.EqualTo(1f));
            Assert.That(settings.WavesPerStage, Is.EqualTo(10));
            Assert.That(settings.BossEvery, Is.EqualTo(5));
            Assert.That(settings.EnemyHpScale, Is.EqualTo(2.56f).Within(0.0001f));
            Assert.That(settings.UnitBaseAttack, Is.EqualTo(12f));
            Assert.That(settings.GetUnitAttackMultiplier(UnitType.Army), Is.EqualTo(1.2f));
            Assert.That(settings.GetUnitAttackMultiplier(UnitType.AirForce), Is.EqualTo(1.5f));
            Assert.That(settings.GetUnitAttackMultiplier(UnitType.Navy), Is.EqualTo(2f));
            Assert.That(settings.BaseMaxHealth, Is.EqualTo(160));
        }

        [TestCase(1, false)]
        [TestCase(5, true)]
        [TestCase(10, true)]
        public void EnemyHpScale_DoublesHpForRegularEnemiesAndBosses(int waveNumber, bool isBossWave)
        {
            var standard = new WaveGenerator(new BattleSettings(), new Random(42)).Create(waveNumber, 5);
            var harder = new WaveGenerator(new BattleSettings { EnemyHpScale = 2f }, new Random(42))
                .Create(waveNumber, 5);

            Assert.That(harder.IsBoss, Is.EqualTo(isBossWave));
            Assert.That(harder.Spawns.Count, Is.EqualTo(standard.Spawns.Count));
            for (int i = 0; i < standard.Spawns.Count; i++)
            {
                var original = standard.Spawns[i];
                var scaled = harder.Spawns[i];

                Assert.That(scaled.Type, Is.EqualTo(original.Type));
                Assert.That(scaled.Lane, Is.EqualTo(original.Lane));
                Assert.That(scaled.Time, Is.EqualTo(original.Time));
                Assert.That(scaled.Stats, Is.EqualTo(original.Stats.WithHpMultiplier(2f)));
            }
        }
    }
}
