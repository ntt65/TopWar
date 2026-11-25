using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public readonly struct WaveSpawn
    {
        public WaveSpawn(float time, UnitType type, int lane, EnemyStats stats)
        {
            Time = time;
            Type = type;
            Lane = lane;
            Stats = stats;
        }

        public float Time { get; }
        public UnitType Type { get; }
        public int Lane { get; }
        public EnemyStats Stats { get; }
    }
}
