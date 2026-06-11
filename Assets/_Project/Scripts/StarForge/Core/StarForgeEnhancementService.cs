using System;
using StarForge.Data;
using StarForge.Save;

namespace StarForge.Core
{
    public sealed class StarForgeEnhancementService
    {
        public StarForgeEnhancementResult TryEnhance(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType currencyType,
            Func<float> roll01)
        {
            StarForgeEnhancementResult result = new StarForgeEnhancementResult();
            result.selectedCurrency = currencyType;
            result.previousLevel = saveData.currentLevel;
            result.newLevel = saveData.currentLevel;

            if (saveData.currentLevel >= balance.maxLevel)
            {
                result.kind = StarForgeResultKind.MaxLevel;
                result.message = "이미 최종 단계입니다.";
                return result;
            }

            AttemptBalance attempt = balance.GetAttempt(saveData.currentLevel);
            if (attempt == null ||
                !balance.TryGetSuccessRate(saveData.currentLevel, currencyType, out result.successRatePercent) ||
                !balance.TryGetCost(saveData.currentLevel, currencyType, out result.cost))
            {
                result.kind = StarForgeResultKind.MaterialUnavailable;
                result.message = "현재 단계에서 사용할 수 없는 재료입니다.";
                return result;
            }

            if (saveData.GetCurrency(currencyType) < result.cost)
            {
                result.kind = StarForgeResultKind.NotEnoughCurrency;
                result.message = StarForgeCurrencyNames.GetDisplayName(currencyType) + "이 부족합니다.";
                return result;
            }

            saveData.TrySpendCurrency(currencyType, result.cost);
            saveData.attemptCount++;

            if (RollPercent(roll01) <= result.successRatePercent)
            {
                ApplySuccess(saveData, balance, currencyType, roll01, result);
                return result;
            }

            ApplyFailure(saveData, balance, attempt, roll01, result);
            return result;
        }

        public StarForgeAttemptPreview GetPreview(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType currencyType)
        {
            StarForgeAttemptPreview preview = new StarForgeAttemptPreview();
            preview.level = saveData.currentLevel;
            preview.currencyType = currencyType;
            preview.isMaxLevel = saveData.currentLevel >= balance.maxLevel;

            AttemptBalance attempt = balance.GetAttempt(saveData.currentLevel);
            if (attempt == null || preview.isMaxLevel)
            {
                preview.isAvailable = false;
                return preview;
            }

            preview.isAvailable =
                balance.TryGetSuccessRate(saveData.currentLevel, currencyType, out preview.successRatePercent) &&
                balance.TryGetCost(saveData.currentLevel, currencyType, out preview.cost);

            preview.hasEnoughCurrency = saveData.GetCurrency(currencyType) >= preview.cost;
            preview.fractureChancePercent = attempt.fractureChancePercent;
            preview.destructionChancePercent = GetEffectiveDestructionChance(saveData, balance, attempt);
            return preview;
        }

        private static void ApplySuccess(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType currencyType,
            Func<float> roll01,
            StarForgeEnhancementResult result)
        {
            int rawLevelGain = GetSuccessLevelGain(saveData.currentLevel, currencyType, roll01);
            bool isGreatSuccess = currencyType != StarForgeCurrencyType.PureCoreShard && rawLevelGain > 1;
            int cap = currencyType == StarForgeCurrencyType.PureCoreShard && rawLevelGain > 1 ? 25 : balance.maxLevel;
            if (saveData.currentLevel < 25 && rawLevelGain > 1)
            {
                cap = Math.Min(cap, 25);
            }

            int newLevel = Math.Min(balance.maxLevel, Math.Min(cap, saveData.currentLevel + rawLevelGain));
            result.levelGain = Math.Max(1, newLevel - saveData.currentLevel);
            result.newLevel = newLevel;
            result.kind = isGreatSuccess ? StarForgeResultKind.GreatSuccess : StarForgeResultKind.Success;

            saveData.currentLevel = newLevel;
            saveData.highestLevel = Math.Max(saveData.highestLevel, newLevel);
            saveData.isFractured = false;
            saveData.successCount++;

            result.message = result.kind == StarForgeResultKind.GreatSuccess
                ? "융합 대성공! +" + result.levelGain + "강 상승"
                : "융합 성공! +" + result.levelGain + "강 상승";
        }

