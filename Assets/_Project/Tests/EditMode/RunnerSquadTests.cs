using System;
using NUnit.Framework;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class RunnerSquadTests
    {
        [Test]
        public void Constructor_CreatesLevelOneUnitsOfRequestedType()
        {
            var squad = new Squad(UnitType.Navy, 5);

            Assert.That(squad.Type, Is.EqualTo(UnitType.Navy));
            Assert.That(squad.Count, Is.EqualTo(5));
            Assert.That(squad.GetCount(1), Is.EqualTo(5));
            Assert.That(squad.HighestLevel, Is.EqualTo(1));
            Assert.That(squad.IsDefeated, Is.False);
        }

        [Test]
        public void Constructor_AppliesCountCap()
        {
            var squad = new Squad(UnitType.Army, int.MaxValue);

            Assert.That(squad.Count, Is.EqualTo(Squad.MaxCount));
            Assert.That(squad.GetCount(1), Is.EqualTo(Squad.MaxCount));
        }

        [Test]
        public void Squad_RejectsInvalidCountsFactorsAndLevels()
        {
            var squad = new Squad(UnitType.Army, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new Squad(UnitType.Army, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.GetCount(GameRules.MinLevel - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.GetCount(GameRules.MaxLevel + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.Add(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.Multiply(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => squad.Remove(0));
        }

        [Test]
        public void Add_IncreasesLevelOneCountAndNotifiesOnce()
        {
            var squad = new Squad(UnitType.Army, 2);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Add(3);

            Assert.That(squad.Count, Is.EqualTo(5));
            Assert.That(squad.GetCount(1), Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Add_CapsCountAndDoesNotNotifyWhenAlreadyFull()
        {
            var squad = new Squad(UnitType.Army, 400);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Add(200);
            squad.Add(int.MaxValue);

            Assert.That(squad.Count, Is.EqualTo(500));
            Assert.That(squad.GetCount(1), Is.EqualTo(500));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Multiply_ScalesEachLevelAndNotifiesOnce()
        {
            var squad = new Squad(UnitType.Army, 2);
            squad.Merge();
            squad.Add(3);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Multiply(2);

            Assert.That(squad.Count, Is.EqualTo(8));
            Assert.That(squad.GetCount(1), Is.EqualTo(6));
            Assert.That(squad.GetCount(2), Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Multiply_RemovesOverflowFromLowestLevels()
        {
            var squad = new Squad(UnitType.Army, 400);
            squad.Merge();
            squad.Merge();
            squad.Add(300);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Multiply(2);

            Assert.That(squad.Count, Is.EqualTo(500));
            Assert.That(squad.GetCount(1), Is.EqualTo(300));
            Assert.That(squad.GetCount(3), Is.EqualTo(200));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Multiply_HandlesLargeFactorAndNotifiesOnlyWhenCountsChange()
        {
            var squad = new Squad(UnitType.Army, 400);
            squad.Merge();
            squad.Add(300);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Multiply(int.MaxValue);
            squad.Multiply(int.MaxValue);

            Assert.That(squad.Count, Is.EqualTo(500));
            Assert.That(squad.GetCount(1), Is.Zero);
            Assert.That(squad.GetCount(2), Is.EqualTo(500));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Remove_DrainsLowestLevelsAndStopsAtZero()
        {
            var squad = new Squad(UnitType.Army, 4);
            squad.Merge();
            squad.Add(3);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Remove(4);

            Assert.That(squad.Count, Is.EqualTo(1));
            Assert.That(squad.GetCount(1), Is.Zero);
            Assert.That(squad.GetCount(2), Is.EqualTo(1));
            Assert.That(squad.HighestLevel, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));

            squad.Remove(5);
            squad.Remove(1);
            squad.Multiply(2);

            Assert.That(squad.Count, Is.Zero);
            Assert.That(squad.IsDefeated, Is.True);
            Assert.That(squad.HighestLevel, Is.Zero);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void Merge_LeavesOddUnitAndDoesNotMergeNewPairs()
        {
            var squad = new Squad(UnitType.Army, 5);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Merge();

            Assert.That(squad.Count, Is.EqualTo(3));
            Assert.That(squad.GetCount(1), Is.EqualTo(1));
            Assert.That(squad.GetCount(2), Is.EqualTo(2));
            Assert.That(squad.HighestLevel, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Merge_DoesNotPairNewUnitsWithExistingHigherLevelUnit()
        {
            var squad = new Squad(UnitType.Army, 2);
            squad.Merge();
            squad.Add(4);

            squad.Merge();

            Assert.That(squad.Count, Is.EqualTo(3));
            Assert.That(squad.GetCount(1), Is.Zero);
            Assert.That(squad.GetCount(2), Is.EqualTo(3));
            Assert.That(squad.GetCount(3), Is.Zero);
        }

        [Test]
        public void Merge_DoesNotNotifyWithoutAnEligiblePair()
        {
            var squad = new Squad(UnitType.Army, 1);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Merge();

            Assert.That(squad.GetCount(1), Is.EqualTo(1));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Merge_LeavesUnitsAtMaxLevelUnchanged()
        {
            var squad = new Squad(UnitType.Army, 1);
            for (int level = GameRules.MinLevel; level < GameRules.MaxLevel; level++)
            {
                squad.Multiply(2);
                squad.Merge();
            }
            squad.Multiply(2);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.Merge();

            Assert.That(squad.Count, Is.EqualTo(2));
            Assert.That(squad.GetCount(GameRules.MaxLevel), Is.EqualTo(2));
            Assert.That(squad.HighestLevel, Is.EqualTo(GameRules.MaxLevel));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void ChangeType_NotifiesOnlyWhenTypeChanges()
        {
            var squad = new Squad(UnitType.Army, 1);
            int changes = 0;
            squad.Changed += () => changes++;

            squad.ChangeType(UnitType.AirForce);
            squad.ChangeType(UnitType.AirForce);

            Assert.That(squad.Type, Is.EqualTo(UnitType.AirForce));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Firepower_SumsAttacksByLevelAndAppliesMultiplier()
        {
            var squad = new Squad(UnitType.Army, 4);
            squad.Merge();
            squad.Merge();
            squad.Add(2);

            Assert.That(squad.Firepower(10f, 1.5f), Is.EqualTo(90f));
        }

        [Test]
        public void GateFactories_StoreKindValueAndType()
        {
            var add = Gate.Add(5);
            var multiply = Gate.Multiply(2);
            var subtract = Gate.Subtract(3);
            var merge = Gate.Merge();
            var changeType = Gate.ChangeType(UnitType.Navy);

            Assert.That(add.Kind, Is.EqualTo(GateKind.Add));
            Assert.That(add.Value, Is.EqualTo(5));
            Assert.That(multiply.Kind, Is.EqualTo(GateKind.Multiply));
            Assert.That(multiply.Value, Is.EqualTo(2));
            Assert.That(subtract.Kind, Is.EqualTo(GateKind.Subtract));
            Assert.That(subtract.Value, Is.EqualTo(3));
            Assert.That(merge.Kind, Is.EqualTo(GateKind.Merge));
            Assert.That(merge.Value, Is.Zero);
            Assert.That(changeType.Kind, Is.EqualTo(GateKind.ChangeType));
            Assert.That(changeType.Value, Is.Zero);
            Assert.That(changeType.UnitType, Is.EqualTo(UnitType.Navy));
        }

        [Test]
        public void GateFactories_RejectInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Add(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Multiply(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Subtract(0));
        }

        [Test]
        public void GateApplyTo_PerformsEachSquadOperation()
        {
            var squad = new Squad(UnitType.Army, 4);

            Gate.Add(1).ApplyTo(squad);
            Assert.That(squad.GetCount(1), Is.EqualTo(5));

            Gate.Multiply(2).ApplyTo(squad);
            Assert.That(squad.GetCount(1), Is.EqualTo(10));

            Gate.Subtract(2).ApplyTo(squad);
            Assert.That(squad.GetCount(1), Is.EqualTo(8));

            Gate.Merge().ApplyTo(squad);
            Assert.That(squad.GetCount(1), Is.Zero);
            Assert.That(squad.GetCount(2), Is.EqualTo(4));

            Gate.ChangeType(UnitType.Navy).ApplyTo(squad);
            Assert.That(squad.Type, Is.EqualTo(UnitType.Navy));
        }

        [Test]
        public void GateApplyTo_RejectsNullSquad()
        {
            Assert.Throws<ArgumentNullException>(() => Gate.Merge().ApplyTo(null));
        }
    }
}
