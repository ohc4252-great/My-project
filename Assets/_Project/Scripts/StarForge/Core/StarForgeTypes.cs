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
