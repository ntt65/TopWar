using TopWar.Core.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private RectTransform _gameOverPanel;
        private Text _gameOverWaveText;
        private Button _restartButton;
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
            BuildGameOver(canvas.transform);

            _baseHealth.Changed += RefreshBase;
            _sim.GameOver += ShowGameOver;

            RefreshBase();
            RefreshWave();
            RefreshBoss();
        }

        private void OnDestroy()
        {
            if (_baseHealth != null)
                _baseHealth.Changed -= RefreshBase;
            if (_sim != null)
                _sim.GameOver -= ShowGameOver;
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

        private void BuildGameOver(Transform root)
        {
            _gameOverPanel = UiFactory.CreateRect(root, "Game Over", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var overlay = _gameOverPanel.gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.75f);
            overlay.raycastTarget = true;

            var center = new Vector2(0.5f, 0.5f);
            var title = UiFactory.CreateText(_gameOverPanel, "Title", 82, TextAnchor.MiddleCenter, Color.white,
                center, center, new Vector2(-350f, 95f), new Vector2(350f, 215f));
            title.text = "GAME OVER";
            _gameOverWaveText = UiFactory.CreateText(_gameOverPanel, "Final Wave", 48,
                TextAnchor.MiddleCenter, Color.white,
                center, center, new Vector2(-250f, 5f), new Vector2(250f, 80f));
            _restartButton = UiFactory.CreateButton(_gameOverPanel, "Restart", "Restart",
                new Color(0.28f, 0.40f, 0.52f),
                center, center, new Vector2(-180f, -165f), new Vector2(180f, -40f), Restart);

            _gameOverPanel.gameObject.SetActive(false);
        }

        private void RefreshWave()
        {
            if (_sim.State == BattleState.Break)
            {
                int seconds = Mathf.CeilToInt(_sim.BreakRemaining);
                if (_shownBreakSeconds == seconds) return;

                _shownBreakSeconds = seconds;
                _waveText.text = $"Next wave in {seconds}";
                return;
            }

            if (_shownBreakSeconds < 0 && _shownWave == _sim.WaveNumber) return;

            _shownBreakSeconds = -1;
            _shownWave = _sim.WaveNumber;
            _waveText.text = $"Wave {_shownWave}";
        }

        private void RefreshBase()
        {
            _baseText.text = $"Base {_baseHealth.Current}/{_baseHealth.Max}";
        }

        private void RefreshBoss()
        {
            var remaining = _sim.BossTimeRemaining;
            int seconds = remaining.HasValue ? Mathf.CeilToInt(remaining.Value) : -1;
            if (_shownBossSeconds == seconds) return;

            _shownBossSeconds = seconds;
            _bossText.gameObject.SetActive(seconds >= 0);
            if (seconds >= 0)
                _bossText.text = $"BOSS {seconds}s";
        }

        private void ShowGameOver()
        {
            _gameOverWaveText.text = $"Wave {_sim.WaveNumber}";
            _gameOverPanel.gameObject.SetActive(true);
            _restartButton.Select();
        }

        private static void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
