using System;
using StarForge.Core;

namespace StarForge.Save
{
    [Serializable]
    public sealed class StarForgeSaveData
    {
        public int currentLevel;
        public int highestLevel;
        public int planetShape;
        public int lastDestroyedShape;
        public bool collectionProgressInitialized;
        public int defaultHighestLevel;
        public int heartHighestLevel;
        public int catHighestLevel;
        public bool isFractured;
        public int fractureCount;
        public int destructionCount;
        public int reviveCount;
        public int attemptCount;
        public int successCount;
        public int failureCount;
        public int totalFractureCount;
        public int greatSuccessCount;
        public int consecutiveFailureCount;
        public int consecutiveFractureCount;
        public int consecutiveSuccessCount;
        public int consecutiveGreatSuccessCount;
        public int selectedCurrency;
        public bool soundEnabled = true;
        public bool audioVolumeInitialized;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;
        public bool enhancementAnimationSkipEnabled;
        // When true, fracture (균열) results skip the popup overlay (alerts muted).
        public bool fractureAlertMuted;
        // When true, the achievement-clear toast is suppressed.
        public bool achievementAlertMuted;
        public string primordialExchangeDate;
        public int primordialExchangeCount;
        public string miningPlayDate;
        public int miningPlayCount;
        public int miningAdBonusCount;
        public int miningCompletedCount;
        public string miningCompletionDate;
        public int miningDailyCompletedCount;
        public int bestMiningScorePermyriad;
        public bool isBlackHole;
        public int blackHoleLevel;
        public int highestBlackHoleLevel;
        public int blackHoleDiscoveryAttemptCount;
        // Normal planet stage held while a black hole is active, so disassembling or
        // losing the black hole restores the player's planet instead of wiping it.
        public int blackHolePreviousLevel;
        public int blackHolePreviousShape;
        // Up to 3 keep-level revive ads per planet "life": each time the player spends
        // an ad to keep a destroyed stage this increments; once it hits the cap the
        // option is withheld until a new star begins (checkpoint revive, 0강 재시작, or a
        // voluntary disassemble), which resets it to 0.
        public int keepLevelAdUses;
        public string[] completedAchievementIds;
        // Achievements whose rewards the player has manually claimed (subset of
        // completed). Completed-but-not-claimed = a reward waiting to be received.
        public string[] claimedAchievementIds;
        public CurrencyAmount[] currencies;

        public static StarForgeSaveData CreateNew(int firstLaunchMeteorFragments)
        {
            StarForgeSaveData data = new StarForgeSaveData();
            data.currentLevel = 0;
            data.highestLevel = 0;
            data.planetShape = (int)StarForgePlanetShape.Default;
            data.lastDestroyedShape = (int)StarForgePlanetShape.Default;
            data.collectionProgressInitialized = false;
            data.defaultHighestLevel = 0;
            data.heartHighestLevel = -1;
            data.catHighestLevel = -1;
            data.isFractured = false;
            data.fractureCount = 0;
            data.destructionCount = 0;
            data.reviveCount = 0;
            data.attemptCount = 0;
            data.successCount = 0;
            data.failureCount = 0;
            data.totalFractureCount = 0;
            data.greatSuccessCount = 0;
            data.consecutiveFailureCount = 0;
            data.consecutiveFractureCount = 0;
            data.consecutiveSuccessCount = 0;
            data.consecutiveGreatSuccessCount = 0;
            data.selectedCurrency = (int)StarForgeCurrencyType.MeteorFragment;
            data.soundEnabled = true;
            data.audioVolumeInitialized = true;
            data.bgmVolume = 1f;
            data.sfxVolume = 1f;
            data.vibrationEnabled = true;
            data.enhancementAnimationSkipEnabled = false;
            data.fractureAlertMuted = false;
            data.achievementAlertMuted = false;
            data.primordialExchangeDate = string.Empty;
            data.primordialExchangeCount = 0;
            data.miningPlayDate = string.Empty;
            data.miningPlayCount = 0;
            data.miningAdBonusCount = 0;
            data.miningCompletedCount = 0;
            data.miningCompletionDate = string.Empty;
            data.miningDailyCompletedCount = 0;
            data.bestMiningScorePermyriad = 0;
            data.isBlackHole = false;
            data.blackHoleLevel = 0;
            data.highestBlackHoleLevel = 0;
            data.blackHoleDiscoveryAttemptCount = 0;
            data.blackHolePreviousLevel = 0;
            data.blackHolePreviousShape = (int)StarForgePlanetShape.Default;
            data.keepLevelAdUses = 0;
            data.completedAchievementIds = new string[0];
            data.claimedAchievementIds = new string[0];
            data.currencies = CreateCurrencyArray(firstLaunchMeteorFragments);
            return data;
        }

