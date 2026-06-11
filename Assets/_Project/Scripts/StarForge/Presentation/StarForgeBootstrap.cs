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

        private RenderTexture videoTexture;
        private VideoPlayer videoPlayer;
        private AudioSource loadingMusicSource;
        private AspectRatioFitter videoAspectRatio;
        private StarForgeGameController gameController;
        private GameObject videoPanel;
        private GameObject loginPanel;
        private GameObject touchPanel;
        private CanvasGroup touchPromptCanvasGroup;
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
            ShowLoginSelection();
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

            loginPanel = CreateFullScreenPanel("Login Selection", root);
            loginPanel.GetComponent<Image>().color = new Color(0.005f, 0.008f, 0.018f, 0.5f);
            BuildLoginSelection(loginPanel.transform);
            loginPanel.SetActive(false);

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

        private void BuildLoginSelection(Transform parent)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(80, 80, 340, 120);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button googleButton = CreateTextureButton(
                "Google Login",
                Resources.Load<Texture2D>("Startup/google_signin"),
                parent);
            googleButton.onClick.AddListener(HandleGoogleLoginSelected);

            Button kakaoButton = CreateTextureButton(
                "Kakao Login",
                Resources.Load<Texture2D>("Startup/kakao_login"),
                parent);
            kakaoButton.onClick.AddListener(HandleKakaoLoginSelected);
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

        private void ShowLoginSelection()
        {
            if (transitionStarted)
            {
                return;
            }

            transitionStarted = true;
            loginPanel.SetActive(true);
        }

        private void HandleGoogleLoginSelected()
        {
            ShowTouchToStart();
        }

        private void HandleKakaoLoginSelected()
        {
            ShowTouchToStart();
        }

        private void ShowTouchToStart()
        {
            loginPanel.SetActive(false);
            touchPanel.SetActive(true);
        }

        private void EnterGame()
        {
            sessionCompleted = true;
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
}
