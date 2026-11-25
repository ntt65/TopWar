using TopWar.Core.Economy;
using TopWar.Core.Units;
using TopWar.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    /// <summary>
    /// 골드, 소환 비용과 병종별 소환 버튼을 표시한다.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        private const float MessageDuration = 1.5f;
        private const int DebugGoldAmount = 100;

        private Wallet _wallet;
        private SummonService _summon;

        private Text _goldText;
        private Text _costText;
        private Text _messageText;
        private float _messageTimer;

        public void Init(Wallet wallet, SummonService summon)
        {
            _wallet = wallet;
            _summon = summon;
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas(transform);
            BuildLayout(canvas.transform);

            _wallet.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_wallet != null)
                _wallet.Changed -= Refresh;
        }

        private void Update()
        {
            if (_messageTimer <= 0f) return;

            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f)
                _messageText.text = string.Empty;
        }

        private void Summon(UnitType type)
        {
            var result = _summon.TrySummon(type, out _);
            switch (result)
            {
                case SummonResult.NotEnoughGold:
                    ShowMessage(Texts.NotEnoughGold);
                    break;
                case SummonResult.GridFull:
                    ShowMessage(Texts.GridFull);
                    break;
            }

            Refresh();
        }

        private void Refresh()
        {
            _goldText.text = string.Format(Texts.Gold, _wallet.Gold);
            _costText.text = string.Format(Texts.SummonCost, _summon.CurrentCost);
        }

        private void ShowMessage(string message)
        {
            _messageText.text = message;
            _messageTimer = MessageDuration;
        }

        // ---------- UI 생성 ----------

        private void BuildLayout(Transform root)
        {
            _goldText = UiFactory.CreateText(root, "Gold", 56, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(0.65f, 1f), new Vector2(48f, -150f), new Vector2(0f, -40f));

            UiFactory.CreateButton(root, "DebugGold", string.Format(Texts.DebugGold, DebugGoldAmount),
                new Color(0.3f, 0.3f, 0.3f, 0.9f),
                new Vector2(0.7f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(-40f, -50f),
                () => _wallet.Add(DebugGoldAmount));

            _messageText = UiFactory.CreateText(root, "Message", 52, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 340f), new Vector2(0f, 420f));

            _costText = UiFactory.CreateText(root, "Cost", 44, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 260f), new Vector2(0f, 330f));

            var types = new[] { UnitType.Army, UnitType.AirForce, UnitType.Navy };
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var minX = i / (float)types.Length;
                var maxX = (i + 1) / (float)types.Length;
                UiFactory.CreateButton(root, $"Summon{type}", UnitPalette.Label(type), UnitPalette.Color(type),
                    new Vector2(minX, 0f), new Vector2(maxX, 0f), new Vector2(24f, 50f), new Vector2(-24f, 240f),
                    () => Summon(type));
            }
        }
    }
}
