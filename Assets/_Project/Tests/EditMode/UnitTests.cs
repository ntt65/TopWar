using System;
using NUnit.Framework;
using TopWar.Core;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public class UnitTests
    {
        [Test]
        public void SameTypeAndLevel_CanMerge()
        {
            var a = new UnitData(UnitType.Army, 3);
            Assert.That(a.CanMergeWith(new UnitData(UnitType.Army, 3)), Is.True);
        }

        [Test]
        public void DifferentType_CannotMerge()
        {
            var a = new UnitData(UnitType.Army, 3);
            Assert.That(a.CanMergeWith(new UnitData(UnitType.Navy, 3)), Is.False);
        }

        [Test]
        public void DifferentLevel_CannotMerge()
        {
            var a = new UnitData(UnitType.Army, 3);
            Assert.That(a.CanMergeWith(new UnitData(UnitType.Army, 2)), Is.False);
        }

        [Test]
        public void MaxLevel_CannotMerge()
        {
            var a = new UnitData(UnitType.AirForce, GameRules.MaxLevel);
            Assert.That(a.CanMergeWith(a), Is.False);
            Assert.Throws<InvalidOperationException>(() => a.Merged());
        }

        [Test]
        public void Merged_IncreasesLevelByOne()
        {
            var merged = new UnitData(UnitType.Navy, 4).Merged();
            Assert.That(merged, Is.EqualTo(new UnitData(UnitType.Navy, 5)));
        }

        [TestCase(0)]
        [TestCase(11)]
        public void InvalidLevel_Throws(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnitData(UnitType.Army, level));
        }

        [TestCase(1, 10f)]
        [TestCase(2, 20f)]
        [TestCase(5, 160f)]
        [TestCase(10, 5120f)]
        public void AttackDoublesPerLevel(int level, float expected)
        {
            Assert.That(UnitStats.AttackAtLevel(10f, level), Is.EqualTo(expected));
        }

        [TestCase(UnitType.Army, UnitType.AirForce, 1.25f)]
        [TestCase(UnitType.AirForce, UnitType.Navy, 1.25f)]
        [TestCase(UnitType.Navy, UnitType.Army, 1.25f)]
        [TestCase(UnitType.AirForce, UnitType.Army, 0.85f)]
        [TestCase(UnitType.Navy, UnitType.AirForce, 0.85f)]
        [TestCase(UnitType.Army, UnitType.Navy, 0.85f)]
        [TestCase(UnitType.Army, UnitType.Army, 1f)]
        public void CounterMultiplier(UnitType attacker, UnitType defender, float expected)
        {
            Assert.That(CounterTable.DamageMultiplier(attacker, defender), Is.EqualTo(expected));
        }
    }
}
