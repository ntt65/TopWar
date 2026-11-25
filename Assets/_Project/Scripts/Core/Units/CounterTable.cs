namespace TopWar.Core.Units
{
    /// <summary>병종 상성: 육군 > 공군 > 해군 > 육군.</summary>
    public static class CounterTable
    {
        public const float AdvantageMultiplier = 1.25f;
        public const float DisadvantageMultiplier = 0.85f;
        public const float NeutralMultiplier = 1f;

        public static bool Beats(UnitType attacker, UnitType defender) =>
            (attacker == UnitType.Army && defender == UnitType.AirForce) ||
            (attacker == UnitType.AirForce && defender == UnitType.Navy) ||
            (attacker == UnitType.Navy && defender == UnitType.Army);

        public static float DamageMultiplier(UnitType attacker, UnitType defender)
        {
            if (Beats(attacker, defender)) return AdvantageMultiplier;
            if (Beats(defender, attacker)) return DisadvantageMultiplier;
            return NeutralMultiplier;
        }
    }
}
