using System;
using StarForge.Core;

namespace StarForge.Data
{
    [Serializable]
    public sealed class StarForgeBalance
    {
        public int maxLevel = 30;
        public int firstLaunchMeteorFragments = 100;
        public float[] shapeChancesPercent = { 80f, 12f, 8f };
        public float fracturedDestructionMultiplier = 2f;
        public CurrencyConfig[] currencies;
        public AttemptBalance[] attempts;
        public StageVisualConfig[] stages;
        public RevivePointConfig[] revivePoints;

        public RevivePointConfig GetRevivePoint(int level)
        {
            if (revivePoints == null)
            {
                return null;
            }

            for (int i = 0; i < revivePoints.Length; i++)
            {
                if (revivePoints[i] != null && revivePoints[i].level == level)
                {
                    return revivePoints[i];
                }
            }

            return null;
        }

        public AttemptBalance GetAttempt(int level)
        {
            if (attempts == null)
            {
                return null;
            }

            for (int i = 0; i < attempts.Length; i++)
            {
                if (attempts[i] != null && attempts[i].level == level)
                {
                    return attempts[i];
                }
            }

            return null;
        }

        public StageVisualConfig GetStage(int level)
        {
            if (stages == null || stages.Length == 0)
            {
                return StageVisualConfig.CreateFallback(level);
            }

            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null && stages[i].level == level)
                {
                    return stages[i];
                }
            }

            return StageVisualConfig.CreateFallback(level);
        }

        /// <summary>모양(기본/하트/고양이)에 맞는 단계 이름을 돌려줍니다.</summary>
        public string GetStageName(int level, StarForgePlanetShape shape)
        {
            StageVisualConfig stage = GetStage(level);
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return string.IsNullOrEmpty(stage.heartName) ? stage.displayName : stage.heartName;
                case StarForgePlanetShape.Cat:
                    return string.IsNullOrEmpty(stage.catName) ? stage.displayName : stage.catName;
                default:
                    return stage.displayName;
            }
        }

        public CurrencyAmount[] GetDisassembleReward(int level)
        {
            StageVisualConfig stage = GetStage(level);
            return stage != null ? stage.disassembleReward : null;
        }

        public CurrencyConfig GetCurrency(StarForgeCurrencyType type)
        {
            if (currencies == null)
            {
                return CurrencyConfig.CreateFallback(type);
            }

            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i] != null && currencies[i].type == type)
                {
                    return currencies[i];
                }
            }

            return CurrencyConfig.CreateFallback(type);
        }

        public bool TryGetSuccessRate(int level, StarForgeCurrencyType currencyType, out float successRatePercent)
        {
            AttemptBalance attempt = GetAttempt(level);
            successRatePercent = -1f;

            if (attempt == null || attempt.successRatePercent == null)
            {
                return false;
            }

            int index = (int)currencyType;
            if (index < 0 || index >= attempt.successRatePercent.Length)
            {
                return false;
            }

            successRatePercent = attempt.successRatePercent[index];
            return successRatePercent >= 0f;
        }

        public bool TryGetCost(int level, StarForgeCurrencyType currencyType, out int cost)
        {
            AttemptBalance attempt = GetAttempt(level);
            cost = -1;

            if (attempt == null || attempt.costs == null)
            {
                return false;
            }

            int index = (int)currencyType;
            if (index < 0 || index >= attempt.costs.Length)
            {
                return false;
            }

            cost = attempt.costs[index];
            return cost >= 0;
        }
    }

    [Serializable]
    public sealed class CurrencyConfig
    {
        public StarForgeCurrencyType type;
        public string displayName;
        public string shortName;

        public static CurrencyConfig CreateFallback(StarForgeCurrencyType type)
        {
            CurrencyConfig config = new CurrencyConfig();
            config.type = type;
            config.displayName = StarForgeCurrencyNames.GetDisplayName(type);
            config.shortName = config.displayName;
            return config;
        }
    }

    [Serializable]
    public sealed class AttemptBalance
    {
        public int level;
        public float[] successRatePercent;
        public int[] costs;
        public float fractureChancePercent;
        public float destructionChancePercent;
        public CurrencyAmount[] destructionReward;
    }

    [Serializable]
    public sealed class RevivePointConfig
    {
        public int level;
        public CurrencyAmount[] cost;
    }

    [Serializable]
    public sealed class StageVisualConfig
    {
        public int level;
        public string displayName;
        public string heartName;
        public string catName;
        public string color;
        public float scale = 1f;
        public float emission = 0.4f;
        public float rotationSpeed = 18f;
        public CurrencyAmount[] disassembleReward;

        public static StageVisualConfig CreateFallback(int level)
        {
            StageVisualConfig config = new StageVisualConfig();
            config.level = level;
            config.displayName = level + "강 천체";
            config.color = "#8FA8C8";
            config.scale = 1f + level * 0.04f;
            config.emission = 0.4f + level * 0.02f;
            config.rotationSpeed = 18f + level * 0.4f;
            return config;
        }
    }
}
