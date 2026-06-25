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
            saveData.ResetFractures();
            saveData.reviveCount++;
            // 파괴된 그 행성을 되살리므로 이전 모양을 복원
            saveData.planetShape = saveData.lastDestroyedShape;
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                config.level,
                balance.maxLevel);

            result.success = true;
            result.message = config.level + "강에서 부활했습니다.";
            return result;
        }

        public StarForgeReviveResult TryKeepDestroyedLevel(
            StarForgeSaveData saveData,
            int destroyedLevel,
            CurrencyAmount[] destructionRewards)
        {
            StarForgeReviveResult result = new StarForgeReviveResult();
            result.level = destroyedLevel;

            if (saveData == null ||
                destroyedLevel <= 0 ||
                saveData.currentLevel != 0)
            {
                result.message = "유지할 수 있는 파괴 단계가 없습니다.";
                return result;
            }

            if (destructionRewards != null)
            {
                int[] rollbackAmounts = new int[5];
                for (int i = 0; i < destructionRewards.Length; i++)
                {
                    CurrencyAmount reward = destructionRewards[i];
                    if (reward == null || reward.amount <= 0)
                    {
                        continue;
                    }

                    int currencyIndex = (int)reward.type;
                    if (currencyIndex < 0 ||
                        currencyIndex >= rollbackAmounts.Length)
                    {
                        result.message = "파괴 회수 보상 정보가 올바르지 않습니다.";
                        return result;
                    }

                    rollbackAmounts[currencyIndex] += reward.amount;
                }

                for (int i = 0; i < rollbackAmounts.Length; i++)
                {
                    if (saveData.GetCurrency((StarForgeCurrencyType)i) <
                        rollbackAmounts[i])
                    {
                        result.message = "파괴 회수 보상을 되돌릴 수 없습니다.";
                        return result;
                    }
                }

                for (int i = 0; i < rollbackAmounts.Length; i++)
                {
                    if (rollbackAmounts[i] > 0)
                    {
                        saveData.TrySpendCurrency(
                            (StarForgeCurrencyType)i,
                            rollbackAmounts[i]);
                    }
                }
            }

            saveData.currentLevel = destroyedLevel;
            saveData.highestLevel = Math.Max(
                saveData.highestLevel,
                destroyedLevel);
            saveData.planetShape = saveData.lastDestroyedShape;
            saveData.RecordPlanetProgress(
                (StarForgePlanetShape)saveData.planetShape,
                destroyedLevel,
                Math.Max(saveData.highestLevel, destroyedLevel));
            saveData.ResetFractures();
            saveData.reviveCount++;

            result.success = true;
            result.message = destroyedLevel + "강 단계를 유지했습니다.";
            return result;
        }
    }
}
