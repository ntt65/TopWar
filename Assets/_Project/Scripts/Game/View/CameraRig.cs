using UnityEngine;

namespace TopWar.Game.View
{
    /// <summary>
    /// 고정 쿼터뷰 카메라. 화면 비율이 바뀌면(기기 회전, Device Simulator) 그리드가 화면에 들어오도록 다시 맞춘다.
    /// 그리드 위쪽(+Z)에 적 레인이 보이도록 공간을 남긴다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        private const float Pitch = 55f;
        private const float FieldOfView = 45f;
        private const float SideMargin = 0.6f;
        private const float LaneSpace = 4f;   // 그리드 위쪽 적 진격 구간
        private const float BottomSpace = 1.5f; // 그리드 아래 기지 구간

        private Camera _camera;
        private GridView _grid;
        private float _fittedAspect;
        private float _fittedWidth;

        public void Init(GridView grid)
        {
            _camera = GetComponent<Camera>();
            _grid = grid;
            Fit();
        }

        private void LateUpdate()
        {
            if (_grid == null) return;
            if (!Mathf.Approximately(_camera.aspect, _fittedAspect) || !Mathf.Approximately(_grid.WorldWidth, _fittedWidth))
                Fit();
        }

        private void Fit()
        {
            _camera.fieldOfView = FieldOfView;

            var halfV = FieldOfView * 0.5f * Mathf.Deg2Rad;
            var halfH = Mathf.Atan(Mathf.Tan(halfV) * _camera.aspect);

            var halfWidth = _grid.WorldWidth * 0.5f + SideMargin;
            var halfDepth = (_grid.WorldDepth + LaneSpace + BottomSpace) * 0.5f;

            // 비스듬히 보면 깊이가 sin(pitch)만큼 줄어 보인다.
            var distanceForWidth = halfWidth / Mathf.Tan(halfH);
            var distanceForDepth = halfDepth * Mathf.Sin(Pitch * Mathf.Deg2Rad) / Mathf.Tan(halfV);
            var distance = Mathf.Max(distanceForWidth, distanceForDepth);

            var target = _grid.transform.position + new Vector3(0f, 0f, (LaneSpace - BottomSpace) * 0.5f);
            var rotation = Quaternion.Euler(Pitch, 0f, 0f);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);

            _fittedAspect = _camera.aspect;
            _fittedWidth = _grid.WorldWidth;
        }
    }
}