        public void NormalizeAudioSettings()
        {
            if (!audioVolumeInitialized)
            {
                bgmVolume = 1f;
                sfxVolume = 1f;
                audioVolumeInitialized = true;
            }

            bgmVolume = Math.Max(0f, Math.Min(1f, bgmVolume));
            sfxVolume = Math.Max(0f, Math.Min(1f, sfxVolume));
        }

        public void NormalizeFractureProgress()
        {
            fractureCount = Math.Max(0, fractureCount);
            if (isFractured && fractureCount == 0)
            {
                fractureCount = 1;
            }

            isFractured = fractureCount > 0;
        }

        public void AddFracture()
        {
            fractureCount = Math.Max(0, fractureCount) + 1;
            isFractured = true;
        }

        public void ResetFractures()
        {
            fractureCount = 0;
            isFractured = false;
        }

        // Keep-level revive ad: up to this many uses per planet life.
        public const int KeepLevelAdMaxUsesPerLife = 3;

        public bool CanUseKeepLevelAd()
        {
            return Math.Max(0, keepLevelAdUses) < KeepLevelAdMaxUsesPerLife;
        }

        public int RemainingKeepLevelAds()
        {
            return Math.Max(0, KeepLevelAdMaxUsesPerLife - Math.Max(0, keepLevelAdUses));
        }

        public void NormalizeAchievementCounters()
        {
            destructionCount = Math.Max(0, destructionCount);
            reviveCount = Math.Max(0, reviveCount);
            attemptCount = Math.Max(0, attemptCount);
            successCount = Math.Max(0, successCount);
            failureCount = Math.Max(0, failureCount);
            totalFractureCount = Math.Max(0, totalFractureCount);
            greatSuccessCount = Math.Max(0, greatSuccessCount);
            consecutiveFailureCount = Math.Max(0, consecutiveFailureCount);
            consecutiveFractureCount = Math.Max(0, consecutiveFractureCount);
            consecutiveSuccessCount = Math.Max(0, consecutiveSuccessCount);
            consecutiveGreatSuccessCount =
                Math.Max(0, consecutiveGreatSuccessCount);
            miningCompletedCount = Math.Max(0, miningCompletedCount);
            miningCompletionDate = miningCompletionDate ?? string.Empty;
            miningDailyCompletedCount =
                Math.Max(0, miningDailyCompletedCount);
            bestMiningScorePermyriad =
                Math.Min(10000, Math.Max(0, bestMiningScorePermyriad));
        }

        public void RecordMiningCompletion(
            string localDate,
            int scorePermyriad)
        {
            string safeDate = localDate ?? string.Empty;
            if (!string.Equals(
                    miningCompletionDate,
                    safeDate,
                    StringComparison.Ordinal))
            {
                miningCompletionDate = safeDate;
                miningDailyCompletedCount = 0;
            }

            miningCompletedCount = Math.Max(0, miningCompletedCount) + 1;
            miningDailyCompletedCount =
                Math.Max(0, miningDailyCompletedCount) + 1;
            bestMiningScorePermyriad = Math.Max(
                bestMiningScorePermyriad,
                Math.Min(10000, Math.Max(0, scorePermyriad)));
        }

        public void RecordSuccessOutcome(bool greatSuccess)
        {
            successCount = Math.Max(0, successCount) + 1;
            consecutiveSuccessCount =
                Math.Max(0, consecutiveSuccessCount) + 1;
            consecutiveFailureCount = 0;
            consecutiveFractureCount = 0;

            if (greatSuccess)
            {
                greatSuccessCount = Math.Max(0, greatSuccessCount) + 1;
                consecutiveGreatSuccessCount =
                    Math.Max(0, consecutiveGreatSuccessCount) + 1;
            }
            else
            {
                consecutiveGreatSuccessCount = 0;
            }
        }

        public void RecordFailureOutcome()
        {
            failureCount = Math.Max(0, failureCount) + 1;
            consecutiveFailureCount =
                Math.Max(0, consecutiveFailureCount) + 1;
            consecutiveFractureCount = 0;
            consecutiveSuccessCount = 0;
            consecutiveGreatSuccessCount = 0;
        }

