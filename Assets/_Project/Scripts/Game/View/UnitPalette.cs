using TopWar.Core.Units;
using UnityEngine;

namespace TopWar.Game.View
{
    /// <summary>프로토타입용 병종별 도형과 색. Phase 4에서 3D 모델로 교체한다.</summary>
    public static class UnitPalette
    {
        public static PrimitiveType Shape(UnitType type) => type switch
        {
            UnitType.Army => PrimitiveType.Cube,
            UnitType.AirForce => PrimitiveType.Capsule,
            UnitType.Navy => PrimitiveType.Cylinder,
            _ => PrimitiveType.Sphere,
        };

        /// <summary>도형의 기본 비율 (가로, 높이, 세로).</summary>
        public static Vector3 Proportion(UnitType type) => type switch
        {
            UnitType.Army => new Vector3(1f, 0.7f, 1f),
            UnitType.AirForce => new Vector3(0.7f, 0.5f, 0.7f),
            UnitType.Navy => new Vector3(1f, 0.25f, 1f),
            _ => Vector3.one,
        };

        public static Color Color(UnitType type) => type switch
        {
            UnitType.Army => new Color(0.36f, 0.62f, 0.25f),
            UnitType.AirForce => new Color(0.95f, 0.65f, 0.15f),
            UnitType.Navy => new Color(0.2f, 0.45f, 0.9f),
            _ => UnityEngine.Color.white,
        };

        public static string Label(UnitType type) => type switch
        {
            UnitType.Army => "ARMY",
            UnitType.AirForce => "AIR",
            UnitType.Navy => "NAVY",
            _ => type.ToString(),
        };
    }
}
