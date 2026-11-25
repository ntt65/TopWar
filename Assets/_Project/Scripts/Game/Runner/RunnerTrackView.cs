using System.Collections.Generic;
using TopWar.Core.Runner;
using TopWar.Core.Units;
using TopWar.Game.UI;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game.Runner
{
    public sealed class RunnerTrackView : MonoBehaviour
    {
        internal const float RoadShoulderWidth = 0.3f;

        private const int MaxVisibleEnemies = 30;
        private const float EnemySize = 0.38f;
        private const float EnemySpacing = 0.46f;
        private const float CrateSize = 0.9f;
        private const float CratePulseDuration = 0.1f;

        private static Font _font;
        private readonly Dictionary<GateRow, Transform> _gates = new Dictionary<GateRow, Transform>();
        private readonly Dictionary<SupplyCrate, CrateDisplay> _crates = new Dictionary<SupplyCrate, CrateDisplay>();
        private readonly Dictionary<EnemyGroup, EnemyDisplay> _enemies = new Dictionary<EnemyGroup, EnemyDisplay>();
        private readonly List<Transform> _labels = new List<Transform>();

        private RunnerSimulation _simulation;
        private Camera _camera;
        private float _halfWidth;
        private MaterialPropertyBlock _missedCrateColor;
        private Transform _bossBody;
        private TextMesh _bossHealth;

        internal Transform SquadTransform { private get; set; }

        public void Init(RunnerSimulation sim, RunnerSettings settings)
        {
            _simulation = sim;
            _camera = Camera.main;
            _halfWidth = settings.HalfWidth;

            _missedCrateColor = new MaterialPropertyBlock();
            var gray = new Color(0.39f, 0.40f, 0.42f);
            _missedCrateColor.SetColor(Shader.PropertyToID("_BaseColor"), gray);
            _missedCrateColor.SetColor(Shader.PropertyToID("_Color"), gray);

            BuildRoad(sim.Track.Length);
            foreach (var row in sim.Track.Gates) BuildGateRow(row);
            foreach (var crate in sim.Track.Crates) BuildCrate(crate);
            foreach (var group in sim.Track.Enemies) BuildEnemyGroup(group);
            BuildBoss(sim.Track.Boss);

            sim.GatePassed += RemoveGateRow;
            sim.CrateHit += OnCrateHit;
            sim.CrateOpened += RemoveCrate;
            sim.CrateMissed += OnCrateMissed;
            sim.EnemyGroupHit += OnEnemyHit;
            sim.EnemyGroupDestroyed += RemoveEnemyGroup;
            sim.EnemyGroupCollided += OnEnemyCollision;
            sim.BossHit += OnBossHit;
        }

        private void BuildRoad(float trackLength)
        {
            float length = trackLength + 20f;
            float center = trackLength * 0.5f;
            CreateBox("Road", new Color(0.32f, 0.33f, 0.36f),
                new Vector3(2f * (_halfWidth + RoadShoulderWidth), 0.1f, length),
                new Vector3(0f, -0.05f, center), transform);
            CreateBox("Center Line", Color.white, new Vector3(0.06f, 0.008f, length),
                new Vector3(0f, 0.004f, center), transform);

            var railSize = new Vector3(0.16f, 0.2f, length);
            var railColor = new Color(0.14f, 0.15f, 0.19f);
            float railX = _halfWidth + 0.22f;
            CreateBox("Left Rail", railColor, railSize, new Vector3(-railX, 0.1f, center), transform);
            CreateBox("Right Rail", railColor, railSize, new Vector3(railX, 0.1f, center), transform);
        }

        private void BuildGateRow(GateRow row)
        {
            var root = CreateRoot("Gate Row", 0f, row.Distance);
            _gates.Add(row, root);
            BuildGate(row.Left, -_halfWidth * 0.5f, root);
            BuildGate(row.Right, _halfWidth * 0.5f, root);
        }

        private void BuildGate(Gate gate, float x, Transform root)
        {
            var color = gate.Kind switch
            {
                GateKind.Subtract => new Color(0.72f, 0.20f, 0.19f),
                GateKind.ChangeType => UnitPalette.Color(gate.UnitType),
                _ => new Color(0.16f, 0.43f, 0.80f),
            };
            CreateBox("Gate Panel", color, new Vector3(_halfWidth * 0.9f, 1.4f, 0.1f),
                new Vector3(x, 0.7f, 0f), root);
            AddLabel(root, "Gate Label", Texts.FormatGate(gate), new Vector3(x, 0.88f, -0.23f), 0.23f);
        }

        private void BuildCrate(SupplyCrate crate)
        {
            float x = (crate.IsLeft ? -0.5f : 0.5f) * _halfWidth;
            var root = CreateRoot("Supply Crate", x, crate.Distance);
            var body = CreateBox("Crate", new Color(0.59f, 0.33f, 0.14f), Vector3.one * CrateSize,
                Vector3.up * (CrateSize * 0.5f), root);
            var health = AddLabel(root, "Crate Health", FormatHealth(crate.Hp), Vector3.up * 1.48f, 0.2f);
            var reward = AddLabel(root, "Crate Reward", string.Format(Texts.CrateReward, crate.RewardCount),
                Vector3.up * 1.1f, 0.14f);
            _crates.Add(crate, new CrateDisplay(root, body, health, reward));
        }

        private void BuildEnemyGroup(EnemyGroup group)
        {
            var root = CreateRoot("Enemy Group", 0f, group.Distance);
            var count = AddLabel(root, "Enemy Count", group.RemainingCount.ToString(), Vector3.up * 1.16f, 0.22f);
            var view = new EnemyDisplay(root, count);
            _enemies.Add(group, view);

            var color = Color.Lerp(UnitPalette.Color(group.Type), new Color(0.44f, 0.12f, 0.10f), 0.55f);
            int visible = Mathf.Min(group.RemainingCount, MaxVisibleEnemies);
            for (int i = 0; i < visible; i++)
                view.Units.Add(CreateUnitBody(group.Type, color, EnemySize, Vector3.zero, root));
            LayoutEnemyGroup(view);
        }

        private void LayoutEnemyGroup(EnemyDisplay view)
        {
            float edge = Mathf.Max(0f, _halfWidth - EnemySize * 0.5f);
            int columns = Mathf.FloorToInt(2f * edge / EnemySpacing) + 1;
            int rows = (view.Units.Count + columns - 1) / columns;
            for (int i = 0; i < view.Units.Count; i++)
            {
                int row = i / columns;
                int rowSize = Mathf.Min(columns, view.Units.Count - row * columns);
                float x = rowSize > 1 ? Mathf.Lerp(-edge, edge, (float)(i % columns) / (rowSize - 1)) : 0f;
                float z = (row - (rows - 1) * 0.5f) * EnemySpacing;
                var unit = view.Units[i];
                unit.localPosition = new Vector3(x, unit.localPosition.y, z);
            }
        }

        private void BuildBoss(RunnerBoss boss)
        {
            var root = CreateRoot("Boss", 0f, boss.Distance);
            _bossBody = CreateUnitBody(boss.Type, UnitPalette.Color(boss.Type), 2.5f, Vector3.zero, root);
            float height = _bossBody.localPosition.y * 2f;
            _bossHealth = AddLabel(root, "Boss Health", FormatHealth(boss.Hp), Vector3.up * (height + 0.62f), 0.3f);
        }

        private void RemoveGateRow(GateRow row, Gate gate, bool isLeft)
        {
            if (_gates.Remove(row, out var root)) RemoveView(root);
        }

        private void OnCrateHit(SupplyCrate crate, float damage)
        {
            var view = _crates[crate];
            view.Health.text = FormatHealth(crate.Hp);
            view.PulseRemaining = CratePulseDuration;
            FireShot(view.Body.position);
        }

        private void RemoveCrate(SupplyCrate crate)
        {
            if (_crates.Remove(crate, out var view)) RemoveView(view.Root);
        }

        private void OnCrateMissed(SupplyCrate crate)
        {
            var view = _crates[crate];
            view.PulseRemaining = 0f;
            view.Body.localScale = Vector3.one * CrateSize;
            view.Renderer.SetPropertyBlock(_missedCrateColor);
            view.Health.gameObject.SetActive(false);
            view.Reward.gameObject.SetActive(false);
        }

        private void OnEnemyHit(EnemyGroup group, float damage)
        {
            var view = _enemies[group];
            int remaining = group.RemainingCount;
            view.Count.text = remaining.ToString();

            int visible = Mathf.Min(remaining, MaxVisibleEnemies);
            if (view.Units.Count > visible)
            {
                for (int i = view.Units.Count - 1; i >= visible; i--)
                {
                    RemoveView(view.Units[i]);
                    view.Units.RemoveAt(i);
                }

                LayoutEnemyGroup(view);
            }

            FireShot(view.Root.position + Vector3.up * 0.25f);
            float multiplier = CounterTable.DamageMultiplier(_simulation.Squad.Type, group.Type);
            DamageNumberView.Show(view.Count.transform.position - Vector3.up * 0.34f, damage, multiplier, transform);
        }

        private void RemoveEnemyGroup(EnemyGroup group)
        {
            if (_enemies.Remove(group, out var view)) RemoveView(view.Root);
        }

        private void OnEnemyCollision(EnemyGroup group, int lost) => RemoveEnemyGroup(group);

        private void OnBossHit(float damage)
        {
            var boss = _simulation.Track.Boss;
            _bossHealth.text = FormatHealth(boss.Hp);
            FireShot(_bossBody.position);
            float multiplier = CounterTable.DamageMultiplier(_simulation.Squad.Type, boss.Type);
            DamageNumberView.Show(_bossHealth.transform.position - Vector3.up * 0.34f, damage, multiplier, transform);
        }

        private void FireShot(Vector3 target)
        {
            float x = SquadTransform != null ? SquadTransform.position.x : _simulation.X;
            var origin = new Vector3(x, 0.5f, _simulation.Distance);
            var color = Color.Lerp(UnitPalette.Color(_simulation.Squad.Type), Color.white, 0.35f);
            RunnerShotView.Fire(origin, target, color, transform);
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                var rotation = _camera.transform.rotation;
                for (int i = _labels.Count - 1; i >= 0; i--)
                {
                    var label = _labels[i];
                    if (label == null) _labels.RemoveAt(i);
                    else label.rotation = rotation;
                }
            }

            foreach (var view in _crates.Values)
            {
                if (view.PulseRemaining <= 0f) continue;
                view.PulseRemaining = Mathf.Max(0f, view.PulseRemaining - Time.deltaTime);
                float pulse = Mathf.Sin(view.PulseRemaining / CratePulseDuration * Mathf.PI);
                view.Body.localScale = Vector3.one * (CrateSize * (1f + 0.15f * pulse));
            }
        }

        private Transform CreateRoot(string objectName, float x, float distance)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(x, 0f, distance);
            return root;
        }

        private static Transform CreateBox(string objectName, Color color, Vector3 size, Vector3 position, Transform parent)
        {
            var box = BattlePrimitive.Create(PrimitiveType.Cube, objectName, color, parent);
            box.localPosition = position;
            box.localScale = size;
            return box;
        }

        internal static Transform CreateUnitBody(UnitType type, Color color, float size, Vector3 groundOffset, Transform parent)
        {
            var shape = UnitPalette.Shape(type);
            var body = BattlePrimitive.Create(shape, "Unit", color, parent);
            var scale = UnitPalette.Proportion(type) * size;
            body.localPosition = groundOffset + Vector3.up * (scale.y * 0.5f);
            if (shape == PrimitiveType.Capsule || shape == PrimitiveType.Cylinder) scale.y *= 0.5f;
            body.localScale = scale;
            return body;
        }

        private TextMesh AddLabel(Transform parent, string objectName, string value, Vector3 position, float size)
        {
            var label = CreateLabel(parent, objectName, value, position, size);
            _labels.Add(label.transform);
            return label;
        }

        internal static TextMesh CreateLabel(Transform parent, string objectName, string value, Vector3 position, float size)
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var label = new GameObject(objectName).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.font = _font;
            label.text = value;
            label.fontSize = 64;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return label;
        }

        private static string FormatHealth(float hp) => Mathf.Ceil(hp).ToString("0");

        private static void RemoveView(Transform root)
        {
            root.gameObject.SetActive(false);
            Destroy(root.gameObject);
        }

        private void OnDestroy()
        {
            if (_simulation == null) return;

            _simulation.GatePassed -= RemoveGateRow;
            _simulation.CrateHit -= OnCrateHit;
            _simulation.CrateOpened -= RemoveCrate;
            _simulation.CrateMissed -= OnCrateMissed;
            _simulation.EnemyGroupHit -= OnEnemyHit;
            _simulation.EnemyGroupDestroyed -= RemoveEnemyGroup;
            _simulation.EnemyGroupCollided -= OnEnemyCollision;
            _simulation.BossHit -= OnBossHit;
        }

        private sealed class CrateDisplay
        {
            public CrateDisplay(Transform root, Transform body, TextMesh health, TextMesh reward)
            {
                Root = root;
                Body = body;
                Renderer = body.GetComponent<Renderer>();
                Health = health;
                Reward = reward;
            }

            public Transform Root { get; }
            public Transform Body { get; }
            public Renderer Renderer { get; }
            public TextMesh Health { get; }
            public TextMesh Reward { get; }
            public float PulseRemaining { get; set; }
        }

        private sealed class EnemyDisplay
        {
            public EnemyDisplay(Transform root, TextMesh count)
            {
                Root = root;
                Count = count;
            }

            public Transform Root { get; }
            public TextMesh Count { get; }
            public List<Transform> Units { get; } = new List<Transform>();
        }
    }
}
