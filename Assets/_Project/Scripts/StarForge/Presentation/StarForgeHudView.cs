using System;
using System.Collections;
using System.Collections.Generic;
using StarForge.Core;
using StarForge.Data;
using StarForge.Save;
using StarForge.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarForge.Presentation
{
    public sealed class StarForgeHudView : MonoBehaviour
    {
        private const int ChanceTextDefaultFontSize = 28;
        private const int BlackHoleChanceTextFontSize = 26;

        private readonly StarForgeEnhancementService previewService = new StarForgeEnhancementService();
        private readonly StarForgeAchievementService achievementService =
            new StarForgeAchievementService();
        private readonly StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();
        public event Action EnhanceClicked;
        public event Action ResetConfirmed;
        public event Action<StarForgeCurrencyType> CurrencySelected;
        public event Action<int, int> MaterialExchangeRequested;
        public event Action<bool> SoundToggled;
        public event Action<float> BgmVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<bool> VibrationToggled;
        public event Action<bool> EnhancementAnimationSkipToggled;
        public event Action<bool> FractureAlertMutedToggled;
        public event Action<bool> AchievementAlertMutedToggled;
        public event Action<Vector2> CameraOrbitDragged;
        public event Action<int> ReviveRequested;
        public event Action RewardedReviveRequested;
        // Player dismissed the revive overlay to start over at 0강 (a new star life).
        public event Action ReviveDismissed;
        public event Action DisassembleRequested;
        public event Action MiningRequested;
        public event Action<string> AchievementClaimRequested;
        public event Action AchievementClaimAllRequested;

        private readonly Button[] currencyButtons = new Button[5];
        private readonly Text[] currencyButtonTexts = new Text[5];
        private readonly Text[] currencyQuantityTexts = new Text[5];
        private readonly Image[] currencyIconImages = new Image[5];
        private readonly Button[] exchangeButtons = new Button[8];
        private readonly Text[] exchangeSourceOwnedTexts = new Text[8];
        private readonly Text[] exchangeRouteStatusTexts = new Text[8];
        private readonly Image[] exchangeSourceIcons = new Image[8];
        private readonly Image[] exchangeTargetIcons = new Image[8];
        private Font font;
        private Sprite[] materialIconSprites;
        private Sprite fallbackMaterialIcon;
        private Text levelText;
        private Outline levelTextOutline;
        private Text highestText;
        private Text selectedMaterialText;
        private Image selectedMaterialIconImage;
        private Text chanceText;
        private Text riskText;
        private Text statusText;
        private Button enhanceButton;
        private bool lastCanEnhance;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultBodyText;
        private GameObject resultRewardRow;
        private readonly Image[] resultRewardIcons = new Image[5];
        private readonly Text[] resultRewardTexts = new Text[5];
        private readonly Queue<StarForgeAchievementUnlock>
            pendingAchievementOverlays =
                new Queue<StarForgeAchievementUnlock>();
        private GameObject settingsPanel;
        private GameObject resetConfirmPanel;
        private Button disassembleButton;
        private GameObject disassembleConfirmPanel;
        private Text disassembleBodyText;
        private string pendingDisassembleSummary = string.Empty;
        private GameObject exchangePanel;
        private Text exchangeStatusText;
        private Button exchangeOpenButton;
        private ScrollRect exchangeScrollRect;
        private GameObject exchangeQuantityPanel;
        private Text exchangeQuantityRouteText;
        private Text exchangeQuantityPreviewText;
        private Text exchangeQuantityStatusText;
        private InputField exchangeQuantityInput;
        private Button exchangeQuantityConfirmButton;
        private GameObject exchangeNumpadPanel;
        private StarForgeSaveData lastSaveData;
        private int pendingExchangeRouteIndex = -1;
        private Button collectionOpenButton;
        private Button miningOpenButton;
        private Text miningOpenButtonText;
        private GameObject collectionPanel;
        private Text collectionTitleText;
        private RawImage collectionPlanetImage;
        private Button collectionPreviousButton;
        private Button collectionNextButton;
        private Toggle collectionSkipToggle;
        private Text collectionSkipToggleText;
        private Toggle enhancementAnimationSkipToggle;
        private readonly Button[] collectionShapeButtons = new Button[3];
        private readonly Text[] collectionShapeButtonTexts = new Text[3];
        private readonly GameObject[] collectionShapeLockIcons = new GameObject[3];
        private readonly int[] collectionShapeMaxLevels = { 0, -1, -1 };
        private readonly bool[] collectionShapeDiscovered = { true, false, false };
        private StarForgeCollectionPreview collectionPreview;
        private int collectionCurrentLevel;
        private int collectionMaxUnlockedLevel;
        private StarForgePlanetShape collectionShape = StarForgePlanetShape.Default;
        private bool collectionSkipEnabled;
        private bool collectionTransitioning;
        private GameObject collectionDetailRoot;
        private GameObject collectionBrowseRoot;
        private GameObject collectionPlanetTabRoot;
        private GameObject collectionAchievementTabRoot;
        private RectTransform collectionPlanetGridContent;
        private RectTransform collectionAchievementContent;
        private Button collectionPlanetTabButton;
        private Button collectionAchievementTabButton;
        private readonly Button[] collectionBrowseShapeButtons = new Button[3];
        private Text collectionAchievementProgressText;
        private Button achievementClaimAllButton;
        private GameObject collectionClaimBadge;
        private GameObject collectionAchievementClaimBadge;
        private GameObject achievementToast;
        private CanvasGroup achievementToastGroup;
        private Image achievementToastIcon;
        private Text achievementToastNameText;
        private Text achievementToastFlavorText;
        private readonly Queue<(string name, string flavor)> achievementToastQueue =
            new Queue<(string name, string flavor)>();
        private Coroutine achievementToastRoutine;
        private Sprite collectionPlanetSprite;
        private StarForgePlanetShape collectionBrowseShape = StarForgePlanetShape.Default;
        private Toggle soundToggle;
        private Slider bgmVolumeSlider;
        private Text bgmVolumeValueText;
        private Slider sfxVolumeSlider;
        private Text sfxVolumeValueText;
        private Toggle vibrationToggle;
        private Toggle fractureAlertToggle;
        private Toggle achievementAlertToggle;
        private StarForgeBalance balanceRef;
        private CanvasGroup mainHudCanvasGroup;
        private Coroutine mainHudFadeRoutine;
        private bool mainHudVisible = true;
        private GameObject revivePanel;
        private Text reviveBodyText;
        private ReviveRow[] reviveRows;
        private int reviveDestroyedLevel;
        private Button rewardedReviveButton;
        private Text rewardedReviveButtonText;
        private bool isBuilt;
        private Sprite chamferedUiSprite;
        private RawImage spaceBackdropImage;
        private Texture2D spaceBackdropTexture;
        private Texture2D expandedSpaceBackdropTexture;

        public bool IsBuilt
        {
            get { return isBuilt; }
        }

        public void Build(StarForgeBalance balance)
        {
            if (isBuilt)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            materialIconSprites = LoadMaterialIconSprites();
            balanceRef = balance;
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.65f;

            gameObject.AddComponent<GraphicRaycaster>();

            Image background = gameObject.AddComponent<Image>();
            background.color = Color.clear;

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            CreateSpaceBackdrop(root);
            RectTransform safeAreaRoot = CreateSafeAreaRoot(root);
            RectTransform mainHudRoot = CreateMainHudRoot(safeAreaRoot);

            BuildPlanetDragSurface(mainHudRoot);
            BuildTopPanel(mainHudRoot);
            BuildSettingsButton(mainHudRoot);
            BuildMaterialPanel(mainHudRoot, balance);
            BuildResultPanel(safeAreaRoot);
            BuildSettingsPanel(safeAreaRoot);
            BuildResetConfirmPanel(safeAreaRoot);
            BuildDisassembleConfirmPanel(safeAreaRoot);
            BuildExchangePanel(safeAreaRoot);
            BuildCollectionPanel(safeAreaRoot, balance);
            BuildRevivePanel(safeAreaRoot, balance);
            BuildCollectionClaimBadge(mainHudRoot);
            BuildAchievementToast(safeAreaRoot);
            isBuilt = true;
        }

        public void Refresh(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType selectedCurrency,
            StarForgeAttemptPreview preview,
            bool isBusy)
        {
            lastSaveData = saveData;
            CurrencyConfig selectedConfig = balance.GetCurrency(selectedCurrency);
            StarForgePlanetShape shape = (StarForgePlanetShape)saveData.planetShape;
            bool isBlackHole = saveData.isBlackHole;
            string stageName = isBlackHole
                ? "블랙홀"
                : balance.GetStageName(saveData.currentLevel, shape);
            int displayLevel = isBlackHole
                ? saveData.blackHoleLevel
                : saveData.currentLevel;

            levelText.text = displayLevel + "강  " + stageName;
            levelText.color = isBlackHole
                ? new Color(0.9f, 0.82f, 1f, 1f)
                : GetLevelTextColor(saveData.currentLevel);
            if (levelTextOutline != null)
            {
                levelTextOutline.effectColor = isBlackHole
                    ? new Color(0.3f, 0.05f, 0.65f, 0.95f)
                    : GetLevelOutlineColor(saveData.currentLevel);
            }

            highestText.text = isBlackHole
                ? "블랙홀 기록 " + saveData.highestBlackHoleLevel + "강"
                : "획득 기록 " + saveData.highestLevel + "강";
            selectedMaterialText.text = isBlackHole
                ? "재료 없음"
                : selectedConfig.displayName;
            if (selectedMaterialIconImage != null)
            {
                selectedMaterialIconImage.sprite = GetMaterialIcon((int)selectedCurrency);
                selectedMaterialIconImage.color = isBlackHole
                    ? new Color(0.72f, 0.62f, 1f, 0.45f)
                    : preview.isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            chanceText.fontSize = isBlackHole
                ? BlackHoleChanceTextFontSize
                : ChanceTextDefaultFontSize;

            if (isBlackHole && preview.isMaxLevel)
            {
                chanceText.text =
                    "<color=#B78CFF>강화 확률 :</color> <color=#FFD56A>MAX</color>";
                riskText.text =
                    "<color=#C7D5EA>최대 등급 블랙홀입니다.</color>";
            }
            else if (isBlackHole)
            {
                chanceText.text =
                    "<color=#B78CFF>강화 확률 :</color> <color=#FFD56A>???</color>";
                riskText.text =
                    "<color=#C7D5EA>실패 시</color>   <color=#FF6672>반드시 소멸</color>";
            }
            else if (preview.isMaxLevel)
            {
                chanceText.text =
                    "<color=#5EBBFF>성공 확률</color>   <color=#FFD56A>MAX</color>";
                riskText.text =
                    "<color=#C7D5EA>더 이상 강화할 수 없습니다.</color>";
            }
            else if (!preview.isAvailable)
            {
                chanceText.text =
                    "<color=#5EBBFF>성공 확률</color>   <color=#FFD56A>-</color>";
                riskText.text =
                    "<color=#C7D5EA>다른 재료를 선택하세요.</color>";
            }
            else
            {
                chanceText.text =
                    "<color=#5EBBFF>성공 확률</color>   <color=#FFD56A>" +
                    StarForgeFormat.Percent(preview.successRatePercent) +
                    "</color>";
                riskText.text =
                    "<color=#C7D5EA>실패 시</color>   <color=#FFBD3E>" +
                    StarForgeFormat.Percent(preview.destructionChancePercent) +
                    "</color> <color=#C7D5EA>확률로 소멸</color>";
            }

            statusText.text = isBlackHole
                ? "<color=#C7D5EA>상태</color>   <color=#B78CFF>블랙홀</color>"
                : saveData.fractureCount > 0
                ? "<color=#C7D5EA>상태</color>   <color=#FF6672>균열 " +
                  saveData.fractureCount +
                  "회</color>"
                : "<color=#C7D5EA>상태</color>   <color=#65D783>안정</color>";
            statusText.color = Color.white;

            bool canEnhance = !isBusy && preview.isAvailable && preview.hasEnoughCurrency && !preview.isMaxLevel;
            lastCanEnhance = canEnhance;
            UpdateEnhanceButtonInteractable();
            SetSpaceBackdropExpanded(isBusy);
            SetMainHudVisible(!isBusy);

            if (disassembleButton != null)
            {
                CurrencyAmount[] disassembleReward =
                    previewService.GetDisassembleRewards(saveData, balance);
                bool canDisassemble = !isBusy &&
                    (isBlackHole || saveData.currentLevel > 0) &&
                    disassembleReward != null &&
                    disassembleReward.Length > 0;
                disassembleButton.interactable = canDisassemble;
                pendingDisassembleSummary = BuildDisassembleSummary(
                    displayLevel,
                    stageName,
                    disassembleReward);
            }

            if (miningOpenButton != null)
            {
                miningOpenButton.interactable = !isBusy;
            }

            for (int i = 0; i < currencyButtons.Length; i++)
            {
                StarForgeCurrencyType currencyType = (StarForgeCurrencyType)i;
                CurrencyConfig config = balance.GetCurrency(currencyType);
                Button button = currencyButtons[i];
                Text text = currencyButtonTexts[i];
                Text quantityText = currencyQuantityTexts[i];
                Image icon = currencyIconImages[i];

                if (button == null || text == null || quantityText == null || icon == null)
                {
                    continue;
                }

                StarForgeAttemptPreview currencyPreview = previewService.GetPreview(saveData, balance, currencyType);
                bool isSelected = selectedCurrency == currencyType;
                button.interactable = !isBusy && !isBlackHole;
                Color slotColor = isSelected
                    ? Color.white
                    : new Color(0.78f, 0.88f, 1f, 0.9f);
                ColorBlock slotColors = button.colors;
                slotColors.normalColor = slotColor;
                slotColors.highlightedColor = Color.white;
                slotColors.pressedColor = new Color(0.62f, 0.76f, 0.96f, 0.95f);
                slotColors.selectedColor = Color.white;
                button.colors = slotColors;
                button.image.color = slotColor;

                Color contentColor = isBlackHole
                    ? new Color(0.54f, 0.56f, 0.68f, 1f)
                    : currencyPreview.isAvailable
                    ? new Color(0.88f, 0.95f, 1f, 1f)
                    : new Color(0.42f, 0.48f, 0.58f, 1f);
                Color quantityColor = isBlackHole
                    ? new Color(0.54f, 0.56f, 0.68f, 1f)
                    : !currencyPreview.isAvailable
                    ? new Color(0.42f, 0.48f, 0.58f, 1f)
                    : currencyPreview.hasEnoughCurrency
                        ? new Color(0.36f, 0.86f, 1f, 1f)
                        : new Color(1f, 0.45f, 0.48f, 1f);
                text.text = config.displayName;
                text.color = contentColor;
                quantityText.text = "x" + StarForgeFormat.Number(saveData.GetCurrency(currencyType));
                quantityText.color = quantityColor;
                icon.color = isBlackHole
                    ? new Color(1f, 1f, 1f, 0.32f)
                    : currencyPreview.isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            if (soundToggle != null)
            {
                soundToggle.SetIsOnWithoutNotify(saveData.soundEnabled);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(
                    Mathf.Clamp01(saveData.bgmVolume));
                UpdateVolumeValueText(
                    bgmVolumeValueText,
                    saveData.bgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(
                    Mathf.Clamp01(saveData.sfxVolume));
                UpdateVolumeValueText(
                    sfxVolumeValueText,
                    saveData.sfxVolume);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(saveData.vibrationEnabled);
            }

            if (achievementAlertToggle != null)
            {
                achievementAlertToggle.SetIsOnWithoutNotify(
                    saveData.achievementAlertMuted);
            }

            if (fractureAlertToggle != null)
            {
                fractureAlertToggle.SetIsOnWithoutNotify(saveData.fractureAlertMuted);
            }

            if (enhancementAnimationSkipToggle != null)
            {
                enhancementAnimationSkipToggle.SetIsOnWithoutNotify(
                    saveData.enhancementAnimationSkipEnabled);
            }

            RefreshExchangePanel(saveData, isBusy);
            RefreshCollectionPanel(saveData, balance, isBusy);
        }

        public void SetMiningAttemptsRemaining(
            int remaining,
            int remainingAdBonuses,
            bool isBusy)
        {
            if (miningOpenButton == null)
            {
                return;
            }

            int clampedRemaining = Mathf.Max(0, remaining);
            bool canWatchAd = remainingAdBonuses > 0;
            miningOpenButton.interactable =
                !isBusy &&
                (clampedRemaining > 0 || canWatchAd);
            if (miningOpenButtonText != null)
            {
                miningOpenButtonText.text = clampedRemaining > 0
                    ? "별 탐사하기\n오늘 " + clampedRemaining + "회"
                    : canWatchAd
                        ? "별 탐사하기\n광고 추가 탐험"
                        : "별 탐사하기\n오늘 완료";
            }
        }

        private void OnDestroy()
        {
            if (collectionPreview != null)
            {
                Destroy(collectionPreview.gameObject);
            }

            if (spaceBackdropTexture != null)
            {
                Destroy(spaceBackdropTexture);
            }

            if (expandedSpaceBackdropTexture != null)
            {
                Destroy(expandedSpaceBackdropTexture);
            }

        }

        // True while a modal guidance popup (result / 균열 / 블랙홀 안내 / 부활) is up.
        // These popups don't cover the enhance button, so enhancing must be blocked
        // until the player dismisses them.
        public bool IsBlockingOverlayOpen
        {
            get
            {
                return (resultPanel != null && resultPanel.activeSelf) ||
                       (revivePanel != null && revivePanel.activeSelf);
            }
        }

        private void UpdateEnhanceButtonInteractable()
        {
            if (enhanceButton != null)
            {
                enhanceButton.interactable =
                    lastCanEnhance && !IsBlockingOverlayOpen;
            }
        }

        public void ShowResult(StarForgeEnhancementResult result)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            SetResultRewards(null);
            resultTitleText.text = GetResultTitle(result);
            resultBodyText.text = BuildResultBody(result);
            UpdateEnhanceButtonInteractable();
        }

        public void ShowDisassembleResult(
            CurrencyAmount[] rewards,
            string newStageName,
            bool isBlackHole = false)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            resultTitleText.text = isBlackHole
                ? "블랙홀 분해 완료"
                : "행성 분해 완료";
            resultBodyText.text =
                "획득 재화\n\n새 행성 : " + newStageName;
            SetResultRewards(rewards);
            UpdateEnhanceButtonInteractable();
        }

        public void ShowAchievementClaimResult(CurrencyAmount[] rewards)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
            resultTitleText.text = "수령 완료";
            resultBodyText.text = "획득 보상";
            SetResultRewards(rewards, "x");
            UpdateEnhanceButtonInteractable();
        }

        public void ShowExchangeResult(StarForgeMaterialExchangeResult result)
        {
            if (exchangeStatusText == null || result == null)
            {
                return;
            }

            Color resultColor = result.success
                ? new Color(0.36f, 0.86f, 1f, 1f)
                : new Color(1f, 0.45f, 0.48f, 1f);

            if (result.success)
            {
                CloseExchangeQuantityPanel();
                exchangeStatusText.text = result.message;
                exchangeStatusText.color = resultColor;
                return;
            }

            if (exchangeQuantityPanel != null && exchangeQuantityPanel.activeSelf)
            {
                exchangeQuantityStatusText.text = result.message;
                exchangeQuantityStatusText.color = resultColor;
                return;
            }

            exchangeStatusText.text = result.message;
            exchangeStatusText.color = resultColor;
        }

        private void BuildTopPanel(RectTransform root)
        {
            GameObject panel = CreatePanel(
                "Top HUD",
                root,
                new Color(0.008f, 0.024f, 0.055f, 0.96f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.055f, 0.75f);
            rect.anchorMax = new Vector2(0.945f, 0.875f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                panel,
                new Color(0.008f, 0.026f, 0.062f, 0.97f),
                new Color(0.05f, 0.54f, 0.92f, 0.96f),
                2f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(38, 38, 18, 8);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            levelText = CreateText(
                "0강 우주 먼지",
                42,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                panel.transform);
            levelTextOutline = levelText.gameObject.AddComponent<Outline>();
            levelTextOutline.effectColor = GetLevelOutlineColor(0);
            levelTextOutline.effectDistance = new Vector2(1.5f, -1.5f);
            highestText = CreateText(
                "획득 기록 0강",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                panel.transform);
            highestText.color = new Color(0.58f, 0.74f, 0.98f, 1f);
            SetPreferredHeight(levelText, 52f);
            SetPreferredHeight(highestText, 30f);
            levelText.resizeTextForBestFit = true;
            levelText.resizeTextMinSize = 30;
            levelText.resizeTextMaxSize = 42;

            disassembleButton = CreateButton("분해", 15, panel.transform);
            RectTransform disassembleRect =
                disassembleButton.GetComponent<RectTransform>();
            disassembleRect.anchorMin = new Vector2(0.78f, 0.08f);
            disassembleRect.anchorMax = new Vector2(0.97f, 0.36f);
            disassembleRect.offsetMin = Vector2.zero;
            disassembleRect.offsetMax = Vector2.zero;
            LayoutElement disassembleLayout = disassembleButton.GetComponent<LayoutElement>();
            disassembleLayout.ignoreLayout = true;
            disassembleLayout.minWidth = 0f;
            disassembleLayout.preferredWidth = 0f;
            disassembleLayout.flexibleWidth = 0f;
            disassembleLayout.minHeight = 0f;
            disassembleLayout.preferredHeight = 0f;
            ApplyCanvasButtonStyle(
                disassembleButton,
                new Color(0.008f, 0.027f, 0.06f, 1f),
                new Color(0.08f, 0.58f, 1f, 1f),
                new Color(1f, 0.78f, 0.25f, 1f));
            Text disassembleLabel =
                disassembleButton.GetComponentInChildren<Text>();
            if (disassembleLabel != null)
            {
                disassembleLabel.resizeTextMinSize = 10;
                disassembleLabel.resizeTextMaxSize = 15;
            }

            disassembleButton.onClick.AddListener(OpenDisassembleConfirm);
        }

        private void BuildDisassembleConfirmPanel(RectTransform root)
        {
            disassembleConfirmPanel = CreatePanel(
                "Disassemble Confirm Popup",
                root,
                new Color(0.008f, 0.025f, 0.055f, 0.99f));
            RectTransform rect = disassembleConfirmPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.3f);
            rect.anchorMax = new Vector2(0.92f, 0.7f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                disassembleConfirmPanel,
                new Color(0.008f, 0.025f, 0.055f, 0.995f),
                new Color(0.08f, 0.52f, 0.86f, 0.98f),
                2f);

            VerticalLayoutGroup layout = disassembleConfirmPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 28, 24);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Text title = CreateText("행성 분해", 38, FontStyle.Bold, TextAnchor.MiddleCenter, disassembleConfirmPanel.transform);
            title.color = new Color(0.94f, 0.97f, 1f, 1f);
            SetPreferredHeight(title, 54f);
            disassembleBodyText = CreateText(
                string.Empty,
                22,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                disassembleConfirmPanel.transform);
            disassembleBodyText.color = new Color(0.78f, 0.86f, 0.96f, 1f);
            disassembleBodyText.resizeTextForBestFit = true;
            disassembleBodyText.resizeTextMinSize = 16;
            disassembleBodyText.resizeTextMaxSize = 22;
            SetPreferredHeight(disassembleBodyText, 240f);

            GameObject row = new GameObject("Disassemble Confirm Buttons", typeof(RectTransform));
            row.transform.SetParent(disassembleConfirmPanel.transform, false);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = 15f;
            rowElement.preferredHeight = 15f;

            Button confirmButton = CreateButton("분해", 26, row.transform);
            ShrinkButtonHeight(confirmButton, 15f);
            ApplyCanvasButtonStyle(
                confirmButton,
                new Color(0.24f, 0.035f, 0.035f, 1f),
                new Color(0.82f, 0.16f, 0.16f, 1f),
                new Color(1f, 0.75f, 0.7f, 1f));
            confirmButton.onClick.AddListener(() =>
            {
                disassembleConfirmPanel.SetActive(false);
                DisassembleRequested?.Invoke();
            });

            Button cancelButton = CreateButton("취소", 26, row.transform);
            ShrinkButtonHeight(cancelButton, 15f);
            ApplyCanvasButtonStyle(
                cancelButton,
                new Color(0.018f, 0.055f, 0.12f, 1f),
                new Color(0.16f, 0.32f, 0.5f, 1f),
                new Color(0.82f, 0.9f, 1f, 1f));
            cancelButton.onClick.AddListener(() => disassembleConfirmPanel.SetActive(false));

            disassembleConfirmPanel.SetActive(false);
        }

        private static void ShrinkButtonHeight(Button button, float height)
        {
            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.minHeight = height;
                element.preferredHeight = height;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.offsetMin = new Vector2(8f, 1f);
                labelRect.offsetMax = new Vector2(-8f, -1f);
            }
        }

        private static string BuildDisassembleSummary(int level, string stageName, CurrencyAmount[] rewards)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append(level);
            builder.Append("강 ");
            builder.Append(stageName);
            builder.Append("을(를) 분해합니다.\n\n획득 재화");

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Length; i++)
                {
                    if (rewards[i] == null)
                    {
                        continue;
                    }

                    builder.Append('\n');
                    builder.Append(StarForgeCurrencyNames.GetDisplayName(rewards[i].type));
                    builder.Append(" +");
                    builder.Append(StarForgeFormat.Number(rewards[i].amount));
                }
            }

            builder.Append("\n\n분해 후 0강 새 행성으로 시작합니다. (모양 재추첨)");
            return builder.ToString();
        }

        private void OpenDisassembleConfirm()
        {
            if (disassembleConfirmPanel == null)
            {
                return;
            }

            disassembleBodyText.text = pendingDisassembleSummary;
            disassembleConfirmPanel.SetActive(true);
        }

        private void BuildPlanetDragSurface(RectTransform root)
        {
            GameObject dragObject = new GameObject(
                "Planet Drag Surface",
                typeof(RectTransform),
                typeof(Image),
                typeof(StarForgeDragRotationInput));
            dragObject.transform.SetParent(root, false);
            RectTransform rect = dragObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.4f);
            rect.anchorMax = new Vector2(0.94f, 0.875f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = dragObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            StarForgeDragRotationInput dragInput =
                dragObject.GetComponent<StarForgeDragRotationInput>();
            dragInput.Dragged += delta => CameraOrbitDragged?.Invoke(delta);
        }

        private void BuildEnhancementAnimationSkipToggle(Transform parent)
        {
            GameObject toggleObject = new GameObject(
                "Enhancement Animation Skip Toggle",
                typeof(RectTransform),
                typeof(Image),
                typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.805f, 0.155f);
            rect.anchorMax = new Vector2(0.947f, 0.235f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image target = ApplyCanvasFrame(
                toggleObject,
                new Color(0.018f, 0.055f, 0.12f, 0.99f),
                new Color(0.12f, 0.58f, 0.92f, 0.98f),
                2f);
            Text label = CreateText(
                "스킵",
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                toggleObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.08f, 0.56f);
            labelRect.anchorMax = new Vector2(0.92f, 0.94f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.82f, 0.92f, 1f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 17;

            GameObject checkboxObject = new GameObject(
                "Checkbox",
                typeof(RectTransform),
                typeof(Image));
            checkboxObject.transform.SetParent(toggleObject.transform, false);
            RectTransform checkboxRect =
                checkboxObject.GetComponent<RectTransform>();
            checkboxRect.anchorMin = new Vector2(0.5f, 0.08f);
            checkboxRect.anchorMax = new Vector2(0.5f, 0.08f);
            checkboxRect.pivot = new Vector2(0.5f, 0f);
            checkboxRect.sizeDelta = new Vector2(34f, 34f);
            checkboxRect.anchoredPosition = Vector2.zero;
            Image checkbox = checkboxObject.GetComponent<Image>();
            checkbox.sprite = null;
            checkbox.type = Image.Type.Simple;
            checkbox.color = new Color(0.008f, 0.025f, 0.055f, 0.96f);
            checkbox.raycastTarget = false;
            AddFourSideBorder(
                checkboxObject,
                new Color(0.38f, 0.94f, 1f, 1f),
                2f);

            Text checkmark = CreateText(
                "V",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                checkboxObject.transform);
            StretchRect(checkmark.rectTransform, Vector2.zero);
            checkmark.color = new Color(0.38f, 0.94f, 1f, 1f);
            checkmark.raycastTarget = false;

            enhancementAnimationSkipToggle =
                toggleObject.GetComponent<Toggle>();
            enhancementAnimationSkipToggle.targetGraphic = target;
            enhancementAnimationSkipToggle.graphic = checkmark;
            enhancementAnimationSkipToggle.SetIsOnWithoutNotify(false);
            enhancementAnimationSkipToggle.onValueChanged.AddListener(
                value => EnhancementAnimationSkipToggled?.Invoke(value));
        }

        private void BuildSettingsButton(RectTransform root)
        {
            miningOpenButton = CreateButton("별 탐사하기\n오늘 3회", 20, root);
            miningOpenButtonText = miningOpenButton.GetComponentInChildren<Text>();
            RectTransform miningRect = miningOpenButton.GetComponent<RectTransform>();
            miningRect.anchorMin = new Vector2(0.03f, 0.895f);
            miningRect.anchorMax = new Vector2(0.254f, 0.965f);
            miningRect.offsetMin = Vector2.zero;
            miningRect.offsetMax = Vector2.zero;
            ApplyCanvasButtonStyle(
                miningOpenButton,
                new Color(0.03f, 0.13f, 0.28f, 0.99f),
                new Color(0.08f, 0.62f, 1f, 1f),
                new Color(0.4f, 0.8f, 1f, 1f));
            if (miningOpenButtonText != null)
            {
                miningOpenButtonText.lineSpacing = 0.82f;
            }
            miningOpenButton.onClick.AddListener(() => MiningRequested?.Invoke());

            exchangeOpenButton = CreateButton("재료 교환", 20, root);
            RectTransform exchangeRect = exchangeOpenButton.GetComponent<RectTransform>();
            exchangeRect.anchorMin = new Vector2(0.269f, 0.895f);
            exchangeRect.anchorMax = new Vector2(0.492f, 0.965f);
            exchangeRect.offsetMin = Vector2.zero;
            exchangeRect.offsetMax = Vector2.zero;
            ApplyCanvasButtonStyle(
                exchangeOpenButton,
                new Color(0.018f, 0.055f, 0.12f, 0.99f),
                new Color(0.24f, 0.3f, 0.38f, 1f),
                new Color(0.78f, 0.86f, 0.98f, 1f));
            exchangeOpenButton.onClick.AddListener(OpenExchangePanel);

            collectionOpenButton = CreateButton("도감", 20, root);
            RectTransform collectionRect = collectionOpenButton.GetComponent<RectTransform>();
            collectionRect.anchorMin = new Vector2(0.508f, 0.895f);
            collectionRect.anchorMax = new Vector2(0.731f, 0.965f);
            collectionRect.offsetMin = Vector2.zero;
            collectionRect.offsetMax = Vector2.zero;
            ApplyCanvasButtonStyle(
                collectionOpenButton,
                new Color(0.018f, 0.055f, 0.12f, 0.99f),
                new Color(0.24f, 0.3f, 0.38f, 1f),
                new Color(0.78f, 0.86f, 0.98f, 1f));
            collectionOpenButton.onClick.AddListener(OpenCollectionPanel);

            Button settingsButton = CreateButton("설정", 20, root);
            RectTransform rect = settingsButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.746f, 0.895f);
            rect.anchorMax = new Vector2(0.97f, 0.965f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasButtonStyle(
                settingsButton,
                new Color(0.018f, 0.055f, 0.12f, 0.99f),
                new Color(0.24f, 0.3f, 0.38f, 1f),
                new Color(0.78f, 0.86f, 0.98f, 1f));
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        }

        private void BuildMaterialPanel(RectTransform root, StarForgeBalance balance)
        {
            GameObject panel = CreatePanel("Bottom HUD", root, new Color(1f, 1f, 1f, 0f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().raycastTarget = false;

            GameObject infoPanel = CreatePanel(
                "Enhance Info",
                panel.transform,
                new Color(0.008f, 0.028f, 0.064f, 0.99f));
            SetRectAnchors(infoPanel, 0.055f, 0.245f, 0.945f, 0.39f);
            ApplyCanvasFrame(
                infoPanel,
                new Color(0.008f, 0.03f, 0.068f, 0.99f),
                new Color(0.05f, 0.48f, 0.82f, 0.96f),
                2f);

            HorizontalLayoutGroup infoLayout = infoPanel.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(40, 34, 10, 10);
            infoLayout.spacing = 28f;
            infoLayout.childAlignment = TextAnchor.MiddleCenter;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = true;

            GameObject selectedColumn = new GameObject(
                "Selected Material Column",
                typeof(RectTransform),
                typeof(LayoutElement));
            selectedColumn.transform.SetParent(infoPanel.transform, false);
            LayoutElement selectedColumnLayout = selectedColumn.GetComponent<LayoutElement>();
            selectedColumnLayout.flexibleWidth = 0.9f;
            selectedColumnLayout.preferredWidth = 210f;
            VerticalLayoutGroup selectedLayout = selectedColumn.AddComponent<VerticalLayoutGroup>();
            selectedLayout.spacing = 2f;
            selectedLayout.childAlignment = TextAnchor.MiddleCenter;
            selectedLayout.childControlWidth = true;
            selectedLayout.childControlHeight = true;
            selectedLayout.childForceExpandWidth = true;
            selectedLayout.childForceExpandHeight = false;

            Text selectedTitleText = CreateText(
                "선택 재료",
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                selectedColumn.transform);
            selectedTitleText.color = new Color(0.25f, 0.72f, 1f, 1f);
            SetPreferredHeight(selectedTitleText, 24f);

            selectedMaterialText = CreateText(
                "운석 파편",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                selectedColumn.transform);
            selectedMaterialText.color = new Color(0.94f, 0.97f, 1f, 1f);
            selectedMaterialText.resizeTextForBestFit = true;
            selectedMaterialText.resizeTextMinSize = 17;
            selectedMaterialText.resizeTextMaxSize = 24;
            SetPreferredHeight(selectedMaterialText, 28f);

            GameObject selectedIconObject = new GameObject(
                "Selected Material Icon",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            selectedIconObject.transform.SetParent(selectedColumn.transform, false);
            SetFixedLayoutSize(selectedIconObject, 92f, 92f);
            selectedMaterialIconImage = selectedIconObject.GetComponent<Image>();
            selectedMaterialIconImage.sprite = GetMaterialIcon(0);
            selectedMaterialIconImage.preserveAspect = true;

            GameObject chanceColumn = new GameObject(
                "Chance Column",
                typeof(RectTransform),
                typeof(LayoutElement));
            chanceColumn.transform.SetParent(infoPanel.transform, false);
            LayoutElement chanceColumnLayout = chanceColumn.GetComponent<LayoutElement>();
            chanceColumnLayout.flexibleWidth = 1.35f;
            chanceColumnLayout.preferredWidth = 320f;
            AddVerticalDivider(
                infoPanel.transform,
                0.42f,
                new Color(0.08f, 0.42f, 0.72f, 0.62f));
            VerticalLayoutGroup chanceLayout = chanceColumn.AddComponent<VerticalLayoutGroup>();
            chanceLayout.padding = new RectOffset(0, 0, 2, 2);
            chanceLayout.spacing = 8f;
            chanceLayout.childAlignment = TextAnchor.MiddleLeft;
            chanceLayout.childControlWidth = true;
            chanceLayout.childControlHeight = true;
            chanceLayout.childForceExpandWidth = true;
            chanceLayout.childForceExpandHeight = false;

            chanceText = CreateText(
                "성공 확률   100%",
                ChanceTextDefaultFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                chanceColumn.transform);
            riskText = CreateText(
                "실패 시   0% 확률로 소멸",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                chanceColumn.transform);
            statusText = CreateText(
                "상태   안정",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                chanceColumn.transform);
            chanceText.supportRichText = true;
            riskText.supportRichText = true;
            statusText.supportRichText = true;
            chanceText.color = new Color(1f, 0.83f, 0.42f, 1f);
            riskText.color = new Color(0.88f, 0.93f, 1f, 1f);
            statusText.color = new Color(0.3f, 0.88f, 0.58f, 1f);
            // Allow the chance line to shrink further so longer text always fits
            // on one line without clipping.
            chanceText.resizeTextMinSize = 9;
            SetPreferredHeight(chanceText, 36f);
            SetPreferredHeight(riskText, 32f);
            SetPreferredHeight(statusText, 32f);

            enhanceButton = CreateButton("강화", 40, panel.transform);
            RectTransform enhanceButtonRect = enhanceButton.GetComponent<RectTransform>();
            enhanceButtonRect.anchorMin = new Vector2(0.14f, 0.155f);
            enhanceButtonRect.anchorMax = new Vector2(0.775f, 0.235f);
            enhanceButtonRect.offsetMin = Vector2.zero;
            enhanceButtonRect.offsetMax = Vector2.zero;
            ApplyCanvasButtonStyle(
                enhanceButton,
                new Color(1f, 0.52f, 0.025f, 1f),
                new Color(1f, 0.83f, 0.2f, 1f),
                new Color(0.02f, 0.045f, 0.09f, 1f));
            Text enhanceLabel = enhanceButton.GetComponentInChildren<Text>();
            Outline enhanceOutline = enhanceLabel.gameObject.AddComponent<Outline>();
            enhanceOutline.effectColor = new Color(1f, 0.88f, 0.28f, 0.7f);
            enhanceOutline.effectDistance = new Vector2(1.5f, -1.5f);
            enhanceButton.onClick.AddListener(() => EnhanceClicked?.Invoke());
            BuildEnhancementAnimationSkipToggle(panel.transform);

            Text inventoryTitle = CreateText(
                "보유 재료",
                25,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                panel.transform);
            SetRectAnchors(
                inventoryTitle.gameObject,
                0.28f,
                0.118f,
                0.72f,
                0.152f);
            inventoryTitle.color = new Color(0.22f, 0.78f, 1f, 1f);
            CreateHorizontalDivider(
                panel.transform,
                0.135f,
                new Color(0.05f, 0.34f, 0.58f, 0.62f),
                0.035f,
                0.27f);
            CreateHorizontalDivider(
                panel.transform,
                0.135f,
                new Color(0.05f, 0.34f, 0.58f, 0.62f),
                0.73f,
                0.965f);

            GameObject materialRow = CreatePanel(
                "Material Slots",
                panel.transform,
                new Color(0.004f, 0.014f, 0.035f, 0.9f));
            SetRectAnchors(materialRow, 0.025f, 0.005f, 0.975f, 0.118f);
            ApplyCanvasFrame(
                materialRow,
                new Color(0.004f, 0.018f, 0.042f, 0.96f),
                new Color(0.04f, 0.28f, 0.5f, 0.84f),
                1.5f);
            HorizontalLayoutGroup rowLayout = materialRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 7, 7);
            rowLayout.spacing = 7f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            for (int i = 0; i < currencyButtons.Length; i++)
            {
                StarForgeCurrencyType currencyType = (StarForgeCurrencyType)i;
                CurrencyConfig config = balance.GetCurrency(currencyType);
                Button button = CreateMaterialButton(i, config.displayName, materialRow.transform);
                int capturedIndex = i;
                button.onClick.AddListener(() => CurrencySelected?.Invoke((StarForgeCurrencyType)capturedIndex));
                currencyButtons[i] = button;
                currencyButtonTexts[i] = button.transform.Find("Name").GetComponent<Text>();
                currencyQuantityTexts[i] = button.transform.Find("Quantity").GetComponent<Text>();
                currencyIconImages[i] = button.transform.Find("Icon").GetComponent<Image>();
            }
        }

        private void BuildResultPanel(RectTransform root)
        {
            resultPanel = CreatePanel(
                "Result Popup",
                root,
                new Color(0.008f, 0.025f, 0.055f, 0.99f));
            RectTransform rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.055f, 0.275f);
            rect.anchorMax = new Vector2(0.945f, 0.745f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                resultPanel,
                new Color(0.008f, 0.026f, 0.058f, 0.995f),
                new Color(0.08f, 0.54f, 0.92f, 0.98f),
                2f);

            VerticalLayoutGroup layout = resultPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 42, 28, 32);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject alertBadge = CreatePanel(
                "Result Alert Badge",
                resultPanel.transform,
                new Color(0.02f, 0.12f, 0.24f, 1f));
            SetFixedLayoutSize(alertBadge, 46f, 46f);
            LayoutElement alertLayout = alertBadge.GetComponent<LayoutElement>();
            alertLayout.ignoreLayout = true;
            RectTransform alertRect = alertBadge.GetComponent<RectTransform>();
            alertRect.anchorMin = new Vector2(0.5f, 1f);
            alertRect.anchorMax = new Vector2(0.5f, 1f);
            alertRect.pivot = new Vector2(0.5f, 0.5f);
            alertRect.sizeDelta = new Vector2(46f, 46f);
            alertRect.anchoredPosition = new Vector2(0f, 1f);
            alertRect.localEulerAngles = new Vector3(0f, 0f, 45f);
            ApplyCanvasFrame(
                alertBadge,
                new Color(0.02f, 0.12f, 0.24f, 1f),
                new Color(0.15f, 0.68f, 1f, 1f),
                1.5f);
            Text alertText = CreateText(
                "!",
                28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                alertBadge.transform);
            StretchRect(alertText.rectTransform, new Vector2(4f, 4f));
            alertText.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            alertText.color = new Color(0.85f, 0.95f, 1f, 1f);

            resultTitleText = CreateText(
                "결과",
                44,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                resultPanel.transform);
            resultTitleText.color = new Color(0.96f, 0.98f, 1f, 1f);
            SetPreferredHeight(resultTitleText, 62f);
            resultBodyText = CreateText(
                "",
                27,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                resultPanel.transform);
            resultBodyText.color = new Color(0.84f, 0.9f, 0.98f, 1f);
            resultBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultBodyText.verticalOverflow = VerticalWrapMode.Truncate;
            resultBodyText.lineSpacing = 1.05f;
            resultBodyText.resizeTextForBestFit = true;
            resultBodyText.resizeTextMinSize = 16;
            resultBodyText.resizeTextMaxSize = 25;
            SetPreferredHeight(resultBodyText, 220f);

            resultRewardRow = CreatePanel(
                "Result Reward Row",
                resultPanel.transform,
                new Color(0.004f, 0.016f, 0.04f, 0.88f));
            ApplyCanvasFrame(
                resultRewardRow,
                new Color(0.004f, 0.018f, 0.045f, 0.94f),
                new Color(0.06f, 0.36f, 0.62f, 0.82f),
                1.25f);
            LayoutElement rewardRowLayout =
                resultRewardRow.AddComponent<LayoutElement>();
            rewardRowLayout.minHeight = 28f;
            rewardRowLayout.preferredHeight = 28f;
            HorizontalLayoutGroup rewardLayout =
                resultRewardRow.AddComponent<HorizontalLayoutGroup>();
            rewardLayout.padding = new RectOffset(12, 12, 2, 2);
            rewardLayout.spacing = 8f;
            rewardLayout.childAlignment = TextAnchor.MiddleCenter;
            rewardLayout.childControlWidth = true;
            rewardLayout.childControlHeight = true;
            rewardLayout.childForceExpandWidth = true;
            rewardLayout.childForceExpandHeight = true;

            for (int i = 0; i < resultRewardIcons.Length; i++)
            {
                GameObject rewardSlot = CreatePanel(
                    "Result Reward " + i,
                    resultRewardRow.transform,
                    new Color(0.008f, 0.03f, 0.065f, 0.96f));
                ApplyCanvasFrame(
                    rewardSlot,
                    new Color(0.008f, 0.03f, 0.065f, 0.96f),
                    new Color(0.05f, 0.28f, 0.48f, 0.78f),
                    1f);
                HorizontalLayoutGroup slotLayout =
                    rewardSlot.AddComponent<HorizontalLayoutGroup>();
                slotLayout.padding = new RectOffset(5, 5, 1, 1);
                slotLayout.spacing = 3f;
                slotLayout.childAlignment = TextAnchor.MiddleCenter;
                slotLayout.childControlWidth = true;
                slotLayout.childControlHeight = true;
                slotLayout.childForceExpandWidth = false;
                slotLayout.childForceExpandHeight = false;

                GameObject iconObject = new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                iconObject.transform.SetParent(rewardSlot.transform, false);
                LayoutElement iconLayout =
                    iconObject.GetComponent<LayoutElement>();
                iconLayout.minWidth = 55f;
                iconLayout.preferredWidth = 55f;
                iconLayout.minHeight = 55f;
                iconLayout.preferredHeight = 55f;
                Image icon = iconObject.GetComponent<Image>();
                icon.preserveAspect = true;
                resultRewardIcons[i] = icon;

                Text amount = CreateText(
                    "+0",
                    25,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    rewardSlot.transform);
                amount.resizeTextForBestFit = true;
                amount.resizeTextMinSize = 16;
                amount.resizeTextMaxSize = 25;
                amount.color = new Color(1f, 0.76f, 0.3f, 1f);
                resultRewardTexts[i] = amount;
            }

            resultRewardRow.SetActive(false);

            Button closeButton = CreateButton("확인", 28, resultPanel.transform);
            LayoutElement closeLayout = closeButton.GetComponent<LayoutElement>();
            closeLayout.preferredHeight = 70f;
            closeLayout.minHeight = 70f;
            closeLayout.minWidth = 320f;
            closeLayout.preferredWidth = 320f;
            closeLayout.flexibleWidth = 0f;
            ApplyCanvasButtonStyle(
                closeButton,
                new Color(0.025f, 0.1f, 0.2f, 1f),
                new Color(0.08f, 0.55f, 0.94f, 1f),
                new Color(0.94f, 0.98f, 1f, 1f));
            closeButton.onClick.AddListener(CloseResultPanel);

            resultPanel.SetActive(false);
        }

        private Text CreateSettingsSectionLabel(string text, Transform parent)
        {
            Text label = CreateText(
                text,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                parent);
            label.color = new Color(0.52f, 0.68f, 0.9f, 1f);
            SetPreferredHeight(label, 26f);
            return label;
        }

        private void CreateSettingsDivider(Transform parent)
        {
            GameObject line = CreatePanel(
                "Divider",
                parent,
                new Color(0.16f, 0.34f, 0.58f, 0.45f));
            line.GetComponent<Image>().raycastTarget = false;
            LayoutElement layoutElement = line.AddComponent<LayoutElement>();
            layoutElement.minHeight = 2f;
            layoutElement.preferredHeight = 2f;
        }

        private void BuildSettingsPanel(RectTransform root)
        {
            settingsPanel = CreatePanel(
                "Settings Popup",
                root,
                new Color(0.008f, 0.025f, 0.055f, 0.99f));
            RectTransform rect = settingsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.1f);
            rect.anchorMax = new Vector2(0.92f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                settingsPanel,
                new Color(0.008f, 0.026f, 0.058f, 0.995f),
                new Color(0.08f, 0.54f, 0.92f, 0.98f),
                2f);

            VerticalLayoutGroup layout = settingsPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text settingsTitle = CreateText("설정", 34, FontStyle.Bold, TextAnchor.MiddleCenter, settingsPanel.transform);
            settingsTitle.color = new Color(0.95f, 0.98f, 1f, 1f);
            SetPreferredHeight(settingsTitle, 44f);

            CreateSettingsSectionLabel("오디오", settingsPanel.transform);
            soundToggle = CreateToggle("전체 사운드", settingsPanel.transform);
            soundToggle.onValueChanged.AddListener(value => SoundToggled?.Invoke(value));
            bgmVolumeSlider = CreateVolumeSlider(
                "배경음악",
                settingsPanel.transform,
                out bgmVolumeValueText);
            bgmVolumeSlider.onValueChanged.AddListener(value =>
            {
                UpdateVolumeValueText(bgmVolumeValueText, value);
                BgmVolumeChanged?.Invoke(value);
            });
            sfxVolumeSlider = CreateVolumeSlider(
                "효과음",
                settingsPanel.transform,
                out sfxVolumeValueText);
            sfxVolumeSlider.onValueChanged.AddListener(value =>
            {
                UpdateVolumeValueText(sfxVolumeValueText, value);
                SfxVolumeChanged?.Invoke(value);
            });

            CreateSettingsDivider(settingsPanel.transform);
            CreateSettingsSectionLabel("게임플레이", settingsPanel.transform);
            vibrationToggle = CreateToggle("진동", settingsPanel.transform);
            vibrationToggle.onValueChanged.AddListener(value => VibrationToggled?.Invoke(value));
            fractureAlertToggle = CreateToggle("균열 알림 끄기", settingsPanel.transform);
            fractureAlertToggle.onValueChanged.AddListener(
                value => FractureAlertMutedToggled?.Invoke(value));

            achievementAlertToggle = CreateToggle("업적 알림 끄기", settingsPanel.transform);
            achievementAlertToggle.onValueChanged.AddListener(
                value => AchievementAlertMutedToggled?.Invoke(value));

            CreateSettingsDivider(settingsPanel.transform);
            CreateSettingsSectionLabel("약관", settingsPanel.transform);
            // Fixed-height row (matches the toggle rows). Buttons are anchored into
            // halves and ignore layout so a nested layout group can't stretch them.
            GameObject policyRow = new GameObject(
                "Policy Links",
                typeof(RectTransform),
                typeof(LayoutElement));
            policyRow.transform.SetParent(settingsPanel.transform, false);
            LayoutElement policyRowLayout = policyRow.GetComponent<LayoutElement>();
            policyRowLayout.minHeight = 58f;
            policyRowLayout.preferredHeight = 58f;
            policyRowLayout.flexibleHeight = 0f;
            policyRowLayout.flexibleWidth = 1f;

            Button privacyButton = CreateButton(
                "개인정보처리방침",
                16,
                policyRow.transform);
            RectTransform privacyRect = privacyButton.GetComponent<RectTransform>();
            privacyRect.anchorMin = new Vector2(0f, 0f);
            privacyRect.anchorMax = new Vector2(0.49f, 1f);
            privacyRect.offsetMin = Vector2.zero;
            privacyRect.offsetMax = Vector2.zero;
            privacyButton.GetComponent<LayoutElement>().ignoreLayout = true;
            privacyButton.onClick.AddListener(
                StarForgeLegal.OpenPrivacyPolicy);

            Button termsButton = CreateButton(
                "이용약관",
                16,
                policyRow.transform);
            RectTransform termsRect = termsButton.GetComponent<RectTransform>();
            termsRect.anchorMin = new Vector2(0.51f, 0f);
            termsRect.anchorMax = new Vector2(1f, 1f);
            termsRect.offsetMin = Vector2.zero;
            termsRect.offsetMax = Vector2.zero;
            termsButton.GetComponent<LayoutElement>().ignoreLayout = true;
            termsButton.onClick.AddListener(
                StarForgeLegal.OpenTermsOfService);

            CreateSettingsDivider(settingsPanel.transform);
            Button resetButton = CreateButton("데이터 초기화", 26, settingsPanel.transform);
            resetButton.GetComponent<LayoutElement>().preferredHeight = 56f;
            ApplyCanvasButtonStyle(
                resetButton,
                new Color(0.22f, 0.025f, 0.035f, 1f),
                new Color(0.78f, 0.12f, 0.16f, 1f),
                new Color(1f, 0.46f, 0.48f, 1f));
            resetButton.onClick.AddListener(() => resetConfirmPanel.SetActive(true));

            Button closeButton = CreateButton("닫기", 26, settingsPanel.transform);
            closeButton.GetComponent<LayoutElement>().preferredHeight = 56f;
            ApplyCanvasButtonStyle(
                closeButton,
                new Color(0.018f, 0.055f, 0.12f, 1f),
                new Color(0.12f, 0.42f, 0.7f, 1f),
                new Color(0.88f, 0.95f, 1f, 1f));
            closeButton.onClick.AddListener(() => settingsPanel.SetActive(false));

            settingsPanel.SetActive(false);
        }

        private void BuildResetConfirmPanel(RectTransform root)
        {
            resetConfirmPanel = CreatePanel("Reset Confirm Popup", root, new Color(0.02f, 0.025f, 0.05f, 0.98f));
            RectTransform rect = resetConfirmPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.35f);
            rect.anchorMax = new Vector2(0.92f, 0.65f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                resetConfirmPanel,
                new Color(0.03f, 0.025f, 0.045f, 0.99f),
                new Color(0.72f, 0.18f, 0.2f, 0.98f),
                2f);

            VerticalLayoutGroup layout = resetConfirmPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Text resetTitle = CreateText("데이터 초기화", 40, FontStyle.Bold, TextAnchor.MiddleCenter, resetConfirmPanel.transform);
            resetTitle.color = new Color(1f, 0.48f, 0.5f, 1f);
            Text body = CreateText("모든 강화 기록과 재화가 삭제됩니다.\n정말 초기화할까요?", 28, FontStyle.Normal, TextAnchor.MiddleCenter, resetConfirmPanel.transform);
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 20;
            body.resizeTextMaxSize = 28;

            GameObject row = new GameObject("Reset Confirm Buttons", typeof(RectTransform));
            row.transform.SetParent(resetConfirmPanel.transform, false);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().preferredHeight = 72f;

            Button confirmButton = CreateButton("초기화", 26, row.transform);
            ApplyCanvasButtonStyle(
                confirmButton,
                new Color(0.22f, 0.025f, 0.035f, 1f),
                new Color(0.82f, 0.1f, 0.14f, 1f),
                new Color(1f, 0.55f, 0.56f, 1f));
            confirmButton.onClick.AddListener(() =>
            {
                resetConfirmPanel.SetActive(false);
                settingsPanel.SetActive(false);
                ResetConfirmed?.Invoke();
            });

            Button cancelButton = CreateButton("취소", 26, row.transform);
            ApplyCanvasButtonStyle(
                cancelButton,
                new Color(0.018f, 0.055f, 0.12f, 1f),
                new Color(0.12f, 0.42f, 0.7f, 1f),
                new Color(0.88f, 0.95f, 1f, 1f));
            cancelButton.onClick.AddListener(() => resetConfirmPanel.SetActive(false));

            resetConfirmPanel.SetActive(false);
        }

        private void BuildExchangePanel(RectTransform root)
        {
            exchangePanel = CreatePanel(
                "Material Exchange Overlay",
                root,
                new Color(0.005f, 0.008f, 0.02f, 0.94f));
            RectTransform overlayRect = exchangePanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject dialog = CreatePanel(
                "Material Exchange Popup",
                exchangePanel.transform,
                new Color(0.025f, 0.03f, 0.065f, 0.99f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.06f, 0.24f);
            dialogRect.anchorMax = new Vector2(0.94f, 0.76f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                dialog,
                new Color(0.012f, 0.028f, 0.06f, 0.99f),
                new Color(0.1f, 0.48f, 0.76f, 0.98f),
                2f);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Text title = CreateText("재료 교환", 34, FontStyle.Bold, TextAnchor.MiddleCenter, dialog.transform);
            title.color = new Color(0.94f, 0.9f, 1f, 1f);
            SetPreferredHeight(title, 44f);

            exchangeStatusText = CreateText("", 17, FontStyle.Bold, TextAnchor.MiddleCenter, dialog.transform);
            SetPreferredHeight(exchangeStatusText, 26f);

            GameObject scrollView = CreatePanel(
                "Exchange Scroll View",
                dialog.transform,
                new Color(0.01f, 0.018f, 0.035f, 0.8f));
            LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 260f;
            scrollLayout.flexibleHeight = 1f;
            exchangeScrollRect = scrollView.AddComponent<ScrollRect>();
            exchangeScrollRect.horizontal = false;
            exchangeScrollRect.vertical = true;
            exchangeScrollRect.movementType = ScrollRect.MovementType.Clamped;
            exchangeScrollRect.inertia = true;
            exchangeScrollRect.scrollSensitivity = 28f;

            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-26f, -4f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.spacing = 7f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject scrollbarObject = CreatePanel(
                "Scrollbar Vertical",
                scrollView.transform,
                new Color(0.025f, 0.035f, 0.065f, 1f));
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-16f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);

            GameObject handleObject = CreatePanel(
                "Handle",
                scrollbarObject.transform,
                new Color(0.34f, 0.16f, 0.5f, 1f));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleObject.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.numberOfSteps = 0;

            exchangeScrollRect.viewport = viewportRect;
            exchangeScrollRect.content = contentRect;
            exchangeScrollRect.verticalScrollbar = scrollbar;
            exchangeScrollRect.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;
            exchangeScrollRect.verticalScrollbarSpacing = 0f;

            for (int i = 0; i < exchangeButtons.Length; i++)
            {
                StarForgeMaterialExchangeRoute route = exchangeService.GetRoute(i);
                GameObject routeContainer = new GameObject(
                    "Exchange Route " + i,
                    typeof(RectTransform),
                    typeof(LayoutElement));
                routeContainer.transform.SetParent(content.transform, false);
                LayoutElement routeLayoutElement = routeContainer.GetComponent<LayoutElement>();
                routeLayoutElement.minHeight = 100f;
                routeLayoutElement.preferredHeight = 100f;

                VerticalLayoutGroup routeLayout = routeContainer.AddComponent<VerticalLayoutGroup>();
                routeLayout.spacing = 2f;
                routeLayout.childControlWidth = true;
                routeLayout.childControlHeight = true;
                routeLayout.childForceExpandWidth = true;
                routeLayout.childForceExpandHeight = false;

                GameObject routeRow = new GameObject(
                    "Exchange Cards",
                    typeof(RectTransform),
                    typeof(LayoutElement));
                routeRow.transform.SetParent(routeContainer.transform, false);
                routeRow.GetComponent<LayoutElement>().preferredHeight = 76f;
                HorizontalLayoutGroup rowLayout = routeRow.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 6f;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;

                Text sourceOwnedText;
                Image sourceIcon;
                CreateExchangeMaterialCard(
                    "Source Material",
                    route.sourceType,
                    route.sourceAmount,
                    "보유 x0",
                    routeRow.transform,
                    out sourceOwnedText,
                    out sourceIcon);

                Text arrow = CreateText("≫", 34, FontStyle.Bold, TextAnchor.MiddleCenter, routeRow.transform);
                arrow.color = new Color(0.34f, 0.72f, 1f, 1f);
                LayoutElement arrowLayout = arrow.GetComponent<LayoutElement>();
                arrowLayout.minWidth = 38f;
                arrowLayout.preferredWidth = 38f;
                arrowLayout.flexibleWidth = 0f;

                Text targetDetailText;
                Image targetIcon;
                CreateExchangeMaterialCard(
                    "Target Material",
                    route.targetType,
                    route.targetAmount,
                    "획득",
                    routeRow.transform,
                    out targetDetailText,
                    out targetIcon);

                Button button = CreateButton("교환", 18, routeRow.transform);
                LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.minWidth = 64f;
                buttonLayout.preferredWidth = 64f;
                buttonLayout.minHeight = 64f;
                buttonLayout.preferredHeight = 64f;
                buttonLayout.flexibleWidth = 0f;
                buttonLayout.flexibleHeight = 0f;
                button.image.color = new Color(0.16f, 0.07f, 0.24f, 1f);

                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.16f, 0.07f, 0.24f, 1f);
                colors.highlightedColor = new Color(0.26f, 0.12f, 0.38f, 1f);
                colors.pressedColor = new Color(0.09f, 0.035f, 0.14f, 1f);
                colors.disabledColor = new Color(0.07f, 0.045f, 0.09f, 0.95f);
                button.colors = colors;
                AddPanelOutline(
                    button.gameObject,
                    new Color(0.34f, 0.22f, 0.72f, 0.96f),
                    new Vector2(1.5f, -1.5f));

                Text routeStatusText = CreateText(
                    "",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    routeContainer.transform);
                routeStatusText.color = new Color(0.62f, 0.72f, 0.84f, 1f);
                SetPreferredHeight(routeStatusText, 22f);

                int capturedIndex = i;
                button.onClick.AddListener(() => OpenExchangeQuantityPanel(capturedIndex));
                exchangeButtons[i] = button;
                exchangeSourceOwnedTexts[i] = sourceOwnedText;
                exchangeRouteStatusTexts[i] = routeStatusText;
                exchangeSourceIcons[i] = sourceIcon;
                exchangeTargetIcons[i] = targetIcon;
            }

            Button closeButton = CreateButton("닫기", 24, dialog.transform);
            closeButton.GetComponent<LayoutElement>().preferredHeight = 52f;
            closeButton.onClick.AddListener(CloseExchangePanel);

            BuildExchangeQuantityPanel(exchangePanel.transform);
            exchangePanel.SetActive(false);
        }

        private void OpenExchangePanel()
        {
            if (exchangePanel == null)
            {
                return;
            }

            CloseExchangeQuantityPanel();
            exchangeStatusText.text = string.Empty;
            exchangePanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            exchangeScrollRect.verticalNormalizedPosition = 1f;
        }

        private void CloseExchangePanel()
        {
            CloseExchangeQuantityPanel();
            exchangePanel.SetActive(false);
        }

        private void BuildExchangeQuantityPanel(Transform parent)
        {
            exchangeQuantityPanel = CreatePanel(
                "Exchange Quantity Overlay",
                parent,
                new Color(0.005f, 0.008f, 0.02f, 0.88f));
            RectTransform overlayRect = exchangeQuantityPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject dialog = CreatePanel(
                "Exchange Quantity Popup",
                exchangeQuantityPanel.transform,
                new Color(0.035f, 0.035f, 0.075f, 1f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.13f, 0.29f);
            dialogRect.anchorMax = new Vector2(0.87f, 0.71f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                dialog,
                new Color(0.018f, 0.035f, 0.075f, 1f),
                new Color(0.34f, 0.24f, 0.72f, 1f),
                2f);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Text title = CreateText(
                "교환 수량",
                32,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            title.color = new Color(0.94f, 0.9f, 1f, 1f);
            SetPreferredHeight(title, 44f);

            exchangeQuantityRouteText = CreateText(
                "",
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            exchangeQuantityRouteText.color = new Color(0.78f, 0.94f, 1f, 1f);
            SetPreferredHeight(exchangeQuantityRouteText, 38f);

            GameObject inputRow = new GameObject(
                "Quantity Input Row",
                typeof(RectTransform),
                typeof(LayoutElement));
            inputRow.transform.SetParent(dialog.transform, false);
            inputRow.GetComponent<LayoutElement>().preferredHeight = 68f;
            HorizontalLayoutGroup inputLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 10f;
            inputLayout.childAlignment = TextAnchor.MiddleCenter;
            inputLayout.childControlWidth = true;
            inputLayout.childControlHeight = true;
            inputLayout.childForceExpandWidth = false;
            inputLayout.childForceExpandHeight = true;

            Button decreaseButton = CreateButton("-", 32, inputRow.transform);
            SetFixedLayoutSize(decreaseButton.gameObject, 68f, 68f);
            decreaseButton.onClick.AddListener(() => AdjustExchangeQuantity(-1));

            exchangeQuantityInput = CreateIntegerInputField(inputRow.transform);
            LayoutElement inputFieldLayout = exchangeQuantityInput.GetComponent<LayoutElement>();
            inputFieldLayout.minWidth = 180f;
            inputFieldLayout.flexibleWidth = 1f;
            inputFieldLayout.preferredHeight = 68f;
            exchangeQuantityInput.onValueChanged.AddListener(RefreshExchangeQuantityPreview);

            Button increaseButton = CreateButton("+", 32, inputRow.transform);
            SetFixedLayoutSize(increaseButton.gameObject, 68f, 68f);
            increaseButton.onClick.AddListener(() => AdjustExchangeQuantity(1));

            exchangeQuantityPreviewText = CreateText(
                "",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            exchangeQuantityPreviewText.color = new Color(0.66f, 0.82f, 0.94f, 1f);
            SetPreferredHeight(exchangeQuantityPreviewText, 34f);

            GameObject presetRow = new GameObject(
                "Quantity Presets",
                typeof(RectTransform),
                typeof(LayoutElement));
            presetRow.transform.SetParent(dialog.transform, false);
            presetRow.GetComponent<LayoutElement>().preferredHeight = 52f;
            HorizontalLayoutGroup presetLayout =
                presetRow.AddComponent<HorizontalLayoutGroup>();
            presetLayout.spacing = 10f;
            presetLayout.childControlWidth = true;
            presetLayout.childControlHeight = true;
            presetLayout.childForceExpandWidth = true;
            presetLayout.childForceExpandHeight = true;

            Button preset10Button = CreateButton("10개 교환", 17, presetRow.transform);
            preset10Button.onClick.AddListener(() => ApplyExchangeQuantityPreset(10));

            Button preset100Button = CreateButton("100개 교환", 17, presetRow.transform);
            preset100Button.onClick.AddListener(() => ApplyExchangeQuantityPreset(100));

            Button presetMaxButton = CreateButton("MAX 교환", 17, presetRow.transform);
            presetMaxButton.image.color = new Color(0.1f, 0.07f, 0.2f, 1f);
            presetMaxButton.onClick.AddListener(ApplyMaxExchangeQuantity);

            exchangeQuantityStatusText = CreateText(
                "",
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            SetPreferredHeight(exchangeQuantityStatusText, 30f);

            GameObject actionRow = new GameObject(
                "Quantity Actions",
                typeof(RectTransform),
                typeof(LayoutElement));
            actionRow.transform.SetParent(dialog.transform, false);
            actionRow.GetComponent<LayoutElement>().preferredHeight = 62f;
            HorizontalLayoutGroup actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 12f;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = true;

            Button cancelButton = CreateButton("취소", 22, actionRow.transform);
            cancelButton.onClick.AddListener(CloseExchangeQuantityPanel);

            exchangeQuantityConfirmButton = CreateButton("교환", 22, actionRow.transform);
            exchangeQuantityConfirmButton.image.color = new Color(0.16f, 0.07f, 0.24f, 1f);
            exchangeQuantityConfirmButton.onClick.AddListener(ConfirmExchangeQuantity);

            BuildExchangeNumpad(exchangeQuantityPanel.transform);

            exchangeQuantityPanel.SetActive(false);
        }

        private void BuildExchangeNumpad(Transform parent)
        {
            exchangeNumpadPanel = CreatePanel(
                "Exchange Numpad Overlay",
                parent,
                new Color(0.004f, 0.006f, 0.016f, 0.62f));
            RectTransform overlayRect = exchangeNumpadPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            // Tapping outside the keypad commits the current value and closes it.
            Button dismissButton = exchangeNumpadPanel.AddComponent<Button>();
            dismissButton.transition = Selectable.Transition.None;
            dismissButton.onClick.AddListener(CloseExchangeNumpad);

            GameObject sheet = CreatePanel(
                "Numpad Sheet",
                exchangeNumpadPanel.transform,
                new Color(0.03f, 0.04f, 0.085f, 1f));
            RectTransform sheetRect = sheet.GetComponent<RectTransform>();
            sheetRect.anchorMin = new Vector2(0.06f, 0.05f);
            sheetRect.anchorMax = new Vector2(0.94f, 0.5f);
            sheetRect.offsetMin = Vector2.zero;
            sheetRect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                sheet,
                new Color(0.016f, 0.03f, 0.07f, 1f),
                new Color(0.2f, 0.42f, 0.78f, 1f),
                2f);

            Text heading = CreateText(
                "교환 수량 입력",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                sheet.transform);
            RectTransform headingRect = heading.GetComponent<RectTransform>();
            headingRect.anchorMin = new Vector2(0.06f, 0.88f);
            headingRect.anchorMax = new Vector2(0.94f, 0.99f);
            headingRect.offsetMin = Vector2.zero;
            headingRect.offsetMax = Vector2.zero;
            heading.color = new Color(0.7f, 0.84f, 1f, 1f);

            string[][] rows =
            {
                new[] { "7", "8", "9" },
                new[] { "4", "5", "6" },
                new[] { "1", "2", "3" },
                new[] { "C", "0", "⌫" }
            };
            const float top = 0.86f;
            const float bottom = 0.18f;
            float rowHeight = (top - bottom) / rows.Length;
            const float left = 0.04f;
            const float right = 0.96f;
            float columnWidth = (right - left) / 3f;
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string key = rows[r][c];
                    Button keyButton = CreateButton(key, 30, sheet.transform);
                    if (key == "C" || key == "⌫")
                    {
                        keyButton.image.color = new Color(0.07f, 0.05f, 0.12f, 1f);
                    }

                    RectTransform keyRect = keyButton.GetComponent<RectTransform>();
                    float yMax = top - r * rowHeight;
                    float yMin = yMax - rowHeight;
                    float xMin = left + c * columnWidth;
                    float xMax = xMin + columnWidth;
                    keyRect.anchorMin = new Vector2(xMin + 0.012f, yMin + 0.014f);
                    keyRect.anchorMax = new Vector2(xMax - 0.012f, yMax - 0.014f);
                    keyRect.offsetMin = Vector2.zero;
                    keyRect.offsetMax = Vector2.zero;
                    string captured = key;
                    keyButton.onClick.AddListener(() => OnExchangeNumpadKey(captured));
                }
            }

            Button confirmButton = CreateButton("확인", 22, sheet.transform);
            RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.04f, 0.03f);
            confirmRect.anchorMax = new Vector2(0.96f, 0.15f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirmButton.image.color = new Color(0.07f, 0.18f, 0.32f, 1f);
            confirmButton.onClick.AddListener(CloseExchangeNumpad);

            exchangeNumpadPanel.SetActive(false);
        }

        private void OpenExchangeNumpad()
        {
            if (exchangeNumpadPanel != null)
            {
                exchangeNumpadPanel.SetActive(true);
            }
        }

        private void CloseExchangeNumpad()
        {
            if (exchangeNumpadPanel == null)
            {
                return;
            }

            int quantity;
            if (exchangeQuantityInput != null &&
                (!int.TryParse(exchangeQuantityInput.text, out quantity) || quantity < 1))
            {
                exchangeQuantityInput.text = "1";
            }

            exchangeNumpadPanel.SetActive(false);
        }

        private void OnExchangeNumpadKey(string key)
        {
            if (exchangeQuantityInput == null)
            {
                return;
            }

            string current = exchangeQuantityInput.text ?? string.Empty;
            if (key == "C")
            {
                current = string.Empty;
            }
            else if (key == "⌫")
            {
                current = current.Length > 0
                    ? current.Substring(0, current.Length - 1)
                    : string.Empty;
            }
            else
            {
                // Drop a lone leading zero so digits read naturally (0 -> 5, not 05).
                if (current == "0")
                {
                    current = string.Empty;
                }

                if (current.Length < 9)
                {
                    current += key;
                }
            }

            exchangeQuantityInput.text = current;
        }

        private InputField CreateIntegerInputField(Transform parent)
        {
            GameObject inputObject = new GameObject(
                "Exchange Quantity Input",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField),
                typeof(LayoutElement));
            inputObject.transform.SetParent(parent, false);
            inputObject.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.075f, 1f);
            ApplyCanvasFrame(
                inputObject,
                new Color(0.012f, 0.045f, 0.08f, 1f),
                new Color(0.12f, 0.48f, 0.68f, 1f),
                2f);

            Text inputText = CreateText(
                "",
                28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                inputObject.transform);
            RectTransform inputTextRect = inputText.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(12f, 6f);
            inputTextRect.offsetMax = new Vector2(-12f, -6f);
            inputText.color = Color.white;

            Text placeholder = CreateText(
                "1 이상 입력",
                22,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                inputObject.transform);
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 6f);
            placeholderRect.offsetMax = new Vector2(-12f, -6f);
            placeholder.color = new Color(0.55f, 0.62f, 0.7f, 0.8f);

            InputField inputField = inputObject.GetComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.characterLimit = 9;
            inputField.caretColor = new Color(0.36f, 0.86f, 1f, 1f);
            inputField.selectionColor = new Color(0.2f, 0.45f, 0.65f, 0.7f);
            // Direct entry is handled by the in-game number pad (consistent on every
            // platform, no OS keyboard), so the field itself is display-only.
            inputField.readOnly = true;

            GameObject tapCatcher = new GameObject(
                "Tap To Type",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            tapCatcher.transform.SetParent(inputObject.transform, false);
            RectTransform tapRect = tapCatcher.GetComponent<RectTransform>();
            tapRect.anchorMin = Vector2.zero;
            tapRect.anchorMax = Vector2.one;
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;
            Image tapImage = tapCatcher.GetComponent<Image>();
            tapImage.color = new Color(0f, 0f, 0f, 0f);
            Button tapButton = tapCatcher.GetComponent<Button>();
            tapButton.transition = Selectable.Transition.None;
            tapButton.onClick.AddListener(OpenExchangeNumpad);
            return inputField;
        }

        private void OpenExchangeQuantityPanel(int routeIndex)
        {
            StarForgeMaterialExchangeRoute route = exchangeService.GetRoute(routeIndex);
            if (exchangeQuantityPanel == null || route == null)
            {
                return;
            }

            pendingExchangeRouteIndex = routeIndex;
            exchangeQuantityRouteText.text =
                StarForgeCurrencyNames.GetDisplayName(route.sourceType) +
                " " +
                route.sourceAmount +
                "개 → " +
                StarForgeCurrencyNames.GetDisplayName(route.targetType) +
                " " +
                route.targetAmount +
                "개";
            exchangeQuantityStatusText.text = string.Empty;
            exchangeQuantityInput.text = "1";
            RefreshExchangeQuantityPreview(exchangeQuantityInput.text);
            CloseExchangeNumpad();
            exchangeQuantityPanel.SetActive(true);
        }

        private void CloseExchangeQuantityPanel()
        {
            pendingExchangeRouteIndex = -1;
            CloseExchangeNumpad();
            if (exchangeQuantityPanel != null)
            {
                exchangeQuantityPanel.SetActive(false);
            }
        }

        // Largest number of exchanges that is actually executable right now, bounded
        // by owned source materials, the daily limit, and the target's storage cap.
        private int ComputeMaxExchangeQuantity(int routeIndex)
        {
            StarForgeMaterialExchangeRoute route = exchangeService.GetRoute(routeIndex);
            if (route == null ||
                lastSaveData == null ||
                route.sourceAmount <= 0 ||
                route.targetAmount <= 0 ||
                !exchangeService.IsUnlocked(lastSaveData, routeIndex))
            {
                return 0;
            }

            long maxBySource =
                (long)lastSaveData.GetCurrency(route.sourceType) / route.sourceAmount;
            long maxByDaily = exchangeService.GetRemainingDailyExchanges(
                lastSaveData, routeIndex, DateTime.Now);
            long maxByTargetCap =
                ((long)int.MaxValue - lastSaveData.GetCurrency(route.targetType)) /
                route.targetAmount;

            long max = Math.Min(maxBySource, Math.Min(maxByDaily, maxByTargetCap));
            max = Math.Max(0L, Math.Min(max, 999999999L));
            return (int)max;
        }

        private void ApplyExchangeQuantityPreset(int requested)
        {
            int max = ComputeMaxExchangeQuantity(pendingExchangeRouteIndex);
            int value = max >= 1 ? Math.Min(requested, max) : 1;
            exchangeQuantityInput.text = Math.Max(1, value).ToString();
        }

        private void ApplyMaxExchangeQuantity()
        {
            int max = ComputeMaxExchangeQuantity(pendingExchangeRouteIndex);
            exchangeQuantityInput.text = Math.Max(1, max).ToString();
        }

        private void AdjustExchangeQuantity(int delta)
        {
            int quantity;
            if (!int.TryParse(exchangeQuantityInput.text, out quantity))
            {
                quantity = 1;
            }

            long adjusted = Math.Max(
                1L,
                Math.Min((long)quantity + delta, 999999999L));
            exchangeQuantityInput.text = adjusted.ToString();
        }

        private void RefreshExchangeQuantityPreview(string value)
        {
            StarForgeMaterialExchangeRoute route =
                exchangeService.GetRoute(pendingExchangeRouteIndex);
            int quantity;
            if (route == null ||
                !int.TryParse(value, out quantity) ||
                quantity <= 0)
            {
                exchangeQuantityPreviewText.text = "교환 수량을 1 이상 입력하세요.";
                return;
            }

            long sourceTotal = (long)route.sourceAmount * quantity;
            long targetTotal = (long)route.targetAmount * quantity;
            exchangeQuantityPreviewText.text =
                "총 필요 " +
                StarForgeFormat.Number(sourceTotal) +
                "개 / 총 획득 " +
                StarForgeFormat.Number(targetTotal) +
                "개";
            exchangeQuantityStatusText.text = string.Empty;
        }

        private void ConfirmExchangeQuantity()
        {
            int quantity;
            if (pendingExchangeRouteIndex < 0 ||
                !int.TryParse(exchangeQuantityInput.text, out quantity) ||
                quantity <= 0)
            {
                exchangeQuantityStatusText.text = "교환 수량을 1 이상 입력하세요.";
                exchangeQuantityStatusText.color = new Color(1f, 0.45f, 0.48f, 1f);
                return;
            }

            exchangeQuantityStatusText.text = string.Empty;
            MaterialExchangeRequested?.Invoke(pendingExchangeRouteIndex, quantity);
        }

        private void RefreshExchangePanel(StarForgeSaveData saveData, bool isBusy)
        {
            if (exchangeOpenButton != null)
            {
                exchangeOpenButton.interactable = !isBusy;
            }

            if (exchangeQuantityConfirmButton != null)
            {
                exchangeQuantityConfirmButton.interactable = !isBusy;
            }

            DateTime localNow = DateTime.Now;
            for (int i = 0; i < exchangeButtons.Length; i++)
            {
                Button button = exchangeButtons[i];
                Text sourceOwnedText = exchangeSourceOwnedTexts[i];
                Text routeStatusText = exchangeRouteStatusTexts[i];
                StarForgeMaterialExchangeRoute route = exchangeService.GetRoute(i);
                if (button == null ||
                    sourceOwnedText == null ||
                    routeStatusText == null ||
                    route == null)
                {
                    continue;
                }

                bool isUnlocked = exchangeService.IsUnlocked(saveData, i);
                int sourceOwned = saveData.GetCurrency(route.sourceType);
                int remaining = exchangeService.GetRemainingDailyExchanges(saveData, i, localNow);
                button.interactable = !isBusy &&
                                      exchangeService.CanExchange(saveData, i, localNow);
                sourceOwnedText.text = "보유 x" + StarForgeFormat.Number(sourceOwned);
                sourceOwnedText.color = sourceOwned >= route.sourceAmount
                    ? new Color(0.36f, 0.86f, 1f, 1f)
                    : new Color(1f, 0.45f, 0.48f, 1f);
                routeStatusText.text = BuildExchangeRouteStatus(route, isUnlocked, remaining);
                routeStatusText.color = isUnlocked
                    ? new Color(0.9f, 0.96f, 1f, 1f)
                    : new Color(0.5f, 0.54f, 0.64f, 1f);

                Color iconColor = isUnlocked
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.32f);
                exchangeSourceIcons[i].color = iconColor;
                exchangeTargetIcons[i].color = iconColor;
            }
        }

        private static string BuildExchangeRouteStatus(
            StarForgeMaterialExchangeRoute route,
            bool isUnlocked,
            int remaining)
        {
            if (!isUnlocked)
            {
                return "잠김 · " + route.requiredHighestLevel + "강 최초 도달 시 해금";
            }

            if (route.dailyLimit > 0)
            {
                return "오늘 남은 교환 " + remaining + "회";
            }

            return "교환 가능";
        }

        private void CreateExchangeMaterialCard(
            string objectName,
            StarForgeCurrencyType currencyType,
            int amount,
            string detail,
            Transform parent,
            out Text detailText,
            out Image icon)
        {
            GameObject card = CreatePanel(
                objectName,
                parent,
                new Color(0.04f, 0.075f, 0.095f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.minWidth = 185f;
            cardLayout.preferredWidth = 215f;
            cardLayout.flexibleWidth = 1f;
            cardLayout.minHeight = 68f;
            cardLayout.preferredHeight = 68f;
            cardLayout.flexibleHeight = 0f;
            ApplyCanvasFrame(
                card,
                new Color(0.02f, 0.055f, 0.085f, 1f),
                new Color(0.1f, 0.38f, 0.52f, 0.96f),
                2f);

            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            iconObject.transform.SetParent(card.transform, false);
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.minWidth = 54f;
            iconLayout.preferredWidth = 54f;
            iconLayout.minHeight = 54f;
            iconLayout.preferredHeight = 54f;
            icon = iconObject.GetComponent<Image>();
            icon.sprite = GetMaterialIcon((int)currencyType);
            icon.preserveAspect = true;

            GameObject textColumn = new GameObject(
                "Material Text",
                typeof(RectTransform),
                typeof(LayoutElement));
            textColumn.transform.SetParent(card.transform, false);
            LayoutElement textColumnLayout = textColumn.GetComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;
            textColumnLayout.minHeight = 60f;

            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 0f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            Text nameText = CreateText(
                StarForgeCurrencyNames.GetDisplayName(currencyType) + " x" + amount,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                textColumn.transform);
            nameText.color = new Color(0.78f, 0.94f, 1f, 1f);
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 12;
            nameText.resizeTextMaxSize = 17;
            SetPreferredHeight(nameText, 34f);

            detailText = CreateText(
                detail,
                15,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                textColumn.transform);
            detailText.color = new Color(0.58f, 0.7f, 0.82f, 1f);
            SetPreferredHeight(detailText, 24f);
        }

        private void BuildCollectionPanel(RectTransform root, StarForgeBalance balance)
        {
            collectionPanel = CreatePanel(
                "Star Collection Overlay",
                root,
                new Color(0.001f, 0.002f, 0.008f, 1f));
            RectTransform overlayRect = collectionPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Detail root holds the existing 3D showcase (shown when a card is tapped).
            collectionDetailRoot = CreatePanel(
                "Collection Detail",
                collectionPanel.transform,
                new Color(0f, 0f, 0f, 0f));
            collectionDetailRoot.GetComponent<Image>().raycastTarget = false;
            StretchRect(collectionDetailRoot.GetComponent<RectTransform>(), Vector2.zero);

            GameObject previewObject = new GameObject(
                "Collection Space View",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(StarForgeDragRotationInput));
            previewObject.transform.SetParent(collectionDetailRoot.transform, false);
            RectTransform previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            collectionPlanetImage = previewObject.GetComponent<RawImage>();
            collectionPlanetImage.color = Color.white;
            collectionPlanetImage.raycastTarget = true;
            collectionPlanetImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            StarForgeDragRotationInput collectionDragInput =
                previewObject.GetComponent<StarForgeDragRotationInput>();
            collectionDragInput.Dragged += delta =>
            {
                if (!collectionTransitioning && collectionPreview != null)
                {
                    collectionPreview.OrbitCurrentCamera(delta);
                }
            };

            GameObject collectionPreviewObject = new GameObject("StarForge Collection Preview");
            collectionPreview = collectionPreviewObject.AddComponent<StarForgeCollectionPreview>();
            collectionPlanetImage.texture = collectionPreview.OutputTexture;
            collectionPreview.Hide();

            Button exitButton = CreateButton("뒤로", 22, collectionDetailRoot.transform);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.025f, 0.875f);
            exitRect.anchorMax = new Vector2(0.205f, 0.94f);
            exitRect.offsetMin = Vector2.zero;
            exitRect.offsetMax = Vector2.zero;
            exitButton.image.color = new Color(0.005f, 0.01f, 0.025f, 0.62f);
            StyleCollectionSpaceButton(
                exitButton,
                new Color(0.16f, 0.72f, 1f, 1f));
            exitButton.onClick.AddListener(ShowCollectionBrowse);

            string[] shapeLabels = { "일반 별", "하트별", "고양이별" };
            for (int i = 0; i < collectionShapeButtons.Length; i++)
            {
                int shapeIndex = i;
                Button shapeButton = CreateButton(
                    shapeLabels[i],
                    17,
                    collectionDetailRoot.transform);
                collectionShapeButtons[i] = shapeButton;
                collectionShapeButtonTexts[i] = shapeButton.GetComponentInChildren<Text>();

                RectTransform shapeRect = shapeButton.GetComponent<RectTransform>();
                float left = 0.215f + i * 0.19f;
                shapeRect.anchorMin = new Vector2(left, 0.875f);
                shapeRect.anchorMax = new Vector2(left + 0.18f, 0.94f);
                shapeRect.offsetMin = Vector2.zero;
                shapeRect.offsetMax = Vector2.zero;
                StyleCollectionSpaceButton(
                    shapeButton,
                    new Color(0.58f, 0.28f, 1f, 1f));
                collectionShapeLockIcons[i] =
                    i == 0 ? null : CreateCollectionLockIcon(shapeButton.transform);
                shapeButton.onClick.AddListener(
                    () => SelectCollectionShape((StarForgePlanetShape)shapeIndex));
            }

            collectionSkipToggle = CreateCollectionSkipToggle(collectionDetailRoot.transform);
            RectTransform skipToggleRect =
                collectionSkipToggle.GetComponent<RectTransform>();
            skipToggleRect.anchorMin = new Vector2(0.795f, 0.875f);
            skipToggleRect.anchorMax = new Vector2(0.975f, 0.94f);
            skipToggleRect.offsetMin = Vector2.zero;
            skipToggleRect.offsetMax = Vector2.zero;
            collectionSkipToggle.SetIsOnWithoutNotify(false);
            collectionSkipToggle.onValueChanged.AddListener(
                OnCollectionSkipToggleChanged);

            collectionTitleText = CreateText(
                "",
                30,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                collectionDetailRoot.transform);
            RectTransform titleRect = collectionTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.12f, 0.76f);
            titleRect.anchorMax = new Vector2(0.88f, 0.825f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            collectionTitleText.resizeTextForBestFit = true;
            collectionTitleText.resizeTextMinSize = 18;
            collectionTitleText.resizeTextMaxSize = 30;

            collectionPreviousButton = CreateButton("<", 48, collectionDetailRoot.transform);
            RectTransform previousRect = collectionPreviousButton.GetComponent<RectTransform>();
            previousRect.anchorMin = new Vector2(0.015f, 0.405f);
            previousRect.anchorMax = new Vector2(0.13f, 0.515f);
            previousRect.offsetMin = Vector2.zero;
            previousRect.offsetMax = Vector2.zero;
            StyleCollectionArrowButton(collectionPreviousButton);
            collectionPreviousButton.onClick.AddListener(() => NavigateCollection(-1));

            collectionNextButton = CreateButton(">", 48, collectionDetailRoot.transform);
            RectTransform nextRect = collectionNextButton.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.87f, 0.405f);
            nextRect.anchorMax = new Vector2(0.985f, 0.515f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;
            StyleCollectionArrowButton(collectionNextButton);
            collectionNextButton.onClick.AddListener(() => NavigateCollection(1));

            collectionMaxUnlockedLevel = 0;
            RefreshCollectionShapeButtons();
            BuildCollectionBrowse(collectionPanel.transform);
            collectionDetailRoot.SetActive(false);
            collectionPanel.SetActive(false);
        }

        private static readonly string[] CollectionShapeLabels =
            { "일반 별", "하트별", "고양이별" };

        private void BuildCollectionBrowse(Transform parent)
        {
            collectionBrowseRoot = CreatePanel(
                "Collection Browse",
                parent,
                new Color(0.004f, 0.008f, 0.02f, 1f));
            StretchRect(collectionBrowseRoot.GetComponent<RectTransform>(), Vector2.zero);

            Text header = CreateText(
                "도감", 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                collectionBrowseRoot.transform);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.2f, 0.905f);
            headerRect.anchorMax = new Vector2(0.8f, 0.975f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.color = new Color(1f, 0.9f, 0.55f, 1f);

            Button closeButton = CreateButton("나가기", 20, collectionBrowseRoot.transform);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.78f, 0.905f);
            closeRect.anchorMax = new Vector2(0.97f, 0.97f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            closeButton.onClick.AddListener(CloseCollectionPanel);

            collectionPlanetTabButton = CreateButton(
                "행성", 20, collectionBrowseRoot.transform);
            RectTransform planetTabRect = collectionPlanetTabButton.GetComponent<RectTransform>();
            planetTabRect.anchorMin = new Vector2(0.06f, 0.83f);
            planetTabRect.anchorMax = new Vector2(0.5f, 0.89f);
            planetTabRect.offsetMin = Vector2.zero;
            planetTabRect.offsetMax = Vector2.zero;
            collectionPlanetTabButton.onClick.AddListener(
                () => SelectCollectionBrowseTab(true));

            collectionAchievementTabButton = CreateButton(
                "업적", 20, collectionBrowseRoot.transform);
            RectTransform achTabRect = collectionAchievementTabButton.GetComponent<RectTransform>();
            achTabRect.anchorMin = new Vector2(0.5f, 0.83f);
            achTabRect.anchorMax = new Vector2(0.94f, 0.89f);
            achTabRect.offsetMin = Vector2.zero;
            achTabRect.offsetMax = Vector2.zero;
            collectionAchievementTabButton.onClick.AddListener(
                () => SelectCollectionBrowseTab(false));

            BuildCollectionPlanetTab(collectionBrowseRoot.transform);
            BuildCollectionAchievementTab(collectionBrowseRoot.transform);
            BuildCollectionAchievementClaimBadge(
                collectionBrowseRoot.GetComponent<RectTransform>());
        }

        private void BuildCollectionPlanetTab(Transform parent)
        {
            collectionPlanetTabRoot = CreatePanel(
                "Planet Tab", parent, new Color(0f, 0f, 0f, 0f));
            collectionPlanetTabRoot.GetComponent<Image>().raycastTarget = false;
            RectTransform rootRect = collectionPlanetTabRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = new Vector2(1f, 0.81f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            float fw = 0.88f / 3f;
            for (int i = 0; i < collectionBrowseShapeButtons.Length; i++)
            {
                int idx = i;
                Button b = CreateButton(
                    CollectionShapeLabels[i], 16, collectionPlanetTabRoot.transform);
                collectionBrowseShapeButtons[i] = b;
                RectTransform r = b.GetComponent<RectTransform>();
                float left = 0.06f + i * fw;
                r.anchorMin = new Vector2(left + 0.006f, 0.9f);
                r.anchorMax = new Vector2(left + fw - 0.006f, 0.97f);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                b.onClick.AddListener(
                    () => SelectBrowseShape((StarForgePlanetShape)idx));
            }

            collectionPlanetGridContent = CreateCollectionScrollArea(
                collectionPlanetTabRoot.transform,
                new Vector2(0.03f, 0.02f),
                new Vector2(0.97f, 0.87f),
                true);
        }

        private void BuildCollectionAchievementTab(Transform parent)
        {
            collectionAchievementTabRoot = CreatePanel(
                "Achievement Tab", parent, new Color(0f, 0f, 0f, 0f));
            collectionAchievementTabRoot.GetComponent<Image>().raycastTarget = false;
            RectTransform rootRect = collectionAchievementTabRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = new Vector2(1f, 0.81f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            collectionAchievementProgressText = CreateText(
                "업적 0 / 0", 18, FontStyle.Bold, TextAnchor.MiddleCenter,
                collectionAchievementTabRoot.transform);
            RectTransform progRect =
                collectionAchievementProgressText.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.06f, 0.9f);
            progRect.anchorMax = new Vector2(0.72f, 0.975f);
            progRect.offsetMin = Vector2.zero;
            progRect.offsetMax = Vector2.zero;
            collectionAchievementProgressText.color = new Color(1f, 0.88f, 0.5f, 1f);

            achievementClaimAllButton = CreateButton(
                "모두 수령", 16, collectionAchievementTabRoot.transform);
            RectTransform claimAllRect =
                achievementClaimAllButton.GetComponent<RectTransform>();
            claimAllRect.anchorMin = new Vector2(0.74f, 0.905f);
            claimAllRect.anchorMax = new Vector2(0.94f, 0.97f);
            claimAllRect.offsetMin = Vector2.zero;
            claimAllRect.offsetMax = Vector2.zero;
            StyleCollectionSpaceButton(
                achievementClaimAllButton,
                new Color(1f, 0.65f, 0.18f, 1f));
            achievementClaimAllButton.onClick.AddListener(
                () => AchievementClaimAllRequested?.Invoke());

            collectionAchievementContent = CreateCollectionScrollArea(
                collectionAchievementTabRoot.transform,
                new Vector2(0.03f, 0.02f),
                new Vector2(0.97f, 0.87f),
                false);
        }

        private RectTransform CreateCollectionScrollArea(
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool grid)
        {
            GameObject scrollObj = CreatePanel(
                "Scroll", parent, new Color(0.006f, 0.012f, 0.03f, 0.55f));
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = anchorMin;
            scrollRect.anchorMax = anchorMax;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 26f;
            sr.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreatePanel(
                "Viewport", scrollObj.transform, new Color(0f, 0f, 0f, 0.001f));
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            if (grid)
            {
                GridLayoutGroup g = content.AddComponent<GridLayoutGroup>();
                g.padding = new RectOffset(8, 8, 10, 10);
                // Smaller, name-only cards so 3 columns never clip on narrow phones.
                g.cellSize = new Vector2(146f, 100f);
                g.spacing = new Vector2(10f, 12f);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 3;
                g.childAlignment = TextAnchor.UpperCenter;
            }
            else
            {
                VerticalLayoutGroup v = content.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(10, 10, 10, 10);
                v.spacing = 10f;
                v.childControlWidth = true;
                v.childForceExpandWidth = true;
                v.childControlHeight = true;
                v.childForceExpandHeight = false;
                v.childAlignment = TextAnchor.UpperCenter;
            }

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRect;
            sr.content = contentRect;
            return contentRect;
        }

        private void ShowCollectionBrowse()
        {
            if (collectionPreview != null)
            {
                collectionPreview.Hide();
            }

            collectionTransitioning = false;
            if (collectionDetailRoot != null)
            {
                collectionDetailRoot.SetActive(false);
            }

            if (collectionBrowseRoot != null)
            {
                collectionBrowseRoot.SetActive(true);
            }

            if (!IsCollectionShapeDiscovered(collectionBrowseShape))
            {
                collectionBrowseShape = StarForgePlanetShape.Default;
            }

            SelectCollectionBrowseTab(true);
        }

        private void ShowCollectionDetail(StarForgePlanetShape shape, int level)
        {
            collectionShape = shape;
            collectionMaxUnlockedLevel = GetCollectionShapeMaxLevel(shape);
            collectionCurrentLevel = Mathf.Clamp(level, 0, collectionMaxUnlockedLevel);
            RefreshCollectionShapeButtons();
            collectionPreview.ResetCameraOrbit();
            if (collectionBrowseRoot != null)
            {
                collectionBrowseRoot.SetActive(false);
            }

            if (collectionDetailRoot != null)
            {
                collectionDetailRoot.SetActive(true);
            }

            ShowCurrentCollectionStage();
        }

        private void SelectCollectionBrowseTab(bool planet)
        {
            if (collectionPlanetTabRoot != null)
            {
                collectionPlanetTabRoot.SetActive(planet);
            }

            if (collectionAchievementTabRoot != null)
            {
                collectionAchievementTabRoot.SetActive(!planet);
            }

            HighlightTabButton(collectionPlanetTabButton, planet);
            HighlightTabButton(collectionAchievementTabButton, !planet);

            if (planet)
            {
                RefreshPlanetCards();
            }
            else
            {
                RefreshAchievementList();
            }
        }

        private static void HighlightTabButton(Button button, bool active)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            button.image.color = active
                ? new Color(0.12f, 0.32f, 0.58f, 1f)
                : new Color(0.02f, 0.06f, 0.13f, 0.98f);
        }

        private void SelectBrowseShape(StarForgePlanetShape shape)
        {
            if (!IsCollectionShapeDiscovered(shape))
            {
                UpdateBrowseShapeButtons();
                return;
            }

            collectionBrowseShape = shape;
            RefreshPlanetCards();
        }

        private void UpdateBrowseShapeButtons()
        {
            for (int i = 0; i < collectionBrowseShapeButtons.Length; i++)
            {
                Button b = collectionBrowseShapeButtons[i];
                if (b == null)
                {
                    continue;
                }

                bool discovered = IsCollectionShapeDiscovered((StarForgePlanetShape)i);
                bool active = discovered &&
                              (StarForgePlanetShape)i == collectionBrowseShape;
                b.interactable = discovered;
                Text label = b.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = discovered ? CollectionShapeLabels[i] : "???";
                }

                if (b.image != null)
                {
                    b.image.color = active
                        ? new Color(0.12f, 0.32f, 0.58f, 1f)
                        : new Color(0.02f, 0.06f, 0.13f, 0.98f);
                }
            }
        }

        private void RefreshPlanetCards()
        {
            if (collectionPlanetGridContent == null || balanceRef == null)
            {
                return;
            }

            for (int i = collectionPlanetGridContent.childCount - 1; i >= 0; i--)
            {
                Destroy(collectionPlanetGridContent.GetChild(i).gameObject);
            }

            int unlockedMax = IsCollectionShapeDiscovered(collectionBrowseShape)
                ? GetCollectionShapeMaxLevel(collectionBrowseShape)
                : -1;
            for (int level = 0; level <= balanceRef.maxLevel; level++)
            {
                CreatePlanetCard(level, collectionBrowseShape, level <= unlockedMax);
            }

            UpdateBrowseShapeButtons();
            LayoutRebuilder.ForceRebuildLayoutImmediate(collectionPlanetGridContent);
        }

        private Sprite GetCollectionPlanetSprite()
        {
            if (collectionPlanetSprite == null)
            {
                Texture2D texture = StarForgeVisualLibrary.SoftCircleTexture;
                collectionPlanetSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }

            return collectionPlanetSprite;
        }

        private void CreatePlanetCard(
            int level, StarForgePlanetShape shape, bool unlocked)
        {
            GameObject card = CreatePanel(
                "Planet Card " + level,
                collectionPlanetGridContent,
                new Color(0.02f, 0.04f, 0.08f, 0.95f));
            ApplyCanvasFrame(
                card,
                new Color(0.015f, 0.035f, 0.075f, 0.98f),
                unlocked
                    ? new Color(0.2f, 0.5f, 0.85f, 0.95f)
                    : new Color(0.2f, 0.24f, 0.32f, 0.7f),
                2f);

            // Name only (no planet orb) — tapping opens the existing 3D detail view.
            Text nameText = CreateText(
                unlocked
                    ? level + "강\n" + balanceRef.GetStageName(level, shape)
                    : level + "강\n???",
                20, FontStyle.Bold, TextAnchor.MiddleCenter, card.transform);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.08f, 0.1f);
            nameRect.anchorMax = new Vector2(0.92f, 0.9f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            nameText.color = unlocked
                ? new Color(0.92f, 0.96f, 1f, 1f)
                : new Color(0.55f, 0.6f, 0.68f, 1f);
            nameText.raycastTarget = false;
            nameText.lineSpacing = 1f;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 12;
            nameText.resizeTextMaxSize = 22;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;

            Button button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = unlocked;
            if (unlocked)
            {
                int capturedLevel = level;
                StarForgePlanetShape capturedShape = shape;
                button.onClick.AddListener(
                    () => ShowCollectionDetail(capturedShape, capturedLevel));
            }
        }

        public void RefreshAchievementList()
        {
            if (collectionAchievementContent == null)
            {
                return;
            }

            for (int i = collectionAchievementContent.childCount - 1; i >= 0; i--)
            {
                Destroy(collectionAchievementContent.GetChild(i).gameObject);
            }

            StarForgeAchievementDefinition[] defs =
                achievementService.GetDefinitions();
            int completedCount = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                if (lastSaveData != null &&
                    lastSaveData.HasCompletedAchievement(defs[i].id))
                {
                    completedCount++;
                }
            }

            if (collectionAchievementProgressText != null)
            {
                collectionAchievementProgressText.text =
                    "업적 " + completedCount + " / " + defs.Length + " 달성";
            }

            // 섹션 없이 한 줄로 정렬: 완료(미수령) 맨 위 → 미완료 → 수령완료 맨 아래.
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    StarForgeAchievementDefinition def = defs[i];
                    bool completed = lastSaveData != null &&
                                     lastSaveData.HasCompletedAchievement(def.id);
                    bool claimed = lastSaveData != null &&
                                   lastSaveData.HasClaimedAchievement(def.id);
                    int rank = claimed ? 2 : completed ? 0 : 1;
                    if (rank == pass)
                    {
                        CreateAchievementRow(def, completed);
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(collectionAchievementContent);
        }

        private void AppendAchievementSection(
            string title,
            StarForgeAchievementDefinition[] defs,
            params StarForgeAchievementKind[] kinds)
        {
            bool headerAdded = false;
            for (int sortGroup = 0; sortGroup < 3; sortGroup++)
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    StarForgeAchievementDefinition def = defs[i];
                    if (Array.IndexOf(kinds, def.kind) < 0 ||
                        GetAchievementSortGroup(def) != sortGroup)
                    {
                        continue;
                    }

                    if (!headerAdded)
                    {
                        Text header = CreateText(
                            title, 16, FontStyle.Bold, TextAnchor.MiddleLeft,
                            collectionAchievementContent);
                        LayoutElement headerLayout =
                            header.gameObject.AddComponent<LayoutElement>();
                        headerLayout.minHeight = 32f;
                        headerLayout.preferredHeight = 32f;
                        header.color = new Color(0.55f, 0.7f, 0.95f, 1f);
                        headerAdded = true;
                    }

                    bool completed = lastSaveData != null &&
                                     lastSaveData.HasCompletedAchievement(def.id);
                    CreateAchievementRow(def, completed);
                }
            }
        }

        private void CreateAchievementRow(
            StarForgeAchievementDefinition def, bool completed)
        {
            bool claimable = IsAchievementClaimable(def);
            bool claimed = IsAchievementClaimed(def);
            Color backgroundColor = claimed
                ? new Color(0.006f, 0.008f, 0.014f, 0.98f)
                : claimable
                    ? new Color(0.025f, 0.075f, 0.045f, 0.98f)
                    : new Color(0.018f, 0.04f, 0.075f, 0.96f);
            Color borderColor = claimed
                ? new Color(0.18f, 0.2f, 0.25f, 0.85f)
                : claimable
                    ? new Color(0.32f, 0.9f, 0.5f, 0.95f)
                    : new Color(0.16f, 0.5f, 1f, 0.82f);
            GameObject row = CreatePanel(
                "Achievement " + def.id,
                collectionAchievementContent,
                backgroundColor);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 98f;
            rowLayout.preferredHeight = 98f;
            ApplyCanvasFrame(
                row,
                backgroundColor,
                borderColor,
                2f);

            Text name = CreateText(
                completed ? def.achievementName : "???",
                22, FontStyle.Bold, TextAnchor.UpperLeft,
                row.transform);
            RectTransform nameRect = name.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.04f, 0.5f);
            nameRect.anchorMax = new Vector2(0.55f, 0.94f);
            // 제목을 10px 아래로 내림.
            nameRect.offsetMin = new Vector2(0f, -10f);
            nameRect.offsetMax = new Vector2(0f, -10f);
            name.color = claimed
                ? new Color(0.72f, 0.76f, 0.82f, 0.95f)
                : claimable
                    ? new Color(1f, 0.86f, 0.45f, 1f)
                    : new Color(0.58f, 0.76f, 1f, 0.85f);
            name.raycastTarget = false;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 13;
            name.resizeTextMaxSize = 22;

            Text desc = CreateText(
                completed ? def.condition + " · " + def.tooltip : "???",
                14, FontStyle.Normal,
                TextAnchor.UpperLeft, row.transform);
            RectTransform descRect = desc.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.04f, 0.06f);
            descRect.anchorMax = new Vector2(0.55f, 0.48f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            desc.color = claimed
                ? new Color(0.48f, 0.52f, 0.58f, 0.9f)
                : completed
                    ? new Color(0.78f, 0.85f, 0.95f, 1f)
                    : new Color(0.48f, 0.6f, 0.78f, 0.75f);
            desc.raycastTarget = false;
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 10;
            desc.resizeTextMaxSize = 14;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Truncate;

            // Status sits at the far right; rewards are right-aligned just left of it
            // and pulled further left as the reward count grows, so they never overlap.
            Text status = CreateText(
                GetAchievementStatusText(def, completed), 14, FontStyle.Bold,
                TextAnchor.MiddleCenter, row.transform);
            RectTransform statusRect = status.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.86f, 0.3f);
            statusRect.anchorMax = new Vector2(0.995f, 0.7f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            status.color = GetAchievementStatusColor(def);
            status.raycastTarget = false;

            CurrencyAmount[] rewards = def.rewards ?? new CurrencyAmount[0];
            int rewardCount = 0;
            if (completed)
            {
                for (int i = 0; i < rewards.Length && i < 3; i++)
                {
                    if (rewards[i] != null)
                    {
                        rewardCount++;
                    }
                }
            }

            // Reward icon + amount sit on the same middle line as the 달성 status
            // (y 0.3~0.7), icon left and "xN" right, so the row reads on one line.
            const float rewardStep = 0.115f;
            const float rewardEnd = 0.845f;
            float rx = rewardEnd - rewardCount * rewardStep;
            if (completed)
            {
                for (int i = 0; i < rewards.Length && i < 3; i++)
                {
                    CurrencyAmount reward = rewards[i];
                    if (reward == null)
                    {
                        continue;
                    }

                    GameObject iconObj = new GameObject(
                        "Reward", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(row.transform, false);
                    RectTransform ir = iconObj.GetComponent<RectTransform>();
                    ir.anchorMin = new Vector2(rx, 0.3f);
                    ir.anchorMax = new Vector2(rx + 0.05f, 0.7f);
                    ir.offsetMin = Vector2.zero;
                    ir.offsetMax = Vector2.zero;
                    Image iconImage = iconObj.GetComponent<Image>();
                    iconImage.sprite = GetMaterialIcon((int)reward.type);
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    iconImage.color = claimed
                        ? new Color(0.72f, 0.76f, 0.82f, 0.78f)
                        : Color.white;

                    Text amount = CreateText(
                        "x" + StarForgeFormat.Number(reward.amount), 13,
                        FontStyle.Bold, TextAnchor.MiddleLeft, row.transform);
                    RectTransform ar = amount.GetComponent<RectTransform>();
                    ar.anchorMin = new Vector2(rx + 0.052f, 0.3f);
                    ar.anchorMax = new Vector2(rx + 0.112f, 0.7f);
                    ar.offsetMin = Vector2.zero;
                    ar.offsetMax = Vector2.zero;
                    amount.horizontalOverflow = HorizontalWrapMode.Overflow;
                    amount.color = claimed
                        ? new Color(0.58f, 0.62f, 0.68f, 0.88f)
                        : new Color(0.9f, 0.96f, 1f, 1f);
                    amount.raycastTarget = false;
                    rx += rewardStep;
                }
            }
            else
            {
                Text hiddenReward = CreateText(
                    "보상 ???", 13, FontStyle.Bold,
                    TextAnchor.MiddleCenter, row.transform);
                RectTransform hiddenRect =
                    hiddenReward.GetComponent<RectTransform>();
                hiddenRect.anchorMin = new Vector2(0.62f, 0.3f);
                hiddenRect.anchorMax = new Vector2(0.845f, 0.7f);
                hiddenRect.offsetMin = Vector2.zero;
                hiddenRect.offsetMax = Vector2.zero;
                hiddenReward.color = new Color(0.48f, 0.68f, 0.95f, 0.8f);
                hiddenReward.raycastTarget = false;
            }

            if (claimable)
            {
                Button claimButton = CreateButton("수령", 13, row.transform);
                RectTransform claimRect =
                    claimButton.GetComponent<RectTransform>();
                claimRect.anchorMin = new Vector2(0.86f, 0.3f);
                claimRect.anchorMax = new Vector2(0.995f, 0.7f);
                claimRect.offsetMin = Vector2.zero;
                claimRect.offsetMax = Vector2.zero;
                StyleCollectionSpaceButton(
                    claimButton,
                    new Color(1f, 0.62f, 0.18f, 1f));
                string capturedId = def.id;
                claimButton.onClick.AddListener(
                    () => AchievementClaimRequested?.Invoke(capturedId));
            }
        }

        private bool IsAchievementClaimable(
            StarForgeAchievementDefinition def)
        {
            return def != null &&
                   def.rewards != null &&
                   def.rewards.Length > 0 &&
                   lastSaveData != null &&
                   lastSaveData.HasCompletedAchievement(def.id) &&
                   !lastSaveData.HasClaimedAchievement(def.id);
        }

        private bool IsAchievementClaimed(
            StarForgeAchievementDefinition def)
        {
            return def != null &&
                   lastSaveData != null &&
                   lastSaveData.HasCompletedAchievement(def.id) &&
                   lastSaveData.HasClaimedAchievement(def.id);
        }

        private int GetAchievementSortGroup(
            StarForgeAchievementDefinition def)
        {
            if (IsAchievementClaimed(def))
            {
                return 2;
            }

            return IsAchievementClaimable(def) ? 0 : 1;
        }

        private string GetAchievementStatusText(
            StarForgeAchievementDefinition def,
            bool completed)
        {
            if (!completed)
            {
                return "미달성";
            }

            return IsAchievementClaimable(def) ? "수령 가능" : "수령 완료";
        }

        private Color GetAchievementStatusColor(
            StarForgeAchievementDefinition def)
        {
            if (IsAchievementClaimable(def))
            {
                return new Color(1f, 0.78f, 0.28f, 1f);
            }

            return IsAchievementClaimed(def)
                ? new Color(0.58f, 0.62f, 0.68f, 0.9f)
                : new Color(0.5f, 0.68f, 0.95f, 0.88f);
        }

        private void OpenCollectionPanel()
        {
            if (collectionPanel == null || balanceRef == null)
            {
                return;
            }

            collectionTransitioning = false;
            if (!IsCollectionShapeDiscovered(collectionShape))
            {
                collectionShape = StarForgePlanetShape.Default;
            }

            collectionMaxUnlockedLevel =
                GetCollectionShapeMaxLevel(collectionShape);
            collectionCurrentLevel = Mathf.Clamp(
                collectionCurrentLevel,
                0,
                collectionMaxUnlockedLevel);
            collectionPreview.ResetCameraOrbit();
            collectionPanel.SetActive(true);
            collectionPanel.transform.SetAsLastSibling();
            ShowCollectionBrowse();
            ResetCollectionScrollsToTop();
        }

        private void ResetCollectionScrollsToTop()
        {
            ResetScrollToTop(collectionPlanetGridContent);
            ResetScrollToTop(collectionAchievementContent);
        }

        private static void ResetScrollToTop(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>(true);
            if (scrollRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void BuildCollectionClaimBadge(RectTransform root)
        {
            // Speech bubble under the 도감 nav button: shown when a reward is waiting.
            collectionClaimBadge = CreateClaimBadge(
                "Collection Claim Badge",
                root,
                new Vector2(0.45f, 0.858f),
                new Vector2(0.79f, 0.892f));
        }

        private void BuildCollectionAchievementClaimBadge(RectTransform root)
        {
            // Same reward bubble, positioned under the 도감 > 업적 tab.
            collectionAchievementClaimBadge = CreateClaimBadge(
                "Collection Achievement Claim Badge",
                root,
                new Vector2(0.55f, 0.79f),
                new Vector2(0.9f, 0.824f));
        }

        private GameObject CreateClaimBadge(
            string name,
            RectTransform root,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject badge = CreatePanel(
                name,
                root,
                new Color(0.06f, 0.045f, 0.012f, 0.98f));
            badge.GetComponent<Image>().raycastTarget = false;
            RectTransform rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                badge,
                new Color(0.07f, 0.05f, 0.014f, 0.99f),
                new Color(1f, 0.82f, 0.32f, 1f),
                1.5f);

            GameObject tail = CreatePanel(
                "Tail",
                badge.transform,
                new Color(1f, 0.82f, 0.32f, 1f));
            RectTransform tailRect = tail.GetComponent<RectTransform>();
            tailRect.anchorMin = new Vector2(0.5f, 1f);
            tailRect.anchorMax = new Vector2(0.5f, 1f);
            tailRect.pivot = new Vector2(0.5f, 0.5f);
            tailRect.sizeDelta = new Vector2(13f, 13f);
            tailRect.anchoredPosition = new Vector2(0f, -1f);
            tailRect.localEulerAngles = new Vector3(0f, 0f, 45f);
            tail.GetComponent<Image>().raycastTarget = false;

            Text text = CreateText(
                "보상을 수령하세요!",
                14,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                badge.transform);
            StretchRect(text.rectTransform, new Vector2(8f, 2f));
            text.color = new Color(1f, 0.92f, 0.62f, 1f);
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 14;

            badge.SetActive(false);
            return badge;
        }

        private void BuildAchievementToast(RectTransform root)
        {
            // Disappearing banner on the main screen when an achievement clears:
            // [gold orb] 업적 달성! / {이름}.
            achievementToast = CreatePanel(
                "Achievement Toast",
                root,
                new Color(0.015f, 0.035f, 0.085f, 0.97f));
            achievementToast.GetComponent<Image>().raycastTarget = false;
            RectTransform rect = achievementToast.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.8f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                achievementToast,
                new Color(0.02f, 0.05f, 0.12f, 0.98f),
                new Color(1f, 0.82f, 0.34f, 1f),
                2f);

            achievementToastGroup = achievementToast.AddComponent<CanvasGroup>();
            achievementToastGroup.alpha = 0f;
            achievementToastGroup.interactable = false;
            achievementToastGroup.blocksRaycasts = false;

            GameObject iconObj = new GameObject(
                "Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(achievementToast.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.02f, 0.5f);
            iconRect.anchorMax = new Vector2(0.02f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(58f, 58f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            achievementToastIcon = iconObj.GetComponent<Image>();
            achievementToastIcon.sprite =
                GetMaterialIcon((int)StarForgeCurrencyType.PrimordialStar);
            achievementToastIcon.preserveAspect = true;
            achievementToastIcon.raycastTarget = false;

            Text title = CreateText(
                "업적 달성!",
                16,
                FontStyle.Bold,
                TextAnchor.LowerLeft,
                achievementToast.transform);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.16f, 0.66f);
            titleRect.anchorMax = new Vector2(0.97f, 0.97f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.84f, 0.4f, 1f);
            title.raycastTarget = false;

            achievementToastNameText = CreateText(
                "",
                25,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                achievementToast.transform);
            RectTransform nameRect =
                achievementToastNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.16f, 0.38f);
            nameRect.anchorMax = new Vector2(0.97f, 0.66f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            achievementToastNameText.color = new Color(1f, 0.96f, 0.74f, 1f);
            achievementToastNameText.raycastTarget = false;
            achievementToastNameText.resizeTextForBestFit = true;
            achievementToastNameText.resizeTextMinSize = 14;
            achievementToastNameText.resizeTextMaxSize = 26;

            achievementToastFlavorText = CreateText(
                "",
                20,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                achievementToast.transform);
            RectTransform flavorRect =
                achievementToastFlavorText.GetComponent<RectTransform>();
            flavorRect.anchorMin = new Vector2(0.16f, 0.06f);
            flavorRect.anchorMax = new Vector2(0.97f, 0.38f);
            flavorRect.offsetMin = Vector2.zero;
            flavorRect.offsetMax = Vector2.zero;
            achievementToastFlavorText.color = new Color(0.74f, 0.82f, 0.95f, 1f);
            achievementToastFlavorText.raycastTarget = false;
            achievementToastFlavorText.resizeTextForBestFit = true;
            achievementToastFlavorText.resizeTextMinSize = 14;
            achievementToastFlavorText.resizeTextMaxSize = 20;
            achievementToastFlavorText.horizontalOverflow = HorizontalWrapMode.Wrap;
            achievementToastFlavorText.verticalOverflow = VerticalWrapMode.Truncate;

            achievementToast.SetActive(false);
        }

        public void ShowAchievementToast(string achievementName, string flavor)
        {
            if (achievementToast == null)
            {
                return;
            }

            achievementToastQueue.Enqueue(
                (achievementName ?? string.Empty, flavor ?? string.Empty));
            if (achievementToastRoutine == null)
            {
                achievementToastRoutine =
                    StartCoroutine(AchievementToastRoutine());
            }
        }

        private IEnumerator AchievementToastRoutine()
        {
            while (achievementToastQueue.Count > 0)
            {
                (string toastName, string toastFlavor) = achievementToastQueue.Dequeue();
                if (achievementToastNameText != null)
                {
                    achievementToastNameText.text = toastName;
                }

                if (achievementToastFlavorText != null)
                {
                    achievementToastFlavorText.text = toastFlavor;
                }

                achievementToast.SetActive(true);
                achievementToast.transform.SetAsLastSibling();
                yield return FadeAchievementToast(0f, 1f, 0.2f);
                yield return new WaitForSecondsRealtime(2f);
                yield return FadeAchievementToast(1f, 0f, 0.4f);
                achievementToast.SetActive(false);
            }

            achievementToastRoutine = null;
        }

        private IEnumerator FadeAchievementToast(
            float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (achievementToastGroup != null)
                {
                    achievementToastGroup.alpha =
                        Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                }

                yield return null;
            }

            if (achievementToastGroup != null)
            {
                achievementToastGroup.alpha = to;
            }
        }

        public void SetAchievementClaimable(bool claimable)
        {
            if (collectionClaimBadge != null &&
                collectionClaimBadge.activeSelf != claimable)
            {
                collectionClaimBadge.SetActive(claimable);
            }

            if (collectionAchievementClaimBadge != null &&
                collectionAchievementClaimBadge.activeSelf != claimable)
            {
                collectionAchievementClaimBadge.SetActive(claimable);
            }

            if (achievementClaimAllButton != null)
            {
                achievementClaimAllButton.interactable = true;
            }
        }

        private void CloseCollectionPanel()
        {
            collectionTransitioning = false;
            if (collectionPreview != null)
            {
                collectionPreview.Hide();
            }

            if (collectionPanel != null)
            {
                collectionPanel.SetActive(false);
            }
        }

        private void NavigateCollection(int direction)
        {
            if (collectionTransitioning ||
                collectionPreview == null ||
                balanceRef == null)
            {
                return;
            }

            int targetLevel = Mathf.Clamp(
                collectionCurrentLevel + (direction >= 0 ? 1 : -1),
                0,
                collectionMaxUnlockedLevel);
            if (targetLevel == collectionCurrentLevel)
            {
                return;
            }

            int lowerLevel = Mathf.Min(collectionCurrentLevel, targetLevel);
            int upperLevel = Mathf.Max(collectionCurrentLevel, targetLevel);
            float diameterMultiplier =
                StarForgeCollectionPreview.GetDiameterMultiplier(
                    lowerLevel,
                    upperLevel);
            float duration =
                StarForgeCollectionPreview.GetTransitionDuration(
                    diameterMultiplier);

            collectionCurrentLevel = targetLevel;
            collectionTransitioning = true;
            RefreshCollectionStageText();
            RefreshCollectionNavigationState();
            collectionPreview.TransitionTo(
                targetLevel,
                duration,
                OnCollectionTransitionComplete);
            if (collectionSkipEnabled)
            {
                collectionPreview.CompleteTransitionImmediately();
            }
        }

        private void SkipCollectionTransition()
        {
            if (!collectionTransitioning || collectionPreview == null)
            {
                return;
            }

            if (!collectionPreview.CompleteTransitionImmediately())
            {
                collectionTransitioning = false;
                RefreshCollectionNavigationState();
            }
        }

        private void OnCollectionSkipToggleChanged(bool enabled)
        {
            collectionSkipEnabled = enabled;
            RefreshCollectionSkipToggle();
            if (collectionSkipEnabled && collectionTransitioning)
            {
                SkipCollectionTransition();
            }
        }

        private void SelectCollectionShape(StarForgePlanetShape shape)
        {
            if (!IsCollectionShapeDiscovered(shape))
            {
                RefreshCollectionShapeButtons();
                return;
            }

            if (collectionTransitioning)
            {
                SkipCollectionTransition();
            }

            if (collectionShape == shape)
            {
                RefreshCollectionShapeButtons();
                return;
            }

            collectionShape = shape;
            collectionMaxUnlockedLevel =
                GetCollectionShapeMaxLevel(collectionShape);
            collectionCurrentLevel = Mathf.Clamp(
                collectionCurrentLevel,
                0,
                collectionMaxUnlockedLevel);
            ShowCurrentCollectionStage();
        }

        private void OnCollectionTransitionComplete()
        {
            collectionTransitioning = false;
            RefreshCollectionNavigationState();
        }

        private void ShowCurrentCollectionStage()
        {
            RefreshCollectionStageText();
            RefreshCollectionNavigationState();
            Canvas.ForceUpdateCanvases();
            Rect viewportRect = collectionPlanetImage.rectTransform.rect;
            collectionPreview.SetViewportSize(
                viewportRect.width,
                viewportRect.height);
            collectionPlanetImage.texture = collectionPreview.OutputTexture;
            collectionPreview.Show(
                balanceRef,
                collectionMaxUnlockedLevel,
                collectionCurrentLevel,
                collectionShape);
        }

        private void RefreshCollectionStageText()
        {
            if (balanceRef == null || collectionTitleText == null)
            {
                return;
            }

            StageVisualConfig stage = balanceRef.GetStage(collectionCurrentLevel);
            collectionTitleText.text =
                collectionCurrentLevel + "강 : " +
                balanceRef.GetStageName(collectionCurrentLevel, collectionShape);
            StarForgeAchievementDefinition achievement =
                achievementService.GetLevelDefinition(
                    collectionCurrentLevel,
                    collectionShape);
            if (achievement != null)
            {
                collectionTitleText.text +=
                    "\n업적 : " + achievement.achievementName;
            }
            collectionTitleText.color = GetLevelTextColor(collectionCurrentLevel);
        }

        private void RefreshCollectionNavigationState()
        {
            if (collectionPreviousButton != null)
            {
                collectionPreviousButton.interactable =
                    !collectionTransitioning && collectionCurrentLevel > 0;
            }

            if (collectionNextButton != null)
            {
                collectionNextButton.interactable =
                    !collectionTransitioning &&
                    collectionCurrentLevel < collectionMaxUnlockedLevel;
            }

            RefreshCollectionSkipToggle();
            RefreshCollectionShapeButtons();
        }

        private void RefreshCollectionSkipToggle()
        {
            if (collectionSkipToggle == null)
            {
                return;
            }

            Image background = collectionSkipToggle.targetGraphic as Image;
            Graphic checkmark = collectionSkipToggle.graphic;
            Color accent = collectionSkipEnabled
                ? new Color(0.38f, 0.94f, 1f, 1f)
                : new Color(0.36f, 0.3f, 0.5f, 1f);

            if (background != null)
            {
                background.color = collectionSkipEnabled
                    ? new Color(0.04f, 0.22f, 0.3f, 0.96f)
                    : new Color(0.025f, 0.035f, 0.075f, 0.94f);
            }

            if (checkmark != null)
            {
                checkmark.color = accent;
            }
        }

        private void RefreshCollectionShapeButtons()
        {
            for (int i = 0; i < collectionShapeButtons.Length; i++)
            {
                Button button = collectionShapeButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool selected = (int)collectionShape == i;
                bool discovered = collectionShapeDiscovered[i];
                button.interactable =
                    !collectionTransitioning && discovered;
                Color accent = selected
                    ? new Color(0.88f, 0.42f, 1f, 1f)
                    : discovered
                        ? new Color(0.18f, 0.48f, 0.78f, 1f)
                        : new Color(0.2f, 0.23f, 0.3f, 1f);
                ApplyCollectionButtonPalette(button, accent, selected);

                Text label = collectionShapeButtonTexts[i];
                if (label != null)
                {
                    label.text = discovered
                        ? GetCollectionShapeLabel(
                            (StarForgePlanetShape)i)
                        : string.Empty;
                    label.color = selected
                        ? new Color(1f, 0.86f, 1f, 1f)
                        : discovered
                            ? new Color(0.58f, 0.7f, 0.84f, 1f)
                            : new Color(0.5f, 0.54f, 0.62f, 1f);
                    label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                }

                if (collectionShapeLockIcons[i] != null)
                {
                    collectionShapeLockIcons[i].SetActive(!discovered);
                }
            }
        }

        private void RefreshCollectionPanel(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            bool isBusy)
        {
            if (collectionOpenButton != null)
            {
                collectionOpenButton.interactable = !isBusy;
            }

            int previousMaxLevel = collectionMaxUnlockedLevel;
            for (int i = 0; i < collectionShapeButtons.Length; i++)
            {
                StarForgePlanetShape shape = (StarForgePlanetShape)i;
                collectionShapeDiscovered[i] =
                    saveData.IsShapeDiscovered(shape);
                collectionShapeMaxLevels[i] = collectionShapeDiscovered[i]
                    ? Mathf.Clamp(
                        saveData.GetShapeHighestLevel(shape),
                        0,
                        balance.maxLevel)
                    : -1;
            }

            if (!IsCollectionShapeDiscovered(collectionShape))
            {
                collectionShape = StarForgePlanetShape.Default;
            }

            collectionMaxUnlockedLevel =
                GetCollectionShapeMaxLevel(collectionShape);

            if (collectionPanel == null || !collectionPanel.activeSelf)
            {
                return;
            }

            if (!collectionTransitioning &&
                collectionCurrentLevel > collectionMaxUnlockedLevel)
            {
                collectionCurrentLevel = collectionMaxUnlockedLevel;
                ShowCurrentCollectionStage();
                return;
            }

            if (!collectionTransitioning &&
                previousMaxLevel != collectionMaxUnlockedLevel)
            {
                ShowCurrentCollectionStage();
                return;
            }

            RefreshCollectionStageText();
            RefreshCollectionNavigationState();
        }

        private bool IsCollectionShapeDiscovered(
            StarForgePlanetShape shape)
        {
            int index = (int)shape;
            return index >= 0 &&
                   index < collectionShapeDiscovered.Length &&
                   collectionShapeDiscovered[index];
        }

        private int GetCollectionShapeMaxLevel(
            StarForgePlanetShape shape)
        {
            int index = (int)shape;
            return index >= 0 &&
                   index < collectionShapeMaxLevels.Length
                ? Mathf.Max(0, collectionShapeMaxLevels[index])
                : 0;
        }

        private static string GetCollectionShapeLabel(
            StarForgePlanetShape shape)
        {
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return "하트별";
                case StarForgePlanetShape.Cat:
                    return "고양이별";
                default:
                    return "일반 별";
            }
        }

        private GameObject CreateCollectionLockIcon(Transform parent)
        {
            GameObject root = new GameObject(
                "Lock Icon",
                typeof(RectTransform),
                typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(52f, 52f);
            rootRect.anchoredPosition = Vector2.zero;

            Image image = root.GetComponent<Image>();
            image.sprite = LoadCollectionLockSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            root.SetActive(false);
            return root;
        }

        private static Sprite LoadCollectionLockSprite()
        {
            Sprite sprite = Resources.Load<Sprite>("CollectionLock");
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>("CollectionLock");
            return texture != null
                ? Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f)
                : null;
        }

        private Toggle CreateCollectionSkipToggle(Transform parent)
        {
            GameObject toggleObject = new GameObject(
                "Collection Skip Toggle",
                typeof(RectTransform),
                typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            GameObject backgroundObject = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            backgroundObject.transform.SetParent(toggleObject.transform, false);
            RectTransform backgroundRect =
                backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.035f, 0.075f, 0.94f);
            Image toggleTarget = ApplyCanvasFrame(
                backgroundObject,
                background.color,
                new Color(0.2f, 0.52f, 0.88f, 0.95f),
                2f);

            GameObject checkboxObject = new GameObject(
                "Checkbox",
                typeof(RectTransform),
                typeof(Image));
            checkboxObject.transform.SetParent(backgroundObject.transform, false);
            RectTransform checkboxRect =
                checkboxObject.GetComponent<RectTransform>();
            checkboxRect.anchorMin = new Vector2(0.8f, 0.5f);
            checkboxRect.anchorMax = new Vector2(0.8f, 0.5f);
            checkboxRect.pivot = new Vector2(0.5f, 0.5f);
            checkboxRect.sizeDelta = new Vector2(34f, 34f);
            checkboxRect.anchoredPosition = Vector2.zero;
            Image checkbox = checkboxObject.GetComponent<Image>();
            checkbox.sprite = null;
            checkbox.type = Image.Type.Simple;
            checkbox.color = new Color(0.008f, 0.025f, 0.055f, 0.96f);
            checkbox.raycastTarget = false;
            AddFourSideBorder(
                checkboxObject,
                new Color(0.38f, 0.94f, 1f, 1f),
                2f);

            Text label = CreateText(
                "스킵",
                15,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                toggleObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.08f, 0f);
            labelRect.anchorMax = new Vector2(0.62f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.78f, 0.88f, 1f, 1f);

            collectionSkipToggleText = CreateText(
                "V",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                checkboxObject.transform);
            RectTransform checkmarkRect =
                collectionSkipToggleText.GetComponent<RectTransform>();
            StretchRect(checkmarkRect, Vector2.zero);
            collectionSkipToggleText.color =
                new Color(0.38f, 0.94f, 1f, 1f);
            collectionSkipToggleText.raycastTarget = false;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = toggleTarget;
            toggle.graphic = collectionSkipToggleText;
            ColorBlock colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.58f, 0.72f, 0.9f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.48f, 0.6f);
            toggle.colors = colors;
            return toggle;
        }

        private static void StyleCollectionSpaceButton(
            Button button,
            Color accent)
        {
            if (button == null)
            {
                return;
            }

            ApplyCollectionButtonPalette(button, accent, false);
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            Outline glow = label.GetComponent<Outline>();
            if (glow == null)
            {
                glow = label.gameObject.AddComponent<Outline>();
            }

            glow.effectColor = new Color(
                accent.r * 0.35f,
                accent.g * 0.35f,
                accent.b * 0.45f,
                0.95f);
            glow.effectDistance = new Vector2(1.5f, -1.5f);
            glow.useGraphicAlpha = true;
        }

        private static void ApplyCollectionButtonPalette(
            Button button,
            Color accent,
            bool selected)
        {
            Image image = button.image;
            Color normal = selected
                ? new Color(
                    accent.r * 0.24f,
                    accent.g * 0.2f,
                    accent.b * 0.3f,
                    0.98f)
                : new Color(0.018f, 0.032f, 0.075f, 0.94f);
            image.color = normal;
            image.raycastTarget = true;

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(
                Mathf.Clamp01(normal.r + accent.r * 0.2f),
                Mathf.Clamp01(normal.g + accent.g * 0.2f),
                Mathf.Clamp01(normal.b + accent.b * 0.2f),
                1f);
            colors.pressedColor = new Color(
                normal.r * 0.62f,
                normal.g * 0.62f,
                normal.b * 0.72f,
                1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.012f, 0.018f, 0.04f, 0.58f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            AddPanelOutline(
                button.gameObject,
                new Color(accent.r, accent.g, accent.b, selected ? 1f : 0.72f),
                new Vector2(selected ? 2.5f : 1.5f, selected ? -2.5f : -1.5f));
        }

        private static void StyleCollectionArrowButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Color accent = new Color(0.26f, 0.76f, 1f, 1f);
            StyleCollectionSpaceButton(button, accent);

            Text arrow = button.GetComponentInChildren<Text>();
            if (arrow == null)
            {
                return;
            }

            arrow.color = Color.white;
            Outline outline = arrow.GetComponent<Outline>();
            if (outline == null)
            {
                outline = arrow.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0.02f, 0.24f, 0.48f, 0.95f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.useGraphicAlpha = true;
        }

        public void ShowReviveOverlay(StarForgeEnhancementResult result, StarForgeSaveData saveData)
        {
            if (revivePanel == null || result == null)
            {
                return;
            }

            reviveDestroyedLevel = result.previousLevel;
            reviveBodyText.text =
                "<color=#F3F8FF>" + result.previousLevel + "강에서 행성이 소멸했습니다.</color>\n" +
                "<size=25><color=#FFB664>회수 보상  " +
                StarForgeFormat.CurrencyList(result.rewards) +
                "</color></size>";
            RefreshReviveRows(saveData);
            // Up to 3 keep-level ads per star life: hide the option once all are spent on
            // this planet. It returns when a new star begins (checkpoint revive, 0강
            // 재시작, or a voluntary disassemble) — the controller resets the counter.
            bool keepAdAvailable = saveData != null && saveData.CanUseKeepLevelAd();
            if (rewardedReviveButton != null)
            {
                rewardedReviveButton.gameObject.SetActive(keepAdAvailable);
            }

            if (keepAdAvailable)
            {
                SetRewardedReviveButtonState(
                    true,
                    "광고 보고 현 단계 유지 (남은 " +
                    saveData.RemainingKeepLevelAds() + "회)");
            }

            revivePanel.SetActive(true);
            UpdateEnhanceButtonInteractable();
        }

        public void HideReviveOverlay()
        {
            if (revivePanel != null)
            {
                revivePanel.SetActive(false);
            }

            UpdateEnhanceButtonInteractable();
        }

        public void SetRewardedReviveButtonState(
            bool interactable,
            string label)
        {
            if (rewardedReviveButton != null)
            {
                rewardedReviveButton.interactable = interactable;
            }

            if (rewardedReviveButtonText != null &&
                !string.IsNullOrEmpty(label))
            {
                rewardedReviveButtonText.text = label;
            }
        }

        private void SetMainHudVisible(bool visible)
        {
            if (mainHudCanvasGroup == null || mainHudVisible == visible)
            {
                return;
            }

            mainHudVisible = visible;

            if (mainHudFadeRoutine != null)
            {
                StopCoroutine(mainHudFadeRoutine);
                mainHudFadeRoutine = null;
            }

            if (!gameObject.activeInHierarchy)
            {
                mainHudCanvasGroup.alpha = visible ? 1f : 0f;
                mainHudCanvasGroup.interactable = visible;
                mainHudCanvasGroup.blocksRaycasts = visible;
                return;
            }

            mainHudFadeRoutine = StartCoroutine(FadeMainHud(visible));
        }

        private IEnumerator FadeMainHud(bool visible)
        {
            mainHudCanvasGroup.interactable = visible;
            mainHudCanvasGroup.blocksRaycasts = visible;

            float from = mainHudCanvasGroup.alpha;
            float to = visible ? 1f : 0f;
            float duration = visible ? 0.28f : 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                mainHudCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainHudCanvasGroup.alpha = to;
            mainHudFadeRoutine = null;
        }

        public void ShowMessage(string title, string body)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
            SetResultRewards(null);
            resultTitleText.text = title;
            resultBodyText.text = body;
            UpdateEnhanceButtonInteractable();
        }

        public void ShowAchievement(StarForgeAchievementUnlock achievement)
        {
            if (resultPanel == null ||
                achievement == null ||
                achievement.definition == null)
            {
                return;
            }

            if (resultPanel.activeSelf)
            {
                pendingAchievementOverlays.Enqueue(achievement);
                return;
            }

            DisplayAchievement(achievement);
        }

        private void DisplayAchievement(StarForgeAchievementUnlock achievement)
        {
            StarForgeAchievementDefinition definition = achievement.definition;
            resultPanel.SetActive(true);
            resultTitleText.text = "업적 달성";
            resultBodyText.text = BuildAchievementBody(definition);
            SetResultRewards(definition.rewards);
            UpdateEnhanceButtonInteractable();
        }

        private void CloseResultPanel()
        {
            resultPanel.SetActive(false);
            if (pendingAchievementOverlays.Count > 0)
            {
                DisplayAchievement(pendingAchievementOverlays.Dequeue());
            }

            UpdateEnhanceButtonInteractable();
        }

        private static string BuildAchievementBody(
            StarForgeAchievementDefinition definition)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            // Title ("업적 달성") lives in resultTitleText; the body shows just the
            // achievement name and its tooltip, each in its own accent color.
            builder.Append("<b><color=#FFD56A>");
            builder.Append(definition.achievementName);
            builder.Append("</color></b>\n\n<color=#9FE7FF>");
            builder.Append(definition.tooltip);
            builder.Append("</color>\n\n보상 : ");

            CurrencyAmount[] rewards = definition.rewards;
            for (int i = 0; i < rewards.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(
                    StarForgeCurrencyNames.GetDisplayName(rewards[i].type));
                builder.Append(' ');
                builder.Append(StarForgeFormat.Number(rewards[i].amount));
                builder.Append("개");
            }

            return builder.ToString();
        }

        private void SetResultRewards(
            CurrencyAmount[] rewards,
            string amountPrefix = "+")
        {
            if (resultRewardRow == null)
            {
                return;
            }

            int rewardCount = rewards != null
                ? Mathf.Min(rewards.Length, resultRewardIcons.Length)
                : 0;
            resultRewardRow.SetActive(rewardCount > 0);
            for (int i = 0; i < resultRewardIcons.Length; i++)
            {
                Image icon = resultRewardIcons[i];
                Text amount = resultRewardTexts[i];
                GameObject slot = icon != null
                    ? icon.transform.parent.gameObject
                    : null;
                bool visible =
                    i < rewardCount &&
                    rewards[i] != null &&
                    rewards[i].amount > 0;
                if (slot != null)
                {
                    slot.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                icon.sprite = GetMaterialIcon((int)rewards[i].type);
                icon.color = Color.white;
                amount.text =
                    amountPrefix + StarForgeFormat.Number(rewards[i].amount);
            }
        }

        private void BuildRevivePanel(RectTransform root, StarForgeBalance balance)
        {
            revivePanel = CreatePanel("Revive Overlay", root, new Color(0.003f, 0.006f, 0.018f, 0.88f));
            RectTransform overlayRect = revivePanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject dialog = CreatePanel(
                "Revive Popup",
                revivePanel.transform,
                new Color(0.018f, 0.025f, 0.052f, 0.99f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.055f, 0.305f);
            dialogRect.anchorMax = new Vector2(0.945f, 0.975f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            ApplyCanvasFrame(
                dialog,
                new Color(0.012f, 0.022f, 0.048f, 0.995f),
                new Color(0.72f, 0.16f, 0.24f, 0.98f),
                2f);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            GameObject header = CreatePanel(
                "Revive Header",
                dialog.transform,
                new Color(0.04f, 0.02f, 0.045f, 0.99f));
            ApplyCanvasFrame(
                header,
                new Color(0.028f, 0.022f, 0.052f, 0.99f),
                new Color(0.8f, 0.2f, 0.26f, 0.96f),
                2f);
            LayoutElement headerElement = header.AddComponent<LayoutElement>();
            headerElement.minHeight = 126f;
            headerElement.preferredHeight = 126f;

            VerticalLayoutGroup headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(14, 14, 8, 8);
            headerLayout.spacing = 1f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;

            Text title = CreateText("행성 소멸", 36, FontStyle.Bold, TextAnchor.MiddleCenter, header.transform);
            title.color = new Color(1f, 0.45f, 0.42f, 1f);
            SetPreferredHeight(title, 43f);
            Outline titleGlow = title.gameObject.AddComponent<Outline>();
            titleGlow.effectColor = new Color(0.42f, 0.02f, 0.06f, 0.92f);
            titleGlow.effectDistance = new Vector2(2f, -2f);

            reviveBodyText = CreateText("", 22, FontStyle.Bold, TextAnchor.MiddleCenter, header.transform);
            reviveBodyText.color = new Color(0.85f, 0.9f, 1f, 1f);
            reviveBodyText.resizeTextForBestFit = true;
            reviveBodyText.resizeTextMinSize = 17;
            reviveBodyText.resizeTextMaxSize = 25;
            SetPreferredHeight(reviveBodyText, 68f);

            RevivePointConfig[] points = balance.revivePoints != null
                ? balance.revivePoints
                : new RevivePointConfig[0];
            reviveRows = new ReviveRow[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                reviveRows[i] = points[i] != null
                    ? BuildReviveRow(dialog.transform, points[i], balance)
                    : null;
            }

            Button restartButton = CreateButton("0강부터 다시 시작", 24, dialog.transform);
            LayoutElement restartLayout = restartButton.GetComponent<LayoutElement>();
            restartLayout.minHeight = 58f;
            restartLayout.preferredHeight = 58f;
            ApplyCanvasFrame(
                restartButton.gameObject,
                new Color(0.11f, 0.025f, 0.045f, 0.98f),
                new Color(0.72f, 0.14f, 0.2f, 0.98f),
                2f);
            ColorBlock restartColors = restartButton.colors;
            restartColors.normalColor = Color.white;
            restartColors.highlightedColor = new Color(1f, 0.86f, 0.88f, 1f);
            restartColors.pressedColor = new Color(0.72f, 0.62f, 0.64f, 1f);
            restartColors.selectedColor = restartColors.highlightedColor;
            restartButton.colors = restartColors;
            SetButtonTextColor(restartButton, new Color(1f, 0.84f, 0.82f, 1f));
            restartButton.onClick.AddListener(() =>
            {
                HideReviveOverlay();
                ReviveDismissed?.Invoke();
            });

            rewardedReviveButton = CreateButton(
                "광고 보고 현 단계 유지",
                22,
                dialog.transform);
            rewardedReviveButtonText =
                rewardedReviveButton.GetComponentInChildren<Text>();
            LayoutElement rewardedLayout =
                rewardedReviveButton.GetComponent<LayoutElement>();
            rewardedLayout.minHeight = 75f;
            rewardedLayout.preferredHeight = 75f;
            ApplyCanvasFrame(
                rewardedReviveButton.gameObject,
                new Color(0.12f, 0.08f, 0.018f, 0.99f),
                new Color(1f, 0.66f, 0.16f, 0.98f),
                2.5f);
            ColorBlock rewardedColors = rewardedReviveButton.colors;
            rewardedColors.normalColor = Color.white;
            rewardedColors.highlightedColor =
                new Color(1f, 0.94f, 0.72f, 1f);
            rewardedColors.pressedColor =
                new Color(0.76f, 0.68f, 0.48f, 1f);
            rewardedColors.selectedColor = rewardedColors.highlightedColor;
            rewardedColors.disabledColor =
                new Color(0.42f, 0.42f, 0.42f, 0.72f);
            rewardedReviveButton.colors = rewardedColors;
            SetButtonTextColor(
                rewardedReviveButton,
                new Color(1f, 0.88f, 0.5f, 1f));
            rewardedReviveButton.onClick.AddListener(
                () => RewardedReviveRequested?.Invoke());

            revivePanel.SetActive(false);
        }

        private ReviveRow BuildReviveRow(Transform parent, RevivePointConfig config, StarForgeBalance balance)
        {
            ReviveRow row = new ReviveRow();
            row.level = config.level;

            GameObject container = CreatePanel(
                "Revive Row " + config.level,
                parent,
                new Color(0.04f, 0.065f, 0.12f, 0.98f));
            row.accentColor = GetLevelTextColor(config.level);
            row.frameImage = container.GetComponent<Image>();
            row.fillImage = ApplyCanvasFrame(
                container,
                new Color(0.018f, 0.035f, 0.075f, 0.99f),
                new Color(
                    row.accentColor.r * 0.72f,
                    row.accentColor.g * 0.72f,
                    row.accentColor.b * 0.82f,
                    0.94f),
                2f);
            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.minHeight = 88f;
            containerLayout.preferredHeight = 88f;

            VerticalLayoutGroup containerLayoutGroup = container.AddComponent<VerticalLayoutGroup>();
            containerLayoutGroup.padding = new RectOffset(13, 13, 7, 6);
            containerLayoutGroup.spacing = 3f;
            containerLayoutGroup.childControlWidth = true;
            containerLayoutGroup.childControlHeight = true;
            containerLayoutGroup.childForceExpandWidth = true;
            containerLayoutGroup.childForceExpandHeight = false;

            GameObject titleLine = new GameObject("Title Line", typeof(RectTransform), typeof(LayoutElement));
            titleLine.transform.SetParent(container.transform, false);
            titleLine.GetComponent<LayoutElement>().preferredHeight = 44f;
            HorizontalLayoutGroup titleLayout = titleLine.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = false;

            StageVisualConfig stage = balance.GetStage(config.level);
            row.titleText = CreateText(
                config.level + "강 부활 · " + stage.displayName,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                titleLine.transform);
            LayoutElement titleTextLayout = row.titleText.GetComponent<LayoutElement>();
            titleTextLayout.flexibleWidth = 1f;
            Outline titleOutline = row.titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(
                row.accentColor.r * 0.12f,
                row.accentColor.g * 0.12f,
                row.accentColor.b * 0.16f,
                0.9f);
            titleOutline.effectDistance = new Vector2(1.25f, -1.25f);

            row.button = CreateButton("부활", 18, titleLine.transform);
            LayoutElement buttonLayout = row.button.GetComponent<LayoutElement>();
            buttonLayout.minWidth = 104f;
            buttonLayout.preferredWidth = 104f;
            buttonLayout.minHeight = 40f;
            buttonLayout.preferredHeight = 40f;
            buttonLayout.flexibleWidth = 0f;
            row.buttonFill = ApplyCanvasFrame(
                row.button.gameObject,
                new Color(0.035f, 0.18f, 0.13f, 0.98f),
                new Color(0.2f, 0.82f, 0.5f, 0.96f),
                2f);
            ColorBlock buttonColors = row.button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = new Color(0.88f, 1f, 0.94f, 1f);
            buttonColors.pressedColor = new Color(0.62f, 0.75f, 0.68f, 1f);
            buttonColors.selectedColor = buttonColors.highlightedColor;
            buttonColors.disabledColor = new Color(0.44f, 0.48f, 0.54f, 0.72f);
            row.button.colors = buttonColors;
            SetButtonTextColor(row.button, new Color(0.82f, 1f, 0.9f, 1f));

            int capturedLevel = config.level;
            row.button.onClick.AddListener(() =>
            {
                HideReviveOverlay();
                ReviveRequested?.Invoke(capturedLevel);
            });

            GameObject costLine = new GameObject("Cost Line", typeof(RectTransform), typeof(LayoutElement));
            costLine.transform.SetParent(container.transform, false);
            costLine.GetComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup costLayout = costLine.AddComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 6f;
            costLayout.childAlignment = TextAnchor.MiddleLeft;
            costLayout.childControlWidth = true;
            costLayout.childControlHeight = true;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = false;

            CurrencyAmount[] costs = config.cost != null ? config.cost : new CurrencyAmount[0];
            row.costTypes = new StarForgeCurrencyType[costs.Length];
            row.costAmounts = new int[costs.Length];
            row.costTexts = new Text[costs.Length];
            row.costIcons = new Image[costs.Length];

            for (int i = 0; i < costs.Length; i++)
            {
                CurrencyAmount cost = costs[i];
                if (cost == null)
                {
                    continue;
                }

                row.costTypes[i] = cost.type;
                row.costAmounts[i] = cost.amount;

                GameObject iconObject = new GameObject(
                    "Cost Icon " + i,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                iconObject.transform.SetParent(costLine.transform, false);
                LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.minWidth = 30f;
                iconLayout.preferredWidth = 30f;
                iconLayout.minHeight = 30f;
                iconLayout.preferredHeight = 30f;
                row.costIcons[i] = iconObject.GetComponent<Image>();
                row.costIcons[i].sprite = GetMaterialIcon((int)cost.type);
                row.costIcons[i].preserveAspect = true;

                row.costTexts[i] = CreateText(
                    "x" + StarForgeFormat.Number(cost.amount),
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    costLine.transform);
                LayoutElement costTextLayout = row.costTexts[i].GetComponent<LayoutElement>();
                costTextLayout.minWidth = 58f;
            }

            row.statusText = CreateText("", 14, FontStyle.Bold, TextAnchor.MiddleRight, costLine.transform);
            row.statusText.GetComponent<LayoutElement>().flexibleWidth = 1f;

            return row;
        }

        private void RefreshReviveRows(StarForgeSaveData saveData)
        {
            if (reviveRows == null || saveData == null)
            {
                return;
            }

            for (int i = 0; i < reviveRows.Length; i++)
            {
                ReviveRow row = reviveRows[i];
                if (row == null)
                {
                    continue;
                }

                bool unlocked = reviveDestroyedLevel >= row.level;
                bool affordable = true;

                for (int c = 0; c < row.costTexts.Length; c++)
                {
                    if (row.costTexts[c] == null)
                    {
                        continue;
                    }

                    bool hasEnough = saveData.GetCurrency(row.costTypes[c]) >= row.costAmounts[c];
                    if (!hasEnough)
                    {
                        affordable = false;
                    }

                    row.costTexts[c].color = !unlocked
                        ? new Color(0.42f, 0.46f, 0.55f, 1f)
                        : hasEnough
                            ? new Color(0.36f, 0.86f, 1f, 1f)
                            : new Color(1f, 0.45f, 0.48f, 1f);
                    if (row.costIcons[c] != null)
                    {
                        row.costIcons[c].color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.32f);
                    }
                }

                row.titleText.color = unlocked
                    ? row.accentColor
                    : new Color(0.48f, 0.52f, 0.62f, 1f);
                row.button.interactable = unlocked && affordable;

                if (row.frameImage != null)
                {
                    row.frameImage.color = unlocked
                        ? new Color(
                            row.accentColor.r * 0.72f,
                            row.accentColor.g * 0.72f,
                            row.accentColor.b * 0.82f,
                            0.94f)
                        : new Color(0.16f, 0.2f, 0.3f, 0.72f);
                }

                if (row.fillImage != null)
                {
                    row.fillImage.color = unlocked
                        ? new Color(0.018f, 0.035f, 0.075f, 0.99f)
                        : new Color(0.015f, 0.022f, 0.044f, 0.96f);
                }

                if (row.buttonFill != null)
                {
                    row.buttonFill.color = !unlocked
                        ? new Color(0.035f, 0.045f, 0.065f, 0.96f)
                        : affordable
                            ? new Color(0.035f, 0.18f, 0.13f, 0.98f)
                            : new Color(0.18f, 0.055f, 0.075f, 0.98f);
                }

                row.button.image.color = !unlocked
                    ? new Color(0.18f, 0.22f, 0.3f, 0.72f)
                    : affordable
                        ? new Color(0.2f, 0.82f, 0.5f, 0.96f)
                        : new Color(0.82f, 0.22f, 0.28f, 0.94f);
                SetButtonTextColor(
                    row.button,
                    !unlocked
                        ? new Color(0.5f, 0.55f, 0.64f, 1f)
                        : affordable
                            ? new Color(0.82f, 1f, 0.9f, 1f)
                            : new Color(1f, 0.7f, 0.72f, 1f));
                row.statusText.text = !unlocked
                    ? row.level + "강 이상 파괴 시 가능"
                    : affordable
                        ? "부활 가능"
                        : "재화 부족";
                row.statusText.color = !unlocked
                    ? new Color(0.48f, 0.52f, 0.62f, 1f)
                    : affordable
                        ? new Color(0.45f, 0.95f, 0.6f, 1f)
                        : new Color(1f, 0.45f, 0.48f, 1f);
            }
        }

        private sealed class ReviveRow
        {
            public int level;
            public Button button;
            public Text titleText;
            public Text statusText;
            public Text[] costTexts;
            public Image[] costIcons;
            public Image frameImage;
            public Image fillImage;
            public Image buttonFill;
            public Color accentColor;
            public StarForgeCurrencyType[] costTypes;
            public int[] costAmounts;
        }

        private GameObject CreatePanel(string objectName, Transform parent, Color color)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private Button CreateButton(string text, int fontSize, Transform parent)
        {
            GameObject buttonObject = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            Color normal = new Color(0.018f, 0.055f, 0.12f, 0.98f);
            image.color = normal;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 44f;

            Button button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<StarForgeButtonPress>();
            ApplyCanvasFrame(
                buttonObject,
                normal,
                new Color(0.08f, 0.36f, 0.62f, 0.96f),
                1.5f);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.72f, 1f, 0.95f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.36f, 0.4f, 0.48f, 0.7f);
            button.colors = colors;

            Text label = CreateText(text, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;

            return button;
        }

        private Button CreateMaterialButton(int index, string displayName, Transform parent)
        {
            GameObject buttonObject = new GameObject(displayName + " Slot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.006f, 0.025f, 0.058f, 1f);
            ApplyCanvasFrame(
                buttonObject,
                background.color,
                new Color(0.05f, 0.34f, 0.58f, 0.94f),
                1.25f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 112f;
            layoutElement.preferredHeight = 124f;

            VerticalLayoutGroup layout = buttonObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(3, 3, 3, 2);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = CreateText(displayName, 17, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            label.gameObject.name = "Name";
            label.color = new Color(0.82f, 0.9f, 1f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 17;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.minHeight = 24f;
            labelLayout.preferredHeight = 24f;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(buttonObject.transform, false);
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 58f;
            iconLayout.preferredHeight = 58f;
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = GetMaterialIcon(index);
            icon.preserveAspect = true;

            Text quantity = CreateText("x0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            quantity.gameObject.name = "Quantity";
            quantity.color = new Color(0.16f, 0.82f, 1f, 1f);
            quantity.resizeTextForBestFit = true;
            quantity.resizeTextMinSize = 13;
            quantity.resizeTextMaxSize = 20;
            LayoutElement quantityLayout = quantity.GetComponent<LayoutElement>();
            quantityLayout.minHeight = 25f;
            quantityLayout.preferredHeight = 25f;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.76f, 0.96f, 0.95f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.38f, 0.44f, 0.55f, 0.72f);
            button.colors = colors;
            button.targetGraphic = background;
            return button;
        }

        private Slider CreateVolumeSlider(
            string text,
            Transform parent,
            out Text valueText)
        {
            GameObject rowObject = new GameObject(
                text + " Volume Row",
                typeof(RectTransform),
                typeof(Image),
                typeof(Slider),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.minHeight = 62f;
            rowLayout.preferredHeight = 62f;
            rowLayout.flexibleWidth = 1f;

            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.sprite = GetChamferedUiSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(0.02f, 0.065f, 0.12f, 0.96f);
            AddPanelOutline(
                rowObject,
                new Color(0.1f, 0.34f, 0.56f, 0.86f),
                new Vector2(1.25f, -1.25f));

            Text label = CreateText(
                text,
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                rowObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.06f, 0f);
            labelRect.anchorMax = new Vector2(0.28f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.84f, 0.94f, 1f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            GameObject trackObject = new GameObject(
                "Track",
                typeof(RectTransform),
                typeof(Image));
            trackObject.transform.SetParent(rowObject.transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.32f, 0.42f);
            trackRect.anchorMax = new Vector2(0.78f, 0.58f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.sprite = GetChamferedUiSprite();
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(0.035f, 0.12f, 0.2f, 1f);

            GameObject fillArea = new GameObject(
                "Fill Area",
                typeof(RectTransform));
            fillArea.transform.SetParent(trackObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 0f);
            fillAreaRect.offsetMax = new Vector2(-4f, 0f);

            GameObject fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(Image));
            fillObject.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = GetChamferedUiSprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.2f, 0.72f, 1f, 1f);

            GameObject handleArea = new GameObject(
                "Handle Slide Area",
                typeof(RectTransform));
            handleArea.transform.SetParent(rowObject.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0.32f, 0.22f);
            handleAreaRect.anchorMax = new Vector2(0.78f, 0.78f);
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handleObject = new GameObject(
                "Handle",
                typeof(RectTransform),
                typeof(Image));
            handleObject.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(26f, 26f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = GetChamferedUiSprite();
            handleImage.type = Image.Type.Sliced;
            handleImage.color = new Color(0.8f, 0.96f, 1f, 1f);

            valueText = CreateText(
                "100%",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                rowObject.transform);
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.8f, 0f);
            valueRect.anchorMax = new Vector2(0.95f, 1f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            valueText.color = new Color(1f, 0.84f, 0.36f, 1f);

            Slider slider = rowObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            return slider;
        }

        private static void UpdateVolumeValueText(Text valueText, float value)
        {
            if (valueText == null)
            {
                return;
            }

            valueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private Toggle CreateToggle(string text, Transform parent)
        {
            GameObject toggleObject = new GameObject(
                text + " Toggle",
                typeof(RectTransform),
                typeof(Image),
                typeof(Toggle),
                typeof(LayoutElement));
            toggleObject.transform.SetParent(parent, false);
            LayoutElement rowLayout = toggleObject.GetComponent<LayoutElement>();
            rowLayout.minHeight = 58f;
            rowLayout.preferredHeight = 58f;
            rowLayout.flexibleWidth = 1f;

            Image rowImage = toggleObject.GetComponent<Image>();
            rowImage.sprite = GetChamferedUiSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(0.02f, 0.065f, 0.12f, 0.96f);
            AddPanelOutline(
                toggleObject,
                new Color(0.1f, 0.34f, 0.56f, 0.86f),
                new Vector2(1.25f, -1.25f));

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.74f, 0.22f);
            backgroundRect.anchorMax = new Vector2(0.94f, 0.78f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image toggleTarget = background.GetComponent<Image>();
            toggleTarget.sprite = GetChamferedUiSprite();
            toggleTarget.type = Image.Type.Sliced;
            toggleTarget.color = new Color(0.035f, 0.12f, 0.2f, 1f);
            AddPanelOutline(
                background,
                new Color(0.12f, 0.52f, 0.84f, 0.92f),
                new Vector2(1f, -1f));

            // Knob anchors: it slides between the left ("off") and right ("on")
            // edges of the track instead of appearing/disappearing.
            Vector2 knobOffAnchorMin = new Vector2(0.1f, 0.14f);
            Vector2 knobOffAnchorMax = new Vector2(0.44f, 0.86f);
            Vector2 knobOnAnchorMin = new Vector2(0.56f, 0.14f);
            Vector2 knobOnAnchorMax = new Vector2(0.9f, 0.86f);
            Color knobOffColor = new Color(0.46f, 0.52f, 0.58f, 1f);
            Color knobOnColor = new Color(0.38f, 0.92f, 1f, 1f);
            Color trackOffColor = new Color(0.035f, 0.12f, 0.2f, 1f);
            Color trackOnColor = new Color(0.06f, 0.3f, 0.5f, 1f);

            GameObject checkmark = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = knobOffAnchorMin;
            checkmarkRect.anchorMax = knobOffAnchorMax;
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            Image checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.sprite = GetChamferedUiSprite();
            checkmarkImage.type = Image.Type.Sliced;
            checkmarkImage.color = knobOffColor;

            Text label = CreateText(
                text,
                27,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                toggleObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.07f, 0f);
            labelRect.anchorMax = new Vector2(0.64f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.84f, 0.94f, 1f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = toggleTarget;
            // Leave graphic null so Unity does not show/hide the knob; the
            // switch component below slides and recolours it instead.
            toggle.graphic = null;

            StarForgeToggleSwitch toggleSwitch =
                toggleObject.AddComponent<StarForgeToggleSwitch>();
            toggleSwitch.Configure(
                toggle,
                checkmarkRect,
                checkmarkImage,
                toggleTarget,
                knobOffAnchorMin,
                knobOffAnchorMax,
                knobOnAnchorMin,
                knobOnAnchorMax,
                knobOffColor,
                knobOnColor,
                trackOffColor,
                trackOnColor);
            return toggle;
        }

        private void CreateSpaceBackdrop(RectTransform root)
        {
            GameObject backdropObject = new GameObject(
                "Space Backdrop",
                typeof(RectTransform),
                typeof(RawImage));
            backdropObject.transform.SetParent(root, false);
            RectTransform rect = backdropObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            spaceBackdropImage = backdropObject.GetComponent<RawImage>();
            spaceBackdropImage.texture = GetSpaceBackdropTexture(false);
            spaceBackdropImage.color = new Color(0.7f, 0.86f, 1f, 0.72f);
            spaceBackdropImage.raycastTarget = false;
            GetSpaceBackdropTexture(true);
        }

        private void SetSpaceBackdropExpanded(bool expanded)
        {
            if (spaceBackdropImage == null)
            {
                return;
            }

            spaceBackdropImage.texture = GetSpaceBackdropTexture(expanded);
        }

        private Texture2D GetSpaceBackdropTexture(bool expanded)
        {
            Texture2D cachedTexture = expanded
                ? expandedSpaceBackdropTexture
                : spaceBackdropTexture;
            if (cachedTexture != null)
            {
                return cachedTexture;
            }

            const int width = 256;
            const int height = 512;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float nebula = Mathf.PerlinNoise(u * 2.4f + 7.1f, v * 3.2f + 2.8f);
                    float alpha = Mathf.Clamp01((nebula - 0.62f) * 0.09f);
                    pixels[y * width + x] = IsPlanetBackdropExclusion(
                        x,
                        y,
                        width,
                        height,
                        expanded)
                        ? Color.clear
                        : new Color(0.08f, 0.22f, 0.48f, alpha);
                }
            }

            System.Random random = new System.Random(302930);
            for (int i = 0; i < 190; i++)
            {
                int centerX = random.Next(2, width - 2);
                int centerY = random.Next(2, height - 2);
                int radius = random.NextDouble() > 0.88 ? 2 : 1;
                float brightness = 0.28f + (float)random.NextDouble() * 0.62f;
                Color starColor = random.NextDouble() > 0.72
                    ? new Color(0.42f, 0.72f, 1f, brightness)
                    : new Color(0.82f, 0.92f, 1f, brightness);

                if (IsPlanetBackdropExclusion(
                    centerX,
                    centerY,
                    width,
                    height,
                    expanded))
                {
                    continue;
                }

                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
                        if (distance > radius)
                        {
                            continue;
                        }

                        int pixelX = centerX + offsetX;
                        int pixelY = centerY + offsetY;
                        if (IsPlanetBackdropExclusion(
                            pixelX,
                            pixelY,
                            width,
                            height,
                            expanded))
                        {
                            continue;
                        }

                        int pixelIndex = pixelY * width + pixelX;
                        float falloff = 1f - distance / (radius + 0.5f);
                        Color existing = pixels[pixelIndex];
                        pixels[pixelIndex] = Color.Lerp(existing, starColor, falloff);
                    }
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            texture.name = expanded
                ? "StarForge Space Backdrop Expanded Exclusion"
                : "StarForge Space Backdrop";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply(true, false);

            if (expanded)
            {
                expandedSpaceBackdropTexture = texture;
            }
            else
            {
                spaceBackdropTexture = texture;
            }

            return texture;
        }

        private static bool IsPlanetBackdropExclusion(
            int x,
            int y,
            int width,
            int height,
            bool expanded)
        {
            float normalizedX = ((float)x + 0.5f) / width;
            float normalizedY = ((float)y + 0.5f) / height;
            float horizontalRadius = expanded ? 0.52f : 0.3f;
            float verticalRadius = expanded ? 0.36f : 0.19f;
            float ellipseX = (normalizedX - 0.5f) / horizontalRadius;
            float ellipseY = (normalizedY - 0.55f) / verticalRadius;
            return ellipseX * ellipseX + ellipseY * ellipseY <= 1f;
        }

        private Image ApplySpaceFrame(
            GameObject target,
            Color fill,
            Color accent,
            float inset)
        {
            Image outer = target.GetComponent<Image>();
            if (outer == null)
            {
                return null;
            }

            Sprite sprite = GetChamferedUiSprite();
            outer.sprite = sprite;
            outer.type = Image.Type.Sliced;
            outer.color = accent;

            Transform existingFill = target.transform.Find("Space Frame Fill");
            Image inner;
            if (existingFill == null)
            {
                GameObject fillObject = new GameObject(
                    "Space Frame Fill",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                fillObject.transform.SetParent(target.transform, false);
                fillObject.transform.SetAsFirstSibling();
                fillObject.GetComponent<LayoutElement>().ignoreLayout = true;
                inner = fillObject.GetComponent<Image>();
                inner.raycastTarget = false;
            }
            else
            {
                inner = existingFill.GetComponent<Image>();
            }

            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(inset, inset);
            innerRect.offsetMax = new Vector2(-inset, -inset);
            inner.sprite = sprite;
            inner.type = Image.Type.Sliced;
            inner.color = fill;

            Transform existingSheen = target.transform.Find("Space Frame Sheen");
            if (existingSheen == null)
            {
                GameObject sheenObject = new GameObject(
                    "Space Frame Sheen",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                sheenObject.transform.SetParent(target.transform, false);
                sheenObject.transform.SetSiblingIndex(1);
                sheenObject.GetComponent<LayoutElement>().ignoreLayout = true;
                Image sheen = sheenObject.GetComponent<Image>();
                sheen.sprite = sprite;
                sheen.type = Image.Type.Sliced;
                sheen.color = new Color(0.45f, 0.78f, 1f, 0.035f);
                sheen.raycastTarget = false;

                RectTransform sheenRect = sheenObject.GetComponent<RectTransform>();
                sheenRect.anchorMin = new Vector2(0.025f, 0.56f);
                sheenRect.anchorMax = new Vector2(0.975f, 0.96f);
                sheenRect.offsetMin = Vector2.zero;
                sheenRect.offsetMax = Vector2.zero;
            }

            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                inner.raycastTarget = true;
                button.targetGraphic = inner;
            }

            AddPanelOutline(
                target,
                new Color(accent.r * 0.55f, accent.g * 0.72f, accent.b, accent.a),
                new Vector2(1.25f, -1.25f));
            return inner;
        }

        private Image ApplyCanvasFrame(
            GameObject target,
            Color fill,
            Color accent,
            float inset)
        {
            Image inner = ApplySpaceFrame(target, fill, accent, inset);
            if (target == null)
            {
                return inner;
            }

            Transform sheen = target.transform.Find("Space Frame Sheen");
            if (sheen != null)
            {
                sheen.gameObject.SetActive(false);
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(
                    accent.r * 0.32f,
                    accent.g * 0.5f,
                    accent.b,
                    Mathf.Min(accent.a, 0.58f));
                outline.effectDistance = new Vector2(0.75f, -0.75f);
            }

            return inner;
        }

        private void ApplyCanvasButtonStyle(
            Button button,
            Color fill,
            Color accent,
            Color textColor)
        {
            if (button == null)
            {
                return;
            }

            Image target = ApplyCanvasFrame(button.gameObject, fill, accent, 1.5f);
            if (target != null)
            {
                target.raycastTarget = true;
                button.targetGraphic = target;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.67f, 0.79f, 0.94f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.38f, 0.42f, 0.5f, 0.68f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            SetButtonTextColor(button, textColor);

            Text label = button.GetComponentInChildren<Text>();
            if (label != null && label.GetComponent<Shadow>() == null)
            {
                Shadow shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private static void SetRectAnchors(
            GameObject target,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchRect(RectTransform rect, Vector2 inset)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static void CreateHorizontalDivider(
            Transform parent,
            float normalizedY,
            Color color,
            float minX,
            float maxX)
        {
            GameObject divider = new GameObject(
                "Horizontal Divider",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            divider.transform.SetParent(parent, false);
            divider.GetComponent<LayoutElement>().ignoreLayout = true;

            RectTransform rect = divider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, normalizedY);
            rect.anchorMax = new Vector2(maxX, normalizedY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 1.5f);
            rect.anchoredPosition = Vector2.zero;

            Image image = divider.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private Sprite GetChamferedUiSprite()
        {
            if (chamferedUiSprite != null)
            {
                return chamferedUiSprite;
            }

            const int size = 64;
            const float radius = 16f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StarForge Rounded UI";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed-distance rounded rectangle with a 1px anti-aliased edge so
                    // every panel/button corner reads as a soft, space-console curve.
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px < radius
                        ? radius - px
                        : (px > size - radius ? px - (size - radius) : 0f);
                    float dy = py < radius
                        ? radius - py
                        : (py > size - radius ? py - (size - radius) : 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] =
                        new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            chamferedUiSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            chamferedUiSprite.name = "StarForge Rounded UI Sprite";
            return chamferedUiSprite;
        }

        private Text CreateText(string text, int fontSize, FontStyle style, TextAnchor anchor, Transform parent)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            SetPreferredHeight(label, Mathf.Ceil(fontSize * 1.25f) + 6f);
            return label;
        }

        private Sprite GetMaterialIcon(int index)
        {
            if (materialIconSprites != null &&
                index >= 0 &&
                index < materialIconSprites.Length &&
                materialIconSprites[index] != null)
            {
                return materialIconSprites[index];
            }

            if (fallbackMaterialIcon == null)
            {
                fallbackMaterialIcon = CreateCirclePlaceholderSprite();
            }

            return fallbackMaterialIcon;
        }

        private static Sprite[] LoadMaterialIconSprites()
        {
            Sprite[] sprites = new Sprite[5];
            for (int i = 0; i < sprites.Length; i++)
            {
                string path = "MaterialIcons/Material_" + (i + 1);
                sprites[i] = Resources.Load<Sprite>(path);
                if (sprites[i] != null)
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprites[i] = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            return sprites;
        }

        private static Sprite CreateCirclePlaceholderSprite()
        {
            const int size = 64;
            const float radius = 24f;
            const float thickness = 5f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StarForge Material Placeholder";
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool isRing = distance >= radius - thickness && distance <= radius + thickness;
                    texture.SetPixel(x, y, isRing ? Color.black : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void AddPanelOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddVerticalDivider(
            Transform parent,
            float normalizedX,
            Color color)
        {
            GameObject divider = new GameObject(
                "Vertical Divider",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            divider.transform.SetParent(parent, false);
            divider.GetComponent<LayoutElement>().ignoreLayout = true;

            RectTransform rect = divider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(normalizedX, 0.12f);
            rect.anchorMax = new Vector2(normalizedX, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1.5f, 0f);
            rect.anchoredPosition = Vector2.zero;

            Image image = divider.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void AddFourSideBorder(GameObject target, Color color, float thickness)
        {
            RectTransform top = CreateBorderEdge("Border Top", target.transform, color);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(0.5f, 1f);
            top.offsetMin = new Vector2(0f, -thickness);
            top.offsetMax = Vector2.zero;

            RectTransform bottom = CreateBorderEdge("Border Bottom", target.transform, color);
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.offsetMin = Vector2.zero;
            bottom.offsetMax = new Vector2(0f, thickness);

            RectTransform left = CreateBorderEdge("Border Left", target.transform, color);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0f, 0.5f);
            left.offsetMin = Vector2.zero;
            left.offsetMax = new Vector2(thickness, 0f);

            RectTransform right = CreateBorderEdge("Border Right", target.transform, color);
            right.anchorMin = new Vector2(1f, 0f);
            right.anchorMax = Vector2.one;
            right.pivot = new Vector2(1f, 0.5f);
            right.offsetMin = new Vector2(-thickness, 0f);
            right.offsetMax = Vector2.zero;
        }

        private static void SetFixedLayoutSize(
            GameObject target,
            float width,
            float height)
        {
            LayoutElement layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = target.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
            layoutElement.flexibleWidth = 0f;
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 0f;
        }

        private static RectTransform CreateBorderEdge(string name, Transform parent, Color color)
        {
            GameObject edge = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            edge.transform.SetParent(parent, false);

            Image image = edge.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            edge.GetComponent<LayoutElement>().ignoreLayout = true;
            return edge.GetComponent<RectTransform>();
        }

        private static Color GetLevelTextColor(int level)
        {
            if (level >= 30)
            {
                return new Color(1f, 0.94f, 0.28f, 1f);
            }

            if (level >= 29)
            {
                return new Color(0.78f, 0.38f, 1f, 1f);
            }

            if (level >= 28)
            {
                return new Color(1f, 0.32f, 0.76f, 1f);
            }

            if (level >= 25)
            {
                return new Color(1f, 0.5f, 0.22f, 1f);
            }

            if (level >= 20)
            {
                return new Color(1f, 0.76f, 0.24f, 1f);
            }

            if (level >= 15)
            {
                return new Color(0.42f, 1f, 0.68f, 1f);
            }

            if (level >= 10)
            {
                return new Color(0.28f, 0.9f, 1f, 1f);
            }

            if (level >= 5)
            {
                return new Color(0.48f, 0.68f, 1f, 1f);
            }

            return new Color(0.9f, 0.96f, 1f, 1f);
        }

        private static Color GetLevelOutlineColor(int level)
        {
            if (level >= 30)
            {
                return new Color(1f, 0.28f, 0.02f, 0.9f);
            }

            if (level >= 29)
            {
                return new Color(0.22f, 0.04f, 0.48f, 0.95f);
            }

            if (level >= 28)
            {
                return new Color(0.42f, 0.02f, 0.34f, 0.95f);
            }

            if (level >= 25)
            {
                return new Color(0.48f, 0.12f, 0.02f, 0.95f);
            }

            if (level >= 20)
            {
                return new Color(0.42f, 0.28f, 0.02f, 0.9f);
            }

            if (level >= 15)
            {
                return new Color(0.04f, 0.34f, 0.18f, 0.9f);
            }

            if (level >= 10)
            {
                return new Color(0.02f, 0.24f, 0.42f, 0.9f);
            }

            if (level >= 5)
            {
                return new Color(0.04f, 0.1f, 0.36f, 0.9f);
            }

            return new Color(0.03f, 0.06f, 0.12f, 0.85f);
        }

        private static void SetButtonTextColor(Button button, Color color)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = color;
            }
        }

        private static RectTransform CreateSafeAreaRoot(RectTransform root)
        {
            GameObject safeAreaObject = new GameObject("Safe Area", typeof(RectTransform), typeof(StarForgeSafeArea));
            safeAreaObject.transform.SetParent(root, false);
            RectTransform safeArea = safeAreaObject.GetComponent<RectTransform>();
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            return safeArea;
        }

        private RectTransform CreateMainHudRoot(RectTransform root)
        {
            GameObject mainHudObject = new GameObject("Main HUD", typeof(RectTransform), typeof(CanvasGroup));
            mainHudObject.transform.SetParent(root, false);
            RectTransform mainHud = mainHudObject.GetComponent<RectTransform>();
            mainHud.anchorMin = Vector2.zero;
            mainHud.anchorMax = Vector2.one;
            mainHud.offsetMin = Vector2.zero;
            mainHud.offsetMax = Vector2.zero;
            mainHudCanvasGroup = mainHudObject.GetComponent<CanvasGroup>();
            return mainHud;
        }

        private static void SetPreferredHeight(Text text, float height)
        {
            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = text.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        private static string GetResultTitle(StarForgeEnhancementResult result)
        {
            if (result.discoveredBlackHole)
            {
                return "블랙홀 발견";
            }

            if (result.isBlackHole)
            {
                switch (result.kind)
                {
                    case StarForgeResultKind.Success:
                        return "블랙홀 강화 성공";
                    case StarForgeResultKind.Destroyed:
                        return "블랙홀 소멸";
                    case StarForgeResultKind.MaxLevel:
                        return "블랙홀 최대 등급";
                }
            }

            switch (result.kind)
            {
                case StarForgeResultKind.Success:
                    return "융합 성공";
                case StarForgeResultKind.GreatSuccess:
                    return "융합 대성공";
                case StarForgeResultKind.Fracture:
                    return "균열 발생";
                case StarForgeResultKind.Destroyed:
                    return "소멸";
                case StarForgeResultKind.Failure:
                    return "일반 실패";
                default:
                    return "알림";
            }
        }

        private static string BuildResultBody(StarForgeEnhancementResult result)
        {
            if (result.discoveredBlackHole)
            {
                return "당신은 블랙홀을 발견했습니다.\n\n" +
                       "블랙홀은 등급이 높아질수록\n" +
                       "분해 시 훨씬 많은 보상을 지급하지만,\n" +
                       "소멸 시에는 아무런 보상도 획득할 수 없습니다.";
            }

            if (result.isBlackHole)
            {
                if (result.kind == StarForgeResultKind.Destroyed)
                {
                    return result.message + "\n회수 보상: 없음";
                }

                if (result.kind == StarForgeResultKind.Success)
                {
                    return result.message +
                           "\n블랙홀 " +
                           result.previousBlackHoleLevel +
                           "강 → " +
                           result.newBlackHoleLevel +
                           "강";
                }

                return result.message;
            }

            string body = result.message;
            if (result.kind != StarForgeResultKind.Failure &&
                result.kind != StarForgeResultKind.Fracture)
            {
                body += "\n" +
                        result.previousLevel +
                        "강 → " +
                        result.newLevel +
                        "강";
            }

            if (result.kind == StarForgeResultKind.Destroyed)
            {
                body += "\n회수 보상: " + StarForgeFormat.CurrencyList(result.rewards);
            }

            return body;
        }

        private static void EnsureEventSystem()
        {
            GameObject eventSystemObject;
            if (EventSystem.current != null)
            {
                eventSystemObject = EventSystem.current.gameObject;
            }
            else
            {
                eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            }
#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                UnityEngine.Object.Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
#else
            if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
#endif
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(eventSystemObject);
            }
        }
    }

    internal sealed class StarForgeDragRotationInput :
        MonoBehaviour,
        IDragHandler
    {
        public event Action<Vector2> Dragged;

        public void OnDrag(PointerEventData eventData)
        {
            Dragged?.Invoke(eventData.delta);
        }
    }
}
