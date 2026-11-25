using System.Collections.Generic;
using TopWar.Core.Grid;
using UnityEngine;

namespace TopWar.Game.View
{
    /// <summary>
    /// GridModel을 3D로 표시한다. 그리드 중심이 월드 원점.
    /// X = 열(레인), +Z = 적이 오는 쪽, -Z = 기지 쪽.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        public const float CellSize = 1.2f;

        private static readonly Color TileColorA = new Color(0.42f, 0.47f, 0.40f);
        private static readonly Color TileColorB = new Color(0.36f, 0.41f, 0.35f);

        private readonly Dictionary<GridPos, UnitView> _units = new Dictionary<GridPos, UnitView>();
        private GridModel _grid;
        private Transform _tilesRoot;
        private Transform _unitsRoot;
        private int _builtWidth;
        private int _builtHeight;

        public float WorldWidth => _grid.Width * CellSize;
        public float WorldDepth => _grid.Height * CellSize;

        public void Init(GridModel grid)
        {
            _grid = grid;
            _tilesRoot = new GameObject("Tiles").transform;
            _tilesRoot.SetParent(transform, false);
            _unitsRoot = new GameObject("Units").transform;
            _unitsRoot.SetParent(transform, false);

            _grid.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_grid != null)
                _grid.Changed -= Refresh;
        }

        public Vector3 CellToWorld(GridPos pos) => GridToWorld(pos.X, pos.Y);

        public Vector3 GridToWorld(float x, float y) => transform.TransformPoint(GridToLocal(x, y));

        public bool TryWorldToCell(Vector3 world, out GridPos pos)
        {
            var local = transform.InverseTransformPoint(world);
            var x = Mathf.RoundToInt(local.x / CellSize + (_grid.Width - 1) * 0.5f);
            var y = Mathf.RoundToInt(local.z / CellSize + (_grid.Height - 1) * 0.5f);
            pos = new GridPos(x, y);
            return _grid.InBounds(pos);
        }

        public UnitView GetUnitView(GridPos pos) =>
            _units.TryGetValue(pos, out var view) ? view : null;

        /// <summary>드래그가 취소됐을 때 유닛을 원래 칸으로 되돌린다.</summary>
        public void ResetUnitPosition(GridPos pos)
        {
            var view = GetUnitView(pos);
            if (view != null)
                view.transform.localPosition = CellToLocal(pos);
        }

        private Vector3 CellToLocal(GridPos pos) => GridToLocal(pos.X, pos.Y);

        private Vector3 GridToLocal(float x, float y) => new Vector3(
            (x - (_grid.Width - 1) * 0.5f) * CellSize,
            0f,
            (y - (_grid.Height - 1) * 0.5f) * CellSize);

        private void Refresh()
        {
            if (_builtWidth != _grid.Width || _builtHeight != _grid.Height)
            {
                BuildTiles();
                ClearUnits();
            }

            var occupied = new HashSet<GridPos>();
            foreach (var (pos, unit) in _grid.Units())
            {
                occupied.Add(pos);

                if (_units.TryGetValue(pos, out var existing))
                {
                    if (existing.Data == unit)
                    {
                        existing.transform.localPosition = CellToLocal(pos);
                        continue;
                    }

                    Destroy(existing.gameObject);
                }

                _units[pos] = UnitView.Create(unit, _unitsRoot, CellToLocal(pos));
            }

            var removed = new List<GridPos>();
            foreach (var pos in _units.Keys)
                if (!occupied.Contains(pos))
                    removed.Add(pos);

            foreach (var pos in removed)
            {
                Destroy(_units[pos].gameObject);
                _units.Remove(pos);
            }
        }

        private void BuildTiles()
        {
            for (var i = _tilesRoot.childCount - 1; i >= 0; i--)
                Destroy(_tilesRoot.GetChild(i).gameObject);

            for (var y = 0; y < _grid.Height; y++)
            for (var x = 0; x < _grid.Width; x++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile {x},{y}";
                Destroy(tile.GetComponent<Collider>());
                tile.transform.SetParent(_tilesRoot, false);
                tile.transform.localPosition = CellToLocal(new GridPos(x, y)) + Vector3.down * 0.05f;
                tile.transform.localScale = new Vector3(CellSize * 0.94f, 0.1f, CellSize * 0.94f);
                tile.GetComponent<Renderer>().material.color = (x + y) % 2 == 0 ? TileColorA : TileColorB;
            }

            _builtWidth = _grid.Width;
            _builtHeight = _grid.Height;
        }

        private void ClearUnits()
        {
            foreach (var view in _units.Values)
                Destroy(view.gameObject);
            _units.Clear();
        }
    }
}
