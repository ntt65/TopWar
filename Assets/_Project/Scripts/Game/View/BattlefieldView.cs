using TopWar.Core.Battle;
using UnityEngine;

namespace TopWar.Game.View
{
    public sealed class BattlefieldView : MonoBehaviour
    {
        public void Init(GridView grid, BattleSimulation sim, BattleSettings settings)
        {
            transform.SetParent(grid.transform, false);

            float cellSize = GridView.CellSize;
            int lanes = Mathf.RoundToInt(grid.WorldWidth / cellSize);
            float laneStart = grid.WorldDepth / cellSize - 0.5f;
            float laneEnd = sim.SpawnY;
            if (laneEnd > laneStart)
            {
                float laneMiddle = (laneStart + laneEnd) * 0.5f;
                for (int x = 0; x < lanes; x++)
                {
                    var lane = BattlePrimitive.Create(PrimitiveType.Cube, $"Lane {x}",
                        new Color(0.30f, 0.35f, 0.29f), transform);
                    lane.position = grid.GridToWorld(x, laneMiddle) + Vector3.down * 0.025f;
                    lane.localScale = new Vector3(cellSize * 0.8f, 0.05f, (laneEnd - laneStart) * cellSize);
                }
            }

            var baseBlock = BattlePrimitive.Create(PrimitiveType.Cube, "Base",
                new Color(0.33f, 0.42f, 0.51f), transform);
            baseBlock.position = grid.GridToWorld((lanes - 1) * 0.5f, settings.BaseY) + Vector3.up * 0.15f;
            baseBlock.localScale = new Vector3(grid.WorldWidth, 0.3f, cellSize * 0.6f);
        }
    }

    internal static class BattlePrimitive
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();

        internal static Transform Create(PrimitiveType shape, string name, Color color, Transform parent)
        {
            var primitive = GameObject.CreatePrimitive(shape);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);

            var collider = primitive.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);

            Properties.SetColor(BaseColor, color);
            Properties.SetColor(ColorProperty, color);
            primitive.GetComponent<Renderer>().SetPropertyBlock(Properties);
            return primitive.transform;
        }
    }
}
