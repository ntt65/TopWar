using System;
using TopWar.Core.Runner;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game.Runner
{
    public sealed class RunnerController : MonoBehaviour
    {
        private RunnerSimulation _simulation;
        private RunnerInput _input;
        private RunnerCamera _runnerCamera;
        private CameraRig _cameraRig;
        private bool _cameraRigWasEnabled;
        private Action<bool> _onFinished;

        public void Init(RunnerSimulation sim, RunnerSettings settings, Camera camera, Action<bool> onFinished)
        {
            _simulation = sim;
            _onFinished = onFinished;

            _cameraRig = camera.GetComponent<CameraRig>();
            if (_cameraRig != null)
            {
                _cameraRigWasEnabled = _cameraRig.enabled;
                _cameraRig.enabled = false;
            }

            _runnerCamera = camera.GetComponent<RunnerCamera>();
            if (_runnerCamera == null) _runnerCamera = camera.gameObject.AddComponent<RunnerCamera>();
            _runnerCamera.Init(() => sim.Distance, settings.HalfWidth);

            var squadView = CreateChild<SquadView>("Squad");
            squadView.HalfWidth = settings.HalfWidth;
            squadView.Init(sim);

            var trackView = CreateChild<RunnerTrackView>("Track");
            trackView.SquadTransform = squadView.transform;
            trackView.Init(sim, settings);
            CreateChild<RunnerHud>("Runner HUD").Init(sim);

            _input = gameObject.AddComponent<RunnerInput>();
            _input.Init(sim, settings.HalfWidth);
            sim.Finished += OnFinished;
        }

        private T CreateChild<T>(string objectName) where T : MonoBehaviour
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        private void Update()
        {
            _simulation?.Tick(Time.deltaTime);
        }

        private void OnFinished(bool victory)
        {
            _input.enabled = false;
            _onFinished(victory);
        }

        private void OnDestroy()
        {
            if (_simulation != null) _simulation.Finished -= OnFinished;
            if (_runnerCamera != null) _runnerCamera.StopFollowing();
            if (_cameraRig != null) _cameraRig.enabled = _cameraRigWasEnabled;
        }
    }
}
