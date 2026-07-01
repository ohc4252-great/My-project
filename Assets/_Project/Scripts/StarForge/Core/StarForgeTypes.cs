using System;

namespace StarForge.Core
{
    public enum StarForgeCurrencyType
    {
        MeteorFragment = 0,
        StarShard = 1,
        PureCoreShard = 2,
        SingularityShard = 3,
        PrimordialStar = 4
    }

    public enum StarForgePlanetShape
    {
        Default = 0,
        Heart = 1,
        Cat = 2
    }

    public static class StarForgePlanetShapes
    {
        /// <summary>chances는 [기본, 하트, 고양이] 퍼센트. roll01은 0~1 난수.</summary>
        public static StarForgePlanetShape Roll(float[] chances, Func<float> roll01)
        {
            float defaultChance = chances != null && chances.Length > 0 ? Math.Max(0f, chances[0]) : 80f;
            float heartChance = chances != null && chances.Length > 1 ? Math.Max(0f, chances[1]) : 12f;
            float catChance = chances != null && chances.Length > 2 ? Math.Max(0f, chances[2]) : 8f;
            float total = defaultChance + heartChance + catChance;
            if (total <= 0f)
            {
                return StarForgePlanetShape.Default;
            }

            float roll = (roll01 != null ? Math.Max(0f, Math.Min(1f, roll01())) : 0f) * total;
            if (roll < defaultChance)
            {
                return StarForgePlanetShape.Default;
            }

            return roll < defaultChance + heartChance
                ? StarForgePlanetShape.Heart
                : StarForgePlanetShape.Cat;
        }
    }

    public enum StarForgeResultKind
    {
        None = 0,
        MaterialUnavailable = 1,
        NotEnoughCurrency = 2,
        MaxLevel = 3,
        Failure = 4,
        Fracture = 5,
        Destroyed = 6,
        Success = 7,
        GreatSuccess = 8
    }

    [Serializable]
    public sealed class CurrencyAmount
    {
        public StarForgeCurrencyType type;
        public int amount;

        public CurrencyAmount()
        {
        }

        public CurrencyAmount(StarForgeCurrencyType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }

    public static class StarForgeCurrencyNames
    {
        public static string GetDisplayName(StarForgeCurrencyType type)
        {
            switch (type)
            {
                case StarForgeCurrencyType.MeteorFragment:
                    return "운석 파편";
                case StarForgeCurrencyType.StarShard:
                    return "별의 조각";
                case StarForgeCurrencyType.PureCoreShard:
                    return "온전한 별핵 조각";
                case StarForgeCurrencyType.SingularityShard:
                    return "특이성 조각";
                case StarForgeCurrencyType.PrimordialStar:
                    return "원초의 별";
                default:
                    return type.ToString();
            }
        }
    }

    public static class StarForgeBlackHoleRules
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;
        public const int DiscoveryMinNormalLevel = 20;
        public const float DiscoveryChancePercent = 1f;
        // Pity ceiling removed — discovery is now purely the flat 1% roll. This value is
        // retained only as the upper clamp bound for the (now-vestigial) attempt counter
        // so existing saves stay in range.
        public const int DiscoveryAttemptThreshold = 100;

        public static bool CanDiscoverFromNormalLevel(
            int normalLevel,
            int maxNormalLevel)
        {
            return normalLevel >= DiscoveryMinNormalLevel &&
                   normalLevel <= maxNormalLevel;
        }

        // 단계별 블랙홀 강화 성공률(현재 단계 → 다음 단계). 10강은 MAX라 0%.
        public static float GetSuccessRatePercent(int blackHoleLevel)
        {
            switch (Math.Max(MinLevel, Math.Min(MaxLevel, blackHoleLevel)))
            {
                case 1: return 80f;
                case 2: return 70f;
                case 3: return 50f;
                case 4: return 30f;
                case 5: return 20f;
                case 6: return 15f;
                case 7: return 10f;
                case 8: return 7f;
                case 9: return 5f;
                default: return 0f;
            }
        }

        public static CurrencyAmount[] GetDisassembleRewards(int blackHoleLevel)
        {
            int clampedLevel = Math.Max(
                MinLevel,
                Math.Min(MaxLevel, blackHoleLevel));
            // Rewards boosted ~1.6x to speed the 25~29강 endgame supply of 원초의 별
            // (lets players keep using the highest-success material) without touching
            // success rates. Targets a ~30h (≈5일) median climb to 30강.
            int primordialStarAmount;
            int singularityShardAmount;
            switch (clampedLevel)
            {
                case 1:
                    primordialStarAmount = 3;
                    singularityShardAmount = 16;
                    break;
                case 2:
                    primordialStarAmount = 6;
                    singularityShardAmount = 40;
                    break;
                case 3:
                    primordialStarAmount = 13;
                    singularityShardAmount = 80;
                    break;
                case 4:
                    primordialStarAmount = 32;
                    singularityShardAmount = 256;
                    break;
                case 5:
                    primordialStarAmount = 40;
                    singularityShardAmount = 352;
                    break;
                default:
                    primordialStarAmount = 8 * (clampedLevel + 1);
                    singularityShardAmount = 80 * clampedLevel;
                    break;
            }

            return new[]
            {
                new CurrencyAmount(
                    StarForgeCurrencyType.PrimordialStar,
                    primordialStarAmount),
                new CurrencyAmount(
                    StarForgeCurrencyType.SingularityShard,
                    singularityShardAmount)
            };
        }
    }
}
