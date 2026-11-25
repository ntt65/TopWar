using System;
using TopWar.Core.Progression;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public static class RunnerTrackGenerator
    {
        private const int GateKindCount = (int)GateKind.ChangeType + 1;
        private static readonly float[] _gateDistances = { 20f, 40f, 80f, 100f, 140f };
        private static readonly float[] _enemyDistances = { 60f, 120f, 160f };

        public static RunnerTrack Create(int stage, int seed)
        {
            float scale = StageRules.EnemyHpScale(stage);
            var random = new Random(seed);

            var gates = new GateRow[_gateDistances.Length];
            for (int i = 0; i < gates.Length; i++)
            {
                int left = random.Next(GateKindCount);
                int right = random.Next(GateKindCount - 1);
                if (right >= left) right++;

                gates[i] = new GateRow(_gateDistances[i],
                    CreateGate((GateKind)left, random), CreateGate((GateKind)right, random));
            }

            var enemies = new EnemyGroup[_enemyDistances.Length];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = new EnemyGroup(_enemyDistances[i], RandomUnitType(random), 10 + 10 * i, 10f * scale);

            var boss = new RunnerBoss(200f, RandomUnitType(random), 2000f * scale, 3);

            var crates = new SupplyCrate[7];
            for (int i = 0; i < crates.Length; i++)
            {
                crates[i] = new SupplyCrate(30f + 20f * i, random.Next(2) == 0,
                    20f * (i + 1) * scale, random.Next(2, 6) + i);
            }

            return new RunnerTrack(gates, enemies, crates, boss);
        }

        private static Gate CreateGate(GateKind kind, Random random) => kind switch
        {
            GateKind.Add => Gate.Add(random.Next(3, 11)),
            GateKind.Multiply => Gate.Multiply(random.Next(2, 4)),
            GateKind.Subtract => Gate.Subtract(random.Next(2, 7)),
            GateKind.Merge => Gate.Merge(),
            GateKind.ChangeType => Gate.ChangeType(RandomUnitType(random)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private static UnitType RandomUnitType(Random random) => (UnitType)random.Next((int)UnitType.Navy + 1);
    }
}
