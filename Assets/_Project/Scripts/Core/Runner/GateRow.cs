namespace TopWar.Core.Runner
{
    public sealed class GateRow
    {
        public GateRow(float distance, Gate left, Gate right)
        {
            Distance = distance;
            Left = left;
            Right = right;
        }

        public float Distance { get; }
        public Gate Left { get; }
        public Gate Right { get; }
        public bool Passed { get; internal set; }
    }
}
