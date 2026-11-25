using TopWar.Core.Runner;
using TopWar.Game.UI;
using TopWar.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace TopWar.Game.Runner
{
    public sealed class RunnerHud : MonoBehaviour
    {
        private const float GateNoticeDuration = 0.6f;

        private RunnerSimulation _simulation;
        private RectTransform _progressFill;
        private Text _squadCount;
        private GameObject _bossStatus;
        private Text _bossTime;
        private RectTransform _bossFill;
        private Text _gateNotice;
        private float _gateTime;
        private int _shownBossTime = -1;

        public void Init(RunnerSimulation sim)
        {
            _simulation = sim;

            var root = UiFactory.CreateCanvas(transform).transform;
            var topLeft = new Vector2(0.14f, 1f);
            var topRight = new Vector2(0.86f, 1f);
            _progressFill = CreateBar(root, "Progress", new Color(0.22f, 0.75f, 0.75f),
                topLeft, topRight, -48f, -28f);
            _squadCount = UiFactory.CreateText(root, "Squad Count", 42, TextAnchor.MiddleCenter, Color.white,
                topLeft, topRight, new Vector2(0f, -105f), new Vector2(0f, -48f));

            var bossPanel = UiFactory.CreateRect(root, "Boss Status", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _bossStatus = bossPanel.gameObject;
            _bossTime = UiFactory.CreateText(bossPanel, "Boss Time", 36, TextAnchor.MiddleCenter, Color.white,
                topLeft, topRight, new Vector2(0f, -156f), new Vector2(0f, -105f));
            _bossFill = CreateBar(bossPanel, "Boss Health", new Color(0.82f, 0.21f, 0.21f),
                topLeft, topRight, -174f, -156f);
            _bossStatus.SetActive(false);

            var center = Vector2.one * 0.5f;
            _gateNotice = UiFactory.CreateText(root, "Gate Notice", 78, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.3f), center, center, new Vector2(-285f, -64f), new Vector2(285f, 64f));
            _gateNotice.gameObject.SetActive(false);

            sim.Squad.Changed += RefreshSquad;
            sim.GatePassed += ShowGate;
            sim.BossFightStarted += ShowBoss;
            sim.BossHit += RefreshBossHealth;
            RefreshSquad();
            SetBar(_progressFill, sim.Progress);
        }

        private void LateUpdate()
        {
            if (_simulation == null) return;

            SetBar(_progressFill, _simulation.Progress);
            if (_bossStatus.activeSelf) RefreshBossTime();

            if (_gateTime <= 0f) return;
            _gateTime -= Time.deltaTime;
            if (_gateTime <= 0f) _gateNotice.gameObject.SetActive(false);
        }

        private void RefreshSquad()
        {
            var squad = _simulation.Squad;
            _squadCount.text = string.Format(Texts.SquadCount, UnitPalette.Label(squad.Type), squad.Count);
        }

        private void ShowGate(GateRow row, Gate gate, bool isLeft)
        {
            _gateNotice.text = Texts.FormatGate(gate);
            _gateTime = GateNoticeDuration;
            _gateNotice.gameObject.SetActive(true);
        }

        private void ShowBoss()
        {
            _bossStatus.SetActive(true);
            var boss = _simulation.Track.Boss;
            SetBar(_bossFill, boss.Hp / boss.MaxHp);
            RefreshBossTime();
        }

        private void RefreshBossTime()
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_simulation.BossTimeRemaining));
            if (seconds == _shownBossTime) return;

            _shownBossTime = seconds;
            _bossTime.text = string.Format(Texts.Boss, seconds);
        }

        private void RefreshBossHealth(float damage)
        {
            var boss = _simulation.Track.Boss;
            SetBar(_bossFill, boss.Hp / boss.MaxHp);
        }

        private static RectTransform CreateBar(Transform parent, string name, Color color,
            Vector2 topLeft, Vector2 topRight, float bottom, float top)
        {
            var background = UiFactory.CreateRect(parent, name, topLeft, topRight,
                new Vector2(0f, bottom), new Vector2(0f, top));
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.12f, 0.16f, 0.92f);
            backgroundImage.raycastTarget = false;

            var fill = UiFactory.CreateRect(background, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = color;
            fillImage.raycastTarget = false;
            return fill;
        }

        private static void SetBar(RectTransform fill, float ratio)
        {
            float width = Mathf.Clamp01(ratio);
            if (fill.anchorMax.x != width) fill.anchorMax = new Vector2(width, 1f);
        }

        private void OnDestroy()
        {
            if (_simulation == null) return;

            _simulation.Squad.Changed -= RefreshSquad;
            _simulation.GatePassed -= ShowGate;
            _simulation.BossFightStarted -= ShowBoss;
            _simulation.BossHit -= RefreshBossHealth;
        }
    }
}
