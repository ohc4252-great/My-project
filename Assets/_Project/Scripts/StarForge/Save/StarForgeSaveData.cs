using System;
using StarForge.Core;

namespace StarForge.Save
{
    [Serializable]
    public sealed class StarForgeSaveData
    {
        public int currentLevel;
        public int highestLevel;
        public bool isFractured;
        public int destructionCount;
        public int reviveCount;
        public int attemptCount;
        public int successCount;
        public int selectedCurrency;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public string primordialExchangeDate;
        public int primordialExchangeCount;
        public CurrencyAmount[] currencies;

        public static StarForgeSaveData CreateNew(int firstLaunchMeteorFragments)
        {
            StarForgeSaveData data = new StarForgeSaveData();
            data.currentLevel = 0;
            data.highestLevel = 0;
            data.isFractured = false;
            data.destructionCount = 0;
            data.reviveCount = 0;
            data.attemptCount = 0;
            data.successCount = 0;
            data.selectedCurrency = (int)StarForgeCurrencyType.MeteorFragment;
            data.soundEnabled = true;
            data.vibrationEnabled = true;
            data.primordialExchangeDate = string.Empty;
            data.primordialExchangeCount = 0;
            data.currencies = CreateCurrencyArray(firstLaunchMeteorFragments);
            return data;
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
