using System;
using NUnit.Framework;
using TopWar.Core.Battle;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public sealed class BattleDataTests
    {
        [TestCase(UnitType.Army, 30f, 0.6f, 5, 5)]
        [TestCase(UnitType.AirForce, 20f, 0.9f, 5, 5)]
        [TestCase(UnitType.Navy, 45f, 0.45f, 8, 7)]
        public void GetEnemyBaseStats_ReturnsStatsForType(
            UnitType type, float hp, float speed, int baseDamage, int reward)
        {
            var stats = new BattleSettings().GetEnemyBaseStats(type);

            Assert.That(stats, Is.EqualTo(new EnemyStats(hp, speed, baseDamage, reward, false)));
        }

        [TestCase(0f, 0f, 0, 0)]
        [TestCase(-1f, 0f, 0, 0)]
        [TestCase(1f, -1f, 0, 0)]
        [TestCase(1f, 0f, -1, 0)]
        [TestCase(1f, 0f, 0, -1)]
        public void EnemyStats_RejectsInvalidValues(float hp, float speed, int baseDamage, int reward)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStats(hp, speed, baseDamage, reward, false));
        }

        [Test]
        public void WithHpMultiplier_OnlyChangesMaxHp()
        {
            var stats = new EnemyStats(30f, 0.6f, 5, 7, true);

            Assert.That(stats.WithHpMultiplier(2f), Is.EqualTo(new EnemyStats(60f, 0.6f, 5, 7, true)));
            Assert.That(stats.MaxHp, Is.EqualTo(30f));
        }

        [Test]
        public void EnemyDamage_AccumulatesAndReportsDeathOnce()
        {
            var stats = new EnemyStats(30f, 0.6f, 5, 5, false);
            var enemy = new Enemy(7, UnitType.Army, stats, 2, 10f);

            Assert.That(enemy.Id, Is.EqualTo(7));
            Assert.That(enemy.Type, Is.EqualTo(UnitType.Army));
            Assert.That(enemy.Stats, Is.EqualTo(stats));
            Assert.That(enemy.Lane, Is.EqualTo(2));
            Assert.That(enemy.Y, Is.EqualTo(10f));
            Assert.That(enemy.Hp, Is.EqualTo(30f));
            Assert.That(enemy.TakeDamage(10f), Is.False);
            Assert.That(enemy.TakeDamage(5f), Is.False);
            Assert.That(enemy.Hp, Is.EqualTo(15f));
            Assert.That(enemy.HpRatio, Is.EqualTo(0.5f));
            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(enemy.TakeDamage(20f), Is.True);
            Assert.That(enemy.TakeDamage(1f), Is.False);
            Assert.That(enemy.Hp, Is.Zero);
            Assert.That(enemy.HpRatio, Is.Zero);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => enemy.TakeDamage(-1f));
        }

        [TestCase(UnitType.Army, 3f, 1f, 1f, 0f)]
        [TestCase(UnitType.AirForce, 2.5f, 0.5f, 0.45f, 0.8f)]
        [TestCase(UnitType.Navy, 5f, 2f, 2.6f, 0f)]
        public void CombatProfile_UsesTypeValuesAndDoublesAttackPerLevel(
            UnitType type, float range, float interval, float multiplier, float splash)
        {
            var profile = CombatProfile.For(type);

            Assert.That(profile.Range, Is.EqualTo(range));
            Assert.That(profile.AttackInterval, Is.EqualTo(interval));
            Assert.That(profile.AttackMultiplier, Is.EqualTo(multiplier));
            Assert.That(profile.SplashRadius, Is.EqualTo(splash));
            Assert.That(profile.AttackPower(10f, 1), Is.EqualTo(10f * multiplier));
            Assert.That(profile.AttackPower(10f, 2), Is.EqualTo(20f * multiplier));
            Assert.That(profile.AttackPower(10f, 3), Is.EqualTo(40f * multiplier));
        }

        [Test]
        public void BaseHealth_ClampsDamageAndNotifiesOnlyOnChange()
        {
            var health = new BaseHealth(100);
            int changes = 0;
            health.Changed += () => changes++;

            health.TakeDamage(10);
            Assert.That(health.Current, Is.EqualTo(90));
            health.TakePercentOfMax(0.5f);
            Assert.That(health.Current, Is.EqualTo(40));
            Assert.That(health.IsDestroyed, Is.False);
            health.TakeDamage(0);
            health.TakeDamage(50);
            health.TakeDamage(1);

            Assert.That(health.Max, Is.EqualTo(100));
            Assert.That(health.Current, Is.Zero);
            Assert.That(health.IsDestroyed, Is.True);
            Assert.That(changes, Is.EqualTo(3));
        }

        [Test]
        public void BaseHealth_PercentDamageRoundsUp()
        {
            var health = new BaseHealth(101);

            health.TakePercentOfMax(0.5f);

            Assert.That(health.Current, Is.EqualTo(50));
        }

        [Test]
        public void BaseHealth_RejectsInvalidValues()
        {
            var health = new BaseHealth(100);

            Assert.Throws<ArgumentOutOfRangeException>(() => new BaseHealth(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.TakeDamage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.TakePercentOfMax(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.TakePercentOfMax(1.1f));
        }

        [Test]
        public void WavePlan_SortsSpawnsAndDetectsBossAnywhere()
        {
            var normal = new WaveSpawn(2f, UnitType.Navy, 2, new EnemyStats(45f, 0.45f, 8, 7, false));
            var boss = new WaveSpawn(1f, UnitType.Army, 1, new EnemyStats(100f, 0.3f, 0, 50, true));
            var spawns = new[] { normal, boss };
            var plan = new WavePlan(5, spawns);
            spawns[1] = normal;

            Assert.That(plan.Number, Is.EqualTo(5));
            Assert.That(plan.Spawns, Is.EqualTo(new[] { boss, normal }));
            Assert.That(plan.IsBoss, Is.True);
        }
    }
}
