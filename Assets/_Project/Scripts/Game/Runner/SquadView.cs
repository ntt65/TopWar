using System.Collections.Generic;
using TopWar.Core;
using TopWar.Core.Runner;
using TopWar.Game.UI;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game.Runner
{
    public sealed class SquadView : MonoBehaviour
    {
        private const int MaxVisibleCount = 60;
        private const float SpiralSpacing = 0.22f;
        private const float SpiralAngle = 137.5f * Mathf.Deg2Rad;

        private readonly List<(Transform Body, float OffsetX)> _formation = new List<(Transform, float)>();
        private RunnerSimulation _simulation;
        private Camera _camera;
        private Transform _soldiers;
        private TextMesh _count;
        private TextMesh _level;
        private float _displayX;
        private float _leftRadius;
        private float _rightRadius;
        private float _bodyHalfWidth;

        internal float HalfWidth { private get; set; } = float.PositiveInfinity;

        public void Init(RunnerSimulation sim)
        {
            _simulation = sim;
            _camera = Camera.main;
            _displayX = sim.X;
            transform.localPosition = new Vector3(_displayX, 0f, sim.Distance);

            _count = RunnerTrackView.CreateLabel(transform, "Count", string.Empty, Vector3.up * 1.1f, 0.24f);
            _level = RunnerTrackView.CreateLabel(transform, "Level", string.Empty, Vector3.up * 0.74f, 0.16f);
            sim.Squad.Changed += Rebuild;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_soldiers != null)
            {
                _soldiers.gameObject.SetActive(false);
                Destroy(_soldiers.gameObject);
            }

            _soldiers = new GameObject("Soldiers").transform;
            _soldiers.SetParent(transform, false);
            _formation.Clear();
            _leftRadius = 0f;
            _rightRadius = 0f;
            _bodyHalfWidth = 0f;

            var squad = _simulation.Squad;
            int visibleCount = Mathf.Min(squad.Count, MaxVisibleCount);
            float outerRadius = SpiralSpacing * Mathf.Sqrt(Mathf.Max(0, visibleCount - 1));
            float maxRadius = HalfWidth * 0.9f;
            float spacing = outerRadius > maxRadius ? SpiralSpacing * maxRadius / outerRadius : SpiralSpacing;
            var color = UnitPalette.Color(squad.Type);
            int index = 0;
            for (int level = squad.HighestLevel; level >= GameRules.MinLevel && index < visibleCount; level--)
            {
                int count = Mathf.Min(squad.GetCount(level), visibleCount - index);
                float size = 0.28f + 0.03f * (level - 1);
                for (int current = 0; current < count; current++, index++)
                {
                    float radius = spacing * Mathf.Sqrt(index);
                    float angle = index * SpiralAngle;
                    var offset = new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
                    var soldier = RunnerTrackView.CreateUnitBody(squad.Type, color, size, offset, _soldiers);
                    _formation.Add((soldier, offset.x));
                    _leftRadius = Mathf.Max(_leftRadius, -offset.x);
                    _rightRadius = Mathf.Max(_rightRadius, offset.x);
                    _bodyHalfWidth = Mathf.Max(_bodyHalfWidth, soldier.localScale.x * 0.5f);
                }
            }

            _count.text = squad.Count.ToString();
            _level.gameObject.SetActive(squad.HighestLevel > 1);
            if (squad.HighestLevel > 1) _level.text = string.Format(Texts.Level, squad.HighestLevel);
            FitToRoad();
        }

        private void FitToRoad()
        {
            // 무리 중심과 병사 크기를 유지하면서 도로 가장자리에 닿는 쪽의 간격을 줄인다.
            float edge = HalfWidth + RunnerTrackView.RoadShoulderWidth - _bodyHalfWidth;
            float leftScale = _leftRadius > 0f ? Mathf.Clamp01((edge + _displayX) / _leftRadius) : 1f;
            float rightScale = _rightRadius > 0f ? Mathf.Clamp01((edge - _displayX) / _rightRadius) : 1f;
            for (int i = 0; i < _formation.Count; i++)
            {
                var (body, offsetX) = _formation[i];
                var position = body.localPosition;
                float x = offsetX * (offsetX < 0f ? leftScale : rightScale);
                if (position.x == x) continue;

                position.x = x;
                body.localPosition = position;
            }
        }

        private void LateUpdate()
        {
            if (_simulation == null) return;

            var follow = 1f - Mathf.Exp(-12f * Time.deltaTime);
            float displayX = Mathf.Lerp(_displayX, _simulation.X, follow);
            if (_displayX != displayX)
            {
                _displayX = displayX;
                FitToRoad();
            }
            transform.localPosition = new Vector3(_displayX, 0f, _simulation.Distance);

            if (_camera != null)
            {
                var rotation = _camera.transform.rotation;
                _count.transform.rotation = rotation;
                _level.transform.rotation = rotation;
            }
        }

        private void OnDestroy()
        {
            if (_simulation != null) _simulation.Squad.Changed -= Rebuild;
        }
    }
}
