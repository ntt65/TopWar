namespace TopWar.Core
{
    /// <summary>게임 전체에서 고정된 규칙 값. 밸런스 값(비용, 공격력 등)은 ScriptableObject에서 주입한다.</summary>
    public static class GameRules
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        public const int DefaultGridSize = 5;
        public const int MaxGridSize = 7;
    }
}
