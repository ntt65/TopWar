using System;

namespace TopWar.Core.Grid
{
    /// <summary>그리드 좌표. X = 열(레인), Y = 행 (0이 기지 쪽, 위로 갈수록 적 쪽).</summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public int X { get; }
        public int Y { get; }

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);
    }
}
