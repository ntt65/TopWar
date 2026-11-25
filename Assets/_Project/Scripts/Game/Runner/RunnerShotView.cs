using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game.Runner
{
    public sealed class RunnerShotView : MonoBehaviour
    {
        private const float Duration = 0.12f;

        private Vector3 _from;
        private Vector3 _to;
        private float _elapsed;

        public static void Fire(Vector3 from, Vector3 to, Color color, Transform parent)
        {
            var shot = BattlePrimitive.Create(PrimitiveType.Sphere, "Runner Shot", color, parent);
            shot.position = from;
            shot.localScale = Vector3.one * 0.12f;

            var view = shot.gameObject.AddComponent<RunnerShotView>();
            view._from = from;
            view._to = to;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(_from, _to, _elapsed / Duration);
            if (_elapsed >= Duration) Destroy(gameObject);
        }
    }
}
