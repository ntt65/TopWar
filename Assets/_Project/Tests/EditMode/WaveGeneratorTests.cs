using System;
using NUnit.Framework;
using TopWar.Core.Battle;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public sealed class WaveGeneratorTests
    {
        private readonly BattleSettings _settings = new BattleSettings();

        [Test]
        public void WaveOne_SpawnsSixNormalEnemiesInValidLanes()
        {
            var wave = new WaveGenerator(_settings, new Random(42)).Create(1, 5);

            Assert.That(wave.Number, Is.EqualTo(1));
            Assert.That(wave.IsBoss, Is.False);
            Assert.That(wave.Spawns.Count, Is.EqualTo(6));
            for (int i = 0; i < wave.Spawns.Count; i++)
            {
                var spawn = wave.Spawns[i];
                Assert.That(spawn.Time, Is.EqualTo(i * 1.2f).Within(0.0001f));
                Assert.That(spawn.Lane, Is.InRange(0, 4));
                Assert.That(spawn.Stats, Is.EqualTo(_settings.GetEnemyBaseStats(spawn.Type)));
            }
        }

        [Test]
        public void WaveThree_ScalesHpAndShortensInterval()
        {
            var wave = new WaveGenerator(_settings, new Random(42)).Create(3, 5);

            Assert.That(wave.Spawns.Count, Is.EqualTo(10));
            for (int i = 0; i < wave.Spawns.Count; i++)
            {
                var spawn = wave.Spawns[i];
                float baseHp = _settings.GetEnemyBaseStats(spawn.Type).MaxHp;
                Assert.That(spawn.Stats.MaxHp, Is.EqualTo(baseHp * 1.18f * 1.18f).Within(0.0001f));
                Assert.That(spawn.Time, Is.EqualTo(i * 1.1f).Within(0.0001f));
            }
        }

        [Test]
        public void WaveFive_EndsWithArmyBossInMiddleLane()
        {
            var wave = new WaveGenerator(_settings, new Random(42)).Create(5, 5);

            Assert.That(wave.IsBoss, Is.True);
            Assert.That(wave.Spawns.Count, Is.EqualTo(8));
            for (int i = 0; i < 7; i++)
            {
                Assert.That(wave.Spawns[i].Stats.IsBoss, Is.False);
                Assert.That(wave.Spawns[i].Time, Is.EqualTo(i).Within(0.0001f));
            }

            var boss = wave.Spawns[7];
            Assert.That(boss.Time, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(boss.Type, Is.EqualTo(UnitType.Army));
            Assert.That(boss.Lane, Is.EqualTo(2));
            Assert.That(boss.Stats.IsBoss, Is.True);
            Assert.That(boss.Stats.MaxHp, Is.EqualTo(30 * Math.Pow(1.18f, 4) * 25).Within(0.001f));
            Assert.That(boss.Stats.Speed, Is.EqualTo(0.3f));
            Assert.That(boss.Stats.BaseDamage, Is.Zero);
            Assert.That(boss.Stats.Reward, Is.EqualTo(50));
        }

        [TestCase(10, UnitType.AirForce)]
        [TestCase(15, UnitType.Navy)]
        [TestCase(20, UnitType.Army)]
        public void LaterBossWaves_CycleThroughUnitTypes(int waveNumber, UnitType expectedType)
        {
            var wave = new WaveGenerator(_settings, new Random(42)).Create(waveNumber, 4);
            var boss = wave.Spawns[wave.Spawns.Count - 1];

            Assert.That(boss.Type, Is.EqualTo(expectedType));
            Assert.That(boss.Lane, Is.EqualTo(2));
            Assert.That(boss.Stats.IsBoss, Is.True);
        }

        [Test]
        public void Create_ClampsIntervalAndRoundsDownBossEscortCount()
        {
            var settings = new BattleSettings { EnemyCountBase = 7, EnemyCountPerWave = 0 };
            var wave = new WaveGenerator(settings, new Random(42)).Create(20, 1);

            Assert.That(wave.Spawns.Count, Is.EqualTo(4));
            Assert.That(wave.Spawns[1].Time, Is.EqualTo(0.4f));
            Assert.That(wave.Spawns[3].Time, Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void Create_WithSameSeedProducesSameSpawns()
        {
            var first = new WaveGenerator(_settings, new Random(42));
            var second = new WaveGenerator(_settings, new Random(42));

            Assert.That(first.Create(1, 5).Spawns, Is.EqualTo(second.Create(1, 5).Spawns));
            Assert.That(first.Create(5, 5).Spawns, Is.EqualTo(second.Create(5, 5).Spawns));
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void Create_RejectsInvalidWaveOrLaneCount(int waveNumber, int laneCount)
        {
            var generator = new WaveGenerator(_settings);

            Assert.Throws<ArgumentOutOfRangeException>(() => generator.Create(waveNumber, laneCount));
        }
    }
}
