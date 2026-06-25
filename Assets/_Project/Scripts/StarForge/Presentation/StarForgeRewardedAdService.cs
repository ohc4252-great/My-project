using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace StarForge.Presentation
{
    public interface IStarForgeRewardedAdService
    {
        bool IsReady(string placementId);

        void Show(string placementId, Action<bool> onCompleted);

        void ShowPrivacyOptions(Action<bool> onCompleted);
    }

    public sealed class StarForgeRewardedAdPlaceholderService :
        IStarForgeRewardedAdService
    {
        public bool IsReady(string placementId)
        {
            return Application.isEditor;
        }

        public void Show(string placementId, Action<bool> onCompleted)
        {
            bool completed = Application.isEditor;
            Debug.Log(
                "Rewarded ad placeholder " +
                (completed ? "completed" : "blocked") +
                ". Placement: " +
                placementId);
            onCompleted?.Invoke(completed);
        }

        public void ShowPrivacyOptions(Action<bool> onCompleted)
        {
            onCompleted?.Invoke(false);
        }
    }

    public sealed class StarForgeAdMobRewardedAdService :
        MonoBehaviour,
        IStarForgeRewardedAdService
    {
        private const string MiningBonusPlacement =
            "mining_bonus_attempt";
        private const string DestructionKeepPlacement =
            "destruction_keep_level";
        private const string AndroidMiningBonusRewardedAdUnitId =
            "ca-app-pub-3971219491693844/1293240258";
        private const string AndroidDestructionKeepRewardedAdUnitId =
            "ca-app-pub-3971219491693844/5277054006";
        private const string IosDestructionKeepRewardedAdUnitId =
            "ca-app-pub-3971219491693844/7495686527";
        private const string IosMiningBonusRewardedAdUnitId =
            "ca-app-pub-3971219491693844/4869523183";
        private const string AndroidTestRewardedAdUnitId =
            "ca-app-pub-3940256099942544/5224354917";
        private const string IosTestRewardedAdUnitId =
            "ca-app-pub-3940256099942544/1712485313";

        [Header("AdMob Rewarded Ad Unit IDs")]
#pragma warning disable CS0414
        [SerializeField] private string androidMiningBonusRewardedAdUnitId =
            AndroidMiningBonusRewardedAdUnitId;
        [SerializeField] private string iosMiningBonusRewardedAdUnitId =
            IosMiningBonusRewardedAdUnitId;
        [SerializeField] private string androidDestructionKeepRewardedAdUnitId =
            AndroidDestructionKeepRewardedAdUnitId;
        [SerializeField] private string iosDestructionKeepRewardedAdUnitId =
            IosDestructionKeepRewardedAdUnitId;
        [SerializeField] private bool useTestAds;
#pragma warning restore CS0414

        private RewardedAd rewardedAd;
        private bool initialized;
        private bool sdkInitialized;
        private bool consentRequestStarted;
        private bool consentRequestCompleted;
        private bool canRequestAds;
        private bool loadInProgress;
        private bool rewardEarned;
        private string loadedPlacementId;
        private Action<bool> pendingCompletion;

        public static bool IsSdkAvailable()
        {
            return true;
        }

        public void Configure(string androidAdUnitId, string iosAdUnitId)
        {
            Configure(androidAdUnitId, iosAdUnitId, null, null, false);
        }

        public void Configure(
            string androidMiningBonusAdUnitId,
            string iosMiningBonusAdUnitId,
            string androidDestructionKeepAdUnitId,
            string iosDestructionKeepAdUnitId,
            bool useTestAdUnits)
        {
            if (!string.IsNullOrWhiteSpace(androidMiningBonusAdUnitId))
            {
                androidMiningBonusRewardedAdUnitId =
                    androidMiningBonusAdUnitId;
            }

            if (!string.IsNullOrWhiteSpace(iosMiningBonusAdUnitId))
            {
                iosMiningBonusRewardedAdUnitId = iosMiningBonusAdUnitId;
            }

            if (!string.IsNullOrWhiteSpace(androidDestructionKeepAdUnitId))
            {
                androidDestructionKeepRewardedAdUnitId =
                    androidDestructionKeepAdUnitId;
            }

            if (!string.IsNullOrWhiteSpace(iosDestructionKeepAdUnitId))
            {
                iosDestructionKeepRewardedAdUnitId =
                    iosDestructionKeepAdUnitId;
            }

            useTestAds = useTestAdUnits;
        }

        private void Start()
        {
            GatherConsent();
        }

        private void OnDestroy()
        {
            DestroyLoadedAd();
            CompletePending(false);
        }

        public bool IsReady(string placementId)
        {
            InitializeSdk();
            if (!canRequestAds || !sdkInitialized)
            {
                return false;
            }

            string normalizedPlacementId = NormalizePlacementId(placementId);
            if (rewardedAd != null &&
                loadedPlacementId != normalizedPlacementId)
            {
                DestroyLoadedAd();
            }

            if (rewardedAd == null && !loadInProgress)
            {
                LoadAd(normalizedPlacementId);
            }

            return rewardedAd != null &&
                   loadedPlacementId == normalizedPlacementId &&
                   rewardedAd.CanShowAd();
        }

        public void Show(string placementId, Action<bool> onCompleted)
        {
            InitializeSdk();
            if (!canRequestAds || !sdkInitialized)
            {
                onCompleted?.Invoke(false);
                return;
            }

            string normalizedPlacementId = NormalizePlacementId(placementId);
            if (rewardedAd == null ||
                loadedPlacementId != normalizedPlacementId ||
                !rewardedAd.CanShowAd())
            {
                LoadAd(normalizedPlacementId);
                onCompleted?.Invoke(false);
                return;
            }

            RewardedAd adToShow = rewardedAd;
            rewardedAd = null;
            loadedPlacementId = null;
            rewardEarned = false;
            pendingCompletion = onCompleted;

            try
            {
                adToShow.Show(reward =>
                {
                    rewardEarned = true;
                    Debug.Log(
                        "AdMob rewarded ad earned reward: " +
                        reward.Amount +
                        " " +
                        reward.Type);
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("AdMob rewarded ad show failed: " + ex);
                CompletePending(false);
                DestroyAd(adToShow);
                LoadAd(normalizedPlacementId);
            }
        }

        public void ShowPrivacyOptions(Action<bool> onCompleted)
        {
            if (!consentRequestCompleted ||
                ConsentInformation.PrivacyOptionsRequirementStatus !=
                    PrivacyOptionsRequirementStatus.Required)
            {
                onCompleted?.Invoke(false);
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                RunOnMainThread(() =>
                {
                    if (error != null)
                    {
                        Debug.LogWarning(
                            "AdMob privacy options form failed: " +
                            error.Message);
                        onCompleted?.Invoke(false);
                        return;
                    }

                    canRequestAds = ConsentInformation.CanRequestAds();
                    if (canRequestAds)
                    {
                        ReloadAdsAfterPrivacyChange();
                    }
                    else
                    {
                        DestroyLoadedAd();
                    }

                    onCompleted?.Invoke(true);
                });
            });
        }

        private void GatherConsent()
        {
            if (consentRequestStarted)
            {
                return;
            }

            consentRequestStarted = true;
            ConsentRequestParameters requestParameters =
                new ConsentRequestParameters
                {
                    TagForUnderAgeOfConsent = false
                };
            ConsentInformation.Update(requestParameters, updateError =>
            {
                RunOnMainThread(() =>
                {
                    if (updateError != null)
                    {
                        CompleteConsentRequest(updateError);
                        return;
                    }

                    if (ConsentInformation.CanRequestAds())
                    {
                        CompleteConsentRequest(null);
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                    {
                        RunOnMainThread(
                            () => CompleteConsentRequest(showError));
                    });
                });
            });
        }

        private void CompleteConsentRequest(FormError error)
        {
            consentRequestCompleted = true;
            canRequestAds = ConsentInformation.CanRequestAds();
            if (error != null)
            {
                Debug.LogWarning(
                    "AdMob consent update failed: " +
                    error.Message);
            }

            // The UMP consent form can be unavailable (no form configured for
            // this app, or the publisher account config could not be read). In
            // that case CanRequestAds() stays false and ads would never load.
            // UMP only restricts personalized ads where consent is legally
            // required; elsewhere we may still serve ads, so fall back to
            // initializing rather than letting a messaging misconfiguration
            // block ad serving entirely.
            if (!canRequestAds)
            {
                Debug.LogWarning(
                    "AdMob consent unavailable; initializing ads without the " +
                    "consent gate (publisher messaging not configured).");
                canRequestAds = true;
            }

            if (canRequestAds)
            {
                InitializeSdk();
            }
        }

        private void InitializeSdk()
        {
            if (initialized || !consentRequestCompleted || !canRequestAds)
            {
                return;
            }

            initialized = true;
            ConfigureAdRequests();
            Debug.Log("AdMob initializing rewarded ads.");
            MobileAds.Initialize(status =>
            {
                RunOnMainThread(() =>
                {
                    if (status == null)
                    {
                        Debug.LogWarning("AdMob SDK initialization failed.");
                        initialized = false;
                        return;
                    }

                    sdkInitialized = true;
                    Debug.Log("AdMob SDK initialized.");
                    LoadAd(MiningBonusPlacement);
                });
            });
        }

        private void ConfigureAdRequests()
        {
            RequestConfiguration requestConfiguration =
                new RequestConfiguration
                {
                    MaxAdContentRating = MaxAdContentRating.T,
                    TagForChildDirectedTreatment =
                        TagForChildDirectedTreatment.False
                };

            if (useTestAds)
            {
                requestConfiguration.TestDeviceIds = new List<string>
                {
                    AdRequest.TestDeviceSimulator
                };
            }

            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        private void LoadAd(string placementId)
        {
            if (!sdkInitialized || loadInProgress)
            {
                return;
            }

            string normalizedPlacementId = NormalizePlacementId(placementId);
            string adUnitId = GetRewardedAdUnitId(normalizedPlacementId);
            if (string.IsNullOrEmpty(adUnitId))
            {
                Debug.LogWarning(
                    "AdMob rewarded ad unit id is empty. Placement: " +
                    normalizedPlacementId);
                return;
            }

            loadInProgress = true;
            Debug.Log(
                "AdMob loading rewarded ad. Placement: " +
                normalizedPlacementId +
                ", testAds: " +
                useTestAds);
            RewardedAd.Load(
                adUnitId,
                new AdRequest(),
                (ad, error) =>
                {
                    RunOnMainThread(
                        () => HandleLoadCompleted(
                            normalizedPlacementId,
                            ad,
                            error));
                });
        }

        private void HandleLoadCompleted(
            string placementId,
            RewardedAd ad,
            LoadAdError error)
        {
            loadInProgress = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning(
                    "AdMob rewarded ad failed to load. Placement: " +
                    placementId +
                    ", error: " +
                    error);
                return;
            }

            DestroyLoadedAd();
            rewardedAd = ad;
            loadedPlacementId = placementId;
            RegisterFullScreenCallbacks(ad, placementId);
            Debug.Log(
                "AdMob rewarded ad loaded. Placement: " +
                placementId);
        }

        private void RegisterFullScreenCallbacks(
            RewardedAd ad,
            string placementId)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                RunOnMainThread(() =>
                {
                    bool completed = rewardEarned;
                    CompletePending(completed);
                    DestroyAd(ad);
                    LoadAd(placementId);
                });
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                RunOnMainThread(() =>
                {
                    Debug.LogWarning(
                        "AdMob rewarded ad failed to show. Placement: " +
                        placementId +
                        ", error: " +
                        error);
                    CompletePending(false);
                    DestroyAd(ad);
                    LoadAd(placementId);
                });
            };
        }

        private void ReloadAdsAfterPrivacyChange()
        {
            DestroyLoadedAd();
            InitializeSdk();
            LoadAd(MiningBonusPlacement);
        }

        private void DestroyLoadedAd()
        {
            if (rewardedAd == null)
            {
                return;
            }

            DestroyAd(rewardedAd);
            rewardedAd = null;
            loadedPlacementId = null;
        }

        private void DestroyAd(RewardedAd ad)
        {
            if (ad == null)
            {
                return;
            }

            ad.Destroy();
            if (ReferenceEquals(rewardedAd, ad))
            {
                rewardedAd = null;
                loadedPlacementId = null;
            }
        }

        private void CompletePending(bool completed)
        {
            Action<bool> completion = pendingCompletion;
            pendingCompletion = null;
            completion?.Invoke(completed);
        }

        private string GetRewardedAdUnitId(string placementId)
        {
            if (useTestAds)
            {
#if UNITY_IOS
                return IosTestRewardedAdUnitId;
#else
                return AndroidTestRewardedAdUnitId;
#endif
            }

#if UNITY_IOS
            if (placementId == MiningBonusPlacement)
            {
                return string.IsNullOrWhiteSpace(iosMiningBonusRewardedAdUnitId)
                    ? IosMiningBonusRewardedAdUnitId
                    : iosMiningBonusRewardedAdUnitId;
            }

            if (placementId == DestructionKeepPlacement)
            {
                return string.IsNullOrWhiteSpace(
                        iosDestructionKeepRewardedAdUnitId)
                    ? IosDestructionKeepRewardedAdUnitId
                    : iosDestructionKeepRewardedAdUnitId;
            }

            return string.Empty;
#else
            if (placementId == MiningBonusPlacement)
            {
                return string.IsNullOrWhiteSpace(
                        androidMiningBonusRewardedAdUnitId)
                    ? AndroidMiningBonusRewardedAdUnitId
                    : androidMiningBonusRewardedAdUnitId;
            }

            if (placementId == DestructionKeepPlacement)
            {
                return string.IsNullOrWhiteSpace(
                        androidDestructionKeepRewardedAdUnitId)
                    ? AndroidDestructionKeepRewardedAdUnitId
                    : androidDestructionKeepRewardedAdUnitId;
            }

            return string.Empty;
#endif
        }

        private static string NormalizePlacementId(string placementId)
        {
            return string.IsNullOrEmpty(placementId)
                ? MiningBonusPlacement
                : placementId;
        }

        private static void RunOnMainThread(Action action)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }
}