        public void RecordFractureOutcome()
        {
            totalFractureCount = Math.Max(0, totalFractureCount) + 1;
            consecutiveFractureCount =
                Math.Max(0, consecutiveFractureCount) + 1;
            consecutiveFailureCount = 0;
            consecutiveSuccessCount = 0;
            consecutiveGreatSuccessCount = 0;
        }

        public void RecordDestructionOutcome()
        {
            destructionCount = Math.Max(0, destructionCount) + 1;
            consecutiveFailureCount = 0;
            consecutiveFractureCount = 0;
            consecutiveSuccessCount = 0;
            consecutiveGreatSuccessCount = 0;
        }

        public void EnsureCollectionProgress(int maxLevel)
        {
            int clampedMaxLevel = Math.Max(0, maxLevel);
            if (!collectionProgressInitialized)
            {
                defaultHighestLevel = 0;
                heartHighestLevel = -1;
                catHighestLevel = -1;
                collectionProgressInitialized = true;
            }

            defaultHighestLevel = Math.Min(
                clampedMaxLevel,
                Math.Max(0, defaultHighestLevel));
            heartHighestLevel = Math.Min(
                clampedMaxLevel,
                Math.Max(-1, heartHighestLevel));
            catHighestLevel = Math.Min(
                clampedMaxLevel,
                Math.Max(-1, catHighestLevel));
            RecordPlanetProgress(
                (StarForgePlanetShape)planetShape,
                currentLevel,
                clampedMaxLevel);
        }

        public void RecordPlanetProgress(
            StarForgePlanetShape shape,
            int level,
            int maxLevel)
        {
            int clampedLevel = Math.Min(
                Math.Max(0, maxLevel),
                Math.Max(0, level));
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    heartHighestLevel = Math.Max(
                        heartHighestLevel,
                        clampedLevel);
                    break;
                case StarForgePlanetShape.Cat:
                    catHighestLevel = Math.Max(
                        catHighestLevel,
                        clampedLevel);
                    break;
                default:
                    defaultHighestLevel = Math.Max(
                        defaultHighestLevel,
                        clampedLevel);
                    break;
            }
        }

