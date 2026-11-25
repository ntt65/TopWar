using TopWar.Core.Battle;
using TopWar.Core.Units;
using UnityEngine;

namespace TopWar.Game.View
{
    public sealed class EnemyView : MonoBehaviour
    {
        private Enemy _enemy;
        private GridView _grid;
        private Transform _camera;
        private Transform _healthBar;
        private Transform _healthFill;
        private Vector3 _healthFillScale;
        private float _aimHeight;
        private Vector3 _lastAimPosition;

        internal Vector3 AimPosition => _grid != null
            ? _grid.GridToWorld(_enemy.Lane, _enemy.Y) + Vector3.up * _aimHeight
            : _lastAimPosition;

        public static EnemyView Create(Enemy enemy, Transform parent, GridView grid)
        {
            var root = new GameObject($"Enemy {enemy.Id}");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<EnemyView>();
            view._enemy = enemy;
            view._grid = grid;
            var camera = Camera.main;
            if (camera != null)
                view._camera = camera.transform;

            view.BuildBody();
            view.BuildHealthBar();
            view.Update();
            return view;
        }

        private void BuildBody()
        {
            float size = 0.5f;
            Color color = _enemy.Type switch
            {
                UnitType.Army => new Color(0.6f, 0.15f, 0.15f),
                UnitType.AirForce => new Color(0.75f, 0.3f, 0.1f),
                _ => new Color(0.45f, 0.1f, 0.3f),
            };

            if (_enemy.Stats.IsBoss)
            {
                size *= 1.6f;
                color = Color.Lerp(color, Color.black, 0.25f);
            }

            var shape = UnitPalette.Shape(_enemy.Type);
            var body = BattlePrimitive.Create(shape, "Body", color, transform);
            var scale = UnitPalette.Proportion(_enemy.Type) * size;
            _aimHeight = scale.y * 0.5f;
            body.localPosition = Vector3.up * _aimHeight;

            // 캡슐과 실린더의 기본 메시 높이는 2다.
            if (shape == PrimitiveType.Capsule || shape == PrimitiveType.Cylinder)
                scale.y *= 0.5f;
            body.localScale = scale;
        }

        private void BuildHealthBar()
        {
            _healthBar = new GameObject("Health Bar").transform;
            _healthBar.SetParent(transform, false);
            _healthBar.localPosition = Vector3.up * (_aimHeight * 2f + 0.17f);

            float width = _enemy.Stats.IsBoss ? 0.65f : 0.45f;
            var background = BattlePrimitive.Create(PrimitiveType.Cube, "Background", Color.black, _healthBar);
            background.localScale = new Vector3(width + 0.025f, 0.09f, 0.018f);

            _healthFill = BattlePrimitive.Create(PrimitiveType.Cube, "Fill", Color.red, _healthBar);
            _healthFillScale = new Vector3(width, 0.065f, 0.01f);
        }

        private void Update()
        {
            transform.position = _grid.GridToWorld(_enemy.Lane, _enemy.Y);

            var scale = _healthFillScale;
            scale.x *= _enemy.HpRatio;
            _healthFill.localScale = scale;
            _healthFill.localPosition = new Vector3((scale.x - _healthFillScale.x) * 0.5f, 0f, -0.015f);
        }

        private void LateUpdate()
        {
            if (_camera != null)
                _healthBar.rotation = _camera.rotation;
        }

        private void OnDestroy()
        {
            // 투사체는 뷰가 사라져도 마지막 조준점까지 이동한다.
            _lastAimPosition = AimPosition;
            _grid = null;
        }
    }
}
