using System;
using System.Collections.Generic;
using System.Linq;

namespace TopWar.Core.Battle
{
    public sealed class WavePlan
    {
        public WavePlan(int number, IReadOnlyList<WaveSpawn> spawns)
        {
            Number = number;
            Spawns = Array.AsReadOnly(spawns.OrderBy(spawn => spawn.Time).ToArray());
            IsBoss = Spawns.Any(spawn => spawn.Stats.IsBoss);
        }

        public int Number { get; }
        public IReadOnlyList<WaveSpawn> Spawns { get; }
        public bool IsBoss { get; }
    }
}
