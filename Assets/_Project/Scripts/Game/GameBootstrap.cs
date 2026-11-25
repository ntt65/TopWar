using TopWar.Core.Battle;
using TopWar.Core.Economy;
using TopWar.Core.Grid;
using TopWar.Game.Battle;
using TopWar.Game.Config;
using TopWar.Game.Controls;
using TopWar.Game.UI;
using TopWar.Game.View;
using UnityEngine;

namespace TopWar.Game
{
    /// <summary>
    /// 게임 진입점. 씬의 빈 GameObject에 이 컴포넌트 하나만 붙이면
    /// 그리드, 카메라, 드래그 입력, HUD를 모두 코드로 만든다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("비워 두면 기본값으로 실행한다.")]
        [SerializeField] private BalanceConfig balance;

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
            var config = balance != null ? balance : ScriptableObject.CreateInstance<BalanceConfig>();

            Grid = new GridModel(config.GridWidth, config.GridHeight);
            Wallet = new Wallet(config.StartingGold);
            Summon = new SummonService(Grid, Wallet, config.SummonBaseCost, config.SummonCostStep);

            _gridView = new GameObject("Grid").AddComponent<GridView>();
            _gridView.Init(Grid);

            var cam = GetOrCreateMainCamera();
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Init(_gridView);

            _dragController = gameObject.AddComponent<DragController>();
            _dragController.Init(cam, Grid, _gridView);

            var hud = new GameObject("HUD");
            _hudInput = hud.AddComponent<CanvasGroup>();
            hud.AddComponent<Hud>().Init(Wallet, Summon);

            var battleSettings = new BattleSettings { UnitBaseAttack = config.BaseAttack };
            BaseHealth = new BaseHealth(battleSettings.BaseMaxHealth);
            Battle = new BattleSimulation(Grid, Wallet, BaseHealth, new WaveGenerator(battleSettings), battleSettings);
            Battle.GameOver += DisableGameplayInput;

            new GameObject("Battlefield").AddComponent<BattlefieldView>().Init(_gridView, Battle, battleSettings);
            new GameObject("Battle Runner").AddComponent<BattleRunner>().Init(Battle, Grid, _gridView);
            new GameObject("Battle HUD").AddComponent<BattleHud>().Init(Battle, BaseHealth);
        }

        private void OnDestroy()
        {
            if (Battle != null)
                Battle.GameOver -= DisableGameplayInput;
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
