using System;
using TopWar.Core.Grid;
using TopWar.Core.Units;

namespace TopWar.Core.Economy
{
    public enum SummonResult
    {
        Success,
        NotEnoughGold,
        GridFull,
    }

    /// <summary>
    /// 병종별 소환. 비용은 baseCost에서 시작해 소환할 때마다 costStep씩 오른다 (병종 공통).
    /// 소환된 Lv.1 유닛은 빈 칸 중 무작위 위치에 놓인다.
    /// </summary>
    public sealed class SummonService
    {
        private readonly GridModel _grid;
        private readonly Wallet _wallet;
        private readonly Random _random;
        private readonly int _baseCost;
        private readonly int _costStep;

        public int SummonCount { get; private set; }
        public int CurrentCost => _baseCost + _costStep * SummonCount;

        public SummonService(GridModel grid, Wallet wallet, int baseCost, int costStep, Random random = null)
        {
            if (baseCost < 0) throw new ArgumentOutOfRangeException(nameof(baseCost));
            if (costStep < 0) throw new ArgumentOutOfRangeException(nameof(costStep));

            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _baseCost = baseCost;
            _costStep = costStep;
            _random = random ?? new Random();
        }

        public SummonResult TrySummon(UnitType type, out GridPos placedAt)
        {
            placedAt = default;

            var empty = _grid.EmptyCells();
            if (empty.Count == 0)
                return SummonResult.GridFull;

            if (!_wallet.TrySpend(CurrentCost))
                return SummonResult.NotEnoughGold;

            placedAt = empty[_random.Next(empty.Count)];
            _grid.TryPlace(placedAt, new UnitData(type, GameRules.MinLevel));
            SummonCount++;
            return SummonResult.Success;
        }
    }
}
