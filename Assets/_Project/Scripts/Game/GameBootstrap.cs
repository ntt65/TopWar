using TopWar.Core.Battle;
using TopWar.Core.Economy;
using TopWar.Core.Grid;
using TopWar.Core.Progression;
using TopWar.Core.Runner;
using TopWar.Core.Units;
using TopWar.Game.Battle;
using TopWar.Game.Config;
using TopWar.Game.Controls;
using TopWar.Game.Runner;
using TopWar.Game.Save;
using TopWar.Game.UI;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game
{
    /// <summary>
    /// 진행 상태를 불러오고 로비, 전투, 결과 화면을 연결한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("비워 두면 기본값으로 실행한다.")]
        [SerializeField] private BalanceConfig balance;

        private PlayerProgress _progress;
        private Camera _camera;
        private LobbyScreen _lobby;
        private int _stage;
        private GridView _gridView;
        private DragController _dragController;
        private CanvasGroup _hudInput;

        public GridModel Grid { get; private set; }
        public Wallet Wallet { get; private set; }
        public SummonService Summon { get; private set; }
        public BattleSimulation Battle { get; private set; }
        public BaseHealth BaseHealth { get; private set; }

        private void Awake()
        {
            _progress = ProgressStore.Load();
            _progress.Changed += SaveProgress;
            UiFactory.EnsureEventSystem();
            _camera = GetOrCreateMainCamera();

            _lobby = new GameObject("Lobby").AddComponent<LobbyScreen>();
            _lobby.Init(_progress, StartStage, StartRunner);
        }

        private void StartStage(int stage)
        {
            if (_lobby == null) return;

            _progress.Changed -= SaveProgress;
            _lobby.gameObject.SetActive(false);
            Destroy(_lobby.gameObject);
            _lobby = null;
            _stage = stage;

            var config = balance != null ? balance : ScriptableObject.CreateInstance<BalanceConfig>();

            var gridSize = _progress.GetGridSize();
            Grid = new GridModel(gridSize, gridSize);
            Wallet = new Wallet(config.StartingGold);
            Summon = new SummonService(Grid, Wallet, config.SummonBaseCost, config.SummonCostStep);

            _gridView = new GameObject("Grid").AddComponent<GridView>();
            _gridView.Init(Grid);

            var rig = _camera.GetComponent<CameraRig>();
            if (rig == null) rig = _camera.gameObject.AddComponent<CameraRig>();
            rig.Init(_gridView);

            _dragController = gameObject.AddComponent<DragController>();
            _dragController.Init(_camera, Grid, _gridView);

            var hud = new GameObject("HUD");
            _hudInput = hud.AddComponent<CanvasGroup>();
            hud.AddComponent<Hud>().Init(Wallet, Summon);

            var battleSettings = StageRules.CreateSettings(stage, _progress, config.BaseAttack);
            BaseHealth = new BaseHealth(battleSettings.BaseMaxHealth);
            Battle = new BattleSimulation(Grid, Wallet, BaseHealth, new WaveGenerator(battleSettings), battleSettings);
            Battle.GameOver += OnGameOver;
            Battle.StageCleared += OnStageCleared;

            new GameObject("Battlefield").AddComponent<BattlefieldView>().Init(_gridView, Battle, battleSettings);
            new GameObject("Battle Runner").AddComponent<BattleRunner>().Init(Battle, Grid, _gridView);
            new GameObject("Battle HUD").AddComponent<BattleHud>().Init(Battle, BaseHealth);
        }

        private void OnDestroy()
        {
            if (_progress != null)
                _progress.Changed -= SaveProgress;
            if (Battle != null)
            {
                Battle.GameOver -= OnGameOver;
                Battle.StageCleared -= OnStageCleared;
            }
        }

        private void StartRunner(int stage)
        {
            if (_lobby == null) return;

            _progress.Changed -= SaveProgress;
            _lobby.gameObject.SetActive(false);
            Destroy(_lobby.gameObject);
            _lobby = null;

            var config = balance != null ? balance : ScriptableObject.CreateInstance<BalanceConfig>();
            var settings = new RunnerSettings
            {
                UnitBaseAttack = config.BaseAttack,
                ArmyAttackMultiplier = _progress.GetAttackMultiplier(UnitType.Army),
                AirForceAttackMultiplier = _progress.GetAttackMultiplier(UnitType.AirForce),
                NavyAttackMultiplier = _progress.GetAttackMultiplier(UnitType.Navy),
            };

            var track = RunnerTrackGenerator.Create(stage, seed: stage);
            var squad = new Squad(settings.StartType, settings.StartCount);
            var simulation = new RunnerSimulation(squad, track, settings);
            new GameObject("Runner").AddComponent<RunnerController>().Init(simulation, settings, _camera, victory =>
                new GameObject("Result").AddComponent<ResultScreen>().Init(stage, victory, 0, 0, 0));
        }

        private void SaveProgress()
        {
            ProgressStore.Save(_progress);
        }

        private void OnGameOver()
        {
            FinishStage(false);
        }

        private void OnStageCleared()
        {
            FinishStage(true);
        }

        private void FinishStage(bool victory)
        {
            DisableGameplayInput();

            var clearedWaves = Battle.ClearedWaves;
            var gems = StageRules.GemReward(_stage, clearedWaves, victory);
            _progress.AddGems(gems);
            if (victory)
                _progress.RecordStageCleared(_stage);
            SaveProgress();

            new GameObject("Result").AddComponent<ResultScreen>()
                .Init(_stage, victory, clearedWaves, StageRules.WavesPerStage, gems);
        }

        private void DisableGameplayInput()
        {
            // 게임 오버가 드래그 도중 발생해도 놓기 입력을 처리하지 않는다.
            _dragController.enabled = false;
            foreach (var (pos, _) in Grid.Units())
                _gridView.ResetUnitPosition(pos);

            _hudInput.interactable = false;
            _hudInput.blocksRaycasts = false;
        }

        private static Camera GetOrCreateMainCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;

            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            return go.GetComponent<Camera>();
        }
    }
}