        public int GetShapeHighestLevel(StarForgePlanetShape shape)
        {
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return heartHighestLevel;
                case StarForgePlanetShape.Cat:
                    return catHighestLevel;
                default:
                    return Math.Max(0, defaultHighestLevel);
            }
        }

        public bool IsShapeDiscovered(StarForgePlanetShape shape)
        {
            return shape == StarForgePlanetShape.Default ||
                   GetShapeHighestLevel(shape) >= 0;
        }

        public void EnsureAchievementProgress()
        {
            if (completedAchievementIds == null)
            {
                completedAchievementIds = new string[0];
            }

            if (claimedAchievementIds == null)
            {
                claimedAchievementIds = new string[0];
            }
        }

        public bool HasCompletedAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                return false;
            }

            EnsureAchievementProgress();
            for (int i = 0; i < completedAchievementIds.Length; i++)
            {
                if (string.Equals(
                        completedAchievementIds[i],
                        achievementId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryCompleteAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) ||
                HasCompletedAchievement(achievementId))
            {
                return false;
            }

            int previousCount = completedAchievementIds.Length;
            Array.Resize(ref completedAchievementIds, previousCount + 1);
            completedAchievementIds[previousCount] = achievementId;
            return true;
        }

        public bool HasClaimedAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                return false;
            }

            EnsureAchievementProgress();
            for (int i = 0; i < claimedAchievementIds.Length; i++)
            {
                if (string.Equals(
                        claimedAchievementIds[i],
                        achievementId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // Marks an already-completed achievement's reward as claimed. Returns true
        // only when it transitions to claimed (completed and not previously claimed).
        public bool TryClaimAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) ||
                !HasCompletedAchievement(achievementId) ||
                HasClaimedAchievement(achievementId))
            {
                return false;
            }

            int previousCount = claimedAchievementIds.Length;
            Array.Resize(ref claimedAchievementIds, previousCount + 1);
            claimedAchievementIds[previousCount] = achievementId;
            return true;
        }

        public bool EnsureMiningDay(string localDate)
        {
            if (string.Equals(miningPlayDate, localDate, StringComparison.Ordinal))
            {
                return false;
            }

            miningPlayDate = localDate;
            miningPlayCount = 0;
            miningAdBonusCount = 0;
            return true;
        }

        public int GetMiningDailyLimit(
            string localDate,
            int baseDailyLimit,
            int maximumAdBonuses)
        {
            bool hasAdBonusLimit = maximumAdBonuses >= 0;
            int grantedBonuses =
                string.Equals(miningPlayDate, localDate, StringComparison.Ordinal)
                    ? Math.Max(0, miningAdBonusCount)
                    : 0;
            if (hasAdBonusLimit)
            {
                grantedBonuses = Math.Min(
                    grantedBonuses,
                    Math.Max(0, maximumAdBonuses));
            }

            return Math.Max(0, baseDailyLimit) + grantedBonuses;
        }

        public int GetRemainingMiningAdBonuses(
            string localDate,
            int maximumAdBonuses)
        {
            if (maximumAdBonuses < 0)
            {
                return int.MaxValue;
            }

            int grantedBonuses =
                string.Equals(miningPlayDate, localDate, StringComparison.Ordinal)
                    ? Math.Max(0, miningAdBonusCount)
                    : 0;
            return Math.Max(0, maximumAdBonuses - grantedBonuses);
        }

        public bool TryGrantMiningAdBonus(
            string localDate,
            int maximumAdBonuses)
        {
            EnsureMiningDay(localDate);
            int bonusLimit = Math.Max(0, maximumAdBonuses);
            if (maximumAdBonuses >= 0 && miningAdBonusCount >= bonusLimit)
            {
                return false;
            }

            miningAdBonusCount++;
            return true;
        }

        public int GetRemainingMiningPlays(string localDate, int dailyLimit)
        {
            int used = string.Equals(miningPlayDate, localDate, StringComparison.Ordinal)
                ? Math.Max(0, miningPlayCount)
                : 0;
            return Math.Max(0, dailyLimit - used);
        }

        public bool TryUseMiningPlay(string localDate, int dailyLimit)
        {
            EnsureMiningDay(localDate);
            if (miningPlayCount >= dailyLimit)
            {
                return false;
            }

            miningPlayCount++;
            return true;
        }

        public void RefundMiningPlay(string localDate)
        {
            if (!string.Equals(
                    miningPlayDate,
                    localDate,
                    StringComparison.Ordinal))
            {
                return;
            }

            miningPlayCount = Math.Max(0, miningPlayCount - 1);
        }

        public void EnsureCurrencies(int firstLaunchMeteorFragments)
        {
            if (currencies == null || currencies.Length < 5)
            {
                CurrencyAmount[] oldCurrencies = currencies;
                currencies = CreateCurrencyArray(firstLaunchMeteorFragments);

                if (oldCurrencies != null)
                {
                    for (int i = 0; i < oldCurrencies.Length; i++)
                    {
                        if (oldCurrencies[i] != null)
                        {
                            SetCurrency(oldCurrencies[i].type, oldCurrencies[i].amount);
                        }
                    }
                }
            }
        }

        public int GetCurrency(StarForgeCurrencyType type)
        {
            CurrencyAmount amount = FindCurrency(type);
            return amount != null ? amount.amount : 0;
        }

        public void SetCurrency(StarForgeCurrencyType type, int value)
        {
            CurrencyAmount amount = FindCurrency(type);
            if (amount == null)
            {
                return;
            }

            amount.amount = Math.Max(0, value);
        }

        public void AddCurrency(StarForgeCurrencyType type, int value)
        {
            if (value <= 0)
            {
                return;
            }

            SetCurrency(type, GetCurrency(type) + value);
        }

        public bool TrySpendCurrency(StarForgeCurrencyType type, int value)
        {
            if (value < 0)
            {
                return false;
            }

            int current = GetCurrency(type);
            if (current < value)
            {
                return false;
            }

            SetCurrency(type, current - value);
            return true;
        }

        private CurrencyAmount FindCurrency(StarForgeCurrencyType type)
        {
            if (currencies == null)
            {
                return null;
            }

            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i] != null && currencies[i].type == type)
                {
                    return currencies[i];
                }
            }

            return null;
        }

        private static CurrencyAmount[] CreateCurrencyArray(int firstLaunchMeteorFragments)
        {
            CurrencyAmount[] values = new CurrencyAmount[5];
            values[0] = new CurrencyAmount(StarForgeCurrencyType.MeteorFragment, firstLaunchMeteorFragments);
            values[1] = new CurrencyAmount(StarForgeCurrencyType.StarShard, 0);
            values[2] = new CurrencyAmount(StarForgeCurrencyType.PureCoreShard, 0);
            values[3] = new CurrencyAmount(StarForgeCurrencyType.SingularityShard, 0);
            values[4] = new CurrencyAmount(StarForgeCurrencyType.PrimordialStar, 0);
            return values;
        }
    }
}
