using TopWar.Core.Grid;
using TopWar.Game.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TopWar.Game.Controls
{
    /// <summary>
    /// 마우스·터치 드래그로 유닛을 옮긴다. 놓은 칸에 따라 이동 / 스왑 / 합성은 GridModel이 결정한다.
    /// 콜라이더 없이 바닥 평면(y=0)과 레이를 교차해서 칸을 찾는다.
    /// </summary>
    public sealed class DragController : MonoBehaviour
    {
        private const float LiftHeight = 0.4f;

        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);

        private Camera _camera;
        private GridModel _grid;
        private GridView _view;

        private bool _dragging;
        private GridPos _from;
        private UnitView _draggedView;

        public void Init(Camera cam, GridModel grid, GridView view)
        {
            _camera = cam;
            _grid = grid;
            _view = view;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || _grid == null) return;

            var screen = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
                BeginDrag(screen);

            if (!_dragging) return;

            if (pointer.press.isPressed)
                UpdateDrag(screen);

            if (pointer.press.wasReleasedThisFrame)
                EndDrag(screen);
        }

        private void BeginDrag(Vector2 screen)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!TryGetCell(screen, out var cell) || _grid.IsEmpty(cell))
                return;

            _dragging = true;
            _from = cell;
            _draggedView = _view.GetUnitView(cell);
        }

        private void UpdateDrag(Vector2 screen)
        {
            if (_draggedView != null && TryGetGroundPoint(screen, out var world))
                _draggedView.transform.position = world + Vector3.up * LiftHeight;
        }

        private void EndDrag(Vector2 screen)
        {
            _dragging = false;
            _draggedView = null;

            var result = TryGetCell(screen, out var to)
                ? _grid.Move(_from, to)
                : MoveResult.Invalid;

            // 그리드가 바뀌지 않았으면 들고 있던 유닛을 제자리로 되돌린다.
            if (result == MoveResult.Invalid || result == MoveResult.None)
                _view.ResetUnitPosition(_from);
        }

        private bool TryGetGroundPoint(Vector2 screen, out Vector3 world)
        {
            var ray = _camera.ScreenPointToRay(screen);
            if (Ground.Raycast(ray, out var distance))
            {
                world = ray.GetPoint(distance);
                return true;
            }

            world = default;
            return false;
        }

        private bool TryGetCell(Vector2 screen, out GridPos cell)
        {
            cell = default;
            return TryGetGroundPoint(screen, out var world) && _view.TryWorldToCell(world, out cell);
        }
    }
}
