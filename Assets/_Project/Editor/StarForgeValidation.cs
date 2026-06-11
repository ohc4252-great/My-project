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
            StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();

            ValidateMeteorUnavailableAt20(service, balance);
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

            Debug.Log("StarForge core validation passed.");
        }

        private static void ValidateMeteorUnavailableAt20(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 20;

            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.MeteorFragment, AlwaysZero);
            Require(result.kind == StarForgeResultKind.MaterialUnavailable, "20강 이후 운석 파편 사용 차단 실패");
            Require(save.currentLevel == 20, "사용 불가 재료가 단계를 변경했습니다.");
        }

        private static void ValidateGreatSuccessCapAt25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 24;

            SequenceRoller roller = new SequenceRoller(0f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.StarShard, roller.Next);
            Require(result.kind == StarForgeResultKind.GreatSuccess, "24강 대성공 판정 실패");
            Require(save.currentLevel == 25, "24강 대성공은 25강을 초과하면 안 됩니다.");
        }

        private static void ValidatePureCoreCapAt25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 24;

            SequenceRoller roller = new SequenceRoller(0f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.PureCoreShard, roller.Next);
            Require(result.kind == StarForgeResultKind.Success, "온전한 별핵 조각 +2 상한 처리 결과 타입이 예상과 다릅니다.");
            Require(save.currentLevel == 25, "온전한 별핵 조각 +2는 25강을 초과하면 안 됩니다.");
        }

        private static void ValidateNoGreatSuccessAfter25(StarForgeEnhancementService service, StarForgeBalance balance)
        {
            StarForgeSaveData save = CreateRichSave(balance);
            save.currentLevel = 25;

            SequenceRoller roller = new SequenceRoller(0f, 0f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.PrimordialStar, roller.Next);
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
                new SequenceRoller(1f, 0.5f).Next);
            Require(fracture.kind == StarForgeResultKind.Fracture, "실패 분기 30% 균열 검증 실패");

            StarForgeSaveData destroyedSave = CreateRichSave(balance);
            destroyedSave.currentLevel = 10;
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
            int beforeMeteor = save.GetCurrency(StarForgeCurrencyType.MeteorFragment);
            int beforeStarShard = save.GetCurrency(StarForgeCurrencyType.StarShard);

            SequenceRoller roller = new SequenceRoller(1f, 0.9f);
            StarForgeEnhancementResult result = service.TryEnhance(save, balance, StarForgeCurrencyType.MeteorFragment, roller.Next);

            Require(result.kind == StarForgeResultKind.Destroyed, "소멸 판정 검증 실패");
            Require(save.currentLevel == 0, "소멸 후 0강 복귀 실패");
            Require(save.GetCurrency(StarForgeCurrencyType.MeteorFragment) == beforeMeteor - 70 + 120, "소멸 운석 보상 지급 실패");
            Require(save.GetCurrency(StarForgeCurrencyType.StarShard) == beforeStarShard + 5, "소멸 별의 조각 보상 지급 실패");
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
