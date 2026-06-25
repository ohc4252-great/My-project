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
}
