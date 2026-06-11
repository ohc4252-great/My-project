using System.Collections;
using StarForge.Core;
using StarForge.Data;
using StarForge.Save;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgeGameController : MonoBehaviour
    {
        private const float PlanetVerticalOffsetPixels = 50f;
#if UNITY_EDITOR
        // Temporary test data: set false to stop replacing all material balances with 999 on Play.
        private static readonly bool EnableEditorTestCurrencies = true;
        private const int EditorTestCurrencyAmount = 999;
#endif

        [Header("Optional Overrides")]
        [SerializeField] private TextAsset balanceJson;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private StarForgePlanetView planetView;
        [SerializeField] private StarForgeEffectController effectController;
        [SerializeField] private StarForgeHudView hudView;
        [SerializeField] private StarForgeAudioController audioController;

        private readonly StarForgeEnhancementService enhancementService = new StarForgeEnhancementService();
        private readonly StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();
        private readonly StarForgeReviveService reviveService = new StarForgeReviveService();
        private readonly StarForgeSaveRepository saveRepository = new StarForgeSaveRepository();

        private StarForgeBalance balance;
        private StarForgeSaveData saveData;
        private StarForgeCurrencyType selectedCurrency;
        private bool isResolving;
        private int lastDestroyedLevel;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            balance = StarForgeBalanceLoader.Load(balanceJson);
            saveData = saveRepository.Load(balance);
#if UNITY_EDITOR
            ApplyEditorTestCurrencies();
#endif
            selectedCurrency = (StarForgeCurrencyType)Mathf.Clamp(saveData.selectedCurrency, 0, 4);

            EnsureRuntimeObjects();
            BindHud();
        }

        private void Start()
        {
            RefreshViews();
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
            hudView.VibrationToggled += HandleVibrationToggled;
            hudView.ReviveRequested += HandleReviveRequested;
        }

#if UNITY_EDITOR
        private void ApplyEditorTestCurrencies()
        {
            if (!EnableEditorTestCurrencies)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                saveData.SetCurrency((StarForgeCurrencyType)i, EditorTestCurrencyAmount);
            }

            saveRepository.Save(saveData);
        }
#endif

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
            if (isResolving)
            {
                return;
            }

            StarForgeAttemptPreview preview = enhancementService.GetPreview(saveData, balance, selectedCurrency);
            if (!preview.isAvailable || preview.isMaxLevel || !preview.hasEnoughCurrency)
            {
                StarForgeEnhancementResult result = enhancementService.TryEnhance(
                    saveData,
                    balance,
                    selectedCurrency,
                    () => Random.value);

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

            int attemptLevel = saveData.currentLevel;
            float fallbackChargeDuration = Mathf.Min(0.45f + attemptLevel * 0.03f, 1.3f);
            float chargeDuration = audioController.GetChargeDuration(attemptLevel, fallbackChargeDuration);
            int shardCount = Mathf.Clamp(10 + attemptLevel + Mathf.RoundToInt(chargeDuration * 6f), 12, 64);
            float effectIntensity = 1f + attemptLevel * 0.045f;

            Transform planetTarget = planetView.Target;
            audioController.PlayCharge(attemptLevel);
            planetView.PlayChargePulse(chargeDuration);
            yield return effectController.PlayShardFly(planetTarget, shardCount, chargeDuration, attemptLevel);

            StarForgeEnhancementResult result = enhancementService.TryEnhance(
                saveData,
                balance,
                selectedCurrency,
                () => Random.value);

            saveRepository.Save(saveData);
            planetView.ApplyStage(balance.GetStage(saveData.currentLevel));
            audioController.StopCharge();
            audioController.PlayResult(result.kind, attemptLevel);
            effectController.PlayResult(result.kind, planetTarget.position, effectIntensity);

            if (saveData.vibrationEnabled &&
                (result.kind == StarForgeResultKind.Destroyed ||
                 result.kind == StarForgeResultKind.GreatSuccess ||
                 result.kind == StarForgeResultKind.Fracture))
            {
                Handheld.Vibrate();
            }

            yield return PlayCameraReaction(result.kind);

            if (result.kind == StarForgeResultKind.Destroyed &&
                reviveService.HasOptions(balance, result.previousLevel))
            {
                lastDestroyedLevel = result.previousLevel;
                hudView.ShowReviveOverlay(result, saveData);
            }
            else if (result.kind != StarForgeResultKind.Success &&
                     result.kind != StarForgeResultKind.GreatSuccess)
            {
                hudView.ShowResult(result);
            }

            isResolving = false;
            RefreshViews();
        }

        private IEnumerator PlayCameraReaction(StarForgeResultKind resultKind)
        {
            if (targetCamera == null)
            {
                yield break;
            }

            float duration = resultKind == StarForgeResultKind.Destroyed ? 0.6f
                : resultKind == StarForgeResultKind.GreatSuccess ? 0.5f : 0.32f;
            float shake = resultKind == StarForgeResultKind.Destroyed ? 0.18f
                : resultKind == StarForgeResultKind.GreatSuccess ? 0.09f : 0.055f;
            float zoom = resultKind == StarForgeResultKind.GreatSuccess ? 5f : 2f;

            Vector3 originPosition = targetCamera.transform.position;
            float originFov = targetCamera.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float pulse = Mathf.Sin(normalized * Mathf.PI);
                targetCamera.fieldOfView = originFov - pulse * zoom;
                targetCamera.transform.position = originPosition + Random.insideUnitSphere * shake * pulse;

                elapsed += Time.deltaTime;
                yield return null;
            }

            targetCamera.transform.position = originPosition;
            targetCamera.fieldOfView = originFov;
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
                saveRepository.Save(saveData);
                RefreshViews();
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

        private void HandleResetConfirmed()
        {
            if (isResolving)
            {
                return;
            }

            saveData = saveRepository.Reset(balance);
            selectedCurrency = StarForgeCurrencyType.MeteorFragment;
            planetView.ApplyStage(balance.GetStage(saveData.currentLevel));
            RefreshViews();
        }

        private void HandleSoundToggled(bool value)
        {
            saveData.soundEnabled = value;
            if (audioController != null)
            {
                audioController.SoundEnabled = value;
            }

            saveRepository.Save(saveData);
        }

        private void HandleVibrationToggled(bool value)
        {
            saveData.vibrationEnabled = value;
            saveRepository.Save(saveData);
        }

        private void RefreshViews()
        {
            StarForgeAttemptPreview preview = enhancementService.GetPreview(saveData, balance, selectedCurrency);
            planetView.ApplyStage(balance.GetStage(saveData.currentLevel));
            hudView.Refresh(saveData, balance, selectedCurrency, preview, isResolving);
        }

        private void EnsureRuntimeObjects()
        {
            EnsureCamera();
            EnsureLighting();
            EnsurePlanet();
            EnsureEffects();
            EnsureAudio();
            EnsureHud();
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
        }

        private void EnsureLighting()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Key Light");
            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(36f, -28f, 0f);
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
