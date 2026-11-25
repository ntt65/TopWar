using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopWar.Game.UI
{
    public sealed class ResultScreen : MonoBehaviour
    {
        public void Init(int stage, bool victory, int clearedWaves, int totalWaves, int gems)
        {
            var canvas = UiFactory.CreateCanvas(transform);
            canvas.sortingOrder = 2;

            var panel = UiFactory.CreateRect(canvas.transform, "Results", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var overlay = panel.gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.75f);
            overlay.raycastTarget = true;

            var middleLeft = new Vector2(0f, 0.5f);
            var middleRight = new Vector2(1f, 0.5f);
            var titleColor = victory ? Color.yellow : new Color(1f, 0.34f, 0.34f);
            var title = UiFactory.CreateText(panel, "Title", 72, TextAnchor.MiddleCenter, titleColor,
                middleLeft, middleRight, new Vector2(24f, 120f), new Vector2(-24f, 230f));
            title.text = victory ? string.Format(Texts.StageClear, stage) : Texts.Defeat;

            var waves = UiFactory.CreateText(panel, "Waves", 48, TextAnchor.MiddleCenter, Color.white,
                middleLeft, middleRight, new Vector2(24f, 44f), new Vector2(-24f, 114f));
            waves.text = string.Format(Texts.WavesCleared, clearedWaves, totalWaves);

            var reward = UiFactory.CreateText(panel, "Reward", 48, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.3f),
                middleLeft, middleRight, new Vector2(24f, -28f), new Vector2(-24f, 42f));
            reward.text = string.Format(Texts.GemsEarned, gems);

            var center = new Vector2(0.5f, 0.5f);
            var lobby = UiFactory.CreateButton(panel, "Lobby", Texts.Lobby, new Color(0.28f, 0.4f, 0.52f),
                center, center, new Vector2(-180f, -188f), new Vector2(180f, -68f),
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            lobby.Select();
        }
    }
}
