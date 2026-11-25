using TopWar.Core.Economy;
using TopWar.Core.Units;
using TopWar.Game.View;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    /// <summary>
    /// 프로토타입 HUD. 코드로 uGUI를 만든다 (에디터 작업 없음).
    /// 한글 폰트는 Phase 3에서 TextMeshPro로 교체하면서 넣는다.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        private const float MessageDuration = 1.5f;
        private const int DebugGoldAmount = 100;

        private Wallet _wallet;
        private SummonService _summon;
        private Font _font;

        private Text _goldText;
        private Text _costText;
        private Text _messageText;
        private float _messageTimer;

        public void Init(Wallet wallet, SummonService summon)
        {
            _wallet = wallet;
            _summon = summon;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            EnsureEventSystem();
            var canvas = CreateCanvas();
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
                    ShowMessage("Not enough gold");
                    break;
                case SummonResult.GridFull:
                    ShowMessage("Grid is full");
                    break;
            }

            Refresh();
        }

        private void Refresh()
        {
            _goldText.text = $"Gold  {_wallet.Gold}";
            _costText.text = $"Summon cost  {_summon.CurrentCost}";
        }

        private void ShowMessage(string message)
        {
            _messageText.text = message;
            _messageTimer = MessageDuration;
        }

        // ---------- UI 생성 ----------

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void BuildLayout(Transform root)
        {
            _goldText = CreateText(root, "Gold", 56, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(0.65f, 1f), new Vector2(48f, -150f), new Vector2(0f, -40f));

            CreateButton(root, "DebugGold", $"+{DebugGoldAmount}G", new Color(0.3f, 0.3f, 0.3f, 0.9f),
                new Vector2(0.7f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(-40f, -50f),
                () => _wallet.Add(DebugGoldAmount));

            _messageText = CreateText(root, "Message", 52, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 340f), new Vector2(0f, 420f));

            _costText = CreateText(root, "Cost", 44, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 260f), new Vector2(0f, 330f));

            var types = new[] { UnitType.Army, UnitType.AirForce, UnitType.Navy };
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var minX = i / (float)types.Length;
                var maxX = (i + 1) / (float)types.Length;
                CreateButton(root, $"Summon{type}", UnitPalette.Label(type), UnitPalette.Color(type),
                    new Vector2(minX, 0f), new Vector2(maxX, 0f), new Vector2(24f, 50f), new Vector2(-24f, 240f),
                    () => Summon(type));
            }
        }

        private static RectTransform CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private Text CreateText(Transform parent, string name, int size, TextAnchor alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private void CreateButton(Transform parent, string name, string label, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, UnityAction onClick)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateText(rect, "Label", 52, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;
        }
    }
}
