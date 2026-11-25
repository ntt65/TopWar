using System;
using UnityEngine;

namespace TopWar.Game.Runner
{
    [RequireComponent(typeof(Camera))]
    public sealed class RunnerCamera : MonoBehaviour
    {
        private const float Pitch = 50f;
        private const float FieldOfView = 50f;
        private const float LookAhead = 6f;
        private const float FollowSpeed = 8f;

        private Camera _camera;
        private Func<float> _getDistance;
        private float _halfWidth;
        private float _targetZ;
        private float _fittedAspect;
        private Vector3 _offset;
        private Quaternion _rotation;

        public void Init(Func<float> getDistance, float halfWidth)
        {
            _camera = GetComponent<Camera>();
            _camera.fieldOfView = FieldOfView;
            _camera.orthographic = false;
            _getDistance = getDistance;
            _halfWidth = halfWidth;
            _targetZ = getDistance() + LookAhead;
            _rotation = Quaternion.Euler(Pitch, 0f, 0f);

            Fit();
            Place();
            enabled = true;
        }

        internal void StopFollowing()
        {
            _getDistance = null;
            enabled = false;
        }

        private void LateUpdate()
        {
            if (_getDistance == null) return;
            if (!Mathf.Approximately(_camera.aspect, _fittedAspect)) Fit();

            float follow = 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime);
            _targetZ = Mathf.Lerp(_targetZ, _getDistance() + LookAhead, follow);
            Place();
        }

        private void Fit()
        {
            _fittedAspect = _camera.aspect;
            float verticalSlope = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            float horizontalSlope = verticalSlope * Mathf.Max(_fittedAspect, 0.01f);

            // 앞쪽을 바라봐도 무리 뒤쪽까지 화면 가로와 세로에 담는다.
            float rearOffset = LookAhead + _halfWidth;
            float distanceForWidth = (_halfWidth + 0.5f) / horizontalSlope;
            float distanceForHeight = rearOffset * Mathf.Sin(Pitch * Mathf.Deg2Rad) / (verticalSlope * 0.85f);
            float distance = rearOffset * Mathf.Cos(Pitch * Mathf.Deg2Rad) + Mathf.Max(distanceForWidth, distanceForHeight);
            _offset = _rotation * Vector3.forward * distance;
        }

        private void Place()
        {
            transform.SetPositionAndRotation(new Vector3(0f, 0f, _targetZ) - _offset, _rotation);
        }
    }
}
