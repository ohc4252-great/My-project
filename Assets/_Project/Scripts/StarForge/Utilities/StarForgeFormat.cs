using System.Globalization;
using System.Text;
using StarForge.Core;

namespace StarForge.Utilities
{
    public static class StarForgeFormat
    {
        private static readonly CultureInfo KoreanCulture = new CultureInfo("ko-KR");

        public static string Number(int value)
        {
            return value.ToString("N0", KoreanCulture);
        }

        public static string Number(long value)
        {
            return value.ToString("N0", KoreanCulture);
        }

        public static string Percent(float value)
        {
            return value.ToString("0.##", KoreanCulture) + "%";
        }

        public static string CurrencyList(CurrencyAmount[] amounts)
        {
            if (amounts == null || amounts.Length == 0)
            {
                return "없음";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < amounts.Length; i++)
            {
                CurrencyAmount amount = amounts[i];
                if (amount == null || amount.amount <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(StarForgeCurrencyNames.GetDisplayName(amount.type));
                builder.Append(" ");
                builder.Append(Number(amount.amount));
            }

            return builder.Length > 0 ? builder.ToString() : "없음";
        }
    }
}
