using System;
using System.Collections;
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
        private sealed class CollectionCard
        {
            public int level;
            public Button button;
            public RawImage planetImage;
            public Text levelText;
            public Text nameText;
        }

        private readonly StarForgeEnhancementService previewService = new StarForgeEnhancementService();
        private readonly StarForgeMaterialExchangeService exchangeService = new StarForgeMaterialExchangeService();
        public event Action EnhanceClicked;
        public event Action ResetConfirmed;
        public event Action<StarForgeCurrencyType> CurrencySelected;
        public event Action<int, int> MaterialExchangeRequested;
        public event Action<bool> SoundToggled;
        public event Action<bool> VibrationToggled;
        public event Action<int> ReviveRequested;

        private readonly Button[] currencyButtons = new Button[5];
        private readonly Text[] currencyButtonTexts = new Text[5];
        private readonly Text[] currencyQuantityTexts = new Text[5];
        private readonly Image[] currencyIconImages = new Image[5];
        private readonly Button[] exchangeButtons = new Button[8];
        private readonly Text[] exchangeSourceOwnedTexts = new Text[8];
        private readonly Text[] exchangeRouteStatusTexts = new Text[8];
        private readonly Image[] exchangeSourceIcons = new Image[8];
        private readonly Image[] exchangeTargetIcons = new Image[8];
        private CollectionCard[] collectionCards;

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
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultBodyText;
        private GameObject settingsPanel;
        private GameObject resetConfirmPanel;
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
        private int pendingExchangeRouteIndex = -1;
        private Button collectionOpenButton;
        private GameObject collectionPanel;
        private ScrollRect collectionScrollRect;
        private RectTransform collectionViewportRect;
        private GridLayoutGroup collectionGrid;
        private Text collectionProgressText;
        private GameObject collectionDetailPanel;
        private Text collectionDetailTitleText;
        private Text collectionDetailLevelText;
        private RawImage collectionDetailPlanetImage;
        private StarForgeCollectionPreview collectionPreview;
        private Sprite collectionCircleSprite;
        private Toggle soundToggle;
        private Toggle vibrationToggle;
        private StarForgeBalance balanceRef;
        private CanvasGroup bottomHudCanvasGroup;
        private Coroutine bottomHudFadeRoutine;
        private bool bottomHudVisible = true;
        private GameObject revivePanel;
        private Text reviveBodyText;
        private ReviveRow[] reviveRows;
        private int reviveDestroyedLevel;
        private bool isBuilt;

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
            background.color = new Color(0.02f, 0.03f, 0.07f, 0.82f);

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform safeAreaRoot = CreateSafeAreaRoot(root);

            BuildTopPanel(safeAreaRoot);
            BuildSettingsButton(safeAreaRoot);
            BuildMaterialPanel(safeAreaRoot, balance);
            BuildResultPanel(safeAreaRoot);
            BuildSettingsPanel(safeAreaRoot);
            BuildResetConfirmPanel(safeAreaRoot);
            BuildExchangePanel(safeAreaRoot);
            BuildCollectionPanel(safeAreaRoot, balance);
            BuildRevivePanel(safeAreaRoot, balance);
            isBuilt = true;
        }

        public void Refresh(
            StarForgeSaveData saveData,
            StarForgeBalance balance,
            StarForgeCurrencyType selectedCurrency,
            StarForgeAttemptPreview preview,
            bool isBusy)
        {
            StageVisualConfig stage = balance.GetStage(saveData.currentLevel);
            CurrencyConfig selectedConfig = balance.GetCurrency(selectedCurrency);

            levelText.text = saveData.currentLevel + "강  " + stage.displayName;
            levelText.color = GetLevelTextColor(saveData.currentLevel);
            if (levelTextOutline != null)
            {
                levelTextOutline.effectColor = GetLevelOutlineColor(saveData.currentLevel);
            }

            highestText.text = "최고 기록 " + saveData.highestLevel + "강";
            selectedMaterialText.text = selectedConfig.displayName;
            if (selectedMaterialIconImage != null)
            {
                selectedMaterialIconImage.sprite = GetMaterialIcon((int)selectedCurrency);
                selectedMaterialIconImage.color = preview.isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            if (preview.isMaxLevel)
            {
                chanceText.text = "성공확률 : MAX";
                riskText.text = "더 이상 강화할 수 없습니다.";
            }
            else if (!preview.isAvailable)
            {
                chanceText.text = "성공확률 : -";
                riskText.text = "다른 재료를 선택하세요.";
            }
            else
            {
                chanceText.text = "성공확률 : " + StarForgeFormat.Percent(preview.successRatePercent);
                riskText.text = "실패시 " + StarForgeFormat.Percent(preview.destructionChancePercent) + " 확률로 소멸";
            }

            statusText.text = saveData.isFractured
                ? "상태 : 균열"
                : "상태 : 안정";

            bool canEnhance = !isBusy && preview.isAvailable && preview.hasEnoughCurrency && !preview.isMaxLevel;
            enhanceButton.interactable = canEnhance;
            SetBottomHudVisible(!isBusy);

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
                button.interactable = !isBusy;
                button.image.color = isSelected
                    ? new Color(0.08f, 0.16f, 0.32f, 1f)
                    : new Color(0.035f, 0.055f, 0.105f, 1f);

                Color contentColor = currencyPreview.isAvailable
                    ? new Color(0.88f, 0.95f, 1f, 1f)
                    : new Color(0.42f, 0.48f, 0.58f, 1f);
                Color quantityColor = !currencyPreview.isAvailable
                    ? new Color(0.42f, 0.48f, 0.58f, 1f)
                    : currencyPreview.hasEnoughCurrency
                        ? new Color(0.36f, 0.86f, 1f, 1f)
                        : new Color(1f, 0.45f, 0.48f, 1f);
                text.text = config.displayName;
                text.color = contentColor;
                quantityText.text = "x" + StarForgeFormat.Number(saveData.GetCurrency(currencyType));
                quantityText.color = quantityColor;
                icon.color = currencyPreview.isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            if (soundToggle != null)
            {
                soundToggle.SetIsOnWithoutNotify(saveData.soundEnabled);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(saveData.vibrationEnabled);
            }

            RefreshExchangePanel(saveData, isBusy);
            RefreshCollectionPanel(saveData, balance, isBusy);
        }

        private void OnDestroy()
        {
            if (collectionPreview != null)
            {
                Destroy(collectionPreview.gameObject);
            }

            if (collectionCircleSprite != null)
            {
                Texture texture = collectionCircleSprite.texture;
                Destroy(collectionCircleSprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        public void ShowResult(StarForgeEnhancementResult result)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            resultTitleText.text = GetResultTitle(result.kind);
            resultBodyText.text = BuildResultBody(result);
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
            GameObject panel = CreatePanel("Top HUD", root, new Color(0.045f, 0.065f, 0.11f, 0.92f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.8f);
            rect.anchorMax = new Vector2(0.92f, 0.905f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 14);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            levelText = CreateText("0강 우주 먼지", 28, FontStyle.Bold, TextAnchor.MiddleCenter, panel.transform);
            levelTextOutline = levelText.gameObject.AddComponent<Outline>();
            levelTextOutline.effectColor = GetLevelOutlineColor(0);
            levelTextOutline.effectDistance = new Vector2(2f, -2f);
            highestText = CreateText("최고 기록 0강", 17, FontStyle.Normal, TextAnchor.MiddleCenter, panel.transform);
            SetPreferredHeight(levelText, 50f);
            SetPreferredHeight(highestText, 28f);
            levelText.resizeTextForBestFit = true;
            levelText.resizeTextMinSize = 22;
            levelText.resizeTextMaxSize = 34;
        }

        private void BuildSettingsButton(RectTransform root)
        {
            exchangeOpenButton = CreateButton("재료 교환", 16, root);
            RectTransform exchangeRect = exchangeOpenButton.GetComponent<RectTransform>();
            exchangeRect.anchorMin = new Vector2(0.55f, 0.93f);
            exchangeRect.anchorMax = new Vector2(0.73f, 0.985f);
            exchangeRect.offsetMin = new Vector2(0f, -30f);
            exchangeRect.offsetMax = new Vector2(0f, -30f);

            exchangeOpenButton.image.color = new Color(0.075f, 0.045f, 0.13f, 0.96f);
            ColorBlock exchangeColors = exchangeOpenButton.colors;
            exchangeColors.normalColor = new Color(0.075f, 0.045f, 0.13f, 0.96f);
            exchangeColors.highlightedColor = new Color(0.16f, 0.08f, 0.25f, 1f);
            exchangeColors.pressedColor = new Color(0.045f, 0.025f, 0.08f, 1f);
            exchangeOpenButton.colors = exchangeColors;
            SetButtonTextColor(exchangeOpenButton, new Color(0.94f, 0.9f, 1f, 1f));
            AddPanelOutline(
                exchangeOpenButton.gameObject,
                new Color(0.34f, 0.16f, 0.48f, 0.9f),
                new Vector2(1.5f, -1.5f));
            exchangeOpenButton.onClick.AddListener(OpenExchangePanel);

            collectionOpenButton = CreateButton("도감", 16, root);
            RectTransform collectionRect = collectionOpenButton.GetComponent<RectTransform>();
            collectionRect.anchorMin = new Vector2(0.74f, 0.93f);
            collectionRect.anchorMax = new Vector2(0.85f, 0.985f);
            collectionRect.offsetMin = new Vector2(0f, -30f);
            collectionRect.offsetMax = new Vector2(0f, -30f);

            collectionOpenButton.image.color = new Color(0.045f, 0.1f, 0.12f, 0.96f);
            ColorBlock collectionColors = collectionOpenButton.colors;
            collectionColors.normalColor = new Color(0.045f, 0.1f, 0.12f, 0.96f);
            collectionColors.highlightedColor = new Color(0.08f, 0.2f, 0.23f, 1f);
            collectionColors.pressedColor = new Color(0.025f, 0.06f, 0.075f, 1f);
            collectionOpenButton.colors = collectionColors;
            SetButtonTextColor(collectionOpenButton, new Color(0.78f, 0.96f, 1f, 1f));
            AddPanelOutline(
                collectionOpenButton.gameObject,
                new Color(0.12f, 0.38f, 0.42f, 0.9f),
                new Vector2(1.5f, -1.5f));
            collectionOpenButton.onClick.AddListener(OpenCollectionPanel);

            Button settingsButton = CreateButton("설정", 16, root);
            RectTransform rect = settingsButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.86f, 0.93f);
            rect.anchorMax = new Vector2(0.98f, 0.985f);
            rect.offsetMin = new Vector2(0f, -30f);
            rect.offsetMax = new Vector2(0f, -30f);

            settingsButton.image.color = new Color(0.035f, 0.055f, 0.105f, 0.96f);
            ColorBlock colors = settingsButton.colors;
            colors.normalColor = new Color(0.035f, 0.055f, 0.105f, 0.96f);
            colors.highlightedColor = new Color(0.08f, 0.16f, 0.32f, 1f);
            colors.pressedColor = new Color(0.02f, 0.035f, 0.07f, 1f);
            settingsButton.colors = colors;
            SetButtonTextColor(settingsButton, new Color(0.86f, 0.94f, 1f, 1f));
            AddPanelOutline(settingsButton.gameObject, new Color(0.12f, 0.28f, 0.46f, 0.85f), new Vector2(1.5f, -1.5f));
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        }

        private void BuildMaterialPanel(RectTransform root, StarForgeBalance balance)
        {
            GameObject panel = CreatePanel("Bottom HUD", root, new Color(1f, 1f, 1f, 0f));
            bottomHudCanvasGroup = panel.AddComponent<CanvasGroup>();
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0f);
            rect.anchorMax = new Vector2(0.96f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 90f);
            rect.sizeDelta = new Vector2(0f, 394f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerCenter;

            GameObject enhanceButtonBorder = CreatePanel(
                "Enhance Button Border",
                panel.transform,
                new Color(0.16f, 0.035f, 0.24f, 1f));
            LayoutElement enhanceBorderLayout = enhanceButtonBorder.AddComponent<LayoutElement>();
            enhanceBorderLayout.minHeight = 70f;
            enhanceBorderLayout.preferredHeight = 70f;

            enhanceButton = CreateButton("강화", 28, enhanceButtonBorder.transform);
            RectTransform enhanceButtonRect = enhanceButton.GetComponent<RectTransform>();
            enhanceButtonRect.anchorMin = Vector2.zero;
            enhanceButtonRect.anchorMax = Vector2.one;
            enhanceButtonRect.offsetMin = new Vector2(3f, 3f);
            enhanceButtonRect.offsetMax = new Vector2(-3f, -3f);
            enhanceButton.image.color = new Color(0.08f, 0.24f, 0.5f, 1f);
            ColorBlock enhanceColors = enhanceButton.colors;
            enhanceColors.normalColor = new Color(0.08f, 0.24f, 0.5f, 1f);
            enhanceColors.highlightedColor = new Color(0.12f, 0.34f, 0.68f, 1f);
            enhanceColors.pressedColor = new Color(0.04f, 0.16f, 0.34f, 1f);
            enhanceColors.disabledColor = new Color(0.05f, 0.07f, 0.1f, 0.78f);
            enhanceButton.colors = enhanceColors;
            SetButtonTextColor(enhanceButton, new Color(0.92f, 0.98f, 1f, 1f));
            enhanceButton.onClick.AddListener(() => EnhanceClicked?.Invoke());

            GameObject infoPanel = CreatePanel("Enhance Info", panel.transform, new Color(0.035f, 0.04f, 0.075f, 0.98f));
            RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.sizeDelta = new Vector2(520f, 198f);
            LayoutElement infoLayoutElement = infoPanel.AddComponent<LayoutElement>();
            infoLayoutElement.minHeight = 198f;
            infoLayoutElement.preferredHeight = 198f;
            AddPanelOutline(infoPanel, new Color(0.14f, 0.3f, 0.5f, 0.9f), new Vector2(2f, -2f));

            HorizontalLayoutGroup infoLayout = infoPanel.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(8, 8, 2, 2);
            infoLayout.spacing = 10f;
            infoLayout.childAlignment = TextAnchor.MiddleCenter;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = true;

            GameObject selectedColumn = new GameObject("Selected Material Column", typeof(RectTransform), typeof(LayoutElement));
            selectedColumn.transform.SetParent(infoPanel.transform, false);
            LayoutElement selectedColumnLayout = selectedColumn.GetComponent<LayoutElement>();
            selectedColumnLayout.flexibleWidth = 0.78f;
            selectedColumnLayout.preferredWidth = 200f;
            VerticalLayoutGroup selectedLayout = selectedColumn.AddComponent<VerticalLayoutGroup>();
            selectedLayout.padding = new RectOffset(0, 0, 0, 0);
            selectedLayout.spacing = 1f;
            selectedLayout.childAlignment = TextAnchor.MiddleCenter;
            selectedLayout.childControlHeight = true;
            selectedLayout.childForceExpandHeight = false;

            Text selectedTitleText = CreateText("선택 재료", 24, FontStyle.Bold, TextAnchor.MiddleCenter, selectedColumn.transform);
            selectedTitleText.color = new Color(0.9f, 0.96f, 1f, 1f);
            SetPreferredHeight(selectedTitleText, 30f);

            selectedMaterialText = CreateText("운석 파편", 26, FontStyle.Bold, TextAnchor.MiddleCenter, selectedColumn.transform);
            selectedMaterialText.color = new Color(0.9f, 0.96f, 1f, 1f);
            selectedMaterialText.resizeTextForBestFit = true;
            selectedMaterialText.resizeTextMinSize = 18;
            selectedMaterialText.resizeTextMaxSize = 26;
            SetPreferredHeight(selectedMaterialText, 34f);

            GameObject selectedIconObject = new GameObject("Selected Material Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            selectedIconObject.transform.SetParent(selectedColumn.transform, false);
            LayoutElement selectedIconLayout = selectedIconObject.GetComponent<LayoutElement>();
            selectedIconLayout.preferredWidth = 127.5f;
            selectedIconLayout.preferredHeight = 127.5f;
            selectedMaterialIconImage = selectedIconObject.GetComponent<Image>();
            selectedMaterialIconImage.sprite = GetMaterialIcon(0);
            selectedMaterialIconImage.preserveAspect = true;

            GameObject chanceColumn = new GameObject("Chance Column", typeof(RectTransform), typeof(LayoutElement));
            chanceColumn.transform.SetParent(infoPanel.transform, false);
            LayoutElement chanceColumnLayout = chanceColumn.GetComponent<LayoutElement>();
            chanceColumnLayout.flexibleWidth = 1.22f;
            chanceColumnLayout.preferredWidth = 290f;
            VerticalLayoutGroup chanceLayout = chanceColumn.AddComponent<VerticalLayoutGroup>();
            chanceLayout.padding = new RectOffset(0, 0, 0, 0);
            chanceLayout.spacing = 24f;
            chanceLayout.childAlignment = TextAnchor.MiddleLeft;
            chanceLayout.childControlHeight = true;
            chanceLayout.childForceExpandHeight = false;

            chanceText = CreateText("성공확률 : 100%", 26, FontStyle.Bold, TextAnchor.MiddleLeft, chanceColumn.transform);
            riskText = CreateText("실패시 30% 확률로 소멸", 24, FontStyle.Bold, TextAnchor.MiddleLeft, chanceColumn.transform);
            statusText = CreateText("상태 : 안정", 24, FontStyle.Bold, TextAnchor.MiddleLeft, chanceColumn.transform);
            SetPreferredHeight(chanceText, 39f);
            SetPreferredHeight(riskText, 36f);
            SetPreferredHeight(statusText, 36f);

            GameObject materialRow = CreatePanel("Material Slots", panel.transform, new Color(0.025f, 0.04f, 0.085f, 0.98f));
            AddFourSideBorder(materialRow, new Color(0.12f, 0.28f, 0.46f, 0.95f), 2f);
            HorizontalLayoutGroup rowLayout = materialRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(4, 4, 4, 4);
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            LayoutElement rowLayoutElement = materialRow.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 110f;
            rowLayoutElement.preferredHeight = 110f;

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
            resultPanel = CreatePanel("Result Popup", root, new Color(0.02f, 0.025f, 0.05f, 0.96f));
            RectTransform rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.35f);
            rect.anchorMax = new Vector2(0.92f, 0.65f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = resultPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;

            resultTitleText = CreateText("결과", 42, FontStyle.Bold, TextAnchor.MiddleCenter, resultPanel.transform);
            resultBodyText = CreateText("", 28, FontStyle.Normal, TextAnchor.MiddleCenter, resultPanel.transform);
            resultBodyText.resizeTextForBestFit = true;
            resultBodyText.resizeTextMinSize = 20;
            resultBodyText.resizeTextMaxSize = 28;

            Button closeButton = CreateButton("확인", 28, resultPanel.transform);
            closeButton.GetComponent<LayoutElement>().preferredHeight = 72f;
            closeButton.onClick.AddListener(() => resultPanel.SetActive(false));

            resultPanel.SetActive(false);
        }

        private void BuildSettingsPanel(RectTransform root)
        {
            settingsPanel = CreatePanel("Settings Popup", root, new Color(0.02f, 0.025f, 0.05f, 0.96f));
            RectTransform rect = settingsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.28f);
            rect.anchorMax = new Vector2(0.92f, 0.72f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = settingsPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateText("설정", 42, FontStyle.Bold, TextAnchor.MiddleCenter, settingsPanel.transform);
            soundToggle = CreateToggle("효과음", settingsPanel.transform);
            soundToggle.onValueChanged.AddListener(value => SoundToggled?.Invoke(value));
            vibrationToggle = CreateToggle("진동", settingsPanel.transform);
            vibrationToggle.onValueChanged.AddListener(value => VibrationToggled?.Invoke(value));

            Button resetButton = CreateButton("데이터 초기화", 28, settingsPanel.transform);
            resetButton.GetComponent<LayoutElement>().preferredHeight = 72f;
            resetButton.image.color = new Color(0.55f, 0.18f, 0.18f, 1f);
            resetButton.onClick.AddListener(() => resetConfirmPanel.SetActive(true));

            Button closeButton = CreateButton("닫기", 28, settingsPanel.transform);
            closeButton.GetComponent<LayoutElement>().preferredHeight = 72f;
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

            VerticalLayoutGroup layout = resetConfirmPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateText("데이터 초기화", 40, FontStyle.Bold, TextAnchor.MiddleCenter, resetConfirmPanel.transform);
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
            confirmButton.image.color = new Color(0.55f, 0.18f, 0.18f, 1f);
            confirmButton.onClick.AddListener(() =>
            {
                resetConfirmPanel.SetActive(false);
                settingsPanel.SetActive(false);
                ResetConfirmed?.Invoke();
            });

            Button cancelButton = CreateButton("취소", 26, row.transform);
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
            AddPanelOutline(dialog, new Color(0.28f, 0.12f, 0.46f, 0.95f), new Vector2(2f, -2f));

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
                AddFourSideBorder(
                    button.gameObject,
                    new Color(0.48f, 0.22f, 0.66f, 1f),
                    2f);

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
            dialogRect.anchorMin = new Vector2(0.13f, 0.34f);
            dialogRect.anchorMax = new Vector2(0.87f, 0.66f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            AddFourSideBorder(dialog, new Color(0.48f, 0.22f, 0.66f, 1f), 3f);

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

            exchangeQuantityPanel.SetActive(false);
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
            AddFourSideBorder(inputObject, new Color(0.12f, 0.42f, 0.52f, 1f), 2f);

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
            exchangeQuantityPanel.SetActive(true);
            exchangeQuantityInput.Select();
            exchangeQuantityInput.ActivateInputField();
        }

        private void CloseExchangeQuantityPanel()
        {
            pendingExchangeRouteIndex = -1;
            if (exchangeQuantityPanel != null)
            {
                exchangeQuantityPanel.SetActive(false);
            }
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
            AddFourSideBorder(card, new Color(0.12f, 0.34f, 0.4f, 1f), 2f);

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
                new Color(0.005f, 0.009f, 0.02f, 0.99f));
            RectTransform overlayRect = collectionPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject dialog = CreatePanel(
                "Star Collection",
                collectionPanel.transform,
                new Color(0.018f, 0.028f, 0.055f, 1f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.035f, 0.035f);
            dialogRect.anchorMax = new Vector2(0.965f, 0.965f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            AddFourSideBorder(dialog, new Color(0.12f, 0.38f, 0.42f, 1f), 2f);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            Text title = CreateText(
                "별의 도감",
                34,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            title.color = new Color(0.78f, 0.96f, 1f, 1f);
            SetPreferredHeight(title, 44f);

            collectionProgressText = CreateText(
                "",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            collectionProgressText.color = new Color(0.55f, 0.75f, 0.84f, 1f);
            SetPreferredHeight(collectionProgressText, 30f);

            GameObject scrollView = CreatePanel(
                "Collection Scroll View",
                dialog.transform,
                new Color(0.008f, 0.015f, 0.03f, 0.9f));
            LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 600f;
            scrollLayout.flexibleHeight = 1f;

            collectionScrollRect = scrollView.AddComponent<ScrollRect>();
            collectionScrollRect.horizontal = false;
            collectionScrollRect.vertical = true;
            collectionScrollRect.movementType = ScrollRect.MovementType.Clamped;
            collectionScrollRect.inertia = true;
            collectionScrollRect.scrollSensitivity = 32f;

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
            viewportRect.offsetMax = new Vector2(-22f, -4f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            collectionGrid = content.GetComponent<GridLayoutGroup>();
            collectionGrid.padding = new RectOffset(8, 8, 4, 4);
            collectionGrid.spacing = new Vector2(10f, 10f);
            collectionGrid.cellSize = new Vector2(280f, 164f);
            collectionGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            collectionGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            collectionGrid.childAlignment = TextAnchor.UpperCenter;
            collectionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            collectionGrid.constraintCount = 2;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject scrollbarObject = CreatePanel(
                "Scrollbar Vertical",
                scrollView.transform,
                new Color(0.025f, 0.04f, 0.065f, 1f));
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-14f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);

            GameObject handleObject = CreatePanel(
                "Handle",
                scrollbarObject.transform,
                new Color(0.12f, 0.42f, 0.46f, 1f));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleObject.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            collectionScrollRect.viewport = viewportRect;
            collectionScrollRect.content = contentRect;
            collectionScrollRect.verticalScrollbar = scrollbar;
            collectionScrollRect.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;
            collectionViewportRect = viewportRect;

            int cardCount = Mathf.Max(1, balance.maxLevel + 1);
            collectionCards = new CollectionCard[cardCount];
            for (int level = 0; level < cardCount; level++)
            {
                collectionCards[level] = CreateCollectionCard(level, content.transform);
            }

            Button closeButton = CreateButton("닫기", 24, dialog.transform);
            closeButton.GetComponent<LayoutElement>().preferredHeight = 52f;
            closeButton.onClick.AddListener(CloseCollectionPanel);

            BuildCollectionDetailPanel(collectionPanel.transform);
            collectionPanel.SetActive(false);
        }

        private CollectionCard CreateCollectionCard(int level, Transform parent)
        {
            GameObject cardObject = new GameObject(
                "Collection Stage " + level,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            cardObject.transform.SetParent(parent, false);

            Image background = cardObject.GetComponent<Image>();
            background.color = new Color(0.035f, 0.07f, 0.09f, 1f);
            AddFourSideBorder(cardObject, new Color(0.1f, 0.32f, 0.36f, 1f), 2f);

            Button button = cardObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.035f, 0.07f, 0.09f, 1f);
            colors.highlightedColor = new Color(0.07f, 0.15f, 0.18f, 1f);
            colors.pressedColor = new Color(0.02f, 0.045f, 0.06f, 1f);
            colors.disabledColor = new Color(0.018f, 0.025f, 0.035f, 1f);
            button.colors = colors;

            GameObject portraitObject = new GameObject(
                "Planet Portrait",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            portraitObject.transform.SetParent(cardObject.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(68f, 0f);
            portraitRect.sizeDelta = new Vector2(108f, 108f);
            Image portraitMask = portraitObject.GetComponent<Image>();
            portraitMask.sprite = GetCollectionCircleSprite();
            portraitMask.color = Color.white;
            portraitObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject imageObject = new GameObject(
                "Planet Surface",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            imageObject.transform.SetParent(portraitObject.transform, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            RawImage planetImage = imageObject.GetComponent<RawImage>();
            planetImage.uvRect = new Rect(0.25f, 0f, 0.5f, 1f);
            planetImage.raycastTarget = false;
            AspectRatioFitter portraitAspect = imageObject.GetComponent<AspectRatioFitter>();
            portraitAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            portraitAspect.aspectRatio = 1f;

            Text levelText = CreateText(
                level + "강",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                cardObject.transform);
            RectTransform levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 0.52f);
            levelRect.anchorMax = new Vector2(1f, 0.85f);
            levelRect.offsetMin = new Vector2(132f, 0f);
            levelRect.offsetMax = new Vector2(-12f, 0f);

            Text nameText = CreateText(
                "미발견",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                cardObject.transform);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.18f);
            nameRect.anchorMax = new Vector2(1f, 0.55f);
            nameRect.offsetMin = new Vector2(132f, 0f);
            nameRect.offsetMax = new Vector2(-12f, 0f);
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 13;
            nameText.resizeTextMaxSize = 18;

            int capturedLevel = level;
            button.onClick.AddListener(() => OpenCollectionDetail(capturedLevel));

            CollectionCard card = new CollectionCard();
            card.level = level;
            card.button = button;
            card.planetImage = planetImage;
            card.levelText = levelText;
            card.nameText = nameText;
            return card;
        }

        private void BuildCollectionDetailPanel(Transform parent)
        {
            collectionDetailPanel = CreatePanel(
                "Collection Detail Overlay",
                parent,
                new Color(0.003f, 0.006f, 0.015f, 0.985f));
            RectTransform overlayRect = collectionDetailPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject card = CreatePanel(
                "Collection Detail Card",
                collectionDetailPanel.transform,
                new Color(0.012f, 0.02f, 0.04f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.07f, 0.08f);
            cardRect.anchorMax = new Vector2(0.93f, 0.92f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            AddFourSideBorder(card, new Color(0.14f, 0.44f, 0.48f, 1f), 3f);

            collectionDetailTitleText = CreateText(
                "",
                34,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform titleRect = collectionDetailTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, 0.86f);
            titleRect.anchorMax = new Vector2(0.92f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            collectionDetailTitleText.color = new Color(0.86f, 0.98f, 1f, 1f);

            collectionDetailLevelText = CreateText(
                "",
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform levelRect = collectionDetailLevelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.08f, 0.8f);
            levelRect.anchorMax = new Vector2(0.92f, 0.86f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            collectionDetailLevelText.color = new Color(0.48f, 0.78f, 0.88f, 1f);

            GameObject previewRegion = new GameObject(
                "Collection Planet Preview Region",
                typeof(RectTransform));
            previewRegion.transform.SetParent(card.transform, false);
            RectTransform previewRegionRect = previewRegion.GetComponent<RectTransform>();
            previewRegionRect.anchorMin = new Vector2(0.05f, 0.12f);
            previewRegionRect.anchorMax = new Vector2(0.95f, 0.79f);
            previewRegionRect.offsetMin = Vector2.zero;
            previewRegionRect.offsetMax = Vector2.zero;

            GameObject previewObject = new GameObject(
                "Collection Planet Preview",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            previewObject.transform.SetParent(previewRegion.transform, false);
            RectTransform previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            collectionDetailPlanetImage = previewObject.GetComponent<RawImage>();
            collectionDetailPlanetImage.color = Color.white;
            collectionDetailPlanetImage.raycastTarget = false;
            collectionDetailPlanetImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            AspectRatioFitter previewAspect = previewObject.GetComponent<AspectRatioFitter>();
            previewAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            previewAspect.aspectRatio = 1f;

            GameObject collectionPreviewObject = new GameObject("StarForge Collection Preview");
            collectionPreview = collectionPreviewObject.AddComponent<StarForgeCollectionPreview>();
            collectionDetailPlanetImage.texture = collectionPreview.OutputTexture;
            collectionPreview.Hide();

            Button closeButton = CreateButton("닫기", 22, card.transform);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.34f, 0.025f);
            closeRect.anchorMax = new Vector2(0.66f, 0.095f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            closeButton.onClick.AddListener(CloseCollectionDetail);

            collectionDetailPanel.SetActive(false);
        }

        private void OpenCollectionPanel()
        {
            if (collectionPanel == null)
            {
                return;
            }

            CloseCollectionDetail();
            collectionPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            RefreshCollectionGridLayout();
            Canvas.ForceUpdateCanvases();
            collectionScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshCollectionGridLayout()
        {
            if (collectionGrid == null || collectionViewportRect == null)
            {
                return;
            }

            float availableWidth =
                collectionViewportRect.rect.width -
                collectionGrid.padding.horizontal -
                collectionGrid.spacing.x;
            float cardWidth = Mathf.Floor(availableWidth * 0.5f);
            if (cardWidth <= 0f)
            {
                return;
            }

            collectionGrid.cellSize = new Vector2(cardWidth, 164f);
        }

        private void CloseCollectionPanel()
        {
            CloseCollectionDetail();
            collectionPanel.SetActive(false);
        }

        private void OpenCollectionDetail(int level)
        {
            if (balanceRef == null ||
                collectionDetailPanel == null ||
                collectionCards == null ||
                level < 0 ||
                level >= collectionCards.Length ||
                !collectionCards[level].button.interactable)
            {
                return;
            }

            StageVisualConfig stage = balanceRef.GetStage(level);
            collectionDetailTitleText.text = stage.displayName;
            collectionDetailTitleText.color = GetLevelTextColor(level);
            collectionDetailLevelText.text = level + "강 도감 기록";
            collectionDetailPanel.SetActive(true);
            collectionPreview.Show(stage);
        }

        private void CloseCollectionDetail()
        {
            if (collectionPreview != null)
            {
                collectionPreview.Hide();
            }

            if (collectionDetailPanel != null)
            {
                collectionDetailPanel.SetActive(false);
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

            if (collectionCards == null || collectionProgressText == null)
            {
                return;
            }

            int highestLevel = Mathf.Clamp(saveData.highestLevel, 0, balance.maxLevel);
            collectionProgressText.text =
                "발견 " +
                (highestLevel + 1) +
                " / " +
                (balance.maxLevel + 1);

            for (int i = 0; i < collectionCards.Length; i++)
            {
                CollectionCard card = collectionCards[i];
                StageVisualConfig stage = balance.GetStage(card.level);
                bool unlocked = card.level <= highestLevel;

                card.button.interactable = unlocked && !isBusy;
                card.levelText.text = card.level + "강";
                card.levelText.color = unlocked
                    ? GetLevelTextColor(card.level)
                    : new Color(0.42f, 0.48f, 0.56f, 1f);
                card.nameText.text = unlocked ? stage.displayName : "미발견";
                card.nameText.color = unlocked
                    ? new Color(0.8f, 0.94f, 1f, 1f)
                    : new Color(0.32f, 0.36f, 0.42f, 1f);

                if (unlocked)
                {
                    Color stageColor;
                    if (!ColorUtility.TryParseHtmlString(stage.color, out stageColor))
                    {
                        stageColor = Color.white;
                    }

                    StarForgePlanetSurface surface =
                        StarForgePlanetTextureFactory.Get(card.level, stageColor);
                    card.planetImage.texture = surface.baseMap;
                    card.planetImage.color = Color.white;
                }
                else
                {
                    card.planetImage.texture = Texture2D.whiteTexture;
                    card.planetImage.color = new Color(0.005f, 0.007f, 0.012f, 1f);
                }
            }
        }

        public void ShowReviveOverlay(StarForgeEnhancementResult result, StarForgeSaveData saveData)
        {
            if (revivePanel == null || result == null)
            {
                return;
            }

            reviveDestroyedLevel = result.previousLevel;
            reviveBodyText.text =
                result.previousLevel + "강에서 행성이 소멸했습니다.\n" +
                "회수 보상: " + StarForgeFormat.CurrencyList(result.rewards);
            RefreshReviveRows(saveData);
            revivePanel.SetActive(true);
        }

        private void SetBottomHudVisible(bool visible)
        {
            if (bottomHudCanvasGroup == null || bottomHudVisible == visible)
            {
                return;
            }

            bottomHudVisible = visible;

            if (bottomHudFadeRoutine != null)
            {
                StopCoroutine(bottomHudFadeRoutine);
                bottomHudFadeRoutine = null;
            }

            if (!gameObject.activeInHierarchy)
            {
                bottomHudCanvasGroup.alpha = visible ? 1f : 0f;
                bottomHudCanvasGroup.interactable = visible;
                bottomHudCanvasGroup.blocksRaycasts = visible;
                return;
            }

            bottomHudFadeRoutine = StartCoroutine(FadeBottomHud(visible));
        }

        private IEnumerator FadeBottomHud(bool visible)
        {
            bottomHudCanvasGroup.interactable = visible;
            bottomHudCanvasGroup.blocksRaycasts = visible;

            float from = bottomHudCanvasGroup.alpha;
            float to = visible ? 1f : 0f;
            float duration = visible ? 0.28f : 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                bottomHudCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bottomHudCanvasGroup.alpha = to;
            bottomHudFadeRoutine = null;
        }

        public void ShowMessage(string title, string body)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            resultTitleText.text = title;
            resultBodyText.text = body;
        }

        private void BuildRevivePanel(RectTransform root, StarForgeBalance balance)
        {
            revivePanel = CreatePanel("Revive Overlay", root, new Color(0.005f, 0.008f, 0.02f, 0.95f));
            RectTransform overlayRect = revivePanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject dialog = CreatePanel("Revive Popup", revivePanel.transform, new Color(0.035f, 0.025f, 0.05f, 0.99f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.05f, 0.08f);
            dialogRect.anchorMax = new Vector2(0.95f, 0.92f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            AddPanelOutline(dialog, new Color(0.52f, 0.16f, 0.22f, 0.95f), new Vector2(2f, -2f));

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            Text title = CreateText("행성 소멸", 36, FontStyle.Bold, TextAnchor.MiddleCenter, dialog.transform);
            title.color = new Color(1f, 0.45f, 0.42f, 1f);
            SetPreferredHeight(title, 48f);

            reviveBodyText = CreateText("", 19, FontStyle.Bold, TextAnchor.MiddleCenter, dialog.transform);
            reviveBodyText.color = new Color(0.85f, 0.9f, 1f, 1f);
            reviveBodyText.resizeTextForBestFit = true;
            reviveBodyText.resizeTextMinSize = 13;
            reviveBodyText.resizeTextMaxSize = 19;
            SetPreferredHeight(reviveBodyText, 58f);

            Text guide = CreateText(
                "재화를 지불하면 세이브포인트에서 다시 시작할 수 있습니다.",
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                dialog.transform);
            guide.color = new Color(0.6f, 0.68f, 0.8f, 1f);
            SetPreferredHeight(guide, 26f);

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
            restartButton.GetComponent<LayoutElement>().preferredHeight = 62f;
            restartButton.image.color = new Color(0.12f, 0.16f, 0.26f, 1f);
            restartButton.onClick.AddListener(() => revivePanel.SetActive(false));

            revivePanel.SetActive(false);
        }

        private ReviveRow BuildReviveRow(Transform parent, RevivePointConfig config, StarForgeBalance balance)
        {
            ReviveRow row = new ReviveRow();
            row.level = config.level;

            GameObject container = CreatePanel(
                "Revive Row " + config.level,
                parent,
                new Color(0.05f, 0.055f, 0.105f, 0.98f));
            AddPanelOutline(container, new Color(0.2f, 0.3f, 0.5f, 0.85f), new Vector2(1.5f, -1.5f));
            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.minHeight = 104f;
            containerLayout.preferredHeight = 104f;

            VerticalLayoutGroup containerLayoutGroup = container.AddComponent<VerticalLayoutGroup>();
            containerLayoutGroup.padding = new RectOffset(10, 10, 6, 6);
            containerLayoutGroup.spacing = 2f;
            containerLayoutGroup.childControlWidth = true;
            containerLayoutGroup.childControlHeight = true;
            containerLayoutGroup.childForceExpandWidth = true;
            containerLayoutGroup.childForceExpandHeight = false;

            GameObject titleLine = new GameObject("Title Line", typeof(RectTransform), typeof(LayoutElement));
            titleLine.transform.SetParent(container.transform, false);
            titleLine.GetComponent<LayoutElement>().preferredHeight = 50f;
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
                21,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                titleLine.transform);
            LayoutElement titleTextLayout = row.titleText.GetComponent<LayoutElement>();
            titleTextLayout.flexibleWidth = 1f;

            row.button = CreateButton("부활", 20, titleLine.transform);
            LayoutElement buttonLayout = row.button.GetComponent<LayoutElement>();
            buttonLayout.minWidth = 96f;
            buttonLayout.preferredWidth = 96f;
            buttonLayout.minHeight = 46f;
            buttonLayout.preferredHeight = 46f;
            buttonLayout.flexibleWidth = 0f;
            row.button.image.color = new Color(0.1f, 0.32f, 0.2f, 1f);
            ColorBlock buttonColors = row.button.colors;
            buttonColors.normalColor = new Color(0.1f, 0.32f, 0.2f, 1f);
            buttonColors.highlightedColor = new Color(0.16f, 0.46f, 0.3f, 1f);
            buttonColors.pressedColor = new Color(0.06f, 0.2f, 0.13f, 1f);
            buttonColors.disabledColor = new Color(0.06f, 0.09f, 0.08f, 0.85f);
            row.button.colors = buttonColors;

            int capturedLevel = config.level;
            row.button.onClick.AddListener(() =>
            {
                revivePanel.SetActive(false);
                ReviveRequested?.Invoke(capturedLevel);
            });

            GameObject costLine = new GameObject("Cost Line", typeof(RectTransform), typeof(LayoutElement));
            costLine.transform.SetParent(container.transform, false);
            costLine.GetComponent<LayoutElement>().preferredHeight = 36f;
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
                iconLayout.minWidth = 32f;
                iconLayout.preferredWidth = 32f;
                iconLayout.minHeight = 32f;
                iconLayout.preferredHeight = 32f;
                row.costIcons[i] = iconObject.GetComponent<Image>();
                row.costIcons[i].sprite = GetMaterialIcon((int)cost.type);
                row.costIcons[i].preserveAspect = true;

                row.costTexts[i] = CreateText(
                    "x" + StarForgeFormat.Number(cost.amount),
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    costLine.transform);
                LayoutElement costTextLayout = row.costTexts[i].GetComponent<LayoutElement>();
                costTextLayout.minWidth = 64f;
            }

            row.statusText = CreateText("", 15, FontStyle.Bold, TextAnchor.MiddleRight, costLine.transform);
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
                    ? new Color(0.9f, 0.96f, 1f, 1f)
                    : new Color(0.48f, 0.52f, 0.62f, 1f);
                row.button.interactable = unlocked && affordable;
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
            image.color = new Color(0.12f, 0.16f, 0.24f, 0.96f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 44f;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.35f, 0.52f, 1f);
            colors.pressedColor = new Color(0.08f, 0.12f, 0.18f, 1f);
            colors.disabledColor = new Color(0.08f, 0.08f, 0.1f, 0.7f);
            button.colors = colors;

            Text label = CreateText(text, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
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
            background.color = new Color(0.035f, 0.055f, 0.105f, 1f);
            AddPanelOutline(buttonObject, new Color(0.08f, 0.2f, 0.34f, 0.9f), new Vector2(1.5f, -1.5f));

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 100f;
            layoutElement.preferredHeight = 100f;

            VerticalLayoutGroup layout = buttonObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(3, 3, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(buttonObject.transform, false);
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 50f;
            iconLayout.preferredHeight = 50f;
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = GetMaterialIcon(index);
            icon.preserveAspect = true;

            Text label = CreateText(displayName, 17, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            label.gameObject.name = "Name";
            label.color = new Color(0.88f, 0.95f, 1f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 17;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.minHeight = 25f;
            labelLayout.preferredHeight = 25f;

            Text quantity = CreateText("x0", 18, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            quantity.gameObject.name = "Quantity";
            quantity.color = new Color(0.36f, 0.86f, 1f, 1f);
            quantity.resizeTextForBestFit = true;
            quantity.resizeTextMinSize = 14;
            quantity.resizeTextMaxSize = 18;
            LayoutElement quantityLayout = quantity.GetComponent<LayoutElement>();
            quantityLayout.minHeight = 25f;
            quantityLayout.preferredHeight = 25f;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.035f, 0.055f, 0.105f, 1f);
            colors.highlightedColor = new Color(0.07f, 0.13f, 0.24f, 1f);
            colors.pressedColor = new Color(0.025f, 0.04f, 0.075f, 1f);
            colors.disabledColor = new Color(0.02f, 0.03f, 0.055f, 0.8f);
            button.colors = colors;
            return button;
        }

        private Toggle CreateToggle(string text, Transform parent)
        {
            GameObject toggleObject = new GameObject(text + " Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
            toggleObject.transform.SetParent(parent, false);
            toggleObject.GetComponent<LayoutElement>().preferredHeight = 64f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(48f, 48f);
            backgroundRect.anchoredPosition = new Vector2(34f, 0f);
            background.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 1f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            checkmark.GetComponent<Image>().color = new Color(0.3f, 0.8f, 1f, 1f);

            Text label = CreateText(text, 28, FontStyle.Normal, TextAnchor.MiddleLeft, toggleObject.transform);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(86f, 0f);
            labelRect.offsetMax = Vector2.zero;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
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

        private Sprite GetCollectionCircleSprite()
        {
            if (collectionCircleSprite != null)
            {
                return collectionCircleSprite;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);
            texture.name = "StarForge Collection Circle Mask";
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            collectionCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            collectionCircleSprite.name = "StarForge Collection Circle";
            return collectionCircleSprite;
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

        private static string GetResultTitle(StarForgeResultKind kind)
        {
            switch (kind)
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
            string body = result.message + "\n" +
                          result.previousLevel + "강 → " + result.newLevel + "강";

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
            DontDestroyOnLoad(eventSystemObject);
        }
    }
}
