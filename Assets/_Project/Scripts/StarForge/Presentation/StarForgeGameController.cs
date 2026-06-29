using System.Collections;
using StarForge.Core;
using StarForge.Data;
using StarForge.Save;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarForge.Presentation
{
    public sealed class StarForgeGameController : MonoBehaviour
    {
        private const float PlanetVerticalOffsetPixels = 50f;
        private const int BaseDailyMiningLimit = 3;
        private const int DailyMiningAdBonusLimit = 5;
        private const float CameraOrbitDegreesPerPixel = 0.22f;
        private const float CameraOrbitPitchLimit = 55f;
        private const float BlackHoleDefaultCameraPitch = 10f;
        private const string MiningDateFormat = "yyyy-MM-dd";
        private const string DestructionKeepPlacement =
            "destruction_keep_level";
        private const string MiningBonusPlacement =
            "mining_bonus_attempt";
        // Self-imposed throttle on the planet-keep revive ad so repeated views in a
        // short window don't trip AdMob's serving limits.
        private const float ReviveAdCooldownSeconds = 180f;
        private const string ReviveAdCooldownPrefKey =
            "StarForge.ReviveAd.CooldownUntilBinary";
        [Header("Optional Overrides")]
        [SerializeField] private TextAsset balanceJson;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private StarForgePlanetView planetView;
        [SerializeField] private StarForgeEffectController effectController;
        [SerializeField] private StarForgeHudView hudView;
        [SerializeField] private StarForgeAudioController audioController;
        [SerializeField] private StarForgeMiningGameView miningGameView;
        [SerializeField] private MonoBehaviour rewardedAdProvider;

        [Header("AdMob Rewarded Ad Unit IDs")]
#pragma warning disable CS0414
        [SerializeField] private string androidMiningBonusRewardedAdUnitId =
            "ca-app-pub-3971219491693844/1293240258";
        [SerializeField] private string iosMiningBonusRewardedAdUnitId =
            "ca-app-pub-3971219491693844/4869523183";
        [SerializeField] private string androidDestructionKeepRewardedAdUnitId =
            "ca-app-pub-3971219491693844/5277054006";
        [SerializeField] private string iosDestructionKeepRewardedAdUnitId =
            "ca-app-pub-3971219491693844/7495686527";
        [SerializeField] private bool useTestRewardedAds;
#pragma warning restore CS0414

        private readonly StarForgeEnhancementService enhancementService = new StarForgeEnhancementService();
        private readonly StarForgeAchievementService achievementService = new StarForgeAchievementService();
        private readonly StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();
        private readonly StarForgeReviveService reviveService = new StarForgeReviveService();
        private readonly StarForgeSaveRepository saveRepository = new StarForgeSaveRepository();

        private StarForgeBalance balance;
        private StarForgeSaveData saveData;
        private StarForgeCurrencyType selectedCurrency;
        private IStarForgeRewardedAdService rewardedAdService;
        private bool isResolving;
        private bool rewardedAdInProgress;
        private CurrencyAmount[] lastMiningRewards;
        private string lastMiningSettledDateKey;
        private int lastMiningScorePermyriad = -1;
        private int lastDestroyedLevel;
        private StarForgeEnhancementResult pendingDestroyedResult;
        private Coroutine cameraMoveRoutine;
        private float cameraOrbitYaw;
        private float cameraOrbitPitch;
        private float cameraDistance = 7.5f;
        private bool cameraOrbitStateIsBlackHole;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            balance = StarForgeBalanceLoader.Load(balanceJson);
            saveData = saveRepository.Load(balance);
            rewardedAdService = CreateRewardedAdService();
            selectedCurrency = (StarForgeCurrencyType)Mathf.Clamp(saveData.selectedCurrency, 0, 4);

            EnsureRuntimeObjects();
            BindHud();
        }

        private void Start()
        {
            StarForgeAchievementUnlock[] achievements =
                CompleteAvailableAchievements();
            RefreshViews();
            SetCameraZ(GetRestCameraZForCurrentState());
            ShowAchievementOverlays(achievements);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && saveData != null)
            {
                saveRepository.Save(saveData);
            }
        }

        private void OnApplicationQuit()
        {
            if (saveData != null)
            {
                saveRepository.Save(saveData);
            }
        }

        private void BindHud()
        {
            hudView.EnhanceClicked += HandleEnhanceClicked;
            hudView.CurrencySelected += HandleCurrencySelected;
            hudView.MaterialExchangeRequested += HandleMaterialExchangeRequested;
            hudView.ResetConfirmed += HandleResetConfirmed;
            hudView.SoundToggled += HandleSoundToggled;
            hudView.BgmVolumeChanged += HandleBgmVolumeChanged;
            hudView.SfxVolumeChanged += HandleSfxVolumeChanged;
            hudView.VibrationToggled += HandleVibrationToggled;
            hudView.EnhancementAnimationSkipToggled +=
                HandleEnhancementAnimationSkipToggled;
            hudView.FractureAlertMutedToggled +=
                HandleFractureAlertMutedToggled;
            hudView.AchievementAlertMutedToggled +=
                HandleAchievementAlertMutedToggled;
            hudView.CameraOrbitDragged += HandleCameraOrbitDragged;
            hudView.ReviveRequested += HandleReviveRequested;
            hudView.RewardedReviveRequested +=
                HandleRewardedReviveRequested;
            hudView.DisassembleRequested += HandleDisassembleRequested;
            hudView.MiningRequested += HandleMiningRequested;
            hudView.AchievementClaimRequested +=
                HandleAchievementClaimRequested;
            hudView.AchievementClaimAllRequested +=
                HandleAchievementClaimAllRequested;
            miningGameView.MiningStopped += HandleMiningStopped;
            miningGameView.MiningRewardAccepted +=
                HandleMiningRewardAccepted;
            miningGameView.MiningClosed += HandleMiningClosed;
            miningGameView.MiningAbandoned += HandleMiningAbandoned;
            miningGameView.ContinueWithAdRequested +=
                HandleContinueWithAdRequested;
            miningGameView.BonusAttemptWithAdRequested +=
                HandleBonusAttemptWithAdRequested;
        }

        private IStarForgeRewardedAdService CreateRewardedAdService()
        {
            if (rewardedAdProvider is IStarForgeRewardedAdService provider)
            {
                return provider;
            }

#if UNITY_EDITOR
            return new StarForgeRewardedAdPlaceholderService();
#else
            if (StarForgeAdMobRewardedAdService.IsSdkAvailable())
            {
                GameObject adObject = new GameObject(
                    "StarForge AdMob Rewarded Ads");
                adObject.transform.SetParent(transform, false);
                StarForgeAdMobRewardedAdService adService =
                    adObject.AddComponent<StarForgeAdMobRewardedAdService>();
                adService.Configure(
                    androidMiningBonusRewardedAdUnitId,
                    iosMiningBonusRewardedAdUnitId,
                    androidDestructionKeepRewardedAdUnitId,
                    iosDestructionKeepRewardedAdUnitId,
                    useTestRewardedAds);
                return adService;
            }

            return new StarForgeRewardedAdPlaceholderService();
#endif
        }

        private void HandleMiningRequested()
        {
            if (isResolving)
            {
                return;
            }

            int remainingPlays = GetRemainingMiningPlays();
            int remainingAdBonuses = GetRemainingMiningAdBonuses();
            if (remainingPlays <= 0 && remainingAdBonuses <= 0)
            {
                hudView.ShowMessage(
                    "오늘의 별 탐사 완료",
                    "오늘 기본 탐사를 모두 사용했습니다.");
                return;
            }

            miningGameView.Open(remainingPlays, remainingAdBonuses);
            audioController.SetMiningModeActive(true);
        }

        private void HandleMiningStopped(float normalizedScore)
        {
            string today = GetMiningDateKey();
            int dailyLimit = saveData.GetMiningDailyLimit(
                today,
                BaseDailyMiningLimit,
                DailyMiningAdBonusLimit);
            if (!saveData.TryUseMiningPlay(today, dailyLimit))
            {
                miningGameView.ShowDailyLimit();
                RefreshViews();
                return;
            }

            CurrencyAmount[] rewards = BuildMiningRewards(normalizedScore);
            lastMiningRewards = rewards;
            lastMiningSettledDateKey = today;
            lastMiningScorePermyriad =
                Mathf.RoundToInt(Mathf.Clamp01(normalizedScore) * 10000f);
            saveRepository.Save(saveData);
            RefreshViews();
            miningGameView.ShowOutcome(
                normalizedScore,
                rewards,
                saveData.GetRemainingMiningPlays(today, dailyLimit),
                GetRemainingMiningAdBonuses());
        }

        private void HandleMiningRewardAccepted()
        {
            GrantPendingMiningRewards();
        }

        // Player bailed out of an in-progress run via the exit confirmation: the
        // Attempt is spent, but no reward is granted and no result is shown.
        private void HandleMiningAbandoned()
        {
            string today = GetMiningDateKey();
            int dailyLimit = saveData.GetMiningDailyLimit(
                today,
                BaseDailyMiningLimit,
                DailyMiningAdBonusLimit);
            if (saveData.TryUseMiningPlay(today, dailyLimit))
            {
                saveRepository.Save(saveData);
                RefreshViews();
            }
        }

        private void HandleContinueWithAdRequested()
        {
            if (isResolving ||
                rewardedAdInProgress ||
                lastMiningRewards == null)
            {
                return;
            }

            if (!rewardedAdService.IsReady(MiningBonusPlacement))
            {
                miningGameView.SetRewardedAdBusy(false);
                hudView.ShowMessage(
                    "광고 준비 중",
                    "보상형 광고를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.");
                return;
            }

            string settledDateKey = lastMiningSettledDateKey;
            rewardedAdInProgress = true;
            miningGameView.SetRewardedAdBusy(true);
            rewardedAdService.Show(
                MiningBonusPlacement,
                completed =>
                {
                    rewardedAdInProgress = false;
                    if (!completed)
                    {
                        miningGameView.SetRewardedAdBusy(false);
                        return;
                    }

                    saveData.RefundMiningPlay(settledDateKey);
                    lastMiningRewards = null;
                    lastMiningSettledDateKey = string.Empty;
                    lastMiningScorePermyriad = -1;
                    saveRepository.Save(saveData);
                    RefreshViews();
                    miningGameView.ResumeRunWithBooster();
                });
        }

        private void HandleBonusAttemptWithAdRequested()
        {
            if (isResolving || rewardedAdInProgress)
            {
                return;
            }

            string today = GetMiningDateKey();
            if (saveData.EnsureMiningDay(today))
            {
                saveRepository.Save(saveData);
            }

            if (GetRemainingMiningAdBonuses() <= 0)
            {
                miningGameView.ShowDailyLimit();
                RefreshViews();
                return;
            }

            if (!rewardedAdService.IsReady(MiningBonusPlacement))
            {
                miningGameView.SetRewardedAdBusy(false);
                hudView.ShowMessage(
                    "광고 준비 중",
                    "보상형 광고를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.");
                return;
            }

            rewardedAdInProgress = true;
            miningGameView.SetRewardedAdBusy(true);
            rewardedAdService.Show(
                MiningBonusPlacement,
                completed =>
                {
                    rewardedAdInProgress = false;
                    miningGameView.SetRewardedAdBusy(false);
                    if (!completed)
                    {
                        return;
                    }

                    string rewardDate = GetMiningDateKey();
                    if (!saveData.TryGrantMiningAdBonus(
                            rewardDate,
                            DailyMiningAdBonusLimit))
                    {
                        miningGameView.ShowDailyLimit();
                        RefreshViews();
                        return;
                    }

                    saveRepository.Save(saveData);
                    RefreshViews();

                    int dailyLimit = saveData.GetMiningDailyLimit(
                        rewardDate,
                        BaseDailyMiningLimit,
                        DailyMiningAdBonusLimit);
                    miningGameView.Open(
                        saveData.GetRemainingMiningPlays(rewardDate, dailyLimit),
                        GetRemainingMiningAdBonuses());
                    audioController.SetMiningModeActive(true);
                });
        }

        private void HandleMiningClosed()
        {
            GrantPendingMiningRewards();
            audioController.SetMiningModeActive(false);
        }

        private void GrantPendingMiningRewards()
        {
            CurrencyAmount[] rewards = lastMiningRewards;
            if (rewards == null)
            {
                return;
            }

            string miningSettledDateKey = lastMiningSettledDateKey;
            lastMiningRewards = null;
            lastMiningSettledDateKey = string.Empty;
            if (lastMiningScorePermyriad >= 0)
            {
                saveData.RecordMiningCompletion(
                    miningSettledDateKey,
                    lastMiningScorePermyriad);
            }

            lastMiningScorePermyriad = -1;
            for (int i = 0; i < rewards.Length; i++)
            {
                CurrencyAmount reward = rewards[i];
                if (reward != null && reward.amount > 0)
                {
                    saveData.AddCurrency(reward.type, reward.amount);
                }
            }

            StarForgeAchievementUnlock[] achievements =
                achievementService.CompleteAvailable(saveData);
            saveRepository.Save(saveData);
            RefreshViews();
            ShowAchievementOverlays(achievements);
        }

        private static CurrencyAmount[] BuildMiningRewards(float normalizedScore)
        {
            float score = Mathf.Clamp01(normalizedScore);
            if (score >= 1f)
            {
                return new[]
                {
                    new CurrencyAmount(
                        StarForgeCurrencyType.PureCoreShard,
                        100),
                    new CurrencyAmount(
                        StarForgeCurrencyType.SingularityShard,
                        5),
                    new CurrencyAmount(
                        StarForgeCurrencyType.PrimordialStar,
                        1)
                };
            }

            if (score >= 0.5f)
            {
                return new[]
                {
                    new CurrencyAmount(
                        StarForgeCurrencyType.PureCoreShard,
                        100),
                    new CurrencyAmount(
                        StarForgeCurrencyType.SingularityShard,
                        2)
                };
            }

            if (score >= 0.3f)
            {
                return new[]
                {
                    new CurrencyAmount(
                        StarForgeCurrencyType.PureCoreShard,
                        50),
                    new CurrencyAmount(
                        StarForgeCurrencyType.SingularityShard,
                        1)
                };
            }

            if (score >= 0.2f)
            {
                return new[]
                {
                    new CurrencyAmount(
                        StarForgeCurrencyType.MeteorFragment,
                        500),
                    new CurrencyAmount(
                        StarForgeCurrencyType.StarShard,
                        150),
                    new CurrencyAmount(
                        StarForgeCurrencyType.PureCoreShard,
                        15)
                };
            }

            if (score >= 0.1f)
            {
                return new[]
                {
                    new CurrencyAmount(
                        StarForgeCurrencyType.MeteorFragment,
                        200),
                    new CurrencyAmount(
                        StarForgeCurrencyType.StarShard,
                        100),
                    new CurrencyAmount(
                        StarForgeCurrencyType.PureCoreShard,
                        5)
                };
            }

            return new[]
            {
                new CurrencyAmount(
                    StarForgeCurrencyType.MeteorFragment,
                    100),
                new CurrencyAmount(
                    StarForgeCurrencyType.StarShard,
                    30)
            };
        }

        private int GetRemainingMiningPlays()
        {
            string today = GetMiningDateKey();
            if (saveData.EnsureMiningDay(today))
            {
                saveRepository.Save(saveData);
            }

            int dailyLimit = saveData.GetMiningDailyLimit(
                today,
                BaseDailyMiningLimit,
                DailyMiningAdBonusLimit);
            return saveData.GetRemainingMiningPlays(today, dailyLimit);
        }

        private int GetRemainingMiningAdBonuses()
        {
            string today = GetMiningDateKey();
            if (saveData.EnsureMiningDay(today))
            {
                saveRepository.Save(saveData);
            }

            return saveData.GetRemainingMiningAdBonuses(
                today,
                DailyMiningAdBonusLimit);
        }

        private static string GetMiningDateKey()
        {
            return System.DateTime.Now.ToString(
                MiningDateFormat,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private void HandleCurrencySelected(StarForgeCurrencyType currencyType)
        {
            if (isResolving)
            {
                return;
            }

            selectedCurrency = currencyType;
            saveData.selectedCurrency = (int)selectedCurrency;
            saveRepository.Save(saveData);
            RefreshViews();
        }

        private void HandleMaterialExchangeRequested(int routeIndex, int exchangeCount)
        {
            if (isResolving)
            {
                return;
            }

            StarForgeMaterialExchangeResult result =
                exchangeService.TryExchange(
                    saveData,
                    routeIndex,
                    exchangeCount,
                    System.DateTime.Now);
            if (result.success)
            {
                saveRepository.Save(saveData);
            }

            RefreshViews();
            hudView.ShowExchangeResult(result);
        }

        private void HandleEnhanceClicked()
        {
            // Ignore enhance taps while a guidance popup (result / fracture /
            // black hole guide / revive) is open. Those popups don't cover the button, so rapid tapping
            // could otherwise blow past a freshly discovered black hole unseen.
            if (isResolving || hudView.IsBlockingOverlayOpen)
            {
                return;
            }

            StarForgeAttemptPreview preview = enhancementService.GetPreview(saveData, balance, selectedCurrency);
            if (!preview.isAvailable || preview.isMaxLevel || !preview.hasEnoughCurrency)
            {
                // Blocked attempt (unavailable / max / not enough currency): resolve the
                // message without ever rolling a free black hole discovery.
                StarForgeEnhancementResult result = enhancementService.TryEnhance(
                    saveData,
                    balance,
                    selectedCurrency,
                    () => Random.value,
                    false);

                hudView.ShowResult(result);
                RefreshViews();
                return;
            }

            StartCoroutine(EnhanceRoutine());
        }

        private IEnumerator EnhanceRoutine()
        {
            isResolving = true;
            RefreshViews();

            int attemptLevel = GetEnhancementEffectLevel();
            // Black hole moments (enhancing/destruction of an existing black hole, or a black
            // hole discovery) always play their cinematic, even when the enhancement-
            // animation skip is on. Discovery is random, so pre-roll the decision here
            // and feed it to TryEnhance so the skip choice matches the actual outcome.
            bool willDiscoverBlackHole =
                enhancementService.WouldDiscoverBlackHole(
                    saveData, balance, Random.value);
            bool forceBlackHoleCinematic =
                saveData.isBlackHole || willDiscoverBlackHole;
            if (saveData.enhancementAnimationSkipEnabled && !forceBlackHoleCinematic)
            {
                yield return null;
                ResolveEnhancementWithoutAnimation(attemptLevel, false);
                yield break;
            }

            float fallbackChargeDuration = Mathf.Min(0.45f + attemptLevel * 0.03f, 1.3f);
            float chargeDuration = audioController.GetChargeDuration(attemptLevel, fallbackChargeDuration);
            int shardCount = Mathf.Clamp(10 + attemptLevel + Mathf.RoundToInt(chargeDuration * 6f), 12, 64);
            float effectIntensity = 1f + attemptLevel * 0.045f;
            bool useHighTierSuccessTransition =
                saveData.isBlackHole ||
                willDiscoverBlackHole ||
                attemptLevel == 28 ||
                attemptLevel == 29;

            Transform planetTarget = planetView.Target;
            audioController.PlayCharge(attemptLevel);
            planetView.PlayChargePulse(chargeDuration);

            // 강화 시작: 행성 쪽으로 줌 인 (충전 동안 행성은 떨림)
            StartCameraMove(GetRestCameraZ(attemptLevel) * 0.62f, Mathf.Min(0.45f, chargeDuration));

            StarForgeEnhancementResult result;
            StarForgeAchievementUnlock[] unlockedAchievements;
            bool resultAudioPlayedEarly = false;

            if (useHighTierSuccessTransition)
            {
                Coroutine shardFlyRoutine = StartCoroutine(
                    effectController.PlayShardFly(
                        planetTarget,
                        shardCount,
                        chargeDuration,
                        attemptLevel));
                float resultLeadTime = Mathf.Min(0.2f, chargeDuration);
                float resultDelay = Mathf.Max(0f, chargeDuration - resultLeadTime);

                if (resultDelay > 0f)
                {
                    yield return WaitForDuration(resultDelay);
                }

                result = enhancementService.TryEnhance(
                    saveData,
                    balance,
                    selectedCurrency,
                    () => Random.value,
                    willDiscoverBlackHole);
                unlockedAchievements =
                    achievementService.CompleteAvailable(saveData);

                if (result.kind == StarForgeResultKind.Success ||
                    result.kind == StarForgeResultKind.GreatSuccess)
                {
                    audioController.PlayResult(result.kind, attemptLevel);
                    resultAudioPlayedEarly = true;
                }

                saveRepository.Save(saveData);

                if (resultLeadTime > 0f)
                {
                    yield return WaitForDuration(resultLeadTime);
                }

                yield return shardFlyRoutine;
            }
            else
            {
                yield return effectController.PlayShardFly(
                    planetTarget,
                    shardCount,
                    chargeDuration,
                    attemptLevel);

                result = enhancementService.TryEnhance(
                    saveData,
                    balance,
                    selectedCurrency,
                    () => Random.value,
                    willDiscoverBlackHole);
                unlockedAchievements =
                    achievementService.CompleteAvailable(saveData);
                saveRepository.Save(saveData);
            }

            planetView.StopChargePulse();
            audioController.StopCharge();

            bool isSuccess =
                result.kind == StarForgeResultKind.Success ||
                result.kind == StarForgeResultKind.GreatSuccess;

            SyncCameraOrbitDefaultForCurrentState();

            if (useHighTierSuccessTransition && isSuccess)
            {
                StartCameraMove(GetRestCameraZForCurrentState(), 1f);
                Coroutine cameraReaction = null;
                yield return planetView.PlayHighTierSuccessTransition(
                    GetCurrentVisualStage(),
                    1f,
                    () =>
                    {
                        effectController.PlayResult(
                            result.kind,
                            planetTarget.position,
                            effectIntensity);
                        cameraReaction = StartCoroutine(PlayCameraReaction(result.kind));
                    });

                if (cameraReaction != null)
                {
                    yield return cameraReaction;
                }
            }
            else if (isSuccess)
            {
                // 성공: 1.3^상승단계 만큼 부풀어 오르고, 그에 맞춰 카메라가 줌 아웃
                if (!resultAudioPlayedEarly)
                {
                    audioController.PlayResult(result.kind, attemptLevel);
                }

                effectController.PlayResult(result.kind, planetTarget.position, effectIntensity);

                if (saveData.vibrationEnabled && result.kind == StarForgeResultKind.GreatSuccess)
                {
                    Handheld.Vibrate();
                }

                StartCameraMove(GetRestCameraZForCurrentState(), 0.85f);
                yield return planetView.PlaySuccessGrowth(
                    GetCurrentVisualStage(),
                    result.levelGain);
            }
            else
            {
                // 실패/균열/소멸: 행성은 그대로(또는 0강), 카메라만 제자리로 줌 아웃
                ApplyPlanetVisual();
                StartCameraMove(GetRestCameraZForCurrentState(), 0.5f);

                if (!resultAudioPlayedEarly)
                {
                    audioController.PlayResult(result.kind, attemptLevel);
                }

                effectController.PlayResult(result.kind, planetTarget.position, effectIntensity);

                if (saveData.vibrationEnabled &&
                    (result.kind == StarForgeResultKind.Destroyed ||
                     result.kind == StarForgeResultKind.GreatSuccess ||
                     result.kind == StarForgeResultKind.Fracture))
                {
                    Handheld.Vibrate();
                }

                yield return PlayCameraReaction(result.kind);
            }

            if (isSuccess &&
                (result.isBlackHole || result.discoveredBlackHole) &&
                saveData.isBlackHole)
            {
                planetView.ApplyBlackHoleStage(saveData.blackHoleLevel);
            }

            if (result.kind == StarForgeResultKind.Destroyed)
            {
                if (result.isBlackHole)
                {
                    lastDestroyedLevel = 0;
                    pendingDestroyedResult = null;
                    hudView.ShowResult(result);
                }
                else
                {
                    lastDestroyedLevel = result.previousLevel;
                    pendingDestroyedResult = result;
                    hudView.ShowReviveOverlay(result, saveData);
                    hudView.SetReviveAdCooldown(GetReviveAdCooldownRemaining());
                }
            }
            else if (result.kind == StarForgeResultKind.Fracture)
            {
                if (!saveData.fractureAlertMuted)
                {
                    hudView.ShowResult(result);
                }
            }
            else if (result.discoveredBlackHole)
            {
                hudView.ShowMessage(
                    "블랙홀 발견",
                    "당신은 블랙홀을 발견했습니다.\n\n" +
                    "블랙홀은 등급이 높아질수록\n" +
                    "분해 시 훨씬 많은 보상을 지급하지만,\n" +
                    "소멸 시에는 아무런 보상도 획득할 수 없습니다.");
            }

            isResolving = false;
            RefreshViews();
            ShowAchievementOverlays(unlockedAchievements);
        }

        private void ResolveEnhancementWithoutAnimation(
            int attemptLevel,
            bool overrideDiscovery)
        {
            StarForgeEnhancementResult result =
                enhancementService.TryEnhance(
                    saveData,
                    balance,
                    selectedCurrency,
                    () => Random.value,
                    overrideDiscovery);
            StarForgeAchievementUnlock[] unlockedAchievements =
                achievementService.CompleteAvailable(saveData);
            saveRepository.Save(saveData);

            if (cameraMoveRoutine != null)
            {
                StopCoroutine(cameraMoveRoutine);
                cameraMoveRoutine = null;
            }

            audioController.StopCharge();
            planetView.StopChargePulse();
            ApplyPlanetVisual();
            SyncCameraOrbitDefaultForCurrentState();
            SetCameraZ(GetRestCameraZForCurrentState());
            audioController.PlayResult(result.kind, attemptLevel);

            if (saveData.vibrationEnabled &&
                (result.kind == StarForgeResultKind.Destroyed ||
                 result.kind == StarForgeResultKind.GreatSuccess ||
                 result.kind == StarForgeResultKind.Fracture))
            {
                Handheld.Vibrate();
            }

            if (result.kind == StarForgeResultKind.Destroyed)
            {
                if (result.isBlackHole)
                {
                    lastDestroyedLevel = 0;
                    pendingDestroyedResult = null;
                    hudView.ShowResult(result);
                }
                else
                {
                    lastDestroyedLevel = result.previousLevel;
                    pendingDestroyedResult = result;
                    hudView.ShowReviveOverlay(result, saveData);
                    hudView.SetReviveAdCooldown(GetReviveAdCooldownRemaining());
                }
            }
            else if (result.kind == StarForgeResultKind.Fracture)
            {
                if (!saveData.fractureAlertMuted)
                {
                    hudView.ShowResult(result);
                }
            }
            else if (result.discoveredBlackHole)
            {
                hudView.ShowMessage(
                    "블랙홀 발견",
                    "당신은 블랙홀을 발견했습니다.\n\n" +
                    "블랙홀은 등급이 높아질수록\n" +
                    "분해 시 훨씬 많은 보상을 지급하지만,\n" +
                    "소멸 시에는 아무런 보상도 획득할 수 없습니다.");
            }

            isResolving = false;
            RefreshViews();
            ShowAchievementOverlays(unlockedAchievements);
        }

        private StarForgeAchievementUnlock[] CompleteAvailableAchievements()
        {
            StarForgeAchievementUnlock[] achievements =
                achievementService.CompleteAvailable(saveData);
            if (achievements.Length > 0)
            {
                saveRepository.Save(saveData);
            }

            return achievements;
        }

        private void ShowAchievementOverlays(
            StarForgeAchievementUnlock[] achievements)
        {
            if (achievements != null && !saveData.achievementAlertMuted)
            {
                for (int i = 0; i < achievements.Length; i++)
                {
                    hudView.ShowAchievementToast(
                        achievements[i].definition.achievementName,
                        achievements[i].definition.tooltip);
                }
            }

            UpdateAchievementNotification();
        }

        private void UpdateAchievementNotification()
        {
            hudView.SetAchievementClaimable(
                achievementService.HasClaimableRewards(saveData));
        }

        private void HandleAchievementClaimRequested(string achievementId)
        {
            if (!achievementService.TryClaimReward(saveData, achievementId))
            {
                return;
            }

            saveRepository.Save(saveData);
            RefreshViews();
            hudView.RefreshAchievementList();
            UpdateAchievementNotification();
        }

        private void HandleAchievementClaimAllRequested()
        {
            CurrencyAmount[] claimedRewards =
                achievementService.GetClaimableRewardTotals(saveData);
            if (achievementService.ClaimAllRewards(saveData) <= 0)
            {
                hudView.ShowMessage("수령할 보상이 없습니다.", string.Empty);
                return;
            }

            saveRepository.Save(saveData);
            RefreshViews();
            hudView.RefreshAchievementList();
            UpdateAchievementNotification();
            hudView.ShowAchievementClaimResult(claimedRewards);
        }

        private IEnumerator WaitForDuration(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private int GetEnhancementEffectLevel()
        {
            if (saveData == null)
            {
                return 0;
            }

            return saveData.isBlackHole
                ? 29
                : saveData.currentLevel;
        }

        private StageVisualConfig GetCurrentVisualStage()
        {
            return saveData != null && saveData.isBlackHole
                ? CreateBlackHoleVisualStage(saveData.blackHoleLevel)
                : balance.GetStage(saveData.currentLevel);
        }

        private void ApplyPlanetVisual()
        {
            if (saveData != null && saveData.isBlackHole)
            {
                planetView.SetShape(StarForgePlanetShape.Default);
                planetView.ApplyBlackHoleStage(saveData.blackHoleLevel);
                return;
            }

            planetView.SetShape((StarForgePlanetShape)saveData.planetShape);
            planetView.ApplyStage(balance.GetStage(saveData.currentLevel));
        }

        private bool IsBlackHoleCameraState()
        {
            return saveData != null && saveData.isBlackHole;
        }

        private float GetDefaultCameraOrbitPitch()
        {
            return IsBlackHoleCameraState()
                ? BlackHoleDefaultCameraPitch
                : 0f;
        }

        private void SyncCameraOrbitDefaultForCurrentState()
        {
            bool isBlackHole = IsBlackHoleCameraState();
            if (cameraOrbitStateIsBlackHole == isBlackHole)
            {
                return;
            }

            cameraOrbitStateIsBlackHole = isBlackHole;
            cameraOrbitPitch = GetDefaultCameraOrbitPitch();
            ApplyCameraOrbit(Vector3.zero);
        }

        private float GetRestCameraZForCurrentState()
        {
            return GetRestCameraZ(GetCurrentVisualStage());
        }

        private float GetRestCameraZ(int level)
        {
            return GetRestCameraZ(balance.GetStage(level));
        }

        private float GetRestCameraZ(StageVisualConfig stage)
        {
            float scale = Mathf.Max(0.4f, stage != null ? stage.scale : 1f);
            return -(6.0f + scale * 1.15f);
        }

        private static StageVisualConfig CreateBlackHoleVisualStage(
            int blackHoleLevel)
        {
            int clampedLevel = Mathf.Clamp(
                blackHoleLevel,
                StarForgeBlackHoleRules.MinLevel,
                StarForgeBlackHoleRules.MaxLevel);
            return new StageVisualConfig
            {
                level = 30 + clampedLevel,
                displayName = "블랙홀",
                color = clampedLevel >= StarForgeBlackHoleRules.MaxLevel
                    ? "#E8FAFF"
                    : clampedLevel >= 7 ? "#D8AC52" : "#7656D8",
                scale = 1.75f + clampedLevel * 0.08f,
                emission = 2.6f + clampedLevel * 0.12f,
                rotationSpeed = 22f + clampedLevel * 2f
            };
        }

        private void SetCameraZ(float z)
        {
            if (targetCamera == null)
            {
                return;
            }

            cameraDistance = Mathf.Max(0.1f, Mathf.Abs(z));
            ApplyCameraOrbit(Vector3.zero);
        }

        private void StartCameraMove(float targetZ, float duration)
        {
            if (targetCamera == null)
            {
                return;
            }

            if (cameraMoveRoutine != null)
            {
                StopCoroutine(cameraMoveRoutine);
            }

            cameraMoveRoutine = StartCoroutine(CameraMoveRoutine(targetZ, duration));
        }

        private IEnumerator CameraMoveRoutine(float targetZ, float duration)
        {
            float fromDistance = cameraDistance;
            float targetDistance = Mathf.Max(
                0.1f,
                Mathf.Abs(targetZ));
            float elapsed = 0f;
            float clampedDuration = Mathf.Max(0.05f, duration);

            while (elapsed < clampedDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / clampedDuration);
                cameraDistance = Mathf.Lerp(
                    fromDistance,
                    targetDistance,
                    t);
                ApplyCameraOrbit(Vector3.zero);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraDistance = targetDistance;
            ApplyCameraOrbit(Vector3.zero);
            cameraMoveRoutine = null;
        }

        private IEnumerator PlayCameraReaction(StarForgeResultKind resultKind)
        {
            if (targetCamera == null)
            {
                yield break;
            }

            // 줌 이동이 끝난 뒤 흔들림을 시작해 위치 충돌을 막음
            if (cameraMoveRoutine != null)
            {
                yield return cameraMoveRoutine;
            }

            float duration = resultKind == StarForgeResultKind.Destroyed ? 0.6f
                : resultKind == StarForgeResultKind.GreatSuccess ? 0.5f : 0.32f;
            float shake = resultKind == StarForgeResultKind.Destroyed ? 0.18f
                : resultKind == StarForgeResultKind.GreatSuccess ? 0.09f : 0.055f;
            float zoom = resultKind == StarForgeResultKind.GreatSuccess ? 5f : 2f;

            float originFov = targetCamera.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float pulse = Mathf.Sin(normalized * Mathf.PI);
                targetCamera.fieldOfView = originFov - pulse * zoom;
                ApplyCameraOrbit(
                    Random.insideUnitSphere * shake * pulse);

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyCameraOrbit(Vector3.zero);
            targetCamera.fieldOfView = originFov;
        }

        private void ApplyCameraOrbit(Vector3 positionOffset)
        {
            if (targetCamera == null)
            {
                return;
            }

            Quaternion orbitRotation = Quaternion.Euler(
                cameraOrbitPitch,
                cameraOrbitYaw,
                0f);
            Vector3 orbitTarget = planetView != null
                ? planetView.transform.position
                : Vector3.zero;
            targetCamera.transform.position =
                orbitTarget +
                orbitRotation * Vector3.back * cameraDistance +
                positionOffset;
            targetCamera.transform.rotation = orbitRotation;
        }

        private void HandleReviveRequested(int targetLevel)
        {
            if (isResolving)
            {
                return;
            }

            StarForgeReviveResult result = reviveService.TryRevive(
                saveData,
                balance,
                lastDestroyedLevel,
                targetLevel);

            if (result.success)
            {
                pendingDestroyedResult = null;
                saveRepository.Save(saveData);
                RefreshViews();
                StartCameraMove(GetRestCameraZ(saveData.currentLevel), 0.6f);
                audioController.PlayResult(StarForgeResultKind.Success, targetLevel);
                effectController.PlayResult(
                    StarForgeResultKind.Success,
                    planetView.Target.position,
                    1f + targetLevel * 0.045f);
                hudView.ShowMessage(
                    "부활 성공",
                    targetLevel + "강 " + balance.GetStage(targetLevel).displayName + " 단계에서 다시 시작합니다.");
            }
            else
            {
                hudView.ShowMessage("부활 실패", result.message);
                RefreshViews();
            }
        }

        private void HandleRewardedReviveRequested()
        {
            if (isResolving ||
                rewardedAdInProgress ||
                pendingDestroyedResult == null)
            {
                return;
            }

            float cooldownRemaining = GetReviveAdCooldownRemaining();
            if (cooldownRemaining > 0f)
            {
                hudView.SetReviveAdCooldown(cooldownRemaining);
                return;
            }

            if (!rewardedAdService.IsReady(DestructionKeepPlacement))
            {
                hudView.SetRewardedReviveButtonState(
                    true,
                    "광고 준비 중 · 다시 시도");
                return;
            }

            rewardedAdInProgress = true;
            hudView.SetRewardedReviveButtonState(
                false,
                "광고 불러오는 중");
            rewardedAdService.Show(
                DestructionKeepPlacement,
                completed =>
                {
                    rewardedAdInProgress = false;
                    if (!completed)
                    {
                        hudView.SetRewardedReviveButtonState(
                            true,
                            "광고 보고 현 단계 유지");
                        return;
                    }

                    // A real impression happened; start the 3-minute throttle.
                    StartReviveAdCooldown();

                    StarForgeEnhancementResult destroyedResult =
                        pendingDestroyedResult;
                    StarForgeReviveResult reviveResult =
                        reviveService.TryKeepDestroyedLevel(
                            saveData,
                            destroyedResult.previousLevel,
                            destroyedResult.rewards);
                    if (!reviveResult.success)
                    {
                        hudView.SetReviveAdCooldown(
                            GetReviveAdCooldownRemaining());
                        hudView.ShowMessage(
                            "단계 유지 실패",
                            reviveResult.message);
                        return;
                    }

                    pendingDestroyedResult = null;
                    lastDestroyedLevel = 0;
                    saveRepository.Save(saveData);
                    hudView.HideReviveOverlay();
                    RefreshViews();
                    StartCameraMove(
                        GetRestCameraZ(saveData.currentLevel),
                        0.6f);
                    audioController.PlayResult(
                        StarForgeResultKind.Success,
                        saveData.currentLevel);
                    effectController.PlayResult(
                        StarForgeResultKind.Success,
                        planetView.Target.position,
                        1f + saveData.currentLevel * 0.045f);
                    hudView.ShowMessage(
                        "단계 유지 완료",
                        reviveResult.message);
                });
        }

        // Remaining seconds before the planet-keep revive ad can be watched again.
        // Persisted as wall-clock so it survives app restarts (mirrors AdMob's limit).
        private float GetReviveAdCooldownRemaining()
        {
            string stored = PlayerPrefs.GetString(ReviveAdCooldownPrefKey, string.Empty);
            if (string.IsNullOrEmpty(stored) ||
                !long.TryParse(stored, out long untilBinary))
            {
                return 0f;
            }

            System.DateTime until;
            try
            {
                until = System.DateTime.FromBinary(untilBinary);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }

            double remaining = (until - System.DateTime.UtcNow).TotalSeconds;
            return remaining > 0d ? (float)remaining : 0f;
        }

        private void StartReviveAdCooldown()
        {
            System.DateTime until =
                System.DateTime.UtcNow.AddSeconds(ReviveAdCooldownSeconds);
            PlayerPrefs.SetString(
                ReviveAdCooldownPrefKey,
                until.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private void HandleResetConfirmed()
        {
            if (isResolving)
            {
                return;
            }

            saveData = saveRepository.Reset(balance);
            StarForgeAchievementUnlock[] achievements =
                CompleteAvailableAchievements();
            pendingDestroyedResult = null;
            rewardedAdInProgress = false;
            selectedCurrency = StarForgeCurrencyType.MeteorFragment;
            RefreshViews();
            StartCameraMove(GetRestCameraZ(saveData.currentLevel), 0.6f);
            ShowAchievementOverlays(achievements);
        }

        private void HandleSoundToggled(bool value)
        {
            saveData.soundEnabled = value;
            if (audioController != null)
            {
                audioController.SoundEnabled = value;
                audioController.SetVolumes(
                    saveData.bgmVolume,
                    saveData.sfxVolume);
            }

            if (miningGameView != null)
            {
                miningGameView.SoundEnabled = value;
            }

            saveRepository.Save(saveData);
        }

        private void HandleBgmVolumeChanged(float value)
        {
            saveData.bgmVolume = Mathf.Clamp01(value);
            if (audioController != null)
            {
                audioController.SetVolumes(
                    saveData.bgmVolume,
                    saveData.sfxVolume);
            }

            saveRepository.Save(saveData);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            saveData.sfxVolume = Mathf.Clamp01(value);
            if (audioController != null)
            {
                audioController.SetVolumes(
                    saveData.bgmVolume,
                    saveData.sfxVolume);
            }

            if (miningGameView != null)
            {
                miningGameView.SfxVolume = saveData.sfxVolume;
            }

            saveRepository.Save(saveData);
        }

        private void HandleVibrationToggled(bool value)
        {
            saveData.vibrationEnabled = value;
            saveRepository.Save(saveData);
        }

        private void HandleEnhancementAnimationSkipToggled(bool value)
        {
            saveData.enhancementAnimationSkipEnabled = value;
            saveRepository.Save(saveData);
        }

        private void HandleFractureAlertMutedToggled(bool value)
        {
            saveData.fractureAlertMuted = value;
            saveRepository.Save(saveData);
        }

        private void HandleAchievementAlertMutedToggled(bool value)
        {
            saveData.achievementAlertMuted = value;
            saveRepository.Save(saveData);
        }

        private void HandleCameraOrbitDragged(Vector2 dragDelta)
        {
            if (isResolving || targetCamera == null)
            {
                return;
            }

            cameraOrbitYaw +=
                dragDelta.x * CameraOrbitDegreesPerPixel;
            cameraOrbitPitch = Mathf.Clamp(
                cameraOrbitPitch -
                dragDelta.y * CameraOrbitDegreesPerPixel,
                -CameraOrbitPitchLimit,
                CameraOrbitPitchLimit);
            ApplyCameraOrbit(Vector3.zero);
        }

        private void RefreshViews()
        {
            StarForgeAttemptPreview preview = enhancementService.GetPreview(saveData, balance, selectedCurrency);
            ApplyPlanetVisual();
            SyncCameraOrbitDefaultForCurrentState();
            hudView.Refresh(saveData, balance, selectedCurrency, preview, isResolving);
            hudView.SetMiningAttemptsRemaining(
                GetRemainingMiningPlays(),
                GetRemainingMiningAdBonuses(),
                isResolving);
            UpdateAchievementNotification();
        }

        private void HandleDisassembleRequested()
        {
            if (isResolving)
            {
                return;
            }

            StarForgeDisassembleResult result = enhancementService.TryDisassemble(
                saveData,
                balance,
                () => Random.value);

            if (!result.success)
            {
                hudView.ShowMessage("분해 불가", result.message);
                return;
            }

            StarForgeAchievementUnlock[] achievements =
                achievementService.CompleteAvailable(saveData);
            saveRepository.Save(saveData);
            int disassembleEffectLevel = result.isBlackHole ? 29 : result.level;
            audioController.PlayResult(
                StarForgeResultKind.Destroyed,
                disassembleEffectLevel);
            effectController.PlayResult(
                StarForgeResultKind.Destroyed,
                planetView.Target.position,
                1f + disassembleEffectLevel * 0.035f);

            RefreshViews();
            StartCameraMove(GetRestCameraZForCurrentState(), 0.6f);

            hudView.ShowDisassembleResult(
                result.rewards,
                balance.GetStageName(
                    saveData.currentLevel,
                    (StarForgePlanetShape)saveData.planetShape),
                result.isBlackHole);
            ShowAchievementOverlays(achievements);
        }

        private void EnsureRuntimeObjects()
        {
            EnsureCamera();
            EnsureLighting();
            EnsurePlanet();
            InitializeCameraOrbit();
            EnsureEffects();
            EnsureAudio();
            EnsureHud();
            EnsureMiningGame();
        }

        private void EnsureMiningGame()
        {
            if (miningGameView != null)
            {
                if (!miningGameView.IsBuilt)
                {
                    miningGameView.Build();
                }

                miningGameView.SoundEnabled = saveData == null || saveData.soundEnabled;
                miningGameView.SfxVolume = saveData != null ? saveData.sfxVolume : 1f;
                return;
            }

            GameObject miningObject = new GameObject("StarForge Mining Game");
            miningGameView = miningObject.AddComponent<StarForgeMiningGameView>();
            miningGameView.Build();
            miningGameView.SoundEnabled = saveData == null || saveData.soundEnabled;
            miningGameView.SfxVolume = saveData != null ? saveData.sfxVolume : 1f;
        }

        private void EnsureAudio()
        {
            if (audioController == null)
            {
                GameObject audioObject = new GameObject("StarForge Audio");
                audioObject.transform.SetParent(transform, false);
                audioController = audioObject.AddComponent<StarForgeAudioController>();
            }

            audioController.EnsureCreated();
            audioController.SoundEnabled = saveData == null || saveData.soundEnabled;
            audioController.SetVolumes(
                saveData != null ? saveData.bgmVolume : 1f,
                saveData != null ? saveData.sfxVolume : 1f);
            audioController.SetMiningModeActive(false);
        }

        private void EnsureCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                targetCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            targetCamera.transform.position = new Vector3(0f, 0.25f, -7.5f);
            targetCamera.transform.rotation = Quaternion.identity;
            targetCamera.fieldOfView = 42f;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = new Color(0.015f, 0.018f, 0.035f);
            targetCamera.allowHDR = true;

            // 씬에 AudioListener가 하나도 없으면 메인 카메라에 추가한다 (없으면 경고 발생).
            if (FindObjectsByType<AudioListener>().Length == 0)
            {
                targetCamera.gameObject.AddComponent<AudioListener>();
            }

            UniversalAdditionalCameraData cameraData =
                targetCamera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
        }

        private void EnsureLighting()
        {
            Light keyLight = null;
            Light[] sceneLights = FindObjectsByType<Light>();
            for (int i = 0; i < sceneLights.Length; i++)
            {
                if (sceneLights[i].type == LightType.Directional)
                {
                    keyLight = sceneLights[i];
                    break;
                }
            }

            if (keyLight == null)
            {
                GameObject lightObject = new GameObject("Key Light");
                lightObject.transform.SetParent(transform, false);
                keyLight = lightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
            }

            keyLight.intensity = 1.35f;
            keyLight.color = Color.white;
            keyLight.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            // The collection preview (layer 30) and mining field (layer 29) render on
            // their own cameras with their own dedicated key lights. This directional
            // light defaults to lighting Everything, so without excluding those layers
            // their planets/ship get double directional lighting and look brighter than
            // the main scene. Keep this light off the isolated layers.
            keyLight.cullingMask = ~((1 << 29) | (1 << 30));
        }

        private void EnsurePlanet()
        {
            if (planetView == null)
            {
                GameObject planetObject = new GameObject("StarForge Planet View");
                planetObject.transform.SetParent(transform, false);
                planetObject.transform.position = new Vector3(0f, 0.45f, 0f);
                planetView = planetObject.AddComponent<StarForgePlanetView>();
            }

            Vector3 screenPosition = targetCamera.WorldToScreenPoint(planetView.transform.position);
            screenPosition.y += PlanetVerticalOffsetPixels;
            planetView.transform.position = targetCamera.ScreenToWorldPoint(screenPosition);
        }

        private void InitializeCameraOrbit()
        {
            cameraOrbitYaw = 0f;
            cameraOrbitPitch = GetDefaultCameraOrbitPitch();
            cameraOrbitStateIsBlackHole = IsBlackHoleCameraState();
            cameraDistance = targetCamera != null && planetView != null
                ? Mathf.Max(
                    0.1f,
                    Vector3.Distance(
                        targetCamera.transform.position,
                        planetView.transform.position))
                : 7.5f;
            ApplyCameraOrbit(Vector3.zero);
        }

        private void EnsureEffects()
        {
            if (effectController != null)
            {
                effectController.EnsureCreated();
                return;
            }

            GameObject effectsObject = new GameObject("StarForge Effects");
            effectsObject.transform.SetParent(transform, false);
            effectController = effectsObject.AddComponent<StarForgeEffectController>();
            effectController.EnsureCreated();
        }

        private void EnsureHud()
        {
            if (hudView != null)
            {
                if (!hudView.IsBuilt)
                {
                    hudView.Build(balance);
                }

                return;
            }

            GameObject hudObject = new GameObject("StarForge HUD");
            hudView = hudObject.AddComponent<StarForgeHudView>();
            hudView.Build(balance);
        }
    }
}
