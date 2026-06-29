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
            data.NormalizeAchievementCounters();
            data.EnsureCollectionProgress(balance.maxLevel);
            // Migration: before manual claiming, rewards were auto-granted on
            // completion. Treat all already-completed achievements as already claimed
            // so they don't reappear as claimable after this update.
            if (data.claimedAchievementIds == null &&
                data.completedAchievementIds != null)
            {
                data.claimedAchievementIds =
                    (string[])data.completedAchievementIds.Clone();
            }
            data.EnsureAchievementProgress();
            data.miningPlayDate = data.miningPlayDate ?? string.Empty;
            data.miningPlayCount = Mathf.Max(0, data.miningPlayCount);
            data.miningAdBonusCount = Mathf.Max(0, data.miningAdBonusCount);
            data.miningCompletedCount = Mathf.Max(
                0,
                data.miningCompletedCount);
            data.miningCompletionDate =
                data.miningCompletionDate ?? string.Empty;
            data.miningDailyCompletedCount = Mathf.Max(
                0,
                data.miningDailyCompletedCount);
            data.bestMiningScorePermyriad = Mathf.Clamp(
                data.bestMiningScorePermyriad,
                0,
                10000);
            data.highestBlackHoleLevel = Mathf.Clamp(
                data.highestBlackHoleLevel,
                0,
                StarForgeBlackHoleRules.MaxLevel);
            data.blackHoleDiscoveryAttemptCount = Mathf.Clamp(
                data.blackHoleDiscoveryAttemptCount,
                0,
                StarForgeBlackHoleRules.DiscoveryAttemptThreshold);
            if (data.isBlackHole)
            {
                data.blackHoleLevel = Mathf.Clamp(
                    data.blackHoleLevel,
                    StarForgeBlackHoleRules.MinLevel,
                    StarForgeBlackHoleRules.MaxLevel);
                data.highestBlackHoleLevel = Mathf.Max(
                    data.highestBlackHoleLevel,
                    data.blackHoleLevel);
            }
            else
            {
                data.blackHoleLevel = 0;
            }

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
