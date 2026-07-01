using System;
using StarForge.Data;
using StarForge.Save;

namespace StarForge.Core
{
    public sealed class StarForgeEnhancementService
    {
        private const float BaseDestructionChancePercent = 0f;
        private const float DestructionChancePerFracturePercent = 5f;
        private const float MaximumOutcomeChancePercent = 100f;

        public StarForgeEnhancementResult TryEnhance(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType currencyType,
            Func<float> roll01,
            bool? overrideDiscovery = null)
        {
            StarForgeEnhancementResult result = new StarForgeEnhancementResult();
            result.selectedCurrency = currencyType;
            result.previousLevel = saveData.currentLevel;
            result.newLevel = saveData.currentLevel;
            result.previousBlackHoleLevel = saveData.blackHoleLevel;
            result.newBlackHoleLevel = saveData.blackHoleLevel;

            if (saveData.isBlackHole)
            {
                ApplyBlackHoleEnhancement(saveData, balance, roll01, result);
                return result;
            }

            if (saveData.currentLevel >= balance.maxLevel)
            {
                result.kind = StarForgeResultKind.MaxLevel;
                result.message = "이미 최종 단계입니다.";
                return result;
            }

            // Eligible normal attempt: tick the hidden pity counter and decide
            // discovery. The controller may pre-roll the discovery (so it can force
            // the cinematic past the skip toggle); otherwise decide it here.
            if (StarForgeBlackHoleRules.CanDiscoverFromNormalLevel(
                    saveData.currentLevel,
                    balance.maxLevel))
            {
                saveData.blackHoleDiscoveryAttemptCount =
                    Math.Max(0, saveData.blackHoleDiscoveryAttemptCount) + 1;
                // Pity ceiling removed: discovery is purely the flat 1% roll, no
                // guaranteed discovery after N attempts.
                bool discoverBlackHole = overrideDiscovery
                    ?? (RollPercent(roll01) <=
                            StarForgeBlackHoleRules.DiscoveryChancePercent);
                if (discoverBlackHole)
                {
                    ApplyBlackHoleDiscovery(saveData, result);
                    return result;
                }
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

            if (saveData.isBlackHole)
            {
                preview.isBlackHole = true;
                preview.blackHoleLevel = saveData.blackHoleLevel;
                preview.isAvailable = true;
                preview.isMaxLevel =
                    saveData.blackHoleLevel >= StarForgeBlackHoleRules.MaxLevel;
                preview.hasEnoughCurrency = true;
                preview.cost = 0;
                preview.successRatePercent =
                    StarForgeBlackHoleRules.GetSuccessRatePercent(
                        saveData.blackHoleLevel);
                preview.destructionChancePercent =
                    preview.isMaxLevel
                        ? 0f
                        : MaximumOutcomeChancePercent -
                          preview.successRatePercent;
                preview.fractureChancePercent = 0f;
                return preview;
            }

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
            FailureDistribution distribution =
                GetFailureDistribution(saveData, attempt);
            preview.fractureChancePercent =
                distribution.fractureChancePercent;
            preview.destructionChancePercent =
                distribution.destructionChancePercent;
            return preview;
        }

        // Pure predicate (no state mutation): would this normal-level enhancement
        // attempt discover a black hole, given a 0..1 roll value? Lets the controller
        // decide up front so the discovery cinematic can be forced past the skip toggle.
        public bool WouldDiscoverBlackHole(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            float roll01Value)
        {
            if (saveData == null ||
                balance == null ||
                saveData.isBlackHole ||
                saveData.currentLevel >= balance.maxLevel ||
                !StarForgeBlackHoleRules.CanDiscoverFromNormalLevel(
                    saveData.currentLevel,
                    balance.maxLevel))
            {
                return false;
            }

            // Pity ceiling removed: a discovery only happens on the flat 1% roll.
            return Math.Max(0f, Math.Min(1f, roll01Value)) * 100f <=
                   StarForgeBlackHoleRules.DiscoveryChancePercent;
        }

        private static void ApplyBlackHoleDiscovery(
            StarForgeSaveData saveData,
            StarForgeEnhancementResult result)
        {
            // Remember the planet we had so it can be restored when the black hole
            // ends (disassemble or 소멸) instead of being wiped to 0강.
            saveData.blackHolePreviousLevel = saveData.currentLevel;
            saveData.blackHolePreviousShape = saveData.planetShape;

            saveData.isBlackHole = true;
            saveData.blackHoleLevel = StarForgeBlackHoleRules.MinLevel;
            saveData.highestBlackHoleLevel = Math.Max(
                saveData.highestBlackHoleLevel,
                saveData.blackHoleLevel);
            saveData.currentLevel = 0;
            saveData.blackHoleDiscoveryAttemptCount = 0;
            saveData.ResetFractures();
            saveData.RecordSuccessOutcome(false);
            saveData.attemptCount++;

            result.kind = StarForgeResultKind.Success;
            result.discoveredBlackHole = true;
            result.isBlackHole = true;
            result.levelGain = 1;
            result.newLevel = 0;
            result.newBlackHoleLevel = saveData.blackHoleLevel;
            result.successRatePercent = MaximumOutcomeChancePercent;
            result.cost = 0;
            result.message = "블랙홀을 발견했습니다.";
        }

        private static void ApplyBlackHoleEnhancement(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            Func<float> roll01,
            StarForgeEnhancementResult result)
        {
            result.isBlackHole = true;
            result.cost = 0;
            result.successRatePercent =
                StarForgeBlackHoleRules.GetSuccessRatePercent(
                    saveData.blackHoleLevel);

            if (saveData.blackHoleLevel >= StarForgeBlackHoleRules.MaxLevel)
            {
                result.kind = StarForgeResultKind.MaxLevel;
                result.message = "이미 블랙홀 최종 단계입니다.";
                return;
            }

            saveData.attemptCount++;
            if (RollPercent(roll01) <= result.successRatePercent)
            {
                int previousLevel = saveData.blackHoleLevel;
                saveData.blackHoleLevel = Math.Min(
                    StarForgeBlackHoleRules.MaxLevel,
                    saveData.blackHoleLevel + 1);
                saveData.highestBlackHoleLevel = Math.Max(
                    saveData.highestBlackHoleLevel,
                    saveData.blackHoleLevel);
                saveData.RecordSuccessOutcome(false);

                result.kind = StarForgeResultKind.Success;
                result.levelGain = saveData.blackHoleLevel - previousLevel;
                result.newBlackHoleLevel = saveData.blackHoleLevel;
                result.message =
                    "블랙홀 강화 성공! " +
                    saveData.blackHoleLevel +
                    "강에 도달했습니다.";
                return;
            }

            ApplyBlackHoleDestroyed(saveData, balance, roll01, result);
        }

        private static void ApplyBlackHoleDestroyed(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            Func<float> roll01,
            StarForgeEnhancementResult result)
        {
            result.kind = StarForgeResultKind.Destroyed;
            result.isBlackHole = true;
            result.rewards = null;
            result.previousBlackHoleLevel = saveData.blackHoleLevel;
            result.newBlackHoleLevel = 0;
            result.message = "블랙홀이 소멸했습니다. 이전 행성 단계로 복귀합니다.";

            // The black hole vanishes, but the player's original planet is restored
            // rather than wiped — return to the stage/shape held before discovery.
            saveData.isBlackHole = false;
            saveData.blackHoleLevel = 0;
            saveData.currentLevel = saveData.blackHolePreviousLevel;
            saveData.planetShape = saveData.blackHolePreviousShape;
            saveData.blackHolePreviousLevel = 0;
            saveData.blackHolePreviousShape = (int)StarForgePlanetShape.Default;
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                saveData.currentLevel,
                balance.maxLevel);
            saveData.ResetFractures();
            saveData.RecordDestructionOutcome();

            result.newLevel = saveData.currentLevel;
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
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                newLevel,
                balance.maxLevel);
            saveData.ResetFractures();
            saveData.RecordSuccessOutcome(
                result.kind == StarForgeResultKind.GreatSuccess);

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
            FailureDistribution distribution =
                GetFailureDistribution(saveData, attempt);
            result.destructionChancePercent =
                distribution.destructionChancePercent;
            result.fractureChancePercent =
                distribution.fractureChancePercent;

