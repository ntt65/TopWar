using System;
using System.Collections.Generic;
using TopWar.Core.Units;

namespace TopWar.Core.Grid
{
    /// <summary>유닛 배치 그리드. 화면과 무관한 순수 상태와 규칙만 가진다.</summary>
    public sealed class GridModel
    {
        private UnitData?[,] _cells;

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>그리드 내용이 바뀔 때마다 발생한다. 뷰는 이 이벤트로 갱신한다.</summary>
        public event Action Changed;

        public GridModel(int width = GameRules.DefaultGridSize, int height = GameRules.DefaultGridSize)
        {
            ValidateSize(width, height);
            Width = width;
            Height = height;
            _cells = new UnitData?[width, height];
        }

        public bool InBounds(GridPos pos) =>
            pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

        public UnitData? Get(GridPos pos)
        {
            EnsureInBounds(pos);
            return _cells[pos.X, pos.Y];
        }

        public bool IsEmpty(GridPos pos) => Get(pos) == null;

        public IReadOnlyList<GridPos> EmptyCells()
        {
            var result = new List<GridPos>();
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (_cells[x, y] == null)
                    result.Add(new GridPos(x, y));
            return result;
        }

        public IEnumerable<(GridPos Pos, UnitData Unit)> Units()
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (_cells[x, y] is UnitData unit)
                    yield return (new GridPos(x, y), unit);
        }

        public bool TryPlace(GridPos pos, UnitData unit)
        {
            if (!InBounds(pos) || _cells[pos.X, pos.Y] != null)
                return false;

            _cells[pos.X, pos.Y] = unit;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 드래그 앤 드롭 처리. 빈 칸이면 이동, 같은 병종·레벨이면 합성, 그 외에는 스왑.
        /// </summary>
        public MoveResult Move(GridPos from, GridPos to)
        {
            if (!InBounds(from) || !InBounds(to) || _cells[from.X, from.Y] == null)
                return MoveResult.Invalid;

            if (from == to)
                return MoveResult.None;

            var moving = _cells[from.X, from.Y].Value;
            var target = _cells[to.X, to.Y];
            MoveResult result;

            if (target == null)
            {
                _cells[to.X, to.Y] = moving;
                _cells[from.X, from.Y] = null;
                result = MoveResult.Moved;
            }
            else if (moving.CanMergeWith(target.Value))
            {
                _cells[to.X, to.Y] = moving.Merged();
                _cells[from.X, from.Y] = null;
                result = MoveResult.Merged;
            }
            else
            {
                _cells[to.X, to.Y] = moving;
                _cells[from.X, from.Y] = target;
                result = MoveResult.Swapped;
            }

            Changed?.Invoke();
            return result;
        }

        /// <summary>그리드 확장. 기존 유닛은 같은 좌표에 그대로 남는다. 축소는 허용하지 않는다.</summary>
        public void Expand(int newWidth, int newHeight)
        {
            ValidateSize(newWidth, newHeight);
            if (newWidth < Width || newHeight < Height)
                throw new ArgumentException("Grid cannot shrink.");

            var cells = new UnitData?[newWidth, newHeight];
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                cells[x, y] = _cells[x, y];

            _cells = cells;
            Width = newWidth;
            Height = newHeight;
            Changed?.Invoke();
        }

        private void EnsureInBounds(GridPos pos)
        {
            if (!InBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), pos, "Position is outside the grid.");
        }

        private static void ValidateSize(int width, int height)
        {
            if (width < 1 || height < 1 || width > GameRules.MaxGridSize || height > GameRules.MaxGridSize)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"Grid size must be between 1 and {GameRules.MaxGridSize}.");
        }
    }
}
