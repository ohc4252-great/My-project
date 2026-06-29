#if UNITY_EDITOR
using System;
using StarForge.Core;
using StarForge.Data;
using StarForge.Save;
using UnityEditor;
using UnityEngine;

namespace StarForge.Editor
{
    public static class StarForgeValidation
    {
        [MenuItem("Star Forge/Run Core Validation")]
        public static void RunCoreValidation()
        {
            TextAsset balanceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Resources/StarForgeBalance.json");
            StarForgeBalance balance = StarForgeBalanceLoader.Load(balanceAsset);
            StarForgeEnhancementService service = new StarForgeEnhancementService();
            StarForgeAchievementService achievementService =
                new StarForgeAchievementService();
            StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();

            ValidateMeteorUnavailableAfter20(service, balance);
            ValidateBlackHoleDiscoveryChance(service, balance);
            ValidateBlackHoleEnhancementAndDisassemble(service, balance);
            ValidateGreatSuccessCapAt25(service, balance);
            ValidatePureCoreCapAt25(service, balance);
            ValidateNoGreatSuccessAfter25(service, balance);
            ValidateMaxLevel(service, balance);
            ValidateNotEnoughCurrencyDoesNotSpend(service, balance);
            ValidateFailureOutcomeSplit(service, balance);
            ValidateDestroyedRewardOnce(service, balance);
            ValidateMaterialExchangeRoutes(exchangeService, balance);
            ValidateMaterialExchangeUnlocks(exchangeService, balance);
            ValidateMaterialExchangeQuantity(exchangeService, balance);
            ValidatePrimordialDailyLimit(exchangeService, balance);
            ValidateCollectionStages(balance);
            ValidateAchievements(achievementService, balance);

            Debug.Log("StarForge core validation passed.");
        }

        private static void ValidateMeteorUnavailableAfter20(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 21;

            StarForgeEnhancementResult result = service.TryEnhance(
                save,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(0.002f).Next,
                false);
            Require(result.kind == StarForgeResultKind.MaterialUnavailable, "20강 이후 운석 파편 사용 차단 실패");
            Require(save.currentLevel == 21, "사용 불가 재료가 단계를 변경했습니다.");
        }

        private static void ValidateBlackHoleDiscoveryChance(
            StarForgeEnhancementService service,
            StarForgeBalance balance)
        {
            // Pure predicate: 1% 이내 굴림은 발견, 초과는 미발견, 20강 미만은 항상 미발견.
            StarForgeSaveData probe = CreateRichSave(balance);
            probe.currentLevel = 20;
            Require(
                service.WouldDiscoverBlackHole(probe, balance, 0.005f),
                "20강·1% 이내 굴림에서 블랙홀이 발견되어야 합니다.");
            Require(
                !service.WouldDiscoverBlackHole(probe, balance, 0.5f),
                "20강·1% 초과 굴림에서 블랙홀이 발견되면 안 됩니다.");
            probe.currentLevel = 19;
            Require(
                !service.WouldDiscoverBlackHole(probe, balance, 0.005f),
                "19강에서는 블랙홀이 발견되면 안 됩니다.");

            // 1% 이내 굴림이면 강화 대신 블랙홀 발견.
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 20;
            StarForgeEnhancementResult result = service.TryEnhance(
                save,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                AlwaysZero);

            Require(result.discoveredBlackHole, "1% 블랙홀 발견 성공 판정 실패");
            Require(save.isBlackHole, "블랙홀 발견 후 저장 상태가 블랙홀이 아닙니다.");
            Require(save.blackHoleLevel == 1, "블랙홀 발견 후 1강으로 시작해야 합니다.");

            // 1% 초과 굴림(0.5 → 50%)이면 발견되지 않아야 한다.
            save = CreateRichSave(balance);
            save.currentLevel = 20;
            result = service.TryEnhance(
                save,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(0.5f).Next);

            Require(!result.discoveredBlackHole, "1% 초과 롤에서 블랙홀이 발견되었습니다.");
            Require(!save.isBlackHole, "1% 초과 롤에서 저장 상태가 블랙홀로 변경되었습니다.");

            // 천장(pity): 임계치 도달 시 굴림과 무관하게 반드시 발견.
            StarForgeSaveData pity = CreateRichSave(balance);
            pity.currentLevel = 20;
            pity.blackHoleDiscoveryAttemptCount =
                StarForgeBlackHoleRules.DiscoveryAttemptThreshold - 1;
            StarForgeEnhancementResult pityResult = service.TryEnhance(
                pity,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(0.99f).Next);
            Require(pityResult.discoveredBlackHole, "천장 도달 시 블랙홀이 발견되어야 합니다.");
            Require(
                pity.blackHoleDiscoveryAttemptCount == 0,
                "블랙홀 발견 후 천장 카운터가 초기화되어야 합니다.");
        }

