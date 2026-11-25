using TopWar.Core.Economy;
using TopWar.Core.Grid;
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

        public GridModel Grid { get; private set; }
        public Wallet Wallet { get; private set; }
        public SummonService Summon { get; private set; }

        private void Awake()
        {
            var config = balance != null ? balance : ScriptableObject.CreateInstance<BalanceConfig>();

            Grid = new GridModel(config.GridWidth, config.GridHeight);
            Wallet = new Wallet(config.StartingGold);
            Summon = new SummonService(Grid, Wallet, config.SummonBaseCost, config.SummonCostStep);

            var gridView = new GameObject("Grid").AddComponent<GridView>();
            gridView.Init(Grid);

            var cam = GetOrCreateMainCamera();
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Init(gridView);

            gameObject.AddComponent<DragController>().Init(cam, Grid, gridView);

            new GameObject("HUD").AddComponent<Hud>().Init(Wallet, Summon);
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
