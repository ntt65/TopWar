using UnityEngine;

namespace TopWar.Game.View
{
    /// <summary>
    /// 고정 쿼터뷰 카메라. 화면 비율이 바뀌면(기기 변경, Device Simulator) 다시 맞춘다.
    /// - 그리드 가로 폭이 화면에 꽉 차게 거리를 정한다.
    /// - 그리드 아래 끝을 HUD 버튼 바로 위(GridBottomViewport)에 붙인다.
    /// - 그리드 위쪽 남는 공간은 적이 내려오는 레인 구간이 된다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        private const float Pitch = 55f;
        private const float FieldOfView = 45f;
        private const float SideMargin = 0.3f;

        /// <summary>그리드 아래 끝이 놓일 화면 높이 비율 (0 = 맨 아래). HUD 버튼 영역 바로 위.</summary>
        private const float GridBottomViewport = 0.2f;

        /// <summary>그리드 위로 최소한 보여야 하는 적 레인 길이 (월드 단위).</summary>
        private const float MinLaneSpace = 4f;

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

            // 가로: 그리드 폭이 화면에 맞도록
            var distanceForWidth = (_grid.WorldWidth * 0.5f + SideMargin) / Mathf.Tan(halfH);

            // 세로: 화면이 넓적할 때 그리드 + 레인이 화면 위쪽(1 - GridBottomViewport)에 들어가도록
            var visibleFraction = 1f - GridBottomViewport;
            var depthOnScreen = (_grid.WorldDepth + MinLaneSpace) * Mathf.Sin(Pitch * Mathf.Deg2Rad);
            var distanceForDepth = depthOnScreen / (2f * Mathf.Tan(halfV) * visibleFraction);

            var distance = Mathf.Max(distanceForWidth, distanceForDepth);
            var rotation = Quaternion.Euler(Pitch, 0f, 0f);

            // 그리드 아래 끝이 GridBottomViewport 높이에 오도록 바라보는 지점을 앞뒤로 옮긴다 (이분 탐색).
            var center = _grid.transform.position;
            var nearEdge = center + new Vector3(0f, 0f, -_grid.WorldDepth * 0.5f);
            var low = -distance;
            var high = distance;
            for (var i = 0; i < 24; i++)
            {
                var mid = (low + high) * 0.5f;
                Place(center + new Vector3(0f, 0f, mid), rotation, distance);

                // 바라보는 지점이 앞(+Z)으로 갈수록 그리드 아래 끝은 화면 아래로 내려간다.
                if (_camera.WorldToViewportPoint(nearEdge).y < GridBottomViewport)
                    high = mid;
                else
                    low = mid;
            }

            Place(center + new Vector3(0f, 0f, (low + high) * 0.5f), rotation, distance);

            _fittedAspect = _camera.aspect;
            _fittedWidth = _grid.WorldWidth;
        }

        private void Place(Vector3 target, Quaternion rotation, float distance)
        {
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
        }
    }
}