            float destructionChance =
                distribution.destructionChancePercent;
            float fractureChance =
                distribution.fractureChancePercent;
            float failureChance =
                distribution.failureChancePercent;
            float outcomeRoll = RollPercent(roll01);

            if (outcomeRoll < failureChance)
            {
                result.kind = StarForgeResultKind.Failure;
                saveData.RecordFailureOutcome();
                result.message = saveData.isFractured
                    ? "실패. 균열 상태가 유지됩니다."
                    : "일반 실패. 단계는 유지됩니다.";
                return;
            }

            if (outcomeRoll < failureChance + fractureChance)
            {
                saveData.AddFracture();
                saveData.RecordFractureOutcome();
                result.kind = StarForgeResultKind.Fracture;
                result.message =
                    "균열 발생. 누적 " +
                    saveData.fractureCount +
                    "회로 소멸 위험이 증가했습니다.";
                return;
            }

            if (destructionChance > 0f)
            {
                ApplyDestroyed(saveData, balance, roll01, result);
                return;
            }

            result.kind = StarForgeResultKind.Failure;
            saveData.RecordFailureOutcome();
            result.message = "일반 실패. 단계는 유지됩니다.";
        }

        private static void ApplyDestroyed(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            Func<float> roll01,
            StarForgeEnhancementResult result)
        {
            result.kind = StarForgeResultKind.Destroyed;
            result.rewards = BuildDestructionRewards(saveData, balance);

            if (result.rewards != null)
            {
                for (int i = 0; i < result.rewards.Length; i++)
                {
                    CurrencyAmount reward = result.rewards[i];
                    if (reward != null)
                    {
                        saveData.AddCurrency(reward.type, reward.amount);
                    }
                }
            }

            // 새 0강 행성: 이전 모양은 부활용으로 보관하고 모양을 재추첨
            saveData.lastDestroyedShape = saveData.planetShape;
            saveData.planetShape = (int)StarForgePlanetShapes.Roll(balance.shapeChancesPercent, roll01);
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                0,
                balance.maxLevel);

