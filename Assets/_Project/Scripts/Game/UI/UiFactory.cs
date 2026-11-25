using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    internal static class UiFactory
    {
        private static Font _font;

        internal static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        internal static RectTransform CreateRect(Transform parent, string name,
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

        internal static Text CreateText(Transform parent, string name, int size, TextAnchor alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        internal static Button CreateButton(Transform parent, string name, string label, Color color,
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
            return button;
        }
    }
}
