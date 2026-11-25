using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TopWar.Core.Progression;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class RunnerSimulationTests
    {
        [Test]
        public void Constructor_RejectsNullDependencies()
        {
            var squad = new Squad(UnitType.Army, 5);
            var track = CreateTrack();
            var settings = new RunnerSettings();

            Assert.Throws<ArgumentNullException>(() => new RunnerSimulation(null, track, settings));
            Assert.Throws<ArgumentNullException>(() => new RunnerSimulation(squad, null, settings));
            Assert.Throws<ArgumentNullException>(() => new RunnerSimulation(squad, track, null));

            var gates = Array.Empty<GateRow>();
            var enemies = Array.Empty<EnemyGroup>();
            var crates = Array.Empty<SupplyCrate>();
            Assert.Throws<ArgumentNullException>(() => new RunnerTrack(null, enemies, crates, track.Boss));
            Assert.Throws<ArgumentNullException>(() => new RunnerTrack(gates, null, crates, track.Boss));
            Assert.Throws<ArgumentNullException>(() => new RunnerTrack(gates, enemies, null, track.Boss));
            Assert.Throws<ArgumentNullException>(() => new RunnerTrack(gates, enemies, crates, null));
        }

        [Test]
        public void TrackTargets_RejectInvalidHealthAndCounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyGroup(4f, UnitType.Army, 0, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyGroup(4f, UnitType.Army, 1, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SupplyCrate(4f, true, 0f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SupplyCrate(4f, true, 10f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunnerBoss(0f, UnitType.Army, 100f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunnerBoss(20f, UnitType.Army, 0f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunnerBoss(20f, UnitType.Army, 100f, 0));
        }

        [Test]
        public void Track_KeepsSortedCopiesOfRowsEnemiesAndCrates()
        {
            var nearRow = new GateRow(5f, Gate.Add(1), Gate.Merge());
            var farRow = new GateRow(20f, Gate.Add(2), Gate.Merge());
            var nearEnemy = new EnemyGroup(8f, UnitType.Army, 1, 10f);
            var farEnemy = new EnemyGroup(22f, UnitType.Army, 1, 10f);
            var nearCrate = new SupplyCrate(7f, false, 10f, 1);
            var farCrate = new SupplyCrate(21f, true, 10f, 1);
            var gates = new[] { farRow, nearRow };
            var enemies = new[] { farEnemy, nearEnemy };
            var crates = new[] { farCrate, nearCrate };
            var track = CreateTrack(gates, enemies, crates);

            gates[1] = farRow;
            enemies[1] = farEnemy;
            crates[1] = farCrate;

            Assert.That(track.Gates, Is.EqualTo(new[] { nearRow, farRow }));
            Assert.That(track.Enemies, Is.EqualTo(new[] { nearEnemy, farEnemy }));
            Assert.That(track.Crates, Is.EqualTo(new[] { nearCrate, farCrate }));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_RejectsInvalidTime(float deltaTime)
        {
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 5), CreateTrack(), new RunnerSettings());

            Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Tick(deltaTime));
        }

        [Test]
        public void RunningTick_AdvancesAndReportsProgress()
        {
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 5), CreateTrack(), new RunnerSettings());

            Assert.That(simulation.State, Is.EqualTo(RunnerState.Running));
            Assert.That(simulation.Distance, Is.Zero);
            Assert.That(simulation.X, Is.Zero);
            Assert.That(simulation.Progress, Is.Zero);
            Assert.That(simulation.BossTimeRemaining, Is.EqualTo(20f));

            simulation.Tick(1f);

            Assert.That(simulation.Distance, Is.EqualTo(6f));
            Assert.That(simulation.Progress, Is.EqualTo(6f / simulation.Track.Length));
            Assert.That(simulation.BossTimeRemaining, Is.EqualTo(20f));
        }

        [TestCase(-5f, -2f, true, 7)]
        [TestCase(5f, 2f, false, 10)]
        [TestCase(0f, 0f, false, 10)]
        public void SetX_ClampsAndSelectsGateOnce(float x, float clampedX, bool left, int expectedCount)
        {
            var row = new GateRow(6f, Gate.Add(2), Gate.Multiply(2));
            var squad = new Squad(UnitType.Army, 5);
            var simulation = new RunnerSimulation(squad, CreateTrack(gates: new[] { row }), new RunnerSettings());
            int passes = 0;
            simulation.GatePassed += (passedRow, gate, isLeft) =>
            {
                Assert.That(passedRow, Is.SameAs(row));
                Assert.That(passedRow.Passed, Is.True);
                Assert.That(isLeft, Is.EqualTo(left));
                Assert.That(gate.Kind, Is.EqualTo(left ? GateKind.Add : GateKind.Multiply));
                Assert.That(gate.Value, Is.EqualTo(2));
                Assert.That(squad.Count, Is.EqualTo(expectedCount));
                passes++;
            };

            simulation.SetX(x);
            simulation.Tick(1f);
            simulation.Tick(1f);

            Assert.That(simulation.X, Is.EqualTo(clampedX));
            Assert.That(passes, Is.EqualTo(1));
            Assert.That(squad.Count, Is.EqualTo(expectedCount));
        }

        [Test]
        public void RunningTick_CrossesMultipleGatesInDistanceOrder()
        {
            var first = new GateRow(5f, Gate.Merge(), Gate.Add(3));
            var second = new GateRow(10f, Gate.Merge(), Gate.Multiply(2));
            var squad = new Squad(UnitType.Army, 5);
            var simulation = new RunnerSimulation(squad, CreateTrack(gates: new[] { second, first }), new RunnerSettings());
            var passes = new List<GateRow>();
            simulation.GatePassed += (row, gate, isLeft) => passes.Add(row);

            simulation.Tick(2f);

            Assert.That(passes, Is.EqualTo(new[] { first, second }));
            Assert.That(squad.Count, Is.EqualTo(16));
        }

        [Test]
        public void RunningTick_ResolvesReachedObjectsBeforeFiring()
        {
            var row = new GateRow(12f, Gate.Merge(), Gate.Add(3));
            var crate = new SupplyCrate(12f, false, 5f, 2);
            var blockingEnemy = new EnemyGroup(12f, UnitType.Army, 2, 10f);
            var nextEnemy = new EnemyGroup(20f, UnitType.Army, 10, 10f);
            var track = CreateTrack(new[] { row }, new[] { nextEnemy, blockingEnemy }, new[] { crate });
            var squad = new Squad(UnitType.Army, 2);
            var simulation = new RunnerSimulation(squad, track, new RunnerSettings { FireInterval = 2f });
            var actions = new List<string>();
            simulation.GatePassed += (gateRow, gate, isLeft) => actions.Add("gate");
            simulation.CrateMissed += target => actions.Add("miss");
            simulation.EnemyGroupCollided += (enemy, lost) => actions.Add("collision");
            simulation.EnemyGroupHit += (enemy, damage) => actions.Add("shot");

            simulation.Tick(2f);

            Assert.That(actions, Is.EqualTo(new[] { "gate", "miss", "collision", "shot" }));
            Assert.That(squad.Count, Is.EqualTo(3));
            Assert.That(nextEnemy.Hp, Is.EqualTo(40f));
        }

        [Test]
        public void Fire_ConsumesOutOfRangeShotsAndAppliesUpgradeAndCounter()
        {
            var enemy = new EnemyGroup(15f, UnitType.AirForce, 4, 10f);
            var track = CreateTrack(enemies: new[] { enemy });
            var settings = new RunnerSettings { ArmyAttackMultiplier = 2f };
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 4), track, settings);
            var hits = new List<float>();
            var destroyed = new List<EnemyGroup>();
            simulation.EnemyGroupHit += (target, damage) => hits.Add(damage);
            simulation.EnemyGroupDestroyed += destroyed.Add;

            simulation.Tick(0.25f);

            Assert.That(enemy.Hp, Is.EqualTo(40f));
            Assert.That(hits, Is.Empty);

            simulation.Tick(0.25f);

            Assert.That(hits, Is.EqualTo(new[] { 25f }));
            Assert.That(enemy.Hp, Is.EqualTo(15f));
            Assert.That(enemy.RemainingCount, Is.EqualTo(2));

            simulation.Tick(0.5f);

            Assert.That(hits, Is.EqualTo(new[] { 25f, 25f }));
            Assert.That(destroyed, Is.EqualTo(new[] { enemy }));
            Assert.That(enemy.Hp, Is.Zero);
            Assert.That(enemy.RemainingCount, Is.Zero);
            Assert.That(enemy.IsAlive, Is.False);
        }

        [TestCase(true, -1f, 0f)]
        [TestCase(false, 0f, -1f)]
        public void Fire_OpensCrateOnlyOnSameSideAndRewardsBeforeOpenEvent(bool isLeft, float sameX, float oppositeX)
        {
            var crate = new SupplyCrate(7f, isLeft, 15f, 4);
            var squad = new Squad(UnitType.Army, 4);
            var track = CreateTrack(crates: new[] { crate });
            var settings = new RunnerSettings { ArmyAttackMultiplier = 2f };
            var simulation = new RunnerSimulation(squad, track, settings);
            var hits = new List<float>();
            var opened = new List<SupplyCrate>();
            var missed = new List<SupplyCrate>();
            simulation.CrateHit += (target, damage) =>
            {
                Assert.That(target, Is.SameAs(crate));
                Assert.That(squad.Count, Is.EqualTo(4));
                hits.Add(damage);
            };
            simulation.CrateOpened += target =>
            {
                Assert.That(squad.GetCount(1), Is.EqualTo(8));
                opened.Add(target);
            };
            simulation.CrateMissed += missed.Add;

            simulation.SetX(oppositeX);
            simulation.Tick(0.25f);

            Assert.That(crate.Hp, Is.EqualTo(15f));
            Assert.That(hits, Is.Empty);

            simulation.SetX(sameX);
            simulation.Tick(0.25f);
            simulation.Tick(1f);

            Assert.That(hits, Is.EqualTo(new[] { 20f }));
            Assert.That(opened, Is.EqualTo(new[] { crate }));
            Assert.That(missed, Is.Empty);
            Assert.That(crate.IsOpen, Is.True);
            Assert.That(crate.IsActive, Is.False);
            Assert.That(squad.Count, Is.EqualTo(8));
        }

        [Test]
        public void PassingUnopenedCrate_MissesOnceWithoutChangingSquad()
        {
            var crate = new SupplyCrate(3f, false, 100f, 3);
            var squad = new Squad(UnitType.Army, 4);
            var simulation = new RunnerSimulation(squad, CreateTrack(crates: new[] { crate }), new RunnerSettings());
            var missed = new List<SupplyCrate>();
            simulation.CrateMissed += missed.Add;

            simulation.Tick(0.25f);
            simulation.Tick(0.25f);
            simulation.Tick(1f);

            Assert.That(missed, Is.EqualTo(new[] { crate }));
            Assert.That(crate.Hp, Is.EqualTo(90f));
            Assert.That(crate.IsMissed, Is.True);
            Assert.That(crate.IsOpen, Is.False);
            Assert.That(crate.IsActive, Is.False);
            Assert.That(squad.Count, Is.EqualTo(4));
        }

        [TestCase(9f, true)]
        [TestCase(10f, false)]
        [TestCase(11f, false)]
        public void Fire_SelectsNearestTargetAndPrefersEnemyOnTie(float crateDistance, bool shootsCrate)
        {
            var crate = new SupplyCrate(crateDistance, false, 50f, 2);
            var enemy = new EnemyGroup(10f, UnitType.Army, 5, 10f);
            var track = CreateTrack(enemies: new[] { enemy }, crates: new[] { crate });
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 4), track, new RunnerSettings());

            simulation.Tick(0.25f);

            Assert.That(crate.Hp, Is.EqualTo(shootsCrate ? 40f : 50f));
            Assert.That(enemy.Hp, Is.EqualTo(shootsCrate ? 50f : 40f));
        }

        [TestCase(5, 2, RunnerState.Running)]
        [TestCase(1, 1, RunnerState.Defeat)]
        public void Collision_RemovesUpToRemainingEnemyCount(int initialCount, int expectedLoss, RunnerState state)
        {
            var enemy = new EnemyGroup(6f, UnitType.Army, 3, 10f);
            var squad = new Squad(UnitType.Army, initialCount);
            var simulation = new RunnerSimulation(squad, CreateTrack(enemies: new[] { enemy }), new RunnerSettings());
            var losses = new List<int>();
            var outcomes = new List<bool>();
            simulation.EnemyGroupCollided += (target, lost) =>
            {
                Assert.That(target, Is.SameAs(enemy));
                Assert.That(target.Hp, Is.Zero);
                losses.Add(lost);
            };
            simulation.Finished += outcomes.Add;

            simulation.Tick(0.25f);
            simulation.Tick(0.75f);
            simulation.Tick(1f);

            Assert.That(losses, Is.EqualTo(new[] { expectedLoss }));
            Assert.That(squad.Count, Is.EqualTo(initialCount - expectedLoss));
            Assert.That(simulation.State, Is.EqualTo(state));
            Assert.That(outcomes, Is.EqualTo(state == RunnerState.Defeat ? new[] { false } : Array.Empty<bool>()));
            if (state == RunnerState.Defeat) Assert.That(simulation.Distance, Is.EqualTo(6f));
        }

        [Test]
        public void BossFight_StopsAtGapAndStartsTimerOnNextTick()
        {
            var boss = new RunnerBoss(20f, UnitType.Army, 100f, 3);
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 5), CreateTrack(boss: boss), new RunnerSettings());
            int starts = 0;
            simulation.BossFightStarted += () =>
            {
                Assert.That(simulation.State, Is.EqualTo(RunnerState.BossFight));
                starts++;
            };

            simulation.SetX(-1f);
            simulation.Tick(10f);

            Assert.That(simulation.Distance, Is.EqualTo(12f));
            Assert.That(simulation.Progress, Is.EqualTo(1f));
            Assert.That(simulation.BossTimeRemaining, Is.EqualTo(20f));
            Assert.That(boss.Hp, Is.EqualTo(100f));

            simulation.SetX(2f);
            simulation.Tick(0.125f);

            Assert.That(simulation.Distance, Is.EqualTo(12f));
            Assert.That(simulation.X, Is.EqualTo(-1f));
            Assert.That(simulation.BossTimeRemaining, Is.EqualTo(19.875f));
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(boss.Hp, Is.EqualTo(100f));
        }

        [Test]
        public void BossFight_KillTakesPriorityOverAttackAndTimeout()
        {
            var boss = new RunnerBoss(14f, UnitType.AirForce, 10f, 3);
            var squad = new Squad(UnitType.Army, 5);
            var settings = new RunnerSettings { BossTimeLimit = 2f };
            var simulation = new RunnerSimulation(squad, CreateTrack(boss: boss), settings);
            var hits = new List<float>();
            var attacks = new List<int>();
            var outcomes = new List<bool>();
            simulation.BossHit += hits.Add;
            simulation.BossAttacked += attacks.Add;
            simulation.Finished += outcomes.Add;

            simulation.Tick(1f);
            simulation.Tick(2f);
            simulation.Tick(10f);

            Assert.That(hits, Is.EqualTo(new[] { 15.625f }));
            Assert.That(attacks, Is.Empty);
            Assert.That(outcomes, Is.EqualTo(new[] { true }));
            Assert.That(boss.Hp, Is.Zero);
            Assert.That(squad.Count, Is.EqualTo(5));
            Assert.That(simulation.State, Is.EqualTo(RunnerState.Victory));
            Assert.That(simulation.Progress, Is.EqualTo(1f));
            Assert.That(simulation.BossTimeRemaining, Is.Zero);
        }

        [Test]
        public void BossFight_ExpiresWhenBossSurvivesTimeLimit()
        {
            var boss = new RunnerBoss(14f, UnitType.Army, 1000f, 3);
            var settings = new RunnerSettings { BossTimeLimit = 1f };
            var simulation = new RunnerSimulation(new Squad(UnitType.Army, 5), CreateTrack(boss: boss), settings);
            var outcomes = new List<bool>();
            simulation.Finished += outcomes.Add;

            simulation.Tick(1f);
            simulation.Tick(1f);
            simulation.Tick(1f);

            Assert.That(boss.Hp, Is.EqualTo(950f));
            Assert.That(simulation.BossTimeRemaining, Is.Zero);
            Assert.That(simulation.State, Is.EqualTo(RunnerState.Defeat));
            Assert.That(outcomes, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void BossFight_AttacksAtEachIntervalUntilSquadIsDefeated()
        {
            var boss = new RunnerBoss(14f, UnitType.Army, 1000f, 3);
            var squad = new Squad(UnitType.Army, 8);
            var settings = new RunnerSettings { BossAttackInterval = 1f };
            var simulation = new RunnerSimulation(squad, CreateTrack(boss: boss), settings);
            var attacks = new List<int>();
            var outcomes = new List<bool>();
            simulation.BossAttacked += attacks.Add;
            simulation.Finished += outcomes.Add;

            simulation.Tick(1f);
            simulation.Tick(0.75f);

            Assert.That(attacks, Is.Empty);

            simulation.Tick(1.75f);

            Assert.That(attacks, Is.EqualTo(new[] { 3, 3 }));
            Assert.That(squad.Count, Is.EqualTo(2));
            Assert.That(simulation.State, Is.EqualTo(RunnerState.BossFight));

            simulation.Tick(0.5f);

            Assert.That(attacks, Is.EqualTo(new[] { 3, 3, 2 }));
            Assert.That(squad.IsDefeated, Is.True);
            Assert.That(simulation.State, Is.EqualTo(RunnerState.Defeat));
            Assert.That(outcomes, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Generator_RepeatsTrackForSameStageAndSeed()
        {
            var first = RunnerTrackGenerator.Create(3, 42);
            var second = RunnerTrackGenerator.Create(3, 42);

            Assert.That(first.Gates.Select(row => (row.Distance, GateData(row.Left), GateData(row.Right))),
                Is.EqualTo(second.Gates.Select(row => (row.Distance, GateData(row.Left), GateData(row.Right)))));
            Assert.That(first.Enemies.Select(enemy => (enemy.Distance, enemy.Type, enemy.RemainingCount, enemy.HpPerEnemy)),
                Is.EqualTo(second.Enemies.Select(enemy => (enemy.Distance, enemy.Type, enemy.RemainingCount, enemy.HpPerEnemy))));
            Assert.That(first.Crates.Select(crate => (crate.Distance, crate.IsLeft, crate.RewardCount, crate.MaxHp)),
                Is.EqualTo(second.Crates.Select(crate => (crate.Distance, crate.IsLeft, crate.RewardCount, crate.MaxHp))));
            Assert.That((first.Boss.Distance, first.Boss.Type, first.Boss.MaxHp, first.Boss.KillsPerAttack),
                Is.EqualTo((second.Boss.Distance, second.Boss.Type, second.Boss.MaxHp, second.Boss.KillsPerAttack)));
        }

        [Test]
        public void Generator_UsesFixedPositionsAndScalesHealthWithStage()
        {
            var firstStage = RunnerTrackGenerator.Create(1, 42);
            var laterStage = RunnerTrackGenerator.Create(3, 42);
            float scale = StageRules.EnemyHpScale(3);

            Assert.That(laterStage.Gates.Select(row => row.Distance), Is.EqualTo(new[] { 20f, 40f, 80f, 100f, 140f }));
            Assert.That(laterStage.Gates.All(row => row.Left.Kind != row.Right.Kind), Is.True);
            Assert.That(laterStage.Enemies.Select(enemy => enemy.Distance), Is.EqualTo(new[] { 60f, 120f, 160f }));
            Assert.That(laterStage.Enemies.Select(enemy => enemy.RemainingCount), Is.EqualTo(new[] { 10, 20, 30 }));
            Assert.That(laterStage.Crates.Select(crate => crate.Distance), Is.EqualTo(new[] { 30f, 50f, 70f, 90f, 110f, 130f, 150f }));
            Assert.That(laterStage.Length, Is.EqualTo(200f));
            Assert.That(laterStage.Boss.KillsPerAttack, Is.EqualTo(3));
            Assert.That(laterStage.Boss.MaxHp, Is.EqualTo(2000f * scale));
            Assert.That(laterStage.Boss.MaxHp, Is.GreaterThan(firstStage.Boss.MaxHp));

            for (int i = 0; i < laterStage.Enemies.Count; i++)
            {
                Assert.That(laterStage.Enemies[i].HpPerEnemy, Is.EqualTo(10f * scale));
                Assert.That(laterStage.Enemies[i].MaxHp, Is.GreaterThan(firstStage.Enemies[i].MaxHp));
            }

            for (int i = 0; i < laterStage.Crates.Count; i++)
            {
                Assert.That(laterStage.Crates[i].MaxHp, Is.EqualTo(20f * (i + 1) * scale));
                Assert.That(laterStage.Crates[i].RewardCount, Is.InRange(2 + i, 5 + i));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => RunnerTrackGenerator.Create(0, 42));
        }

        private static RunnerTrack CreateTrack(GateRow[] gates = null, EnemyGroup[] enemies = null,
            SupplyCrate[] crates = null, RunnerBoss boss = null) => new RunnerTrack(
            gates ?? Array.Empty<GateRow>(), enemies ?? Array.Empty<EnemyGroup>(),
            crates ?? Array.Empty<SupplyCrate>(), boss ?? new RunnerBoss(100f, UnitType.Army, 1000f, 3));

        private static (GateKind Kind, int Value, UnitType Type) GateData(Gate gate) =>
            (gate.Kind, gate.Value, gate.UnitType);
    }
}