            saveData.currentLevel = 0;
            saveData.ResetFractures();
            saveData.RecordDestructionOutcome();

            result.newLevel = 0;
            result.message = "소멸. 남은 재료를 회수했습니다.";
        }

        /// <summary>행성 분해: 현 단계 가치만큼 재화를 받고 0강 새 행성(모양 재추첨)으로 시작합니다.</summary>
        public StarForgeDisassembleResult TryDisassemble(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            Func<float> roll01)
        {
            StarForgeDisassembleResult result = new StarForgeDisassembleResult();
            result.level = saveData.currentLevel;

            if (saveData.isBlackHole)
            {
                return TryDisassembleBlackHole(
                    saveData,
                    balance,
                    roll01,
                    result);
            }

            if (saveData.currentLevel <= 0)
            {
                result.message = "0강 행성은 분해할 수 없습니다.";
                return result;
            }

            CurrencyAmount[] rewards = GetDisassembleRewards(saveData, balance);
            if (rewards == null || rewards.Length == 0)
            {
                result.message = "이 단계는 분해 보상이 없습니다.";
                return result;
            }

            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i] != null)
                {
                    saveData.AddCurrency(rewards[i].type, rewards[i].amount);
                }
            }

            result.rewards = rewards;
            result.previousShape = (StarForgePlanetShape)saveData.planetShape;

            saveData.planetShape = (int)StarForgePlanetShapes.Roll(balance.shapeChancesPercent, roll01);
            saveData.lastDestroyedShape = saveData.planetShape;
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                0,
                balance.maxLevel);
            saveData.currentLevel = 0;
            saveData.ResetFractures();

            result.newShape = (StarForgePlanetShape)saveData.planetShape;
            result.success = true;
            result.message = "행성을 분해해 재료를 회수했습니다.";
            return result;
        }

        public CurrencyAmount[] GetDisassembleRewards(
            StarForgeSaveData saveData,
            StarForgeBalance balance)
        {
            if (saveData != null && saveData.isBlackHole)
            {
                return StarForgeBlackHoleRules.GetDisassembleRewards(
                    saveData.blackHoleLevel);
            }

            return BuildDisassembleRewards(saveData, balance);
        }

        private static StarForgeDisassembleResult TryDisassembleBlackHole(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            Func<float> roll01,
            StarForgeDisassembleResult result)
        {
            result.isBlackHole = true;
            result.level = saveData.blackHoleLevel;
            result.rewards =
                StarForgeBlackHoleRules.GetDisassembleRewards(
                    saveData.blackHoleLevel);

            for (int i = 0; i < result.rewards.Length; i++)
            {
                CurrencyAmount reward = result.rewards[i];
                if (reward != null)
                {
                    saveData.AddCurrency(reward.type, reward.amount);
                }
            }

            result.previousShape = (StarForgePlanetShape)saveData.planetShape;

            // Disassembling the black hole returns the player's original planet
            // (stage + shape held before discovery) instead of rolling a fresh 0강.
            saveData.isBlackHole = false;
            saveData.blackHoleLevel = 0;
            saveData.currentLevel = saveData.blackHolePreviousLevel;
            saveData.planetShape = saveData.blackHolePreviousShape;
            saveData.blackHolePreviousLevel = 0;
            saveData.blackHolePreviousShape = (int)StarForgePlanetShape.Default;
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                saveData.currentLevel,
                balance.maxLevel);
            saveData.ResetFractures();

            result.newShape = (StarForgePlanetShape)saveData.planetShape;
            result.success = true;
            result.message =
                "블랙홀을 분해해 재료를 회수하고 이전 행성으로 돌아왔습니다.";
            return result;
        }

        private static CurrencyAmount[] BuildDisassembleRewards(
            StarForgeSaveData saveData,
            StarForgeBalance balance)
        {
            if (saveData == null || balance == null)
            {
                return null;
            }

            CurrencyAmount[] rewards =
                balance.GetDisassembleReward(saveData.currentLevel);
            if (rewards == null)
            {
                return null;
            }

            int multiplier = GetDisassembleRewardMultiplier(
                (StarForgePlanetShape)saveData.planetShape);
            CurrencyAmount[] multipliedRewards =
                new CurrencyAmount[rewards.Length];
            for (int i = 0; i < rewards.Length; i++)
            {
                CurrencyAmount reward = rewards[i];
                if (reward == null)
                {
                    continue;
                }

                multipliedRewards[i] =
                    new CurrencyAmount(reward.type, reward.amount * multiplier);
            }

            return multipliedRewards;
        }

        private static CurrencyAmount[] BuildDestructionRewards(
            StarForgeSaveData saveData,
            StarForgeBalance balance)
        {
            CurrencyAmount[] disassembleRewards =
                BuildDisassembleRewards(saveData, balance);
            if (disassembleRewards == null)
            {
                return null;
            }

            CurrencyAmount[] destructionRewards =
                new CurrencyAmount[disassembleRewards.Length];
            for (int i = 0; i < disassembleRewards.Length; i++)
            {
                CurrencyAmount reward = disassembleRewards[i];
                if (reward == null || reward.amount <= 0)
                {
                    continue;
                }

                destructionRewards[i] =
                    new CurrencyAmount(
                        reward.type,
                        Math.Max(1, reward.amount / 2));
            }

            return destructionRewards;
        }

        private static int GetDisassembleRewardMultiplier(
            StarForgePlanetShape shape)
        {
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return 2;
                case StarForgePlanetShape.Cat:
                    return 3;
                default:
                    return 1;
            }
        }

        private static FailureDistribution GetFailureDistribution(
            StarForgeSaveData saveData,
            AttemptBalance attempt)
        {
            FailureDistribution distribution =
                new FailureDistribution();
            float baseDestructionChance = BaseDestructionChancePercent;
            float baseFractureChance = Math.Min(
                MaximumOutcomeChancePercent - baseDestructionChance,
                Math.Max(0f, attempt.fractureChancePercent));
            float baseFailureChance = Math.Max(
                0f,
                MaximumOutcomeChancePercent -
                baseDestructionChance -
                baseFractureChance);

            if (saveData.currentLevel <= 0)
            {
                distribution.failureChancePercent =
                    baseFailureChance + baseDestructionChance;
                distribution.fractureChancePercent =
                    baseFractureChance;
                distribution.destructionChancePercent = 0f;
                return distribution;
            }

            float destructionIncrease = Math.Min(
                MaximumOutcomeChancePercent - baseDestructionChance,
                Math.Max(0, saveData.fractureCount) *
                DestructionChancePerFracturePercent);
            float failureReduction = Math.Min(
                baseFailureChance,
                destructionIncrease);

            distribution.failureChancePercent =
                baseFailureChance - failureReduction;
            distribution.destructionChancePercent =
                baseDestructionChance + destructionIncrease;
            distribution.fractureChancePercent = Math.Max(
                0f,
                MaximumOutcomeChancePercent -
                distribution.failureChancePercent -
                distribution.destructionChancePercent);
            return distribution;
        }

        private static float RollPercent(Func<float> roll01)
        {
            if (roll01 == null)
            {
                return 100f;
            }

            return Math.Max(0f, Math.Min(1f, roll01())) * 100f;
        }

        private struct FailureDistribution
        {
            public float failureChancePercent;
            public float fractureChancePercent;
            public float destructionChancePercent;
        }
    }

    public sealed class StarForgeEnhancementResult
    {
        public StarForgeResultKind kind;
        public StarForgeCurrencyType selectedCurrency;
        public bool isBlackHole;
        public bool discoveredBlackHole;
        public int previousLevel;
        public int newLevel;
        public int previousBlackHoleLevel;
        public int newBlackHoleLevel;
        public int levelGain;
        public int cost;
        public float successRatePercent;
        public float fractureChancePercent;
        public float destructionChancePercent;
        public CurrencyAmount[] rewards;
        public string message;
    }

    public sealed class StarForgeDisassembleResult
    {
        public bool success;
        public bool isBlackHole;
        public int level;
        public CurrencyAmount[] rewards;
        public StarForgePlanetShape previousShape;
        public StarForgePlanetShape newShape;
        public string message;
    }

    public sealed class StarForgeAttemptPreview
    {
        public int level;
        public StarForgeCurrencyType currencyType;
        public bool isBlackHole;
        public int blackHoleLevel;
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
                0),
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
