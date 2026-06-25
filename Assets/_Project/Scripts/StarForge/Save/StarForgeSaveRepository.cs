using StarForge.Core;
using StarForge.Data;
using UnityEngine;

namespace StarForge.Save
{
    public sealed class StarForgeSaveRepository
    {
        private const string SaveKey = "StarForge.SaveData.v2";

        public StarForgeSaveData Load(StarForgeBalance balance)
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return CreateNewWithShape(balance);
            }

            string json = PlayerPrefs.GetString(SaveKey);
            StarForgeSaveData data = JsonUtility.FromJson<StarForgeSaveData>(json);
            if (data == null)
            {
                return CreateNewWithShape(balance);
            }

            data.EnsureCurrencies(balance.firstLaunchMeteorFragments);
            data.currentLevel = Mathf.Clamp(data.currentLevel, 0, balance.maxLevel);
            data.highestLevel = Mathf.Clamp(data.highestLevel, data.currentLevel, balance.maxLevel);
            data.planetShape = Mathf.Clamp(
                data.planetShape,
                (int)StarForgePlanetShape.Default,
                (int)StarForgePlanetShape.Cat);
            data.lastDestroyedShape = Mathf.Clamp(
                data.lastDestroyedShape,
                (int)StarForgePlanetShape.Default,
                (int)StarForgePlanetShape.Cat);
            data.NormalizeFractureProgress();
            data.NormalizeAudioSettings();
            data.EnsureCollectionProgress(balance.maxLevel);
            data.miningPlayDate = data.miningPlayDate ?? string.Empty;
            data.miningPlayCount = Mathf.Max(0, data.miningPlayCount);
            data.miningAdBonusCount = Mathf.Max(0, data.miningAdBonusCount);
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
            return CreateNewWithShape(balance);
        }

        private static StarForgeSaveData CreateNewWithShape(StarForgeBalance balance)
        {
            StarForgeSaveData data = StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
            data.planetShape = (int)StarForgePlanetShapes.Roll(balance.shapeChancesPercent, () => Random.value);
            data.lastDestroyedShape = data.planetShape;
            data.EnsureCollectionProgress(balance.maxLevel);
            return data;
        }
    }
}
