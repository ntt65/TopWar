using TopWar.Core;
using UnityEngine;

namespace TopWar.Game.Config
{
    /// <summary>밸런스 값. Project 창에서 Create → TopWar → Balance Config로 에셋을 만들어 조정한다.</summary>
    [CreateAssetMenu(menuName = "TopWar/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField, Range(1, GameRules.MaxGridSize)] private int gridWidth = GameRules.DefaultGridSize;
        [SerializeField, Range(1, GameRules.MaxGridSize)] private int gridHeight = GameRules.DefaultGridSize;

        [Header("Economy")]
        [SerializeField, Min(0)] private int startingGold = 200;
        [SerializeField, Min(0)] private int summonBaseCost = 10;
        [SerializeField, Min(0)] private int summonCostStep = 2;

        [Header("Units")]
        [SerializeField, Min(0f)] private float baseAttack = 10f;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int StartingGold => startingGold;
        public int SummonBaseCost => summonBaseCost;
        public int SummonCostStep => summonCostStep;
        public float BaseAttack => baseAttack;
    }
}
