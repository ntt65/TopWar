using TopWar.Core.Battle;
using TopWar.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    public sealed class BattleHud : MonoBehaviour
    {
        private BattleSimulation _sim;
        private BaseHealth _baseHealth;
        private Text _waveText;
        private Text _baseText;
        private Text _bossText;
        private int _shownWave = -1;
        private int _shownBreakSeconds = -1;
        private int _shownBossSeconds = -1;

        public void Init(BattleSimulation sim, BaseHealth baseHealth)
        {
            _sim = sim;
            _baseHealth = baseHealth;

            var canvas = UiFactory.CreateCanvas(transform);
            canvas.sortingOrder = 1;
            BuildStatus(canvas.transform);

            _baseHealth.Changed += RefreshBase;

            RefreshBase();
            RefreshWave();
            RefreshBoss();
        }

        private void OnDestroy()
        {
            if (_baseHealth != null)
                _baseHealth.Changed -= RefreshBase;
        }

        private void Update()
        {
            if (_sim == null) return;

            RefreshWave();
            RefreshBoss();
        }

        private void BuildStatus(Transform root)
        {
            var topCenter = new Vector2(0.5f, 1f);
            _waveText = UiFactory.CreateText(root, "Wave", 46, TextAnchor.MiddleCenter, Color.white,
                topCenter, topCenter, new Vector2(-300f, -214f), new Vector2(300f, -152f));
            _baseText = UiFactory.CreateText(root, "Base Health", 38, TextAnchor.MiddleCenter, Color.white,
                topCenter, topCenter, new Vector2(-220f, -264f), new Vector2(220f, -214f));
            _bossText = UiFactory.CreateText(root, "Boss Timer", 40, TextAnchor.MiddleCenter, Color.yellow,
                topCenter, topCenter, new Vector2(-220f, -318f), new Vector2(220f, -264f));
            _bossText.gameObject.SetActive(false);
        }

        private void RefreshWave()
        {
            if (_sim.State == BattleState.Break)
            {
                int seconds = Mathf.CeilToInt(_sim.BreakRemaining);
                if (_shownBreakSeconds == seconds) return;

                _shownBreakSeconds = seconds;
                _waveText.text = string.Format(Texts.NextWave, seconds);
                return;
            }

            if (_shownBreakSeconds < 0 && _shownWave == _sim.WaveNumber) return;

            _shownBreakSeconds = -1;
            _shownWave = _sim.WaveNumber;
            _waveText.text = string.Format(Texts.Wave, _shownWave, StageRules.WavesPerStage);
        }

        private void RefreshBase()
        {
            _baseText.text = string.Format(Texts.Base, _baseHealth.Current, _baseHealth.Max);
        }

        private void RefreshBoss()
        {
            var remaining = _sim.BossTimeRemaining;
            int seconds = remaining.HasValue ? Mathf.CeilToInt(remaining.Value) : -1;
            if (_shownBossSeconds == seconds) return;

            _shownBossSeconds = seconds;
            _bossText.gameObject.SetActive(seconds >= 0);
            if (seconds >= 0)
                _bossText.text = string.Format(Texts.Boss, seconds);
        }
    }
}
