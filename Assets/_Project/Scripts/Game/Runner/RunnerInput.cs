using TopWar.Core.Runner;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TopWar.Game.Runner
{
    [DefaultExecutionOrder(-10)]
    public sealed class RunnerInput : MonoBehaviour
    {
        private const float Sensitivity = 1.2f;

        private RunnerSimulation _simulation;
        private Pointer _dragPointer;
        private float _dragWorldWidth;
        private float _startScreenX;
        private float _startX;

        public void Init(RunnerSimulation sim, float halfWidth)
        {
            _simulation = sim;
            _dragWorldWidth = 2f * halfWidth * Sensitivity;
        }

        private void Update()
        {
            if (_simulation == null) return;

            if (_dragPointer == null)
            {
                var pointer = Pointer.current;
                if (pointer == null || !pointer.press.wasPressedThisFrame) return;

                var eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.IsPointerOverGameObject()) return;

                _dragPointer = pointer;
                _startScreenX = pointer.position.ReadValue().x;
                _startX = _simulation.X;
            }

            if (!_dragPointer.added || !_dragPointer.press.isPressed)
            {
                _dragPointer = null;
                return;
            }

            int width = Screen.width;
            if (width <= 0) return;

            float screenOffset = _dragPointer.position.ReadValue().x - _startScreenX;
            _simulation.SetX(_startX + screenOffset / width * _dragWorldWidth);
        }

        private void OnDisable()
        {
            _dragPointer = null;
        }
    }
}
