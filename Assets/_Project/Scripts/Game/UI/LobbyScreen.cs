using System;
using TopWar.Core.Progression;
using TopWar.Core.Units;
using TopWar.Game.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    public sealed class LobbyScreen : MonoBehaviour
    {
        private PlayerProgress _progress;
        private Text _gemsText;
        private Text _startLabel;
        private (UpgradeType Type, Text Level, Text Effect, Button Buy, Text BuyLabel)[] _rows;

        public void Init(PlayerProgress progress, Action<int> onStart)
        {
            _progress = progress;
            var canvas = UiFactory.CreateCanvas(transform);
            var root = canvas.transform;

            var background = UiFactory.CreateRect(root, "Background", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            background.color = new Color(0.07f, 0.11f, 0.16f);
            background.raycastTarget = false;

            var title = UiFactory.CreateText(root, "Title", 68, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0.74f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);
            title.text = Texts.LobbyTitle;
            _gemsText = UiFactory.CreateText(root, "Gems", 44, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.3f),
                new Vector2(0f, 0.67f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero);

            var reset = UiFactory.CreateButton(root, "Reset", Texts.Reset, new Color(0.24f, 0.3f, 0.37f),
                Vector2.one, Vector2.one, new Vector2(-180f, -108f), new Vector2(-32f, -36f), ResetProgress);
            reset.GetComponentInChildren<Text>().fontSize = 32;

            var upgrades = UiFactory.CreateRect(root, "Upgrades", new Vector2(0.05f, 0.24f),
                new Vector2(0.95f, 0.64f), Vector2.zero, Vector2.zero);
            _rows = new[]
            {
                CreateUpgradeRow(upgrades, UpgradeType.ArmyAttack, Texts.ArmyAttack, 0),
                CreateUpgradeRow(upgrades, UpgradeType.AirForceAttack, Texts.AirAttack, 1),
                CreateUpgradeRow(upgrades, UpgradeType.NavyAttack, Texts.NavyAttack, 2),
                CreateUpgradeRow(upgrades, UpgradeType.BaseHealth, Texts.BaseHealth, 3),
                CreateUpgradeRow(upgrades, UpgradeType.GridSize, Texts.GridSize, 4),
            };

            var start = UiFactory.CreateButton(root, "Start", string.Empty, new Color(0.15f, 0.43f, 0.62f),
                new Vector2(0.16f, 0.07f), new Vector2(0.84f, 0.19f), Vector2.zero, Vector2.zero,
                () => onStart(_progress.NextStage));
            _startLabel = start.GetComponentInChildren<Text>();

            _progress.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_progress != null)
                _progress.Changed -= Refresh;
        }

        private (UpgradeType Type, Text Level, Text Effect, Button Buy, Text BuyLabel) CreateUpgradeRow(
            Transform parent, UpgradeType type, string name, int index)
        {
            const float rowHeight = 1f / 5f;
            var top = 1f - index * rowHeight;
            var row = UiFactory.CreateRect(parent, type.ToString(),
                new Vector2(0f, top - rowHeight), new Vector2(1f, top),
                new Vector2(0f, 4f), new Vector2(0f, -4f));
            var background = row.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.18f, 0.24f);
            background.raycastTarget = false;

            var nameText = UiFactory.CreateText(row, "Name", 38, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.04f, 0f), new Vector2(0.32f, 1f), Vector2.zero, Vector2.zero);
            nameText.text = name;
            var level = UiFactory.CreateText(row, "Level", 34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.32f, 0f), new Vector2(0.47f, 1f), Vector2.zero, Vector2.zero);
            var effect = UiFactory.CreateText(row, "Effect", 34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.47f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero);
            var buy = UiFactory.CreateButton(row, "Buy", string.Empty, new Color(0.2f, 0.37f, 0.5f),
                new Vector2(0.7f, 0.1f), new Vector2(0.97f, 0.9f), Vector2.zero, Vector2.zero,
                () => _progress.TryUpgrade(type));
            var buyLabel = buy.GetComponentInChildren<Text>();
            buyLabel.fontSize = 34;
            return (type, level, effect, buy, buyLabel);
        }

        private void Refresh()
        {
            _gemsText.text = string.Format(Texts.Gems, _progress.Gems);
            _startLabel.text = string.Format(Texts.StartStage, _progress.NextStage);

            foreach (var row in _rows)
            {
                row.Level.text = string.Format(Texts.Level, _progress.GetLevel(row.Type));
                row.Effect.text = row.Type switch
                {
                    UpgradeType.ArmyAttack => FormatAttackBonus(UnitType.Army),
                    UpgradeType.AirForceAttack => FormatAttackBonus(UnitType.AirForce),
                    UpgradeType.NavyAttack => FormatAttackBonus(UnitType.Navy),
                    UpgradeType.BaseHealth => string.Format(Texts.HealthBonus,
                        _progress.GetBaseMaxHealth(StageRules.BaseMaxHealth)),
                    UpgradeType.GridSize => string.Format(Texts.GridDimensions, _progress.GetGridSize()),
                    _ => string.Empty,
                };
                row.BuyLabel.text = _progress.IsMaxed(row.Type)
                    ? Texts.Max
                    : string.Format(Texts.UpgradeCost, _progress.GetCost(row.Type));
                row.Buy.interactable = _progress.CanUpgrade(row.Type);
            }
        }

        private string FormatAttackBonus(UnitType type)
        {
            var percent = Mathf.RoundToInt((_progress.GetAttackMultiplier(type) - 1f) * 100f);
            return string.Format(Texts.AttackBonus, percent);
        }

        private void ResetProgress()
        {
            gameObject.SetActive(false);
            ProgressStore.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
