using System;
using TopWar.Core.Units;

namespace TopWar.Core.Battle
{
    public sealed class WaveGenerator
    {
        private const int UnitTypeCount = 3;

        private readonly BattleSettings _settings;
        private readonly Random _random;

        public WaveGenerator(BattleSettings settings, Random random = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = random ?? new Random();
        }

        public WavePlan Create(int waveNumber, int laneCount)
        {
            if (waveNumber < 1) throw new ArgumentOutOfRangeException(nameof(waveNumber));
            if (laneCount < 1) throw new ArgumentOutOfRangeException(nameof(laneCount));

            int waveIndex = waveNumber - 1;
            int normalCount = _settings.EnemyCountBase + _settings.EnemyCountPerWave * waveIndex;
            float interval = Math.Max(_settings.SpawnIntervalMin,
                _settings.SpawnIntervalStart - _settings.SpawnIntervalStep * waveIndex);
            float hpMultiplier = (float)Math.Pow(_settings.EnemyHpGrowth, waveIndex);
            bool isBossWave = waveNumber % _settings.BossEvery == 0;
            if (isBossWave) normalCount /= 2;

            var spawns = new WaveSpawn[normalCount + (isBossWave ? 1 : 0)];
            for (int i = 0; i < normalCount; i++)
            {
                var type = (UnitType)_random.Next(UnitTypeCount);
                int lane = _random.Next(laneCount);
                var stats = _settings.GetEnemyBaseStats(type).WithHpMultiplier(hpMultiplier);
                spawns[i] = new WaveSpawn(i * interval, type, lane, stats);
            }

            if (isBossWave)
            {
                int bossIndex = waveNumber / _settings.BossEvery - 1;
                var type = (UnitType)(bossIndex % UnitTypeCount);
                float maxHp = _settings.GetEnemyBaseStats(type).MaxHp * hpMultiplier;
                var stats = new EnemyStats(maxHp * _settings.BossHpMultiplier, _settings.BossSpeed,
                    0, _settings.BossReward, true);
                spawns[normalCount] = new WaveSpawn(normalCount * interval, type, laneCount / 2, stats);
            }

            return new WavePlan(waveNumber, spawns);
        }
    }
}
