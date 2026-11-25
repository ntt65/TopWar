using System;
using System.Collections.Generic;
using TopWar.Core.Economy;
using TopWar.Core.Grid;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public sealed class BattleSimulation
    {
        private readonly GridModel _grid;
        private readonly Wallet _wallet;
        private readonly BaseHealth _baseHealth;
        private readonly WaveGenerator _waveGenerator;
        private readonly BattleSettings _settings;
        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<Enemy> _resolvedEnemies = new List<Enemy>();
        private readonly Dictionary<GridPos, float> _cooldowns = new Dictionary<GridPos, float>();
        private readonly List<GridPos> _emptyCooldownCells = new List<GridPos>();
        private readonly List<GridPos> _unitCells = new List<GridPos>();

        private WavePlan _wave;
        private float _waveElapsed;
        private float _breakRemaining;
        private float _bossTimeRemaining;
        private Enemy _boss;
        private int _nextSpawn;
        private int _nextId;
        private bool _isTicking;

        public BattleSimulation(GridModel grid, Wallet wallet, BaseHealth baseHealth,
            WaveGenerator waveGenerator, BattleSettings settings)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _baseHealth = baseHealth ?? throw new ArgumentNullException(nameof(baseHealth));
            _waveGenerator = waveGenerator ?? throw new ArgumentNullException(nameof(waveGenerator));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            State = BattleState.Break;
            _breakRemaining = _settings.BreakDuration;
            Enemies = _enemies.AsReadOnly();
        }

        public BattleState State { get; private set; }
        public int WaveNumber { get; private set; }
        public bool IsBossWave => _wave != null && _wave.IsBoss;
        public float BreakRemaining => State == BattleState.Break ? _breakRemaining : 0f;
        public float? BossTimeRemaining => _boss != null && _boss.IsAlive ? _bossTimeRemaining : (float?)null;
        public IReadOnlyList<Enemy> Enemies { get; }
        public float SpawnY => _grid.Height - 1 + _settings.SpawnDistance;

        public event Action<int> WaveStarted;
        public event Action<int, int> WaveCleared;
        public event Action<Enemy> EnemySpawned;
        public event Action<Enemy> EnemyKilled;
        public event Action<Enemy> EnemyReachedBase;
        public event Action<Enemy> BossTimedOut;
        public event Action<GridPos, Enemy, float> UnitFired;
        public event Action GameOver;

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            // 이벤트에서 Tick을 호출해도 진행 중인 처리 순서와 재사용 목록을 유지한다.
            if (State == BattleState.GameOver || _isTicking) return;

            _isTicking = true;
            try
            {
                RemoveEmptyCooldowns();
                if (State == BattleState.Break)
                {
                    _breakRemaining -= deltaTime;
                    if (_breakRemaining <= 0f)
                        StartWave();
                    return;
                }

                SpawnDueEnemies(deltaTime);
                MoveEnemiesToBase(deltaTime);
                UpdateBossTimer(deltaTime);

                if (_baseHealth.IsDestroyed)
                {
                    State = BattleState.GameOver;
                    GameOver?.Invoke();
                    return;
                }

                FireUnits(deltaTime);

                if (_nextSpawn == _wave.Spawns.Count && _enemies.Count == 0)
                    ClearWave();
            }
            finally
            {
                _isTicking = false;
            }
        }

        private void StartWave()
        {
            WaveNumber++;
            _wave = _waveGenerator.Create(WaveNumber, _grid.Width);
            _waveElapsed = 0f;
            _nextSpawn = 0;
            State = BattleState.InWave;
            WaveStarted?.Invoke(WaveNumber);
        }

        private void ClearWave()
        {
            int bonus = _settings.WaveClearBonusBase + _settings.WaveClearBonusPerWave * WaveNumber;
            _wallet.Add(bonus);
            WaveCleared?.Invoke(WaveNumber, bonus);
            State = BattleState.Break;
            _breakRemaining = _settings.BreakDuration;
        }

        private void SpawnDueEnemies(float deltaTime)
        {
            _waveElapsed += deltaTime;
            while (_nextSpawn < _wave.Spawns.Count && _wave.Spawns[_nextSpawn].Time <= _waveElapsed)
            {
                var spawn = _wave.Spawns[_nextSpawn++];
                var enemy = new Enemy(_nextId++, spawn.Type, spawn.Stats, spawn.Lane, SpawnY);
                _enemies.Add(enemy);
                if (spawn.Stats.IsBoss)
                {
                    _boss = enemy;
                    _bossTimeRemaining = _settings.BossTimeLimit;
                }

                EnemySpawned?.Invoke(enemy);
            }
        }

        private void MoveEnemiesToBase(float deltaTime)
        {
            _resolvedEnemies.Clear();
            foreach (var enemy in _enemies)
            {
                enemy.Y -= enemy.Stats.Speed * deltaTime;
                if (enemy.Y <= _settings.BaseY)
                    _resolvedEnemies.Add(enemy);
            }

            foreach (var enemy in _resolvedEnemies)
            {
                RemoveEnemy(enemy);
                EnemyReachedBase?.Invoke(enemy);
                if (enemy.Stats.IsBoss)
                    _baseHealth.TakePercentOfMax(_settings.BossBaseDamagePercent);
                else
                    _baseHealth.TakeDamage(enemy.Stats.BaseDamage);
            }
        }

        private void UpdateBossTimer(float deltaTime)
        {
            if (_boss == null || !_boss.IsAlive) return;

            _bossTimeRemaining -= deltaTime;
            if (_bossTimeRemaining > 0f) return;

            var boss = _boss;
            RemoveEnemy(boss);
            BossTimedOut?.Invoke(boss);
            _baseHealth.TakePercentOfMax(_settings.BossBaseDamagePercent);
        }

        private void RemoveEmptyCooldowns()
        {
            _emptyCooldownCells.Clear();
            foreach (var pos in _cooldowns.Keys)
            {
                if (_grid.IsEmpty(pos))
                    _emptyCooldownCells.Add(pos);
            }

            foreach (var pos in _emptyCooldownCells)
                _cooldowns.Remove(pos);
        }

        private void FireUnits(float deltaTime)
        {
            _unitCells.Clear();
            foreach (var entry in _grid.Units())
                _unitCells.Add(entry.Pos);

            // 발사 이벤트에서 유닛을 옮길 수 있으므로 발사할 때 해당 칸을 다시 확인한다.
            foreach (var pos in _unitCells)
            {
                if (!(_grid.Get(pos) is UnitData unit)) continue;

                _cooldowns.TryGetValue(pos, out float cooldown);
                cooldown -= deltaTime;
                if (cooldown > 0f)
                {
                    _cooldowns[pos] = cooldown;
                    continue;
                }

                var profile = CombatProfile.For(unit.Type);
                var target = FindTarget(pos, profile.Range);
                if (target == null)
                {
                    _cooldowns[pos] = 0f;
                    continue;
                }

                Fire(pos, unit, profile, target);
                _cooldowns[pos] = profile.AttackInterval;
            }
        }

        private Enemy FindTarget(GridPos pos, float range)
        {
            Enemy target = null;
            float rangeSquared = range * range;
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive || DistanceSquared(enemy, pos.X, pos.Y) > rangeSquared)
                    continue;

                // 출현 순서로 확인하므로 같은 Y에서는 먼저 출현한 적을 유지한다.
                if (target == null || enemy.Y < target.Y)
                    target = enemy;
            }

            return target;
        }

        private void Fire(GridPos pos, UnitData unit, CombatProfile profile, Enemy target)
        {
            _resolvedEnemies.Clear();
            float attackPower = profile.AttackPower(_settings.UnitBaseAttack, unit.Level);
            float damage = attackPower * CounterTable.DamageMultiplier(unit.Type, target.Type);
            int targetLane = target.Lane;
            float targetY = target.Y;
            if (target.TakeDamage(damage))
                _resolvedEnemies.Add(target);

            UnitFired?.Invoke(pos, target, damage);

            if (profile.SplashRadius > 0f)
            {
                float radiusSquared = profile.SplashRadius * profile.SplashRadius;
                foreach (var enemy in _enemies)
                {
                    if (enemy == target || !enemy.IsAlive || DistanceSquared(enemy, targetLane, targetY) > radiusSquared)
                        continue;

                    float splashDamage = attackPower * CounterTable.DamageMultiplier(unit.Type, enemy.Type);
                    if (enemy.TakeDamage(splashDamage))
                        _resolvedEnemies.Add(enemy);
                }
            }

            foreach (var enemy in _resolvedEnemies)
            {
                _wallet.Add(enemy.Stats.Reward);
                EnemyKilled?.Invoke(enemy);
                RemoveEnemy(enemy);
            }
        }

        private void RemoveEnemy(Enemy enemy)
        {
            _enemies.Remove(enemy);
            if (_boss == enemy)
                _boss = null;
        }

        private static float DistanceSquared(Enemy enemy, int x, float y)
        {
            float dx = enemy.Lane - x;
            float dy = enemy.Y - y;
            return dx * dx + dy * dy;
        }
    }
}
