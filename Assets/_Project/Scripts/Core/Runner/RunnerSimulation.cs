using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public sealed class RunnerSimulation
    {
        private readonly RunnerSettings _settings;
        private double _fireElapsed;
        private double _bossAttackElapsed;
        private bool _isTicking;

        public RunnerSimulation(Squad squad, RunnerTrack track, RunnerSettings settings)
        {
            Squad = squad ?? throw new ArgumentNullException(nameof(squad));
            Track = track ?? throw new ArgumentNullException(nameof(track));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            State = RunnerState.Running;
            BossTimeRemaining = _settings.BossTimeLimit;
        }

        public Squad Squad { get; }
        public RunnerTrack Track { get; }
        public RunnerState State { get; private set; }
        public float Distance { get; private set; }
        public float X { get; private set; }
        public float Progress => State == RunnerState.BossFight || State == RunnerState.Victory
            ? 1f
            : Math.Clamp(Distance / Track.Length, 0f, 1f);
        public float BossTimeRemaining { get; private set; }

        public event Action<GateRow, Gate, bool> GatePassed;
        public event Action<EnemyGroup, float> EnemyGroupHit;
        public event Action<EnemyGroup> EnemyGroupDestroyed;
        public event Action<EnemyGroup, int> EnemyGroupCollided;
        public event Action<SupplyCrate, float> CrateHit;
        public event Action<SupplyCrate> CrateOpened;
        public event Action<SupplyCrate> CrateMissed;
        public event Action BossFightStarted;
        public event Action<float> BossHit;
        public event Action<int> BossAttacked;
        public event Action<bool> Finished;

        public void SetX(float x)
        {
            if (State == RunnerState.Running)
                X = Math.Clamp(x, -_settings.HalfWidth, _settings.HalfWidth);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            // 이벤트에서 재진입해도 진행 중인 처리 순서를 유지한다.
            if (_isTicking || State == RunnerState.Victory || State == RunnerState.Defeat) return;

            _isTicking = true;
            try
            {
                if (State == RunnerState.Running)
                    TickRunning(deltaTime);
                else
                    TickBossFight(deltaTime);
            }
            finally
            {
                _isTicking = false;
            }
        }

        private void TickRunning(float deltaTime)
        {
            float stopDistance = Math.Max(0f, Track.Length - _settings.BossStopGap);
            Distance = Math.Min(Distance + _settings.ForwardSpeed * deltaTime, stopDistance);

            PassReachedGates();
            MissReachedCrates();
            CollideWithReachedEnemies();
            Fire(deltaTime);

            if (Distance >= stopDistance)
            {
                State = RunnerState.BossFight;
                BossFightStarted?.Invoke();
            }

            if (Squad.IsDefeated) Finish(false);
        }

        private void PassReachedGates()
        {
            for (int i = 0; i < Track.Gates.Count; i++)
            {
                GateRow row = Track.Gates[i];
                if (row.Distance > Distance) break;
                if (row.Passed) continue;

                bool isLeft = X < 0f;
                Gate gate = isLeft ? row.Left : row.Right;
                gate.ApplyTo(Squad);
                row.Passed = true;
                GatePassed?.Invoke(row, gate, isLeft);
            }
        }

        private void MissReachedCrates()
        {
            for (int i = 0; i < Track.Crates.Count; i++)
            {
                SupplyCrate crate = Track.Crates[i];
                if (crate.Distance > Distance) break;
                if (!crate.IsActive) continue;

                crate.Miss();
                CrateMissed?.Invoke(crate);
            }
        }

        private void CollideWithReachedEnemies()
        {
            for (int i = 0; i < Track.Enemies.Count && !Squad.IsDefeated; i++)
            {
                EnemyGroup enemy = Track.Enemies[i];
                if (enemy.Distance > Distance) break;
                if (!enemy.IsAlive) continue;

                int lost = Math.Min(enemy.RemainingCount, Squad.Count);
                if (lost == 0) continue;

                Squad.Remove(lost);
                enemy.Clear();
                EnemyGroupCollided?.Invoke(enemy, lost);
            }
        }

        private void TickBossFight(float deltaTime)
        {
            BossTimeRemaining -= deltaTime;
            Fire(deltaTime);
            if (State == RunnerState.Victory) return;

            float interval = _settings.BossAttackInterval;
            if (interval > 0f)
            {
                _bossAttackElapsed += deltaTime;
                while (_bossAttackElapsed >= interval && !Squad.IsDefeated)
                {
                    _bossAttackElapsed -= interval;
                    int lost = Math.Min(Track.Boss.KillsPerAttack, Squad.Count);
                    Squad.Remove(lost);
                    BossAttacked?.Invoke(lost);
                }
            }

            if (BossTimeRemaining <= 0f || Squad.IsDefeated) Finish(false);
        }

        private void Fire(float deltaTime)
        {
            float interval = _settings.FireInterval;
            if (!(interval > 0f)) return;

            _fireElapsed += deltaTime;
            while (_fireElapsed >= interval && !Squad.IsDefeated)
            {
                _fireElapsed -= interval;
                if (State == RunnerState.BossFight)
                {
                    FireAtBoss(interval);
                    if (State == RunnerState.Victory) return;
                }
                else if (!FireAtTrackTarget(interval))
                {
                    // 대상이 없는 동안 채운 사격 간격은 소모한다.
                    _fireElapsed %= interval;
                    return;
                }
            }
        }

        private bool FireAtTrackTarget(float interval)
        {
            EnemyGroup enemy = FindEnemyInRange();
            SupplyCrate crate = FindCrateInRange(enemy);
            if (crate != null)
            {
                float damage = ShotDamage(interval);
                crate.TakeDamage(damage);
                CrateHit?.Invoke(crate, damage);
                if (crate.IsOpen)
                {
                    Squad.Add(crate.RewardCount);
                    CrateOpened?.Invoke(crate);
                }

                return true;
            }

            if (enemy == null) return false;

            float enemyDamage = ShotDamage(interval) * CounterTable.DamageMultiplier(Squad.Type, enemy.Type);
            enemy.TakeDamage(enemyDamage);
            EnemyGroupHit?.Invoke(enemy, enemyDamage);
            if (!enemy.IsAlive) EnemyGroupDestroyed?.Invoke(enemy);
            return true;
        }

        private EnemyGroup FindEnemyInRange()
        {
            for (int i = 0; i < Track.Enemies.Count; i++)
            {
                EnemyGroup enemy = Track.Enemies[i];
                float gap = enemy.Distance - Distance;
                if (gap > _settings.FireRange) break;
                if (gap > 0f && enemy.IsAlive) return enemy;
            }

            return null;
        }

        private SupplyCrate FindCrateInRange(EnemyGroup enemy)
        {
            bool isLeft = X < 0f;
            for (int i = 0; i < Track.Crates.Count; i++)
            {
                SupplyCrate crate = Track.Crates[i];
                float gap = crate.Distance - Distance;
                if (gap > _settings.FireRange || (enemy != null && crate.Distance >= enemy.Distance)) break;
                if (gap > 0f && crate.IsActive && crate.IsLeft == isLeft) return crate;
            }

            return null;
        }

        private void FireAtBoss(float interval)
        {
            RunnerBoss boss = Track.Boss;
            float damage = ShotDamage(interval) * CounterTable.DamageMultiplier(Squad.Type, boss.Type);
            boss.TakeDamage(damage);
            BossHit?.Invoke(damage);
            if (!boss.IsAlive) Finish(true);
        }

        private float ShotDamage(float interval) =>
            Squad.Firepower(_settings.UnitBaseAttack, _settings.GetUnitAttackMultiplier(Squad.Type)) * interval;

        private void Finish(bool victory)
        {
            State = victory ? RunnerState.Victory : RunnerState.Defeat;
            Finished?.Invoke(victory);
        }
    }
}