        private static void ValidateBlackHoleEnhancementAndDisassemble(
            StarForgeEnhancementService service,
            StarForgeBalance balance)
        {
            float[] expectedRates =
            {
                80f, 70f, 50f, 30f, 20f, 15f, 10f, 7f, 5f
            };
            for (int level = StarForgeBlackHoleRules.MinLevel;
                 level < StarForgeBlackHoleRules.MaxLevel;
                 level++)
            {
                Require(
                    Mathf.Approximately(
                        StarForgeBlackHoleRules.GetSuccessRatePercent(level),
                        expectedRates[level - StarForgeBlackHoleRules.MinLevel]),
                    "블랙홀 " + level + "강 강화 성공률이 기대값과 다릅니다.");
            }

            Require(
                Mathf.Approximately(
                    StarForgeBlackHoleRules.GetSuccessRatePercent(
                        StarForgeBlackHoleRules.MaxLevel),
                    0f),
                "블랙홀 최대 단계 성공률은 0%여야 합니다.");

            StarForgeSaveData save = CreateRichSave(balance);
            save.isBlackHole = true;
            save.blackHoleLevel = 1;

            StarForgeEnhancementResult success = service.TryEnhance(
                save,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                AlwaysZero);
            Require(success.isBlackHole, "블랙홀 강화 결과 플래그 누락");
            Require(
                Mathf.Approximately(success.successRatePercent, 80f),
                "블랙홀 1강 강화 성공률이 80%가 아닙니다.");
            Require(success.kind == StarForgeResultKind.Success, "블랙홀 1강 강화 성공 판정 실패");
            Require(save.blackHoleLevel == 2, "블랙홀 강화 성공 후 2강이어야 합니다.");

            save.blackHoleLevel = 1;
            StarForgeDisassembleResult disassemble = service.TryDisassemble(
                save,
                balance,
                AlwaysZero);
            Require(disassemble.success, "블랙홀 분해 실패");
            Require(disassemble.isBlackHole, "블랙홀 분해 결과 플래그 누락");
            Require(
                save.GetCurrency(StarForgeCurrencyType.PrimordialStar) ==
                100000 + 10,
                "블랙홀 1강 원초의 별 분해 보상 실패");
            Require(
                save.GetCurrency(StarForgeCurrencyType.SingularityShard) ==
                100000 + 50,
                "블랙홀 1강 특이성 조각 분해 보상 실패");
        }

        private static void ValidateGreatSuccessCapAt25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 24;

            SequenceRoller roller = new SequenceRoller(0.002f, 0f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.StarShard, roller.Next, false);
            Require(result.kind == StarForgeResultKind.GreatSuccess, "24강 대성공 판정 실패");
            Require(save.currentLevel == 25, "24강 대성공은 25강을 초과하면 안 됩니다.");
        }

        private static void ValidatePureCoreCapAt25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 24;

