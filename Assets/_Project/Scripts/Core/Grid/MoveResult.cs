namespace TopWar.Core.Grid
{
    public enum MoveResult
    {
        /// <summary>범위 밖이거나 출발 칸이 비어 있음.</summary>
        Invalid,

        /// <summary>같은 칸에 놓음. 변화 없음.</summary>
        None,

        /// <summary>빈 칸으로 이동.</summary>
        Moved,

        /// <summary>합성할 수 없는 유닛과 자리 교환.</summary>
        Swapped,

        /// <summary>같은 병종·레벨 유닛과 합성되어 도착 칸에 Lv.N+1 유닛이 생김.</summary>
        Merged,
    }
}