        private static int GetSuccessLevelGain(int currentLevel, StarForgeCurrencyType currencyType, Func<float> roll01)
        {
            if (currentLevel >= 25)
            {
                return 1;
            }

            float roll = RollPercent(roll01);

            if (currencyType == StarForgeCurrencyType.PureCoreShard)
            {
                return roll <= 20f ? 2 : 1;
            }

            if (roll <= 1f)
            {
                return 5;
            }

            if (roll <= 6f)
            {
                return 4;
            }

            if (roll <= 16f)
            {
                return 3;
            }

            return 1;
        }

        private static void ApplyFailure(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            AttemptBalance attempt,
            Func<float> roll01,
            StarForgeEnhancementResult result)
        {
            result.destructionChancePercent = GetEffectiveDestructionChance(saveData, balance, attempt);
            result.fractureChancePercent = attempt.fractureChancePercent;

            float destructionChance = saveData.currentLevel > 0 ? result.destructionChancePercent : 0f;
            float fractureChance = Math.Max(0f, result.fractureChancePercent);
            float failureChance = Math.Max(0f, 100f - fractureChance - destructionChance);
            float outcomeRoll = RollPercent(roll01);

            if (outcomeRoll < failureChance)
            {
                result.kind = StarForgeResultKind.Failure;
                result.message = saveData.isFractured
                    ? "실패. 균열 상태가 유지됩니다."
                    : "일반 실패. 단계는 유지됩니다.";
                return;
            }

            if (outcomeRoll < failureChance + fractureChance)
            {
                saveData.isFractured = true;
                result.kind = StarForgeResultKind.Fracture;
                result.message = "균열 발생. 다음 도전이 더 위험해집니다.";
                return;
            }

            if (destructionChance > 0f)
            {
                ApplyDestroyed(saveData, attempt, result);
                return;
            }

            result.kind = StarForgeResultKind.Failure;
            result.message = "일반 실패. 단계는 유지됩니다.";
        }

        private static void ApplyDestroyed(
            StarForgeSaveData saveData,
            AttemptBalance attempt,
            StarForgeEnhancementResult result)
        {
            result.kind = StarForgeResultKind.Destroyed;
            result.rewards = attempt.destructionReward;

            if (attempt.destructionReward != null)
            {
                for (int i = 0; i < attempt.destructionReward.Length; i++)
                {
                    CurrencyAmount reward = attempt.destructionReward[i];
                    if (reward != null)
                    {
                        saveData.AddCurrency(reward.type, reward.amount);
                    }
                }
            }

            saveData.currentLevel = 0;
            saveData.isFractured = false;
            saveData.destructionCount++;

            result.newLevel = 0;
            result.message = "소멸. 남은 재료를 회수했습니다.";
        }

        private static float GetEffectiveDestructionChance(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            AttemptBalance attempt)
        {
            if (saveData.currentLevel <= 0)
            {
                return 0f;
            }

            float chance = Math.Max(0f, attempt.destructionChancePercent);
            return Math.Min(100f, chance);
        }

        private static float RollPercent(Func<float> roll01)
        {
            if (roll01 == null)
            {
                return 100f;
            }

            return Math.Max(0f, Math.Min(1f, roll01())) * 100f;
        }
    }

    public sealed class StarForgeEnhancementResult
    {
        public StarForgeResultKind kind;
        public StarForgeCurrencyType selectedCurrency;
        public int previousLevel;
        public int newLevel;
        public int levelGain;
        public int cost;
        public float successRatePercent;
        public float fractureChancePercent;
        public float destructionChancePercent;
        public CurrencyAmount[] rewards;
        public string message;
    }

