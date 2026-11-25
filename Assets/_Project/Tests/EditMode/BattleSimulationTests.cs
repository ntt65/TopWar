using System;
using System.Collections.Generic;
using NUnit.Framework;
using TopWar.Core.Economy;
using TopWar.Core.Grid;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public sealed class BattleSimulationTests
    {
        private GridModel _grid;
        private Wallet _wallet;
        private BaseHealth _health;
        private BattleSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _grid = new GridModel(2, 2);
            _wallet = new Wallet();
            _health = new BaseHealth(100);
            _settings = new BattleSettings
            {
                BreakDuration = 2f,
                SpawnDistance = 1f,
                EnemyCountBase = 1,
                EnemyCountPerWave = 0,
                SpawnIntervalStart = 1f,
            };
        }

        [Test]
        public void Break_StartsNextWaveWithoutProcessingSpawnsInSameTick()
        {
            var battle = CreateBattle();
            var started = new List<int>();
            battle.WaveStarted += started.Add;

            Assert.That(battle.State, Is.EqualTo(BattleState.Break));
            Assert.That(battle.BreakRemaining, Is.EqualTo(2f));
            Assert.That(battle.WaveNumber, Is.Zero);
            battle.Tick(1f);
            Assert.That(battle.BreakRemaining, Is.EqualTo(1f));
            battle.Tick(2f);

            Assert.That(battle.State, Is.EqualTo(BattleState.InWave));
            Assert.That(battle.BreakRemaining, Is.Zero);
            Assert.That(battle.WaveNumber, Is.EqualTo(1));
            Assert.That(started, Is.EqualTo(new[] { 1 }));
            Assert.That(battle.Enemies, Is.Empty);
        }

        [Test]
        public void Spawn_UsesPlanOrderAndStartsAtSpawnY()
        {
            _settings.EnemyCountBase = 3;
            var battle = StartBattle();
            var spawned = new List<Enemy>();
            var initialY = new List<float>();
            battle.EnemySpawned += enemy =>
            {
                spawned.Add(enemy);
                initialY.Add(enemy.Y);
            };

            battle.Tick(0.75f);
            Assert.That(spawned.Count, Is.EqualTo(1));
            battle.Tick(0.25f);
            Assert.That(spawned.Count, Is.EqualTo(2));
            battle.Tick(1f);

            Assert.That(spawned.Count, Is.EqualTo(3));
            Assert.That(initialY, Is.EqualTo(new[] { 2f, 2f, 2f }));
            Assert.That(battle.Enemies, Is.EqualTo(spawned));
            Assert.That(spawned[0].Id, Is.LessThan(spawned[1].Id));
            Assert.That(spawned[1].Id, Is.LessThan(spawned[2].Id));
        }

        [Test]
        public void Movement_ReachesBaseAndAppliesDamageWithoutKillReward()
        {
            var battle = StartBattle();
            Enemy reached = null;
            battle.EnemyReachedBase += enemy => reached = enemy;

            battle.Tick(1f);
            var spawned = battle.Enemies[0];
            Assert.That(spawned.Y, Is.EqualTo(battle.SpawnY - spawned.Stats.Speed));
            battle.Tick(10f);

            Assert.That(reached, Is.SameAs(spawned));
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(_health.Current, Is.EqualTo(100 - spawned.Stats.BaseDamage));
            Assert.That(_wallet.Gold, Is.EqualTo(25));
        }

        [Test]
        public void Attack_WaitsForRangeThenAppliesTypeAdvantage()
        {
            _settings.SpawnDistance = 3f;
            var pos = new GridPos(0, 0);
            _grid.TryPlace(pos, new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.AirForce, 0));
            var damages = new List<float>();
            battle.UnitFired += (cell, enemy, damage) =>
            {
                Assert.That(cell, Is.EqualTo(pos));
                Assert.That(enemy.Hp, Is.EqualTo(7.5f));
                damages.Add(damage);
            };

            battle.Tick(1f);
            Assert.That(damages, Is.Empty);
            battle.Tick(0.25f);

            Assert.That(damages, Is.EqualTo(new[] { 12.5f }));
        }

        [Test]
        public void Attack_IncludesRangeBoundaryAndExcludesDiagonalBeyondRange()
        {
            _settings.SpawnDistance = 2f;
            _settings.EnemyCountBase = 2;
            _settings.SpawnIntervalStart = 0f;
            _settings.SpawnIntervalMin = 0f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.Army, 1), (UnitType.Army, 0));
            Enemy target = null;
            battle.UnitFired += (_, enemy, _) => target = enemy;

            battle.Tick(0f);

            Assert.That(target, Is.SameAs(battle.Enemies[1]));
            Assert.That(battle.Enemies[0].Hp, Is.EqualTo(30f));
        }

        [Test]
        public void Attack_OrdersUnitsByRowThenColumn()
        {
            _settings.UnitBaseAttack = 1f;
            var unit = new UnitData(UnitType.Army, 1);
            var upperLeft = new GridPos(0, 1);
            var lowerRight = new GridPos(1, 0);
            var lowerLeft = new GridPos(0, 0);
            _grid.TryPlace(upperLeft, unit);
            _grid.TryPlace(lowerRight, unit);
            _grid.TryPlace(lowerLeft, unit);
            var battle = StartBattle((UnitType.Navy, 0));
            var firedFrom = new List<GridPos>();
            battle.UnitFired += (pos, _, _) => firedFrom.Add(pos);

            battle.Tick(0f);

            Assert.That(firedFrom, Is.EqualTo(new[] { lowerLeft, lowerRight, upperLeft }));
        }

        [Test]
        public void Attack_PicksLowestYEvenWhenItSpawnsLater()
        {
            _settings.EnemyCountBase = 2;
            var battle = StartBattle((UnitType.Navy, 0), (UnitType.AirForce, 0));
            Enemy target = null;
            battle.UnitFired += (_, enemy, _) => target = enemy;

            battle.Tick(0f);
            battle.Tick(1f);
            var first = battle.Enemies[0];
            var second = battle.Enemies[1];
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.Army, 1));
            battle.Tick(1f);

            Assert.That(second.Y, Is.LessThan(first.Y));
            Assert.That(target, Is.SameAs(second));
        }

        [Test]
        public void Attack_TiesFavorEarlierIdAndCooldownDelaysNextShot()
        {
            _settings.EnemyCountBase = 2;
            _settings.SpawnIntervalStart = 0f;
            _settings.SpawnIntervalMin = 0f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.Army, 0), (UnitType.Army, 0));
            var targets = new List<Enemy>();
            battle.UnitFired += (_, enemy, _) => targets.Add(enemy);

            battle.Tick(0f);
            battle.Tick(0.25f);
            Assert.That(targets.Count, Is.EqualTo(1));
            battle.Tick(0.75f);

            Assert.That(targets, Is.EqualTo(new[] { battle.Enemies[0], battle.Enemies[0] }));
        }

        [TestCase(1f)]
        [TestCase(2f)]
        public void AirForceAttack_AppliesSplashWithEachEnemysTypeMultiplier(float attackMultiplier)
        {
            _settings.EnemyCountBase = 3;
            _settings.SpawnIntervalStart = 0f;
            _settings.SpawnIntervalMin = 0f;
            _settings.AirForceAttackMultiplier = attackMultiplier;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.AirForce, 1));
            var battle = StartBattle((UnitType.Army, 0), (UnitType.Navy, 0), (UnitType.Navy, 1));

            battle.Tick(0f);

            Assert.That(battle.Enemies[0].Hp, Is.EqualTo(30f - 4.5f * attackMultiplier * 0.85f).Within(0.0001f));
            Assert.That(battle.Enemies[1].Hp, Is.EqualTo(45f - 4.5f * attackMultiplier * 1.25f).Within(0.0001f));
            Assert.That(battle.Enemies[2].Hp, Is.EqualTo(45f));
        }

        [TestCase(UnitType.Army, 20f)]
        [TestCase(UnitType.AirForce, 4.5f)]
        [TestCase(UnitType.Navy, 26f)]
        public void Attack_AppliesArmyUpgradeOnlyToArmy(UnitType type, float expectedDamage)
        {
            _settings.ArmyAttackMultiplier = 2f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(type, 1));
            var battle = StartBattle((type, 0));

            battle.Tick(0f);

            var enemy = battle.Enemies[0];
            Assert.That(enemy.Hp, Is.EqualTo(enemy.Stats.MaxHp - expectedDamage).Within(0.0001f));
        }

        [Test]
        public void SplashKill_AwardsEachRewardAndLeavesEnemiesOutsideRadius()
        {
            _settings.EnemyCountBase = 3;
            _settings.SpawnIntervalStart = 0f;
            _settings.SpawnIntervalMin = 0f;
            _settings.UnitBaseAttack = 100f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.AirForce, 1));
            var battle = StartBattle((UnitType.Army, 0), (UnitType.Navy, 0), (UnitType.Navy, 1));
            var killed = new List<int>();
            battle.EnemyKilled += enemy => killed.Add(enemy.Id);

            battle.Tick(0f);

            Assert.That(killed.Count, Is.EqualTo(2));
            Assert.That(killed[0], Is.LessThan(killed[1]));
            Assert.That(_wallet.Gold, Is.EqualTo(12));
            Assert.That(battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(battle.Enemies[0].Lane, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(BattleState.InWave));
        }

        [Test]
        public void Kill_AwardsRewardAndClearsWaveAfterLastSpawn()
        {
            _settings.EnemyCountBase = 2;
            _settings.UnitBaseAttack = 100f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.Navy, 0), (UnitType.Navy, 0));
            var killed = new List<Enemy>();
            int clearedWave = 0;
            int clearBonus = 0;
            battle.EnemyKilled += killed.Add;
            battle.WaveCleared += (wave, bonus) =>
            {
                clearedWave = wave;
                clearBonus = bonus;
            };

            battle.Tick(0f);
            Assert.That(battle.State, Is.EqualTo(BattleState.InWave));
            Assert.That(_wallet.Gold, Is.EqualTo(7));
            battle.Tick(1f);

            Assert.That(killed.Count, Is.EqualTo(2));
            Assert.That(killed[0].IsAlive, Is.False);
            Assert.That(killed[1].IsAlive, Is.False);
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(clearedWave, Is.EqualTo(1));
            Assert.That(clearBonus, Is.EqualTo(25));
            Assert.That(_wallet.Gold, Is.EqualTo(39));
            Assert.That(battle.State, Is.EqualTo(BattleState.Break));
            Assert.That(battle.BreakRemaining, Is.EqualTo(2f));
        }

        [Test]
        public void WaveLimit_AwardsFinalBonusAndRaisesVictoryAfterWaveCleared()
        {
            _settings.WavesPerStage = 2;
            _settings.UnitBaseAttack = 100f;
            _grid.TryPlace(new GridPos(0, 0), new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.Army, 0), (UnitType.Army, 0));
            var cleared = new List<int>();
            int stageClears = 0;
            battle.WaveCleared += (wave, _) =>
            {
                Assert.That(battle.State, Is.EqualTo(BattleState.InWave));
                Assert.That(battle.ClearedWaves, Is.EqualTo(wave));
                cleared.Add(wave);
            };
            battle.StageCleared += () =>
            {
                Assert.That(battle.State, Is.EqualTo(BattleState.Victory));
                Assert.That(cleared, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(_wallet.Gold, Is.EqualTo(65));
                stageClears++;
            };

            Assert.That(battle.ClearedWaves, Is.Zero);
            battle.Tick(0f);
            Assert.That(battle.State, Is.EqualTo(BattleState.Break));
            Assert.That(battle.ClearedWaves, Is.EqualTo(1));
            Assert.That(stageClears, Is.Zero);

            battle.Tick(_settings.BreakDuration);
            Assert.That(battle.WaveNumber, Is.EqualTo(2));
            Assert.That(battle.ClearedWaves, Is.EqualTo(1));
            battle.Tick(1f);
            battle.Tick(100f);

            Assert.That(battle.State, Is.EqualTo(BattleState.Victory));
            Assert.That(battle.WaveNumber, Is.EqualTo(2));
            Assert.That(battle.ClearedWaves, Is.EqualTo(2));
            Assert.That(battle.BreakRemaining, Is.Zero);
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(_wallet.Gold, Is.EqualTo(65));
            Assert.That(stageClears, Is.EqualTo(1));
        }

        [Test]
        public void DefaultWaveLimit_ContinuesBeyondTenClearedWaves()
        {
            _settings.EnemyCountBase = 0;
            _settings.BossEvery = 100;
            var battle = CreateBattle();
            int stageClears = 0;
            battle.StageCleared += () => stageClears++;

            for (int wave = 1; wave <= 11; wave++)
            {
                battle.Tick(_settings.BreakDuration);
                Assert.That(battle.WaveNumber, Is.EqualTo(wave));
                battle.Tick(0f);

                Assert.That(battle.ClearedWaves, Is.EqualTo(wave));
                Assert.That(battle.State, Is.EqualTo(BattleState.Break));
            }

            Assert.That(stageClears, Is.Zero);
        }

        [Test]
        public void BossTimeout_RemovesBossAndDealsHalfOfMaxHealth()
        {
            _settings.BossEvery = 1;
            _settings.BossSpeed = 0f;
            _settings.BossTimeLimit = 2f;
            _health.TakeDamage(10);
            var battle = StartBattle();
            Enemy timedOut = null;
            battle.BossTimedOut += enemy => timedOut = enemy;

            Assert.That(battle.IsBossWave, Is.True);
            Assert.That(battle.BossTimeRemaining, Is.Null);
            battle.Tick(1f);
            var boss = battle.Enemies[0];
            Assert.That(battle.BossTimeRemaining, Is.EqualTo(1f));
            battle.Tick(1f);

            Assert.That(timedOut, Is.SameAs(boss));
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(battle.BossTimeRemaining, Is.Null);
            Assert.That(_health.Current, Is.EqualTo(40));
            Assert.That(_wallet.Gold, Is.EqualTo(25));
        }

        [Test]
        public void BossKill_ClearsTimerAndAwardsBothRewards()
        {
            _settings.BossEvery = 1;
            _settings.UnitBaseAttack = 1000f;
            _grid.TryPlace(new GridPos(1, 0), new UnitData(UnitType.Army, 1));
            var battle = StartBattle();
            Enemy killed = null;
            battle.EnemyKilled += enemy => killed = enemy;

            battle.Tick(0f);

            Assert.That(killed, Is.Not.Null);
            Assert.That(killed.Stats.IsBoss, Is.True);
            Assert.That(battle.BossTimeRemaining, Is.Null);
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(_health.Current, Is.EqualTo(100));
            Assert.That(_wallet.Gold, Is.EqualTo(75));
        }

        [Test]
        public void BossBaseArrival_TakesPriorityOverTimeout()
        {
            _settings.BossEvery = 1;
            _settings.BossSpeed = 3f;
            _settings.BossTimeLimit = 1f;
            var battle = StartBattle();
            int reached = 0;
            int timedOut = 0;
            battle.EnemyReachedBase += _ => reached++;
            battle.BossTimedOut += _ => timedOut++;

            battle.Tick(1f);

            Assert.That(reached, Is.EqualTo(1));
            Assert.That(timedOut, Is.Zero);
            Assert.That(_health.Current, Is.EqualTo(50));
            Assert.That(battle.BossTimeRemaining, Is.Null);
        }

        [Test]
        public void GameOver_FiresOnceAndStopsFurtherTicks()
        {
            _settings.BossEvery = 1;
            _settings.BossTimeLimit = 1f;
            _settings.BossBaseDamagePercent = 1f;
            var battle = StartBattle();
            int ended = 0;
            battle.GameOver += () => ended++;

            battle.Tick(1f);
            battle.Tick(100f);

            Assert.That(battle.State, Is.EqualTo(BattleState.GameOver));
            Assert.That(battle.WaveNumber, Is.EqualTo(1));
            Assert.That(battle.ClearedWaves, Is.Zero);
            Assert.That(_health.Current, Is.Zero);
            Assert.That(_wallet.Gold, Is.Zero);
            Assert.That(battle.Enemies, Is.Empty);
            Assert.That(ended, Is.EqualTo(1));
        }

        [Test]
        public void Move_ClearsCooldownWhenOriginalCellIsEmpty()
        {
            var original = new GridPos(0, 0);
            var destination = new GridPos(0, 1);
            _grid.TryPlace(original, new UnitData(UnitType.Army, 1));
            var battle = StartBattle((UnitType.Navy, 0));
            var firedFrom = new List<GridPos>();
            battle.UnitFired += (pos, _, _) => firedFrom.Add(pos);

            battle.Tick(0f);
            _grid.Move(original, destination);
            battle.Tick(0f);
            _grid.Move(destination, original);
            battle.Tick(0f);

            Assert.That(firedFrom, Is.EqualTo(new[] { original, destination, original }));
        }

        [Test]
        public void Attack_CallbackCanMoveAnotherUnitAndCallTick()
        {
            var first = new GridPos(0, 0);
            var second = new GridPos(1, 0);
            var unit = new UnitData(UnitType.Army, 1);
            _grid.TryPlace(second, unit);
            _grid.TryPlace(first, unit);
            var battle = StartBattle((UnitType.Navy, 0));
            var firedFrom = new List<GridPos>();
            battle.UnitFired += (pos, _, _) =>
            {
                firedFrom.Add(pos);
                if (pos == first)
                {
                    _grid.Move(second, new GridPos(1, 1));
                    battle.Tick(10f);
                }
            };

            battle.Tick(0f);

            Assert.That(firedFrom, Is.EqualTo(new[] { first }));
            Assert.That(battle.Enemies[0].Y, Is.EqualTo(battle.SpawnY));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_RejectsInvalidDeltaTimeWithoutChangingState(float deltaTime)
        {
            var battle = StartBattle();
            battle.Tick(0f);
            var enemy = battle.Enemies[0];
            float initialY = enemy.Y;

            Assert.Throws<ArgumentOutOfRangeException>(() => battle.Tick(deltaTime));

            Assert.That(battle.State, Is.EqualTo(BattleState.InWave));
            Assert.That(enemy.Y, Is.EqualTo(initialY));
            Assert.That(_health.Current, Is.EqualTo(100));
        }

        private BattleSimulation CreateBattle(Random random = null) =>
            new BattleSimulation(_grid, _wallet, _health, new WaveGenerator(_settings, random ?? new Random(42)),
                _settings);

        private BattleSimulation StartBattle()
        {
            var battle = CreateBattle();
            battle.Tick(_settings.BreakDuration);
            return battle;
        }

        private BattleSimulation StartBattle(params (UnitType Type, int Lane)[] choices)
        {
            var battle = CreateBattle(new SpawnRandom(choices));
            battle.Tick(_settings.BreakDuration);
            return battle;
        }

        private sealed class SpawnRandom : Random
        {
            private readonly (UnitType Type, int Lane)[] _choices;
            private int _index;

            public SpawnRandom((UnitType Type, int Lane)[] choices) : base(42)
            {
                _choices = choices;
            }

            public override int Next(int maxValue)
            {
                int index = _index++;
                var choice = _choices[index / 2];
                return index % 2 == 0 ? (int)choice.Type : choice.Lane;
            }
        }
    }
}
