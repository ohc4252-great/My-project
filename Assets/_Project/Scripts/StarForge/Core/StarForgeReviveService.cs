using System;
using StarForge.Data;
using StarForge.Save;

namespace StarForge.Core
{
    public sealed class StarForgeReviveResult
    {
        public bool success;
        public int level;
        public string message;
    }

    public sealed class StarForgeReviveService
    {
        public bool HasOptions(StarForgeBalance balance, int destroyedLevel)
        {
            RevivePointConfig[] points = balance != null ? balance.revivePoints : null;
            if (points == null)
            {
                return false;
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null && destroyedLevel >= points[i].level)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsUnlocked(RevivePointConfig config, int destroyedLevel)
        {
            return config != null && destroyedLevel >= config.level;
        }

        public bool IsAffordable(StarForgeSaveData saveData, RevivePointConfig config)
        {
            if (saveData == null || config == null || config.cost == null)
            {
                return false;
            }

            for (int i = 0; i < config.cost.Length; i++)
            {
                CurrencyAmount cost = config.cost[i];
                if (cost != null && saveData.GetCurrency(cost.type) < cost.amount)
                {
                    return false;
                }
            }

            return true;
        }

        public StarForgeReviveResult TryRevive(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            int destroyedLevel,
            int targetLevel)
        {
            StarForgeReviveResult result = new StarForgeReviveResult();
            result.level = targetLevel;

            RevivePointConfig config = balance != null ? balance.GetRevivePoint(targetLevel) : null;
            if (saveData == null || config == null)
            {
                result.message = "유효하지 않은 부활 지점입니다.";
                return result;
            }

            if (!IsUnlocked(config, destroyedLevel))
            {
                result.message = config.level + "강 이상에서 파괴된 경우에만 선택할 수 있습니다.";
                return result;
            }

            if (saveData.currentLevel >= config.level)
            {
                result.message = "이미 " + config.level + "강 이상입니다.";
                return result;
            }

            if (!IsAffordable(saveData, config))
            {
                result.message = "부활에 필요한 재화가 부족합니다.";
                return result;
            }

            if (config.cost != null)
            {
                for (int i = 0; i < config.cost.Length; i++)
                {
                    CurrencyAmount cost = config.cost[i];
                    if (cost != null)
                    {
                        saveData.TrySpendCurrency(cost.type, cost.amount);
                    }
                }
            }

            saveData.currentLevel = config.level;
            saveData.highestLevel = Math.Max(saveData.highestLevel, config.level);
            saveData.isFractured = false;
            saveData.reviveCount++;

            result.success = true;
            result.message = config.level + "강에서 부활했습니다.";
            return result;
        }
    }
}
