using UnityEngine;

namespace TopWar.Game.View
{
    public sealed class ProjectileView : MonoBehaviour
    {
        private const float Duration = 0.15f;

        private Vector3 _from;
        private EnemyView _target;
        private float _elapsed;

        public static void Fire(Vector3 from, EnemyView target, Color color, Transform parent)
        {
            var projectile = BattlePrimitive.Create(PrimitiveType.Sphere, "Projectile", color, parent);
            projectile.position = from;
            projectile.localScale = Vector3.one * 0.15f;

            var view = projectile.gameObject.AddComponent<ProjectileView>();
            view._from = from;
            view._target = target;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / Duration);
            // AimPosition은 뷰가 제거된 뒤에는 마지막 좌표를 돌려준다.
            transform.position = Vector3.Lerp(_from, _target.AimPosition, progress);
            if (progress >= 1f)
                Destroy(gameObject);
        }
    }
}