    public sealed class StarForgeAttemptPreview
    {
        public int level;
        public StarForgeCurrencyType currencyType;
        public bool isAvailable;
        public bool isMaxLevel;
        public bool hasEnoughCurrency;
        public int cost;
        public float successRatePercent;
        public float fractureChancePercent;
        public float destructionChancePercent;
    }

    public sealed class StarForgeMaterialExchangeRoute
    {
        public StarForgeCurrencyType sourceType;
        public int sourceAmount;
        public StarForgeCurrencyType targetType;
        public int targetAmount;
        public int requiredHighestLevel;
        public int dailyLimit;

        public StarForgeMaterialExchangeRoute(
            StarForgeCurrencyType sourceType,
            int sourceAmount,
            StarForgeCurrencyType targetType,
            int targetAmount,
            int requiredHighestLevel,
            int dailyLimit = 0)
        {
            this.sourceType = sourceType;
            this.sourceAmount = sourceAmount;
            this.targetType = targetType;
            this.targetAmount = targetAmount;
            this.requiredHighestLevel = requiredHighestLevel;
            this.dailyLimit = dailyLimit;
        }
    }

    public sealed class StarForgeMaterialExchangeResult
    {
        public bool success;
        public int routeIndex;
        public int exchangeCount;
        public string message;
    }

    public sealed class StarForgeMaterialExchangeService
    {
        private const string ExchangeDateFormat = "yyyyMMdd";

