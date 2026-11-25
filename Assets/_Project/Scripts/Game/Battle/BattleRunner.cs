using System.Collections.Generic;
using TopWar.Core.Battle;
using TopWar.Core.Grid;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game.Battle
{
    public sealed class BattleRunner : MonoBehaviour
    {
        private readonly Dictionary<int, EnemyView> _enemies = new Dictionary<int, EnemyView>();
        private BattleSimulation _sim;
        private GridModel _grid;
        private GridView _gridView;

        public void Init(BattleSimulation sim, GridModel grid, GridView gridView)
        {
            _sim = sim;
            _grid = grid;
            _gridView = gridView;

            _sim.EnemySpawned += CreateEnemy;
            _sim.EnemyKilled += RemoveEnemy;
            _sim.EnemyReachedBase += RemoveEnemy;
            _sim.BossTimedOut += RemoveEnemy;
            _sim.UnitFired += FireProjectile;
        }

        private void Update()
        {
            _sim?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_sim == null) return;

            _sim.EnemySpawned -= CreateEnemy;
            _sim.EnemyKilled -= RemoveEnemy;
            _sim.EnemyReachedBase -= RemoveEnemy;
            _sim.BossTimedOut -= RemoveEnemy;
            _sim.UnitFired -= FireProjectile;
        }

        private void CreateEnemy(Enemy enemy)
        {
            _enemies.Add(enemy.Id, EnemyView.Create(enemy, transform, _gridView));
        }

        private void RemoveEnemy(Enemy enemy)
        {
            if (!_enemies.TryGetValue(enemy.Id, out var view)) return;

            _enemies.Remove(enemy.Id);
            if (view != null)
                Destroy(view.gameObject);
        }

        private void FireProjectile(GridPos pos, Enemy target, float damage)
        {
            if (!_enemies.TryGetValue(target.Id, out var view) || view == null) return;

            var unit = _grid.Get(pos);
            if (!unit.HasValue) return;

            var from = _gridView.CellToWorld(pos) + Vector3.up * 0.5f;
            ProjectileView.Fire(from, view, UnitPalette.Color(unit.Value.Type), transform);
        }
    }
}
