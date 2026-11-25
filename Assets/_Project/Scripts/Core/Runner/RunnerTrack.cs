using System;
using System.Collections.Generic;
using System.Linq;

namespace TopWar.Core.Runner
{
    public sealed class RunnerTrack
    {
        public RunnerTrack(IReadOnlyList<GateRow> gates, IReadOnlyList<EnemyGroup> enemies,
            IReadOnlyList<SupplyCrate> crates, RunnerBoss boss)
        {
            if (gates == null) throw new ArgumentNullException(nameof(gates));
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (crates == null) throw new ArgumentNullException(nameof(crates));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));

            Gates = gates.OrderBy(gate => gate.Distance).ToList().AsReadOnly();
            Enemies = enemies.OrderBy(enemy => enemy.Distance).ToList().AsReadOnly();
            Crates = crates.OrderBy(crate => crate.Distance).ToList().AsReadOnly();
        }

        public IReadOnlyList<GateRow> Gates { get; }
        public IReadOnlyList<EnemyGroup> Enemies { get; }
        public IReadOnlyList<SupplyCrate> Crates { get; }
        public RunnerBoss Boss { get; }
        public float Length => Boss.Distance;
    }
}
