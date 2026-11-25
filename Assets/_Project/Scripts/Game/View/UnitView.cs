using TopWar.Core.Units;
using UnityEngine;

namespace TopWar.Game.View
{
    /// <summary>그리드 위 유닛 하나의 표시. 병종별 도형 + 레벨 숫자.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float SpawnDuration = 0.18f;

        private Transform _label;
        private float _spawnElapsed;

        public UnitData Data { get; private set; }

        public static UnitView Create(UnitData data, Transform parent, Vector3 localPosition)
        {
            var root = new GameObject($"Unit {data}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var view = root.AddComponent<UnitView>();
            view.Data = data;
            view.BuildBody();
            view.BuildLabel();
            root.transform.localScale = Vector3.one * 0.3f;
            return view;
        }

        private void BuildBody()
        {
            var body = GameObject.CreatePrimitive(UnitPalette.Shape(Data.Type));
            body.name = "Body";
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);

            // 레벨이 오를수록 조금씩 커진다.
            var size = 0.55f + 0.04f * (Data.Level - 1);
            var scale = Vector3.Scale(UnitPalette.Proportion(Data.Type), Vector3.one * size);

            // Capsule·Cylinder는 기본 높이가 2라서 절반으로 맞춘다.
            var heightFactor = Data.Type == UnitType.Army ? 1f : 0.5f;
            body.transform.localScale = new Vector3(scale.x, scale.y * heightFactor, scale.z);
            body.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);

            body.GetComponent<Renderer>().material.color = UnitPalette.Color(Data.Type);
        }

        private void BuildLabel()
        {
            var labelObject = new GameObject("Level");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = labelObject.AddComponent<TextMesh>();
            text.font = font;
            text.text = Data.Level.ToString();
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            _label = labelObject.transform;
        }

        private void Update()
        {
            if (_spawnElapsed < SpawnDuration)
            {
                _spawnElapsed += Time.deltaTime;
                var t = Mathf.Clamp01(_spawnElapsed / SpawnDuration);
                // 살짝 커졌다가 돌아오는 팝 효과
                var pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t) * pop;
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null && _label != null)
                _label.rotation = cam.transform.rotation;
        }
    }
}
