using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace StarForge.Presentation
{
    public static class StarForgeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            StarForgeAudioController.SetStartupMainBgmSuppressed(
                !StarForgeStartupGate.SessionCompleted);

            if (Object.FindAnyObjectByType<StarForgeGameController>() == null)
            {
                GameObject controllerObject = new GameObject("StarForge Game Controller");
                controllerObject.AddComponent<StarForgeGameController>();
            }

            StarForgeStartupGate.Show();
        }
    }

    public sealed class StarForgeStartupGate : MonoBehaviour
    {
        private static bool sessionCompleted;

        public static bool SessionCompleted
        {
            get { return sessionCompleted; }
        }

        private RenderTexture videoTexture;
        private VideoPlayer videoPlayer;
        private AudioSource loadingMusicSource;
        private AspectRatioFitter videoAspectRatio;
        private StarForgeGameController gameController;
        private GameObject videoPanel;
        private GameObject consentPanel;
        private GameObject touchPanel;
        private CanvasGroup touchPromptCanvasGroup;
        private Toggle privacyConsentToggle;
        private Toggle termsConsentToggle;
        private Button acceptConsentButton;
        private bool transitionStarted;
        private bool videoStarted;
        private float nextVideoRecoverTime;

        public static void Show()
        {
            if (sessionCompleted || Object.FindAnyObjectByType<StarForgeStartupGate>() != null)
            {
                return;
            }

            GameObject gateObject = new GameObject("StarForge Startup Gate");
            gateObject.AddComponent<StarForgeStartupGate>();
        }

        private void Awake()
        {
            EnsureEventSystem();
            gameController = FindAnyObjectByType<StarForgeGameController>();
            if (gameController != null)
            {
                gameController.enabled = false;
            }

            BuildUi();
            ShowInitialGate();
            PlayLoadingMusic();
            PlayLoadingVideo();
        }

        private void Update()
        {
            if (videoStarted &&
                videoPlayer != null &&
                videoPlayer.isPrepared &&
                !videoPlayer.isPlaying &&
                Time.unscaledTime >= nextVideoRecoverTime)
            {
                nextVideoRecoverTime = Time.unscaledTime + 1f;
                videoPlayer.frame = 0;
                videoPlayer.Play();
            }

            if (touchPromptCanvasGroup == null || !touchPanel.activeSelf)
            {
                return;
            }

            float pulse = Mathf.PingPong(Time.unscaledTime * 1.15f, 1f);
            touchPromptCanvasGroup.alpha = Mathf.Lerp(0.35f, 0.8f, pulse);
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.prepareCompleted -= HandleVideoPrepared;
                videoPlayer.errorReceived -= HandleVideoError;
                videoPlayer.loopPointReached -= HandleVideoLoop;
            }

            if (loadingMusicSource != null)
            {
                loadingMusicSource.Stop();
            }

            if (videoTexture != null)
            {
                videoTexture.Release();
                Destroy(videoTexture);
            }

            if (gameController != null && !gameController.enabled)
            {
                gameController.enabled = true;
            }
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.65f;

            gameObject.AddComponent<GraphicRaycaster>();
            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.005f, 0.008f, 0.018f, 1f);

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            videoPanel = CreateFullScreenPanel("Loading Video", root);
            GameObject videoSurface = new GameObject(
                "Video Surface",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            videoSurface.transform.SetParent(videoPanel.transform, false);
            RectTransform videoSurfaceRect = videoSurface.GetComponent<RectTransform>();
            videoSurfaceRect.anchorMin = Vector2.zero;
            videoSurfaceRect.anchorMax = Vector2.one;
            videoSurfaceRect.offsetMin = Vector2.zero;
            videoSurfaceRect.offsetMax = Vector2.zero;

            RawImage videoImage = videoSurface.GetComponent<RawImage>();
            videoImage.color = Color.white;
            videoAspectRatio = videoSurface.GetComponent<AspectRatioFitter>();
            videoAspectRatio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            videoAspectRatio.aspectRatio = 9f / 16f;

            videoTexture = new RenderTexture(720, 1280, 0, RenderTextureFormat.ARGB32);
            videoTexture.name = "StarForge Loading Video";
            videoImage.texture = videoTexture;

            videoPlayer = videoPanel.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.prepareCompleted += HandleVideoPrepared;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.loopPointReached += HandleVideoLoop;

            consentPanel = CreateFullScreenPanel("Policy Consent", root);
            consentPanel.GetComponent<Image>().color =
                new Color(0.005f, 0.008f, 0.018f, 0.18f);
            BuildPolicyConsent(consentPanel.transform);
            consentPanel.SetActive(false);

            touchPanel = CreateFullScreenPanel("Touch To Start", root);
            touchPanel.GetComponent<Image>().color = new Color(0.005f, 0.008f, 0.018f, 0.35f);
            BuildTouchToStart(touchPanel.transform);
            touchPanel.SetActive(false);
        }

        private void PlayLoadingMusic()
        {
            AudioClip loadingMusic = Resources.Load<AudioClip>("Startup/loading");
            if (loadingMusic == null)
            {
                Debug.LogWarning("Loading music skipped: Startup/loading audio clip was not found.");
                return;
            }

            loadingMusicSource = gameObject.AddComponent<AudioSource>();
            loadingMusicSource.clip = loadingMusic;
            loadingMusicSource.playOnAwake = false;
            loadingMusicSource.loop = true;
            loadingMusicSource.spatialBlend = 0f;
            loadingMusicSource.Play();
        }

        private void BuildPolicyConsent(Transform parent)
        {
            // Compact card pinned to the bottom so the splash art stays visible.
            GameObject card = new GameObject(
                "Consent Card",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            card.transform.SetParent(parent, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardRect.sizeDelta = new Vector2(656f, 0f);
            cardRect.anchoredPosition = new Vector2(0f, 30f);

            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.02f, 0.05f, 0.11f, 0.9f);
            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.08f, 0.5f, 0.9f, 0.85f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = card.GetComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Transform content = card.transform;

            Text title = CreateText(
                "서비스 이용 동의",
                24,
                FontStyle.Bold,
                content);
            title.color = new Color(0.95f, 0.98f, 1f, 1f);
            SetLayoutSize(title.gameObject, 592f, 32f);

            Text body = CreateText(
                "광고 보상 제공을 위해 Google AdMob이 광고 식별자·기기 정보를 " +
                "처리할 수 있습니다. 계속하려면 아래 항목에 동의해 주세요.",
                15,
                FontStyle.Normal,
                content);
            body.color = new Color(0.74f, 0.85f, 1f, 0.95f);
            body.alignment = TextAnchor.UpperCenter;
            SetLayoutSize(body.gameObject, 596f, 46f);

            privacyConsentToggle = CreateConsentToggle(
                "개인정보처리방침에 동의합니다",
                content);
            privacyConsentToggle.onValueChanged.AddListener(
                _ => RefreshConsentButton());

            termsConsentToggle = CreateConsentToggle(
                "이용약관에 동의합니다",
                content);
            termsConsentToggle.onValueChanged.AddListener(
                _ => RefreshConsentButton());

            GameObject linkRow = new GameObject(
                "Policy Links",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            linkRow.transform.SetParent(content, false);
            HorizontalLayoutGroup linkLayout =
                linkRow.GetComponent<HorizontalLayoutGroup>();
            linkLayout.spacing = 12f;
            linkLayout.childAlignment = TextAnchor.MiddleCenter;
            linkLayout.childControlWidth = true;
            linkLayout.childControlHeight = true;
            linkLayout.childForceExpandWidth = true;
            linkLayout.childForceExpandHeight = true;
            SetLayoutSize(linkRow, 596f, 40f);

            Button privacyLink = CreateSimpleButton(
                "개인정보처리방침 보기",
                16,
                linkRow.transform);
            SetLayoutSize(privacyLink.gameObject, 280f, 40f);
            privacyLink.onClick.AddListener(StarForgeLegal.OpenPrivacyPolicy);

            Button termsLink = CreateSimpleButton(
                "이용약관 보기",
                16,
                linkRow.transform);
            SetLayoutSize(termsLink.gameObject, 280f, 40f);
            termsLink.onClick.AddListener(StarForgeLegal.OpenTermsOfService);

            acceptConsentButton = CreateSimpleButton(
                "동의하고 게임 시작",
                20,
                content);
            SetLayoutSize(acceptConsentButton.gameObject, 596f, 56f);
            acceptConsentButton.onClick.AddListener(AcceptPolicies);
            RefreshConsentButton();
        }

        private void BuildTouchToStart(Transform parent)
        {
            Button touchButton = parent.gameObject.AddComponent<Button>();
            touchButton.targetGraphic = parent.GetComponent<Image>();
            touchButton.onClick.AddListener(EnterGame);

            Text prompt = CreateText("화면을 터치하세요", 36, FontStyle.Bold, parent);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = new Color(0.92f, 0.96f, 1f, 0.85f);
            touchPromptCanvasGroup = prompt.gameObject.AddComponent<CanvasGroup>();
            touchPromptCanvasGroup.alpha = 0.6f;
            touchPromptCanvasGroup.blocksRaycasts = false;
            RectTransform promptRect = prompt.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.1f, 0.15f);
            promptRect.anchorMax = new Vector2(0.9f, 0.28f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
        }

        private void PlayLoadingVideo()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "loading.mp4");
            videoPlayer.url = path;
            videoPlayer.Prepare();
        }

        private void HandleVideoPrepared(VideoPlayer player)
        {
            if (player.width > 0 && player.height > 0)
            {
                videoAspectRatio.aspectRatio = (float)player.width / player.height;
            }

            videoStarted = true;
            nextVideoRecoverTime = Time.unscaledTime + 1f;
            player.Play();
        }

        private void HandleVideoLoop(VideoPlayer player)
        {
            if (!player.isPlaying)
            {
                player.frame = 0;
                player.Play();
            }
        }

        private void HandleVideoError(VideoPlayer player, string message)
        {
            Debug.LogWarning("Loading video skipped: " + message);
        }

        private void ShowInitialGate()
        {
            if (transitionStarted)
            {
                return;
            }

            transitionStarted = true;
            if (StarForgeLegal.HasAcceptedRequiredPolicies())
            {
                ShowTouchToStart();
                return;
            }

            consentPanel.SetActive(true);
        }

        private void ShowTouchToStart()
        {
            consentPanel.SetActive(false);
            touchPanel.SetActive(true);
        }

        private void AcceptPolicies()
        {
            if (privacyConsentToggle == null ||
                termsConsentToggle == null ||
                !privacyConsentToggle.isOn ||
                !termsConsentToggle.isOn)
            {
                return;
            }

            StarForgeLegal.AcceptRequiredPolicies();
            EnterGame();
        }

        private void RefreshConsentButton()
        {
            if (acceptConsentButton == null)
            {
                return;
            }

            acceptConsentButton.interactable =
                privacyConsentToggle != null &&
                privacyConsentToggle.isOn &&
                termsConsentToggle != null &&
                termsConsentToggle.isOn;
        }

        private void EnterGame()
        {
            sessionCompleted = true;
            StarForgeAudioController.SetStartupMainBgmSuppressed(false);
            if (gameController != null)
            {
                gameController.enabled = true;
            }

            Destroy(gameObject);
        }

        private static GameObject CreateFullScreenPanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.005f, 0.008f, 0.018f, 1f);
            return panel;
        }

        private static Button CreateTextureButton(string name, Texture2D texture, Transform parent)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(RawImage),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            RawImage image = buttonObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = texture != null ? Color.white : new Color(0.18f, 0.2f, 0.24f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.9f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.92f, 1f);
            button.colors = colors;

            const float buttonWidth = 336f;
            float height = texture != null && texture.width > 0
                ? buttonWidth * texture.height / texture.width
                : 67.2f;
            height = Mathf.Clamp(height, 70f, 84f);
            SetLayoutSize(buttonObject, buttonWidth, height);
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(buttonWidth, height);
            return button;
        }

        private static Toggle CreateConsentToggle(string text, Transform parent)
        {
            GameObject toggleObject = new GameObject(
                text,
                typeof(RectTransform),
                typeof(Toggle),
                typeof(LayoutElement));
            toggleObject.transform.SetParent(parent, false);
            SetLayoutSize(toggleObject, 592f, 40f);

            GameObject boxObject = new GameObject(
                "Box",
                typeof(RectTransform),
                typeof(Image));
            boxObject.transform.SetParent(toggleObject.transform, false);
            RectTransform boxRect = boxObject.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(30f, 30f);
            boxRect.anchoredPosition = new Vector2(18f, 0f);
            Image boxImage = boxObject.GetComponent<Image>();
            boxImage.color = new Color(0.015f, 0.05f, 0.11f, 1f);
            Outline boxOutline = boxObject.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.18f, 0.78f, 1f, 1f);
            boxOutline.effectDistance = new Vector2(2f, -2f);

            Text checkText = CreateText(
                "✓",
                22,
                FontStyle.Bold,
                boxObject.transform);
            checkText.color = new Color(0.95f, 1f, 1f, 1f);
            RectTransform checkRect = checkText.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Text label = CreateText(
                text,
                20,
                FontStyle.Bold,
                toggleObject.transform);
            label.color = new Color(0.9f, 0.96f, 1f, 1f);
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(52f, 0f);
            labelRect.offsetMax = Vector2.zero;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkText;
            toggle.SetIsOnWithoutNotify(false);
            checkText.gameObject.SetActive(false);
            toggle.onValueChanged.AddListener(
                isOn => checkText.gameObject.SetActive(isOn));
            return toggle;
        }

        private static Button CreateSimpleButton(
            string text,
            int fontSize,
            Transform parent)
        {
            GameObject buttonObject = new GameObject(
                text + " Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            SetLayoutSize(buttonObject, 260f, 56f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.018f, 0.055f, 0.12f, 0.98f);
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.54f, 0.92f, 0.98f);
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.82f, 1f, 0.9f);
            colors.disabledColor = new Color(0.34f, 0.38f, 0.45f, 0.75f);
            button.colors = colors;

            Text label = CreateText(
                text,
                fontSize,
                FontStyle.Bold,
                buttonObject.transform);
            label.color = new Color(0.9f, 0.96f, 1f, 1f);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = fontSize;

            return button;
        }

        private static Text CreateText(string value, int fontSize, FontStyle style, Transform parent)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetLayoutSize(GameObject target, float width, float height)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystemObject);
        }
    }

    public static class StarForgeLegal
    {
        public const string PrivacyPolicyUrl =
            "https://ohc4252-great.github.io/starforge-policies/privacy.html";
        public const string TermsOfServiceUrl =
            "https://ohc4252-great.github.io/starforge-policies/terms.html";

        private const string ConsentVersion = "2026-06-16";
        private const string PrivacyAcceptedKey =
            "StarForge.Legal.PrivacyAccepted";
        private const string TermsAcceptedKey =
            "StarForge.Legal.TermsAccepted";
        private const string ConsentVersionKey =
            "StarForge.Legal.ConsentVersion";

        public static bool HasAcceptedRequiredPolicies()
        {
            return PlayerPrefs.GetInt(PrivacyAcceptedKey, 0) == 1 &&
                   PlayerPrefs.GetInt(TermsAcceptedKey, 0) == 1 &&
                   PlayerPrefs.GetString(ConsentVersionKey, string.Empty) ==
                   ConsentVersion;
        }

        public static void AcceptRequiredPolicies()
        {
            PlayerPrefs.SetInt(PrivacyAcceptedKey, 1);
            PlayerPrefs.SetInt(TermsAcceptedKey, 1);
            PlayerPrefs.SetString(ConsentVersionKey, ConsentVersion);
            PlayerPrefs.Save();
        }

        public static void OpenPrivacyPolicy()
        {
            Application.OpenURL(PrivacyPolicyUrl);
        }

        public static void OpenTermsOfService()
        {
            Application.OpenURL(TermsOfServiceUrl);
        }
    }
}