            SequenceRoller roller = new SequenceRoller(0.002f, 0f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.PureCoreShard, roller.Next, false);
            Require(result.kind == StarForgeResultKind.Success, "온전한 별핵 조각 +2 상한 처리 결과 타입이 예상과 다릅니다.");
            Require(save.currentLevel == 25, "온전한 별핵 조각 +2는 25강을 초과하면 안 됩니다.");
        }

        private static void ValidateNoGreatSuccessAfter25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 25;

            SequenceRoller roller = new SequenceRoller(0.002f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.PrimordialStar, roller.Next, false);
            Require(result.kind == StarForgeResultKind.Success, "25강 이후 대성공이 발생하면 안 됩니다.");
            Require(save.currentLevel == 26, "25강 이후 성공은 +1강이어야 합니다.");
        }

        private static void ValidateMaxLevel(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = balance.maxLevel;
            save.highestLevel = balance.maxLevel;

            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.PrimordialStar, AlwaysZero);
            Require(result.kind == StarForgeResultKind.MaxLevel, "30강에서 강화 차단 실패");
        }

        private static void ValidateNotEnoughCurrencyDoesNotSpend(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
            save.currentLevel = 9;
            save.SetCurrency(StarForgeCurrencyType.MeteorFragment, 0);

            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.MeteorFragment, AlwaysZero);
            Require(result.kind == StarForgeResultKind.NotEnoughCurrency, "재화 부족 판정 실패");
            Require(save.GetCurrency(StarForgeCurrencyType.MeteorFragment) == 0, "재화 부족 상태에서 재화가 변경됐습니다.");
        }

        private static void ValidateFailureOutcomeSplit(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData failureSave = CreateRichSave(balance);
            failureSave.currentLevel = 10;
            StarForgeEnhancementResult failure = service.TryEnhance(
                failureSave,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(1f, 0.2f).Next);
            Require(failure.kind == StarForgeResultKind.Failure, "실패 분기 40% 일반 실패 검증 실패");

            StarForgeSaveData fractureSave = CreateRichSave(balance);
            fractureSave.currentLevel = 10;
            StarForgeEnhancementResult fracture = service.TryEnhance(
                fractureSave,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(1f, 0.8f).Next);
            Require(fracture.kind == StarForgeResultKind.Fracture, "실패 분기 30% 균열 검증 실패");

            StarForgeSaveData destroyedSave = CreateRichSave(balance);
            destroyedSave.currentLevel = 10;
            destroyedSave.AddFracture();
            destroyedSave.AddFracture();
            destroyedSave.AddFracture();
            StarForgeEnhancementResult destroyed = service.TryEnhance(
                destroyedSave,
                balance,
                StarForgeCurrencyType.MeteorFragment,
                new SequenceRoller(1f, 0.9f).Next);
            Require(destroyed.kind == StarForgeResultKind.Destroyed, "실패 분기 30% 소멸 검증 실패");
        }

        private static void ValidateDestroyedRewardOnce(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 10;
            save.AddFracture();
            save.AddFracture();
            save.AddFracture();
            int[] beforeCurrencies = new int[5];
            for (int i = 0; i < beforeCurrencies.Length; i++)
            {
                beforeCurrencies[i] =
                    save.GetCurrency((StarForgeCurrencyType)i);
            }

            Require(
                balance.TryGetCost(
                    save.currentLevel,
                    StarForgeCurrencyType.MeteorFragment,
                    out int enhancementCost),
                "소멸 검증용 강화 비용을 찾지 못했습니다.");

            SequenceRoller roller = new SequenceRoller(1f, 0.9f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.MeteorFragment, roller.Next);

            Require(result.kind == StarForgeResultKind.Destroyed, "소멸 판정 검증 실패");
            Require(save.currentLevel == 0, "소멸 후 0강 복귀 실패");
            for (int i = 0; i < beforeCurrencies.Length; i++)
            {
                StarForgeCurrencyType type = (StarForgeCurrencyType)i;
                int expected = beforeCurrencies[i];
                if (type == StarForgeCurrencyType.MeteorFragment)
                {
                    expected -= enhancementCost;
                }

                if (result.rewards != null)
                {
                    for (int rewardIndex = 0;
                         rewardIndex < result.rewards.Length;
                         rewardIndex++)
                    {
                        CurrencyAmount reward = result.rewards[rewardIndex];
                        if (reward != null && reward.type == type)
                        {
                            expected += reward.amount;
                        }
                    }
                }

                Require(
                    save.GetCurrency(type) == expected,
                    "소멸 보상이 정확히 한 번 지급되지 않았습니다: " + type);
            }
            Require(save.destructionCount == 1, "소멸 횟수는 1회만 증가해야 합니다.");
        }

        private static void ValidateMaterialExchangeRoutes(
            StarForgeMaterialExchangeService service,
            StarForgeBalance balance)
        {
            DateTime testDate = new DateTime(2026, 6, 11);
            Require(service.RouteCount == 8, "재료 교환 경로는 8개여야 합니다.");

            for (int i = 0; i < service.RouteCount; i++)
            {
                StarForgeMaterialExchangeRoute route = service.GetRoute(i);
                StarForgeSaveData save = CreateRichSave(balance);
                save.highestLevel = 30;
                save.SetCurrency(route.sourceType, route.sourceAmount);
                save.SetCurrency(route.targetType, 0);

                StarForgeMaterialExchangeResult result = service.TryExchange(save, i, testDate);
                Require(result.success, "재료 교환 경로 " + i + " 실행 실패");
                Require(
                    save.GetCurrency(route.sourceType) == 0,
                    "재료 교환 경로 " + i + " 차감 실패");
                Require(
                    save.GetCurrency(route.targetType) == route.targetAmount,
                    "재료 교환 경로 " + i + " 지급 실패");
            }
        }

        private static void ValidateMaterialExchangeUnlocks(
            StarForgeMaterialExchangeService service,
            StarForgeBalance balance)
        {
            DateTime testDate = new DateTime(2026, 6, 11);
            int[] expectedUnlockLevels = { 0, 0, 10, 10, 15, 15, 25, 28 };

            for (int i = 0; i < expectedUnlockLevels.Length; i++)
            {
                StarForgeMaterialExchangeRoute route = service.GetRoute(i);
                Require(
                    route.requiredHighestLevel == expectedUnlockLevels[i],
                    "재료 교환 경로 " + i + " 해금 단계가 잘못되었습니다.");

                if (route.requiredHighestLevel <= 0)
                {
                    continue;
                }

                StarForgeSaveData save = CreateRichSave(balance);
                save.highestLevel = route.requiredHighestLevel - 1;
                int before = save.GetCurrency(route.sourceType);
                StarForgeMaterialExchangeResult result = service.TryExchange(save, i, testDate);
                Require(!result.success, "해금 전 재료 교환이 실행되었습니다.");
                Require(
                    save.GetCurrency(route.sourceType) == before,
                    "해금 전 실패한 교환이 재료를 차감했습니다.");
            }
        }

        private static void ValidatePrimordialDailyLimit(
            StarForgeMaterialExchangeService service,
            StarForgeBalance balance)
        {
            DateTime firstDay = new DateTime(2026, 6, 11);
            DateTime nextDay = firstDay.AddDays(1);
            const int routeIndex = 6;
            StarForgeMaterialExchangeRoute route = service.GetRoute(routeIndex);
            StarForgeSaveData save = CreateRichSave(balance);
            save.highestLevel = 30;

            Require(
                service.TryExchange(
                    save,
                    routeIndex,
                    route.dailyLimit,
                    firstDay).success,
                "원초의 별 일일 제한 내 복수 교환이 실패했습니다.");

            int before = save.GetCurrency(route.sourceType);
            Require(
                !service.TryExchange(save, routeIndex, firstDay).success,
                "원초의 별 일일 제한을 초과했습니다.");
            Require(
                save.GetCurrency(route.sourceType) == before,
                "일일 제한 초과 실패가 재료를 차감했습니다.");
            Require(
                service.TryExchange(save, routeIndex, nextDay).success,
                "날짜 변경 후 원초의 별 교환 횟수가 초기화되지 않았습니다.");
        }

        private static void ValidateMaterialExchangeQuantity(
            StarForgeMaterialExchangeService service,
            StarForgeBalance balance)
        {
            DateTime testDate = new DateTime(2026, 6, 11);
            const int routeIndex = 0;
            const int exchangeCount = 3;
            StarForgeMaterialExchangeRoute route = service.GetRoute(routeIndex);

            StarForgeSaveData successSave = CreateRichSave(balance);
            successSave.SetCurrency(
                route.sourceType,
                route.sourceAmount * exchangeCount);
            successSave.SetCurrency(route.targetType, 0);

            StarForgeMaterialExchangeResult result =
                service.TryExchange(
                    successSave,
                    routeIndex,
                    exchangeCount,
                    testDate);
            Require(result.success, "복수 수량 재료 교환이 실패했습니다.");
            Require(
                successSave.GetCurrency(route.sourceType) == 0,
                "복수 수량 재료 교환 차감이 잘못되었습니다.");
            Require(
                successSave.GetCurrency(route.targetType) ==
                route.targetAmount * exchangeCount,
                "복수 수량 재료 교환 지급이 잘못되었습니다.");

            StarForgeSaveData failureSave = CreateRichSave(balance);
            failureSave.SetCurrency(
                route.sourceType,
                route.sourceAmount * exchangeCount - 1);
            failureSave.SetCurrency(route.targetType, 7);
            int sourceBefore = failureSave.GetCurrency(route.sourceType);
            int targetBefore = failureSave.GetCurrency(route.targetType);

            result = service.TryExchange(
                failureSave,
                routeIndex,
                exchangeCount,
                testDate);
            Require(!result.success, "재료 부족 복수 교환이 성공했습니다.");
            Require(
                failureSave.GetCurrency(route.sourceType) == sourceBefore &&
                failureSave.GetCurrency(route.targetType) == targetBefore,
                "실패한 복수 교환이 재화를 변경했습니다.");
        }

        private static void ValidateCollectionStages(StarForgeBalance balance)
        {
            Require(balance.maxLevel >= 0, "도감 최대 레벨이 잘못되었습니다.");
            for (int level = 0; level <= balance.maxLevel; level++)
            {
                StageVisualConfig stage = balance.GetStage(level);
                Require(stage != null, "도감 " + level + "강 외형 데이터가 없습니다.");
                Require(
                    stage.level == level,
                    "도감 " + level + "강 외형 데이터 레벨이 일치하지 않습니다.");
                Require(
                    !string.IsNullOrWhiteSpace(stage.displayName),
                    "도감 " + level + "강 이름이 없습니다.");
            }
        }

        private static void ValidateAchievements(
            StarForgeAchievementService service,
            StarForgeBalance balance)
        {
            Require(
                service.GetDefinitions().Length == 85,
                "업적 정의 수가 예상과 다릅니다.");
            Require(
                service.GetNormalLevelDefinition(5)?.achievementName ==
                "티끌 모아 행성",
                "도감 5강 업적명이 올바르지 않습니다.");

            StarForgeSaveData normalSave =
                StarForgeSaveData.CreateNew(0);
            normalSave.highestLevel = 25;
            normalSave.defaultHighestLevel = 25;
            StarForgeAchievementUnlock[] normalUnlocks =
                service.CompleteAvailable(normalSave);
            Require(normalUnlocks.Length == 9, "25강 누적 업적 수가 올바르지 않습니다.");
            Require(
                service.ClaimAllRewards(normalSave) == 9,
                "25강 누적 업적 보상 수령 수가 올바르지 않습니다.");
            Require(
                normalSave.GetCurrency(StarForgeCurrencyType.MeteorFragment) ==
                200,
                "25강 누적 운석 파편 업적 보상이 올바르지 않습니다.");
            Require(
                normalSave.GetCurrency(StarForgeCurrencyType.StarShard) == 90,
                "25강 누적 별의 조각 업적 보상이 올바르지 않습니다.");
            Require(
                normalSave.GetCurrency(StarForgeCurrencyType.PureCoreShard) ==
                690,
                "25강 누적 별핵 조각 업적 보상이 올바르지 않습니다.");
            Require(
                normalSave.GetCurrency(
                    StarForgeCurrencyType.SingularityShard) == 20,
                "25강 누적 특이성 조각 업적 보상이 올바르지 않습니다.");
            Require(
                service.CompleteAvailable(normalSave).Length == 0,
                "완료된 일반 업적이 중복 지급되었습니다.");
            normalSave.highestLevel = 30;
            normalSave.defaultHighestLevel = 30;
            Require(
                service.CompleteAvailable(normalSave).Length == 5,
                "26강부터 30강까지의 업적 수가 올바르지 않습니다.");
            Require(
                service.ClaimAllRewards(normalSave) == 5,
                "26강부터 30강까지의 업적 보상 수령 수가 올바르지 않습니다.");
            Require(
                normalSave.GetCurrency(StarForgeCurrencyType.PureCoreShard) ==
                1590 &&
                normalSave.GetCurrency(
                    StarForgeCurrencyType.SingularityShard) == 43 &&
                normalSave.GetCurrency(StarForgeCurrencyType.PrimordialStar) ==
                75,
                "30강 누적 업적 보상이 올바르지 않습니다.");

            StarForgeSaveData specialSave =
                StarForgeSaveData.CreateNew(0);
            specialSave.collectionProgressInitialized = true;
            specialSave.defaultHighestLevel = 0;
            specialSave.heartHighestLevel = -1;
            specialSave.catHighestLevel = 0;
            specialSave.highestBlackHoleLevel = 5;
            StarForgeAchievementUnlock[] specialUnlocks =
                service.CompleteAvailable(specialSave);
            Require(
                specialUnlocks.Length == 7,
                "고양이 별 및 블랙홀 5강 누적 업적 수가 올바르지 않습니다.");
            Require(
                service.ClaimAllRewards(specialSave) == 7,
                "고양이 별 및 블랙홀 5강 누적 업적 보상 수령 수가 올바르지 않습니다.");
            Require(
                specialSave.GetCurrency(StarForgeCurrencyType.MeteorFragment) ==
                50 &&
                specialSave.GetCurrency(StarForgeCurrencyType.StarShard) == 25,
                "고양이 별 발견 업적 보상이 올바르지 않습니다.");
            Require(
                specialSave.GetCurrency(StarForgeCurrencyType.PureCoreShard) ==
                105,
                "고양이 별 및 블랙홀 발견 별핵 보상이 올바르지 않습니다.");
            Require(
                specialSave.GetCurrency(
                    StarForgeCurrencyType.SingularityShard) == 250,
                "블랙홀 5강 누적 특이성 조각 보상이 올바르지 않습니다.");
            Require(
                specialSave.GetCurrency(StarForgeCurrencyType.PrimordialStar) ==
                48,
                "블랙홀 5강 누적 원초의 별 보상이 올바르지 않습니다.");
            Require(
                service.CompleteAvailable(specialSave).Length == 0,
                "완료된 특수 업적이 중복 지급되었습니다.");
            specialSave.highestBlackHoleLevel = 10;
            Require(
                service.CompleteAvailable(specialSave).Length == 5,
                "블랙홀 6강부터 10강까지의 업적 수가 올바르지 않습니다.");
            Require(
                service.ClaimAllRewards(specialSave) == 5,
                "블랙홀 6강부터 10강까지의 업적 보상 수령 수가 올바르지 않습니다.");
            Require(
                specialSave.GetCurrency(StarForgeCurrencyType.PrimordialStar) ==
                10567,
                "블랙홀 10강 누적 원초의 별 보상이 올바르지 않습니다.");

            StarForgeSaveData recordSave = StarForgeSaveData.CreateNew(0);
            recordSave.failureCount = 10000;
            recordSave.consecutiveFailureCount = 20;
            recordSave.totalFractureCount = 3000;
            recordSave.consecutiveFractureCount = 10;
            recordSave.destructionCount = 3000;
            recordSave.successCount = 5000;
            recordSave.consecutiveSuccessCount = 20;
            recordSave.greatSuccessCount = 100;
            recordSave.consecutiveGreatSuccessCount = 3;
            Require(
                service.CompleteAvailable(recordSave).Length == 36,
                "강화 기록 업적 수가 올바르지 않습니다.");
        }

        private static StarForgeSaveData CreateRichSave(StarForgeBalance balance)
        {
            StarForgeSaveData save = StarForgeSaveData.CreateNew(balance.firstLaunchMeteorFragments);
            save.SetCurrency(StarForgeCurrencyType.MeteorFragment, 100000);
            save.SetCurrency(StarForgeCurrencyType.StarShard, 100000);
            save.SetCurrency(StarForgeCurrencyType.PureCoreShard, 100000);
            save.SetCurrency(StarForgeCurrencyType.SingularityShard, 100000);
            save.SetCurrency(StarForgeCurrencyType.PrimordialStar, 100000);
            return save;
        }

        private static float AlwaysZero()
        {
            return 0f;
        }

        private static void Require(bool condition, string message)
        {
            if (condition)
            {
                return;
            }

            Debug.LogError(message);
            EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }

        private sealed class SequenceRoller
        {
            private readonly float[] values;
            private int index;

            public SequenceRoller(params float[] values)
            {
                this.values = values;
            }

            public float Next()
            {
                if (values == null || values.Length == 0)
                {
                    return 0f;
                }

                float value = values[Mathf.Min(index, values.Length - 1)];
                index++;
                return value;
            }
        }
    }
}
#endif
