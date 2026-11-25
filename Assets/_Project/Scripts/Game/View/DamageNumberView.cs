using UnityEngine;

namespace TopWar.Game.View
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        private const float Duration = 0.6f;
        private const float FadeDuration = 0.2f;
        private const float RiseHeight = 0.8f;
        private const float Spread = 0.15f;

        private static Font _font;

        private TextMesh _text;
        private Transform _camera;
        private Vector3 _startPosition;
        private float _elapsed;

        public static void Show(Vector3 worldPosition, float damage, float counterMultiplier, Transform parent)
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("Damage Number");
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition + new Vector3(
                Random.Range(-Spread, Spread), 0f, Random.Range(-Spread, Spread));

            var text = root.AddComponent<TextMesh>();
            text.font = _font;
            text.text = Mathf.RoundToInt(damage).ToString();
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            if (counterMultiplier > 1f)
            {
                text.color = Color.yellow;
                text.characterSize *= 1.3f;
            }
            else if (counterMultiplier < 1f)
            {
                text.color = Color.gray;
                text.characterSize *= 0.85f;
            }
            root.GetComponent<MeshRenderer>().sharedMaterial = _font.material;

            var view = root.AddComponent<DamageNumberView>();
            view._text = text;
            view._startPosition = root.transform.position;
            var camera = Camera.main;
            if (camera != null)
            {
                view._camera = camera.transform;
                root.transform.rotation = view._camera.rotation;
            }
        }

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= Duration)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _startPosition + Vector3.up * (RiseHeight * _elapsed / Duration);
            if (_camera != null)
                transform.rotation = _camera.rotation;

            if (_elapsed > Duration - FadeDuration)
            {
                var color = _text.color;
                color.a = (Duration - _elapsed) / FadeDuration;
                _text.color = color;
            }
        }
    }
}
