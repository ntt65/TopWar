using System;
using TopWar.Core.Progression;
using UnityEngine;

namespace TopWar.Game.Save
{
    public static class ProgressStore
    {
        public const string Key = "TopWar.Progress";

        public static PlayerProgress Load()
        {
            var json = PlayerPrefs.GetString(Key);
            if (string.IsNullOrWhiteSpace(json))
                return new PlayerProgress();

            try
            {
                return PlayerProgress.FromData(JsonUtility.FromJson<PlayerProgressData>(json));
            }
            catch (ArgumentException)
            {
                return new PlayerProgress();
            }
        }

        public static void Save(PlayerProgress progress)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(progress.ToData()));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