        private static readonly StarForgeMaterialExchangeRoute[] Routes =
        {
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.MeteorFragment,
                30,
                StarForgeCurrencyType.StarShard,
                1,
                0),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.StarShard,
                1,
                StarForgeCurrencyType.MeteorFragment,
                15,
                0),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.StarShard,
                15,
                StarForgeCurrencyType.PureCoreShard,
                1,
                10),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.PureCoreShard,
                1,
                StarForgeCurrencyType.StarShard,
                7,
                10),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.PureCoreShard,
                8,
                StarForgeCurrencyType.SingularityShard,
                1,
                15),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.SingularityShard,
                1,
                StarForgeCurrencyType.PureCoreShard,
                4,
                15),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.SingularityShard,
                20,
                StarForgeCurrencyType.PrimordialStar,
                1,
                25,
                3),
            new StarForgeMaterialExchangeRoute(
                StarForgeCurrencyType.PrimordialStar,
                1,
                StarForgeCurrencyType.SingularityShard,
                8,
                28)
        };

        public int RouteCount
        {
            get { return Routes.Length; }
        }

        public StarForgeMaterialExchangeRoute GetRoute(int routeIndex)
        {
            return routeIndex >= 0 && routeIndex < Routes.Length
                ? Routes[routeIndex]
                : null;
        }

        public bool IsUnlocked(StarForgeSaveData saveData, int routeIndex)
        {
            StarForgeMaterialExchangeRoute route = GetRoute(routeIndex);
            return saveData != null &&
                   route != null &&
                   saveData.highestLevel >= route.requiredHighestLevel;
        }

        public int GetRemainingDailyExchanges(
            StarForgeSaveData saveData,
            int routeIndex,
            DateTime localNow)
        {
            StarForgeMaterialExchangeRoute route = GetRoute(routeIndex);
            if (saveData == null || route == null || route.dailyLimit <= 0)
            {
                return int.MaxValue;
            }

            string today = localNow.ToString(ExchangeDateFormat);
            int used = saveData.primordialExchangeDate == today
                ? Math.Max(0, saveData.primordialExchangeCount)
                : 0;
            return Math.Max(0, route.dailyLimit - used);
        }

        public bool CanExchange(
            StarForgeSaveData saveData,
            int routeIndex,
            DateTime localNow)
        {
            return CanExchange(saveData, routeIndex, 1, localNow);
        }

        public bool CanExchange(
            StarForgeSaveData saveData,
            int routeIndex,
            int exchangeCount,
            DateTime localNow)
        {
            StarForgeMaterialExchangeRoute route = GetRoute(routeIndex);
            if (saveData == null ||
                route == null ||
                exchangeCount <= 0 ||
                !IsUnlocked(saveData, routeIndex))
            {
                return false;
            }

            long sourceTotal = (long)route.sourceAmount * exchangeCount;
            long targetTotal = (long)route.targetAmount * exchangeCount;
            return sourceTotal <= int.MaxValue &&
                   saveData.GetCurrency(route.sourceType) >= sourceTotal &&
                   GetRemainingDailyExchanges(saveData, routeIndex, localNow) >= exchangeCount &&
                   saveData.GetCurrency(route.targetType) + targetTotal <= int.MaxValue;
        }

        public StarForgeMaterialExchangeResult TryExchange(
            StarForgeSaveData saveData,
            int routeIndex,
            DateTime localNow)
        {
            return TryExchange(saveData, routeIndex, 1, localNow);
        }

        public StarForgeMaterialExchangeResult TryExchange(
            StarForgeSaveData saveData,
            int routeIndex,
            int exchangeCount,
            DateTime localNow)
        {
            StarForgeMaterialExchangeResult result = new StarForgeMaterialExchangeResult();
            result.routeIndex = routeIndex;
            result.exchangeCount = exchangeCount;

            StarForgeMaterialExchangeRoute route = GetRoute(routeIndex);
            if (saveData == null || route == null)
            {
                result.message = "유효하지 않은 교환입니다.";
                return result;
            }

            if (!IsUnlocked(saveData, routeIndex))
            {
                result.message = route.requiredHighestLevel + "강 최초 도달 후 해금됩니다.";
                return result;
            }

            if (exchangeCount <= 0)
            {
                result.message = "교환 수량은 1 이상이어야 합니다.";
                return result;
            }

            int remainingDailyExchanges =
                GetRemainingDailyExchanges(saveData, routeIndex, localNow);
            if (remainingDailyExchanges < exchangeCount)
            {
                result.message = remainingDailyExchanges <= 0
                    ? "오늘의 원초의 별 교환 횟수를 모두 사용했습니다."
                    : "오늘은 최대 " + remainingDailyExchanges + "회 더 교환할 수 있습니다.";
                return result;
            }

            long sourceTotal = (long)route.sourceAmount * exchangeCount;
            long targetTotal = (long)route.targetAmount * exchangeCount;
            if (sourceTotal > int.MaxValue || targetTotal > int.MaxValue)
            {
                result.message = "한 번에 교환할 수 있는 수량을 초과했습니다.";
                return result;
            }

            if (saveData.GetCurrency(route.targetType) + targetTotal > int.MaxValue)
            {
                result.message = StarForgeCurrencyNames.GetDisplayName(route.targetType) + " 보유 한도를 초과합니다.";
                return result;
            }

            if (!saveData.TrySpendCurrency(route.sourceType, (int)sourceTotal))
            {
                result.message = StarForgeCurrencyNames.GetDisplayName(route.sourceType) + "이 부족합니다.";
                return result;
            }

            saveData.AddCurrency(route.targetType, (int)targetTotal);
            if (route.dailyLimit > 0)
            {
                string today = localNow.ToString(ExchangeDateFormat);
                if (saveData.primordialExchangeDate != today)
                {
                    saveData.primordialExchangeDate = today;
                    saveData.primordialExchangeCount = 0;
                }

                saveData.primordialExchangeCount += exchangeCount;
            }

            result.success = true;
            result.message =
                StarForgeCurrencyNames.GetDisplayName(route.targetType) +
                " " +
                targetTotal +
                "개를 획득했습니다.";
            return result;
        }
    }
}
