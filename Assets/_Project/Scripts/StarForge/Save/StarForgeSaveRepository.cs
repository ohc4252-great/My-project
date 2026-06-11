using StarForge.Data;
using UnityEngine;

namespace StarForge.Save
{
    public sealed class StarForgeSaveRepository
    {
        private const string SaveKey = "StarForge.SaveData.v1";

        public StarForgeSaveData Load(StarForgeBalance balance)
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
            }

            string json = PlayerPrefs.GetString(SaveKey);
            StarForgeSaveData data = JsonUtility.FromJson<StarForgeSaveData>(json);
            if (data == null)
            {
                return StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
            }

            data.EnsureCurrencies(balance.firstLaunchMeteorFragments);
            data.currentLevel = Mathf.Clamp(data.currentLevel, 0, balance.maxLevel);
            data.highestLevel = Mathf.Clamp(data.highestLevel, data.currentLevel, balance.maxLevel);
            return data;
        }

        public void Save(StarForgeSaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public StarForgeSaveData Reset(StarForgeBalance balance)
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            return StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
        }
    }
}
