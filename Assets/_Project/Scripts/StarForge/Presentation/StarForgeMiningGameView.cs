using System;
using System.Collections.Generic;
using StarForge.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StarForge.Presentation
{
    /// <summary>
    /// 3D dodge ("Dragon Flight" style) mining minigame. The explorer ship flies
    /// forward through space; the player drags left/right to dodge incoming asteroid
    /// debris. Survival distance maps to a normalized score (0..1) that drives the
    /// existing reward / daily-attempt / rewarded-ad pipeline in StarForgeGameController.
    ///
    /// The public API (Build/Open/ShowOutcome/ShowDailyLimit/SetRewardedAdBusy/
    /// ResumeRunWithBooster + MiningStopped/ContinueWithAdRequested events) is kept
    /// narrow so the controller can own settlement and ad decisions.
    /// </summary>
    public sealed class StarForgeMiningGameView : MonoBehaviour
    {
        private const int BaseDailyAttempts = 3;

        // 3D field is parked far from the main scene and isolated on its own layer
        // (the collection preview uses layer 30, so mining uses 29) so neither camera
        // renders the other's objects.
        private const int MiningLayer = 29;
        private static readonly Vector3 FieldOrigin = new Vector3(-2000f, 0f, 0f);

        // Field metrics (local to the field root).
        private const float ShipHalfRange = 2.6f;
        private const float ShipBaseY = 0f;
        private const float SpawnZ = 30f;
        private const float DespawnZ = -8f;
        private const float ShipMoveSpeed = 9.5f;

        // Forward/back travel (vertical drag = screen up/down). The ship rests near the
        // camera (low on screen) and can advance toward the incoming rocks.
        private const float ShipZMin = -2.5f;
        private const float ShipZMax = 8f;
        private const float ShipStartZ = -1.5f;

        // Booster: a few seconds of invincible plowing that destroys every rock in
        // front while the engines blast and the ship surges forward.
        private const float BoostDuration = 5f;
        private const float BoostCooldown = 8f;
        private const float BoostSpeedMultiplier = 2.2f;
        private const float BoostReach = 2f;

        // Difficulty ramp.
        private const float BaseForwardSpeed = 16f;
        private const float MaxForwardSpeed = 34f;
        private const float BaseSpawnInterval = 0.72f;
        private const float MinSpawnInterval = 0.28f;
        private const float DifficultyRampSeconds = 75f;

        // Distance that counts as a "perfect" run for scoring purposes.
        private const float PerfectDistance = 2000f;

        // Persisted best flight distance (광년) across all play sessions.
        private const string BestDistancePrefKey = "StarForge.Mining.BestDistance";

        // Collision radii (units).
        private const float ShipHitRadius = 0.42f;

        // Target on-screen size (largest dimension, units) of a rock at scale 1.
        private const float DesiredRockSize = 0.7f;
        private const string HeartIconResourcePath = "Mining/22-Photoroom";
        private const int MaxLives = 3;
        private const int MaxContinuesPerRun = 1;
        private static readonly bool EnableHeartPickups = false;
        private const float HitInvulnerableSeconds = 1.2f;
        private const float ContinueBoostSeconds = 10f;
        private const float HeartSpawnMinSeconds = 12f;
        private const float HeartSpawnMaxSeconds = 20f;
        private const float HeartPickupRadius = 0.45f;
        private const float HeartPickupScale = 0.48f;
        private const float HeartDistanceBonus = 8f;

        public event Action<float> MiningStopped;
        public event Action MiningClosed;
        public event Action ContinueWithAdRequested;
        public event Action BonusAttemptWithAdRequested;

        private enum MiningState
        {
            Hidden,
            Ready,
            Running,
            AwaitingResult,
            Result
        }

        private sealed class Asteroid
        {
            public GameObject gameObject;
            public Transform transform;
            public Vector3 spin;
            public float baseScale;
            public float radius;
            public float previousZ;
        }

        private sealed class StarStreak
        {
            public Transform transform;
            public float speedFactor;
        }

        private sealed class Pickup
        {
            public GameObject gameObject;
            public Transform transform;
            public float previousZ;
            public float bobPhase;
        }

        [Header("Ship model orientation — tunable live during Play")]
        [SerializeField] private Vector3 shipModelEuler = new Vector3(-90f, 0f, 0f);

        [Header("Camera framing & ship size — tunable live during Play")]
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 5f, -11.5f);
        [SerializeField] private Vector3 cameraLocalEuler = new Vector3(20f, 0f, 0f);
        [SerializeField] private float cameraFieldOfView = 55f;
        [SerializeField] private float shipDisplaySize = 1.6f;

        [Header("Engine thruster — tunable live during Play")]
        [SerializeField] private Vector3 thrusterLocalPosition = new Vector3(0f, 0.02f, -0.55f);
        [SerializeField] private float thrusterSize = 1.0f;

        private float shipBaseMaxDim = 1f;

        private Font font;
        private MiningState state;

        // UI
        private RawImage fieldImage;
        private RectTransform fieldImageRect;
        private Text distanceText;
        private Text bestText;
        private GameObject newRecordOverlay;
        private Text newRecordValueText;
        private Text attemptsText;
        private Text instructionText;
        private GameObject resultOverlay;
        private Text resultGradeText;
        private Text resultScoreText;
        private Button replayButton;
        private Text replayButtonText;
        private Button continueAdButton;
        private Text continueAdButtonText;
        private GameObject startOverlay;
        private Button startButton;
        private Button boostButton;
        private Text boostButtonText;
        private readonly Image[] heartIcons = new Image[MaxLives];
        private readonly Image[] resultRewardIcons = new Image[2];
        private readonly Text[] resultRewardTexts = new Text[2];
        private readonly Sprite[] materialIconSprites = new Sprite[5];
        private readonly bool[] ownsMaterialIconSprite = new bool[5];

        // 3D scene
        private GameObject fieldRoot;
        private Camera fieldCamera;
        private Transform shipRoot;
        private Transform shipModelTransform;
        private Transform thrusterTransform;
        private ParticleSystem thrusterParticles;
        private Renderer[] shipRenderers;
        private AudioSource engineAudioSource;
        private AudioSource boosterAudioSource;
        private AudioClip engineAudioClip;
        private AudioClip boosterAudioClip;
        private RenderTexture renderTexture;
        private GameObject heartPrefab;
        private GameObject[] rockPrefabs;
        private float[] rockPrefabBaseScale;
        private Material[] rockMaterials;
        private bool rockPrefabsAreInstances;
        private readonly List<Asteroid> activeRocks = new List<Asteroid>();
        private readonly Stack<Asteroid> rockPool = new Stack<Asteroid>();
        private readonly List<Pickup> activeHearts = new List<Pickup>();
        private readonly Stack<Pickup> heartPool = new Stack<Pickup>();
        private readonly List<StarStreak> starStreaks = new List<StarStreak>();
        private readonly List<Material> runtimeMaterials = new List<Material>();

        // Gameplay state
        private float elapsed;
        private float runDistance;
        private float forwardSpeed;
        private float spawnTimer;
        private float shipX;
        private float shipTargetX;
        private float shipZ;
        private float inputReadyTime;
        private int remainingAttempts;
        private int remainingAdBonuses;
        private bool rewardedAdBusy;
        private bool boosting;
        private float boostTimer;
        private float boostCooldown;
        private int lives;
        private float invulnerableTimer;
        private float heartSpawnTimer;
        private int continuesUsedThisRun;
        private bool canContinueCurrentRun;
        private int bestDistance;
        private bool pendingNewRecord;

        private Texture2D generatedBackdrop;
        private Texture2D generatedPickupStarTexture;
        private Sprite generatedChamferedSprite;
        private Sprite generatedHeartIconSprite;
        private bool ownsGeneratedHeartIconTexture;
        private bool isBuilt;
        private bool fieldBuilt;
        private bool soundEnabled = true;
        private bool engineAudioPausedForBoost;
        private float sfxVolume = 1f;

        public bool IsBuilt
        {
            get { return isBuilt; }
        }

        public bool SoundEnabled
        {
            get { return soundEnabled; }
            set
            {
                soundEnabled = value;
                UpdateMiningAudioState();
            }
        }

        public float SfxVolume
        {
            get { return sfxVolume; }
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                UpdateMiningAudioState();
            }
        }

        public void Build()
        {
            if (isBuilt)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            generatedChamferedSprite = CreateRoundedSprite(64, 16);
            generatedHeartIconSprite = LoadHeartIconSprite();
            if (generatedHeartIconSprite == null)
            {
                generatedHeartIconSprite = CreateHeartSprite(64);
                ownsGeneratedHeartIconTexture = true;
            }
            bestDistance = Mathf.Max(0, PlayerPrefs.GetInt(BestDistancePrefKey, 0));
            LoadMaterialIcons();
            EnsureMiningAudio();
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.65f;

            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildBackdrop(root);
            RectTransform safeArea = CreateSafeAreaRoot(root);
            BuildHeader(safeArea);
            BuildPlayfield(safeArea);
            BuildHearts(safeArea);
            BuildStartOverlay(safeArea);
            BuildResultOverlay(safeArea);
            BuildNewRecordOverlay(safeArea);

            EnsureField();
            UpdateBestText();

            isBuilt = true;
            state = MiningState.Hidden;
            gameObject.SetActive(false);
            SetFieldActive(false);
        }

        public void Open(int availableAttempts, int availableAdBonuses)
        {
            if (!isBuilt)
            {
                Build();
            }

            remainingAttempts = Mathf.Max(0, availableAttempts);
            remainingAdBonuses = Mathf.Max(0, availableAdBonuses);
            rewardedAdBusy = false;
            canContinueCurrentRun = false;
            continuesUsedThisRun = 0;
            gameObject.SetActive(true);
            SetFieldActive(true);
            SetNewRecordVisible(false);
            UpdateBestText();
            UpdateAttemptsText();
            UpdateHeartsHud();
            if (remainingAttempts > 0)
            {
                PrepareRound();
            }
            else
            {
                ShowDailyLimit();
            }
        }

        public void ShowOutcome(
            float normalizedScore,
            CurrencyAmount[] rewards,
            int availableAttempts,
            int availableAdBonuses)
        {
            remainingAttempts = Mathf.Max(0, availableAttempts);
            remainingAdBonuses = Mathf.Max(0, availableAdBonuses);
            rewardedAdBusy = false;
            canContinueCurrentRun = true;
            state = MiningState.Result;
            resultOverlay.SetActive(true);
            SetStartOverlayVisible(false);
            UpdateMiningAudioState();

            float clamped = Mathf.Clamp01(normalizedScore);
            int displayedScore = clamped >= 0.995f
                ? 100
                : Mathf.Clamp(Mathf.RoundToInt(clamped * 100f), 0, 99);
            GetGrade(clamped, out string grade, out Color gradeColor);
            resultGradeText.text = grade;
            resultGradeText.color = gradeColor;
            resultScoreText.text =
                "비행 거리  " + Mathf.RoundToInt(runDistance) + " 광년   ·   회수 효율 " +
                displayedScore + "%";
            UpdateRewardSlots(rewards);
            UpdateAttemptsText();
            UpdateReplayState();

            if (pendingNewRecord)
            {
                pendingNewRecord = false;
                ShowNewRecordCelebration();
            }
        }

        public void ShowDailyLimit()
        {
            if (!isBuilt)
            {
                Build();
            }

            remainingAttempts = 0;
            canContinueCurrentRun = false;
            gameObject.SetActive(true);
            SetFieldActive(true);
            SetNewRecordVisible(false);
            state = MiningState.Result;
            resultOverlay.SetActive(true);
            SetStartOverlayVisible(false);
            UpdateMiningAudioState();
            resultGradeText.text = "오늘의 탐사 완료";
            resultGradeText.color = new Color(0.5f, 0.86f, 1f, 1f);
            resultScoreText.text =
                remainingAdBonuses > 0
                    ? "광고를 보고 계속 탐사할 수 있습니다."
                    : "오늘 가능한 기본 채굴을 모두 사용했습니다.";
            UpdateRewardSlots(null);
            UpdateAttemptsText();
            UpdateReplayState();
        }

        public void SetRewardedAdBusy(bool busy)
        {
            rewardedAdBusy = busy;
            UpdateContinueState();
        }

        public void ResumeRunWithBooster()
        {
            if (!isBuilt)
            {
                Build();
            }

            EnsureField();
            SetFieldActive(true);

            spawnTimer = 0.85f;
            shipX = 0f;
            shipTargetX = 0f;
            shipZ = ShipStartZ;
            inputReadyTime = Time.unscaledTime + 0.35f;
            lives = MaxLives;
            invulnerableTimer = 0f;
            if (EnableHeartPickups)
            {
                heartSpawnTimer = UnityEngine.Random.Range(
                    HeartSpawnMinSeconds,
                    HeartSpawnMaxSeconds);
            }
            boosting = true;
            boostTimer = ContinueBoostSeconds;
            boostCooldown = 0f;
            continuesUsedThisRun++;
            canContinueCurrentRun = false;
            rewardedAdBusy = false;

            ClearRocks();
            ClearHearts();
            if (shipRoot != null)
            {
                shipRoot.localPosition = new Vector3(0f, ShipBaseY, ShipStartZ);
                shipRoot.localRotation = Quaternion.identity;
            }

            state = MiningState.Running;
            resultOverlay.SetActive(false);
            SetStartOverlayVisible(false);
            SetNewRecordVisible(false);
            SetShipRenderersVisible(true);
            UpdateHeartsHud();
            UpdateAttemptsText();
            UpdateDistanceText();
            UpdateBoostButton();
            PlayBoosterAudio();
            UpdateMiningAudioState();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (state == MiningState.Running)
            {
                elapsed += dt;
                UpdateDifficulty();
                UpdateBoost(dt);
                UpdateInvulnerability(dt);
                HandleInput(dt);
                MoveShip(dt);
                UpdateSpawning(dt);
                UpdateRocks(dt, true);
                if (EnableHeartPickups)
                {
                    UpdateHearts(dt, true);
                }
                else if (activeHearts.Count > 0)
                {
                    ClearHearts();
                }
                runDistance += forwardSpeed * dt;
                UpdateDistanceText();
            }
            else
            {
                UpdateRocks(dt, false);
                if (EnableHeartPickups)
                {
                    UpdateHearts(dt, false);
                }
                else if (activeHearts.Count > 0)
                {
                    ClearHearts();
                }
            }

            UpdateStarfield(dt);
            UpdateBoostButton();
            UpdateMiningAudioState();

            // Apply every frame so the orientation can be dialed in live from the
            // Inspector during Play mode (only affects the imported model, not the
            // procedural placeholder).
            if (shipModelTransform != null)
            {
                shipModelTransform.localRotation = Quaternion.Euler(shipModelEuler);
                shipModelTransform.localScale =
                    Vector3.one *
                    (Mathf.Max(0.05f, shipDisplaySize) /
                     Mathf.Max(0.001f, shipBaseMaxDim));
            }

            if (fieldCamera != null)
            {
                fieldCamera.transform.localPosition = cameraLocalPosition;
                fieldCamera.transform.localRotation =
                    Quaternion.Euler(cameraLocalEuler);
                fieldCamera.fieldOfView =
                    Mathf.Clamp(cameraFieldOfView, 20f, 100f);
            }

            if (thrusterTransform != null)
            {
                float boostScale = boosting ? 2.1f : 1f;
                thrusterTransform.localPosition = thrusterLocalPosition;
                thrusterTransform.localScale =
                    Vector3.one * (Mathf.Max(0.05f, thrusterSize) * boostScale);
            }

            if (thrusterParticles != null)
            {
                ParticleSystem.EmissionModule thrusterEmission = thrusterParticles.emission;
                thrusterEmission.rateOverTime = boosting ? 470f : 150f;
                ParticleSystem.MainModule thrusterMain = thrusterParticles.main;
                thrusterMain.startSpeed = boosting ? 13f : 7.5f;
            }

        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                if (fieldCamera != null)
                {
                    fieldCamera.targetTexture = null;
                }

                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (fieldRoot != null)
            {
                Destroy(fieldRoot);
            }

            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    Destroy(runtimeMaterials[i]);
                }
            }

            if (generatedBackdrop != null)
            {
                Destroy(generatedBackdrop);
            }

            if (generatedPickupStarTexture != null)
            {
                Destroy(generatedPickupStarTexture);
            }

            if (generatedChamferedSprite != null)
            {
                Texture2D texture = generatedChamferedSprite.texture;
                Destroy(generatedChamferedSprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            if (generatedHeartIconSprite != null)
            {
                Texture2D texture = ownsGeneratedHeartIconTexture
                    ? generatedHeartIconSprite.texture
                    : null;
                Destroy(generatedHeartIconSprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            for (int i = 0; i < materialIconSprites.Length; i++)
            {
                if (ownsMaterialIconSprite[i] && materialIconSprites[i] != null)
                {
                    Destroy(materialIconSprites[i]);
                }
            }
        }

        private void PrepareRound()
        {
            if (remainingAttempts <= 0)
            {
                ShowDailyLimit();
                return;
            }

            EnsureField();
            SetFieldActive(true);
            ResetRoundState();
            state = MiningState.Ready;
            resultOverlay.SetActive(false);
            SetStartOverlayVisible(true);
            UpdateMiningAudioState();
        }

        private void StartRound()
        {
            if (remainingAttempts <= 0)
            {
                ShowDailyLimit();
                return;
            }

            EnsureField();
            SetFieldActive(true);
            ResetRoundState();
            state = MiningState.Running;
            resultOverlay.SetActive(false);
            SetStartOverlayVisible(false);
            UpdateMiningAudioState();
        }

        private void ResetRoundState()
        {
            elapsed = 0f;
            runDistance = 0f;
            forwardSpeed = BaseForwardSpeed;
            spawnTimer = 0.8f;
            shipX = 0f;
            shipTargetX = 0f;
            shipZ = ShipStartZ;
            boosting = false;
            boostTimer = 0f;
            boostCooldown = 0f;
            lives = MaxLives;
            invulnerableTimer = 0f;
            if (EnableHeartPickups)
            {
                heartSpawnTimer = UnityEngine.Random.Range(
                    HeartSpawnMinSeconds,
                    HeartSpawnMaxSeconds);
            }
            continuesUsedThisRun = 0;
            canContinueCurrentRun = false;
            pendingNewRecord = false;
            inputReadyTime = Time.unscaledTime + 0.35f;

            SetNewRecordVisible(false);
            ClearRocks();
            ClearHearts();
            if (shipRoot != null)
            {
                shipRoot.localPosition = new Vector3(0f, ShipBaseY, ShipStartZ);
                shipRoot.localRotation = Quaternion.identity;
            }

            SetShipRenderersVisible(true);
            UpdateAttemptsText();
            UpdateDistanceText();
            UpdateHeartsHud();
        }

        private void EndRound()
        {
            if (state != MiningState.Running)
            {
                return;
            }

            state = MiningState.AwaitingResult;
            UpdateMiningAudioState();

            int finalDistance = Mathf.RoundToInt(runDistance);
            if (finalDistance > bestDistance)
            {
                bestDistance = finalDistance;
                pendingNewRecord = true;
                SaveBestDistance();
                UpdateBestText();
            }

            float normalizedScore = Mathf.Clamp01(runDistance / PerfectDistance);
            MiningStopped?.Invoke(normalizedScore);
        }

        private void Close()
        {
            state = MiningState.Hidden;
            SetStartOverlayVisible(false);
            SetNewRecordVisible(false);
            canContinueCurrentRun = false;
            UpdateMiningAudioState();
            SetFieldActive(false);
            gameObject.SetActive(false);
            MiningClosed?.Invoke();
        }

        private void HandleReplayButton()
        {
            // When attempts remain this replays; once "오늘 탐사 완료" it returns to main.
            if (remainingAttempts > 0)
            {
                PrepareRound();
            }
            else
            {
                Close();
            }
        }

        private void HandleContinueAdButton()
        {
            if (rewardedAdBusy)
            {
                return;
            }

            if (CanContinueCurrentRunWithAd())
            {
                ContinueWithAdRequested?.Invoke();
                return;
            }

            if (CanRequestBonusAttemptWithAd())
            {
                BonusAttemptWithAdRequested?.Invoke();
            }
        }

        // ----------------------------------------------------------------- gameplay

        private void UpdateDifficulty()
        {
            float t = Mathf.Clamp01(elapsed / DifficultyRampSeconds);
            forwardSpeed = Mathf.Lerp(BaseForwardSpeed, MaxForwardSpeed, t);
        }

        private void UpdateBoost(float dt)
        {
            if (boosting)
            {
                boostTimer -= dt;
                forwardSpeed *= BoostSpeedMultiplier;
                if (boostTimer <= 0f)
                {
                    boosting = false;
                    boostCooldown = BoostCooldown;
                    UpdateMiningAudioState();
                }
            }
            else if (boostCooldown > 0f)
            {
                boostCooldown = Mathf.Max(0f, boostCooldown - dt);
            }
        }

        private void UpdateInvulnerability(float dt)
        {
            if (invulnerableTimer <= 0f)
            {
                SetShipRenderersVisible(true);
                return;
            }

            invulnerableTimer = Mathf.Max(0f, invulnerableTimer - dt);
            bool visible = Mathf.FloorToInt(Time.unscaledTime * 14f) % 2 == 0;
            SetShipRenderersVisible(visible || invulnerableTimer <= 0f);
        }

        private void SetShipRenderersVisible(bool visible)
        {
            if (shipRenderers == null)
            {
                return;
            }

            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null)
                {
                    shipRenderers[i].enabled = visible;
                }
            }
        }

        private void TryStartBoost()
        {
            if (state != MiningState.Running || boosting || boostCooldown > 0f)
            {
                return;
            }

            boosting = true;
            boostTimer = BoostDuration;
            PlayBoosterAudio();
            UpdateMiningAudioState();
        }

        private void UpdateBoostButton()
        {
            if (boostButton == null)
            {
                return;
            }

            bool show = state == MiningState.Running;
            if (boostButton.gameObject.activeSelf != show)
            {
                boostButton.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            if (boosting)
            {
                boostButton.interactable = false;
                if (boostButtonText != null)
                {
                    boostButtonText.text = "부스터 " + Mathf.CeilToInt(boostTimer) + "s";
                }
            }
            else if (boostCooldown > 0f)
            {
                boostButton.interactable = false;
                if (boostButtonText != null)
                {
                    boostButtonText.text = "재충전 " + Mathf.CeilToInt(boostCooldown) + "s";
                }
            }
            else
            {
                boostButton.interactable = true;
                if (boostButtonText != null)
                {
                    boostButtonText.text = "부스터";
                }
            }
        }

        private void HandleInput(float dt)
        {
            bool pointerHandled = false;
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed &&
                Time.unscaledTime >= inputReadyTime)
            {
                Vector2 screenPoint = pointer.position.ReadValue();
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        fieldImageRect, screenPoint, null) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        fieldImageRect, screenPoint, null, out Vector2 localPoint))
                {
                    Rect rect = fieldImageRect.rect;
                    float nx = Mathf.Clamp01(
                        (localPoint.x - rect.xMin) / Mathf.Max(1f, rect.width));
                    shipTargetX = Mathf.Lerp(-ShipHalfRange, ShipHalfRange, nx);
                    pointerHandled = true;
                }
            }

            if (!pointerHandled)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    float axisX = 0f;
                    if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                    {
                        axisX -= 1f;
                    }

                    if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                    {
                        axisX += 1f;
                    }

                    if (Mathf.Abs(axisX) > 0.01f)
                    {
                        shipTargetX = Mathf.Clamp(
                            shipTargetX + axisX * ShipMoveSpeed * dt,
                            -ShipHalfRange,
                            ShipHalfRange);
                    }
                }
            }
        }

        private void MoveShip(float dt)
        {
            if (shipRoot == null)
            {
                return;
            }

            float previousX = shipX;
            shipX = Mathf.MoveTowards(shipX, shipTargetX, ShipMoveSpeed * dt);
            shipX = Mathf.Clamp(shipX, -ShipHalfRange, ShipHalfRange);
            shipZ = ShipStartZ;

            float bob = Mathf.Sin(Time.unscaledTime * 2.1f) * 0.07f;
            shipRoot.localPosition = new Vector3(shipX, ShipBaseY + bob, shipZ);

            float xVelocity = (shipX - previousX) / Mathf.Max(0.0001f, dt);
            float bank = Mathf.Clamp(xVelocity * 4.5f, -26f, 26f);
            float yaw = Mathf.Clamp(xVelocity * 2.2f, -12f, 12f);
            float pitch = 6f;
            shipRoot.localRotation = Quaternion.Euler(pitch, yaw, -bank);
        }

        private void UpdateSpawning(float dt)
        {
            if (EnableHeartPickups)
            {
                UpdateHeartSpawning(dt);
            }

            spawnTimer -= dt;
            if (spawnTimer > 0f)
            {
                return;
            }

            float t = Mathf.Clamp01(elapsed / DifficultyRampSeconds);
            float interval = Mathf.Lerp(BaseSpawnInterval, MinSpawnInterval, t);
            spawnTimer = interval * UnityEngine.Random.Range(0.85f, 1.2f);

            SpawnRock();
            if (t > 0.55f && UnityEngine.Random.value < 0.35f)
            {
                SpawnRock();
            }
        }

        private void UpdateHeartSpawning(float dt)
        {
            heartSpawnTimer -= dt;
            if (heartSpawnTimer > 0f)
            {
                return;
            }

            heartSpawnTimer = UnityEngine.Random.Range(
                HeartSpawnMinSeconds,
                HeartSpawnMaxSeconds);
            if (activeHearts.Count > 0 || lives >= MaxLives)
            {
                return;
            }

            SpawnHeart();
        }

        private void UpdateRocks(float dt, bool running)
        {
            float move = running ? forwardSpeed * dt : 0f;
            for (int i = activeRocks.Count - 1; i >= 0; i--)
            {
                Asteroid rock = activeRocks[i];
                Vector3 position = rock.transform.localPosition;
                rock.previousZ = position.z;
                if (running)
                {
                    position.z -= move;
                    rock.transform.localPosition = position;
                    rock.transform.Rotate(rock.spin * dt, Space.Self);
                }

                if (running && boosting && position.z <= shipZ + BoostReach)
                {
                    DestroyRock(i);
                    continue;
                }

                if (running &&
                    !boosting &&
                    invulnerableTimer <= 0f &&
                    CheckCollision(rock, position))
                {
                    HitObstacle(i);
                    if (state != MiningState.Running)
                    {
                        return;
                    }

                    continue;
                }

                if (position.z < DespawnZ)
                {
                    RecycleRock(i);
                }
            }
        }

        private bool CheckCollision(Asteroid rock, Vector3 position)
        {
            // Detect the frame the rock crosses the ship's depth plane to avoid tunneling.
            bool crossed = rock.previousZ > shipZ && position.z <= shipZ;
            if (!crossed)
            {
                return false;
            }

            float dx = position.x - shipX;
            float dy = position.y - ShipBaseY;
            float reach = rock.radius + ShipHitRadius;
            return (dx * dx + dy * dy) <= reach * reach;
        }

        private void DestroyRock(int activeIndex)
        {
            runDistance += 6f;
            RecycleRock(activeIndex);
        }

        private void HitObstacle(int activeIndex)
        {
            RecycleRock(activeIndex);

            lives = Mathf.Max(0, lives - 1);
            invulnerableTimer = HitInvulnerableSeconds;
            UpdateHeartsHud();

            if (lives <= 0)
            {
                SetShipRenderersVisible(true);
                EndRound();
            }
        }

        private void UpdateHearts(float dt, bool running)
        {
            float move = running ? forwardSpeed * dt : 0f;
            for (int i = activeHearts.Count - 1; i >= 0; i--)
            {
                Pickup heart = activeHearts[i];
                Vector3 position = heart.transform.localPosition;
                heart.previousZ = position.z;
                if (running)
                {
                    position.z -= move;
                    position.y =
                        ShipBaseY +
                        0.38f +
                        Mathf.Sin(Time.unscaledTime * 3.2f + heart.bobPhase) *
                        0.12f;
                    heart.transform.localPosition = position;
                    heart.transform.Rotate(0f, 120f * dt, 0f, Space.Self);
                }

                if (running && CheckHeartPickup(heart, position))
                {
                    CollectHeart(i);
                    continue;
                }

                if (position.z < DespawnZ)
                {
                    RecycleHeart(i);
                }
            }
        }

        private bool CheckHeartPickup(Pickup heart, Vector3 position)
        {
            bool crossed = heart.previousZ > shipZ && position.z <= shipZ;
            if (!crossed)
            {
                return false;
            }

            float dx = position.x - shipX;
            float dy = position.y - ShipBaseY;
            float reach = HeartPickupRadius + ShipHitRadius;
            return (dx * dx + dy * dy) <= reach * reach;
        }

        private void CollectHeart(int activeIndex)
        {
            if (lives < MaxLives)
            {
                lives = Mathf.Min(MaxLives, lives + 1);
                UpdateHeartsHud();
            }
            else
            {
                runDistance += HeartDistanceBonus;
                UpdateDistanceText();
            }

            RecycleHeart(activeIndex);
        }

        private void UpdateStarfield(float dt)
        {
            if (starStreaks.Count == 0)
            {
                return;
            }

            float baseSpeed = Mathf.Max(BaseForwardSpeed, forwardSpeed);
            for (int i = 0; i < starStreaks.Count; i++)
            {
                StarStreak star = starStreaks[i];
                Vector3 position = star.transform.localPosition;
                position.z -= baseSpeed * star.speedFactor * dt;
                if (position.z < DespawnZ - 2f)
                {
                    position = RandomStarPosition();
                    position.z = SpawnZ + UnityEngine.Random.Range(2f, 12f);
                }

                star.transform.localPosition = position;
            }
        }

        // ----------------------------------------------------------------- 3D scene

        private void EnsureField()
        {
            if (fieldBuilt)
            {
                return;
            }

            fieldRoot = new GameObject("StarForge Mining Field");
            fieldRoot.transform.position = FieldOrigin;

            CreateRenderTexture();

            GameObject cameraObject = new GameObject("Mining Camera");
            cameraObject.transform.SetParent(fieldRoot.transform, false);
            cameraObject.transform.localPosition = cameraLocalPosition;
            cameraObject.transform.localRotation = Quaternion.Euler(cameraLocalEuler);
            fieldCamera = cameraObject.AddComponent<Camera>();
            fieldCamera.clearFlags = CameraClearFlags.SolidColor;
            fieldCamera.backgroundColor = new Color(0.002f, 0.005f, 0.018f, 1f);
            fieldCamera.fieldOfView = cameraFieldOfView;
            fieldCamera.nearClipPlane = 0.05f;
            fieldCamera.farClipPlane = 80f;
            fieldCamera.cullingMask = 1 << MiningLayer;
            fieldCamera.targetTexture = renderTexture;
            fieldCamera.allowHDR = true;

            GameObject lightObject = new GameObject("Mining Sun");
            lightObject.transform.SetParent(fieldRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(42f, -24f, 0f);
            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.4f;
            keyLight.color = new Color(0.85f, 0.92f, 1f, 1f);
            keyLight.cullingMask = 1 << MiningLayer;

            BuildBackdropQuad();
            BuildStarfield();
            BuildShip();
            if (EnableHeartPickups)
            {
                BuildHeartPickupPrefab();
            }
            LoadRockPrefabs();

            SetLayerRecursively(fieldRoot, MiningLayer);
            fieldBuilt = true;

            if (fieldImage != null)
            {
                fieldImage.texture = renderTexture;
            }
        }

        private void CreateRenderTexture()
        {
            renderTexture = new RenderTexture(728, 1000, 24, RenderTextureFormat.ARGB32);
            renderTexture.name = "StarForge Mining Field RT";
            renderTexture.antiAliasing = 2;
            renderTexture.Create();
        }

        private void BuildBackdropQuad()
        {
            generatedBackdrop = CreateSpaceBackdropTexture(360, 640);
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Mining Deep Space";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(fieldRoot.transform, false);
            quad.transform.localPosition = new Vector3(0f, 1.5f, SpawnZ + 28f);
            quad.transform.localScale = new Vector3(120f, 80f, 1f);
            Material material = CreateUnlitMaterial(Color.white, generatedBackdrop);
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void BuildStarfield()
        {
            starStreaks.Clear();
        }

        private static Vector3 RandomStarPosition()
        {
            return new Vector3(
                UnityEngine.Random.Range(-7f, 7f),
                UnityEngine.Random.Range(-4.5f, 5f),
                0f);
        }

        private void BuildShip()
        {
            GameObject prefab = Resources.Load<GameObject>("Mining/StellarHarvest");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Mining/ExplorerCraft3D");
            }

            GameObject ship;
            if (prefab != null)
            {
                ship = Instantiate(prefab);
                ship.name = "Mining Ship";
                ApplyShipMaterial(ship);
                NormalizeShipScale(ship);
                shipModelTransform = ship.transform;
            }
            else
            {
                ship = BuildPlaceholderShip();
                shipModelTransform = null;
            }

            shipRoot = new GameObject("Ship Root").transform;
            shipRoot.SetParent(fieldRoot.transform, false);
            shipRoot.localPosition = new Vector3(0f, ShipBaseY, 0f);
            ship.transform.SetParent(shipRoot, false);
            ship.transform.localPosition = Vector3.zero;
            ship.transform.localRotation = shipModelTransform != null
                ? Quaternion.Euler(shipModelEuler)
                : Quaternion.identity;
            shipRenderers = ship.GetComponentsInChildren<Renderer>(true);

            BuildEngineThruster();
        }

        private void BuildEngineThruster()
        {
            GameObject flameObject = new GameObject("Engine Thruster");
            flameObject.transform.SetParent(shipRoot, false);
            flameObject.transform.localPosition = thrusterLocalPosition;
            // The cone emits along +Z, so rotate it backward from the ship.
            flameObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            thrusterTransform = flameObject.transform;

            ParticleSystem flame = flameObject.AddComponent<ParticleSystem>();
            thrusterParticles = flame;
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = flame.main;
            main.loop = true;
            main.startLifetime = 0.32f;
            main.startSpeed = 7.5f;
            main.startSize = 0.55f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 320;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = flame.emission;
            emission.rateOverTime = 150f;

            ParticleSystem.ShapeModule shape = flame.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.12f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                flame.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.97f, 0.65f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.4f),
                    new GradientColorKey(new Color(0.85f, 0.2f, 0.45f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
                flame.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0.05f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer flameRenderer =
                flameObject.GetComponent<ParticleSystemRenderer>();
            Material flameMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            flameRenderer.sharedMaterial = flameMaterial;
            flameRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            runtimeMaterials.Add(flameMaterial);

            flame.Play();
        }

        private void BuildHeartPickupPrefab()
        {
            if (!EnableHeartPickups)
            {
                return;
            }

            if (heartPrefab != null)
            {
                return;
            }

            generatedPickupStarTexture = CreatePickupStarTexture(96);
            Material material = CreateUnlitMaterial(
                new Color(1f, 0.86f, 0.18f, 1f),
                generatedPickupStarTexture);
            ConfigureTransparentPickupMaterial(material);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Pickup Star Prefab";
            quad.transform.SetParent(fieldRoot.transform, false);
            quad.transform.localScale = Vector3.one * HeartPickupScale;
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
            quad.SetActive(false);
            heartPrefab = quad;
        }

        private void ApplyShipMaterial(GameObject ship)
        {
            // The FBX is exported without embedded textures, so rebuild a URP material
            // from the baked base-color map to keep the ship's purple/gold look.
            Texture albedo = Resources.Load<Texture2D>("Mining/StellarHarvest_BaseColor");
            if (albedo == null)
            {
                return;
            }

            Material shipMaterial = CreateLitMaterial(
                Color.white,
                0.55f,
                0.6f,
                Color.black,
                albedo);
            MeshRenderer[] renderers = ship.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = shipMaterial;
            }
        }

        private void NormalizeShipScale(GameObject ship)
        {
            float maxDim = 0f;
            MeshFilter[] filters = ship.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null)
                {
                    continue;
                }

                Vector3 size = filters[i].sharedMesh.bounds.size;
                maxDim = Mathf.Max(maxDim, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
            }

            if (maxDim > 0.001f)
            {
                shipBaseMaxDim = maxDim;
                ship.transform.localScale =
                    Vector3.one *
                    (Mathf.Max(0.05f, shipDisplaySize) /
                     shipBaseMaxDim);
            }
        }

        private GameObject BuildPlaceholderShip()
        {
            GameObject ship = new GameObject("Placeholder Ship");
            Material hull = CreateLitMaterial(
                new Color(0.55f, 0.72f, 0.95f, 1f),
                0.6f,
                0.65f,
                Color.black);
            Material glow = CreateLitMaterial(
                new Color(0.2f, 0.85f, 1f, 1f),
                0f,
                0.5f,
                new Color(0.1f, 0.7f, 1f, 1f) * 2.2f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.name = "Hull";
            body.transform.SetParent(ship.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.95f, 0.55f);
            body.GetComponent<MeshRenderer>().sharedMaterial = hull;

            GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(leftWing.GetComponent<Collider>());
            leftWing.name = "Left Wing";
            leftWing.transform.SetParent(ship.transform, false);
            leftWing.transform.localPosition = new Vector3(-0.55f, -0.05f, -0.2f);
            leftWing.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            leftWing.transform.localScale = new Vector3(0.7f, 0.08f, 0.5f);
            leftWing.GetComponent<MeshRenderer>().sharedMaterial = hull;

            GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(rightWing.GetComponent<Collider>());
            rightWing.name = "Right Wing";
            rightWing.transform.SetParent(ship.transform, false);
            rightWing.transform.localPosition = new Vector3(0.55f, -0.05f, -0.2f);
            rightWing.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            rightWing.transform.localScale = new Vector3(0.7f, 0.08f, 0.5f);
            rightWing.GetComponent<MeshRenderer>().sharedMaterial = hull;

            GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(thruster.GetComponent<Collider>());
            thruster.name = "Thruster";
            thruster.transform.SetParent(ship.transform, false);
            thruster.transform.localPosition = new Vector3(0f, 0f, -0.7f);
            thruster.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            thruster.GetComponent<MeshRenderer>().sharedMaterial = glow;

            return ship;
        }

        private void LoadRockPrefabs()
        {
            // Obstacles come exclusively from Resources/Mining/Obstacles (CC0 asteroid
            // models). Drop a model in and it is auto-registered — keeps its own
            // imported material and is auto-normalized to the standard size, so the
            // set can be changed just by adding/removing files, no code changes.
            List<GameObject> prefabs = new List<GameObject>();
            List<float> baseScales = new List<float>();
            List<Material> materials = new List<Material>();

            GameObject[] obstacleModels =
                Resources.LoadAll<GameObject>("Mining/Obstacles");
            for (int i = 0; i < obstacleModels.Length; i++)
            {
                GameObject prefab = obstacleModels[i];
                if (prefab == null || prefabs.Contains(prefab))
                {
                    continue;
                }

                prefabs.Add(prefab);
                baseScales.Add(DesiredRockSize / EstimatePrefabMaxExtent(prefab));
                materials.Add(null);
            }

            if (prefabs.Count == 0)
            {
                // Fallback: a single procedural rock so the game still works.
                Material rockMaterial = CreateLitMaterial(
                    new Color(0.42f, 0.4f, 0.46f, 1f),
                    0.1f,
                    0.25f,
                    Color.black);
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(placeholder.GetComponent<Collider>());
                placeholder.name = "Placeholder Rock";
                placeholder.GetComponent<MeshRenderer>().sharedMaterial = rockMaterial;
                placeholder.SetActive(false);
                placeholder.transform.SetParent(fieldRoot.transform, false);
                rockPrefabs = new[] { placeholder };
                rockPrefabBaseScale = new[] { DesiredRockSize };
                rockMaterials = new[] { rockMaterial };
                rockPrefabsAreInstances = true;
                return;
            }

            rockPrefabs = prefabs.ToArray();
            rockPrefabBaseScale = baseScales.ToArray();
            rockMaterials = materials.ToArray();
            rockPrefabsAreInstances = false;
        }

        private static float EstimatePrefabMaxExtent(GameObject prefab)
        {
            // Imported model scale is unknown; measure the mesh so runtime objects
            // can be normalized to a consistent on-screen size.
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
            float maxExtent = 0f;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Vector3 size = filter.sharedMesh.bounds.size;
                Vector3 scale = filter.transform.localScale;
                maxExtent = Mathf.Max(
                    maxExtent,
                    Mathf.Max(
                        size.x * Mathf.Abs(scale.x),
                        Mathf.Max(
                        size.y * Mathf.Abs(scale.y),
                        size.z * Mathf.Abs(scale.z))));
            }

            if (maxExtent <= 0.001f)
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Vector3 size = renderers[i].bounds.size;
                    maxExtent = Mathf.Max(
                        maxExtent,
                        Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
                }
            }

            return Mathf.Max(0.01f, maxExtent);
        }

        private void SpawnRock()
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0)
            {
                return;
            }

            Asteroid rock = rockPool.Count > 0 ? rockPool.Pop() : CreateRock();
            if (rock == null)
            {
                return;
            }

            float targetSize = UnityEngine.Random.Range(0.55f, 1.15f);
            rock.transform.localScale = Vector3.one * (rock.baseScale * targetSize);
            rock.radius = DesiredRockSize * targetSize * 0.5f * 0.9f;

            float x = UnityEngine.Random.Range(-ShipHalfRange * 1.05f, ShipHalfRange * 1.05f);
            float y = ShipBaseY + UnityEngine.Random.Range(-0.25f, 0.55f);
            rock.transform.localPosition = new Vector3(x, y, SpawnZ);
            rock.previousZ = SpawnZ;
            rock.transform.localRotation = UnityEngine.Random.rotation;
            rock.spin = new Vector3(
                UnityEngine.Random.Range(-90f, 90f),
                UnityEngine.Random.Range(-90f, 90f),
                UnityEngine.Random.Range(-90f, 90f));
            rock.gameObject.SetActive(true);
            activeRocks.Add(rock);
        }

        private void SpawnHeart()
        {
            if (!EnableHeartPickups)
            {
                return;
            }

            if (heartPrefab == null)
            {
                return;
            }

            Pickup heart = heartPool.Count > 0 ? heartPool.Pop() : CreateHeart();
            if (heart == null)
            {
                return;
            }

            float x = UnityEngine.Random.Range(
                -ShipHalfRange * 0.82f,
                ShipHalfRange * 0.82f);
            float y = ShipBaseY + 0.38f;
            heart.transform.localPosition = new Vector3(x, y, SpawnZ);
            heart.transform.localRotation = Quaternion.identity;
            heart.transform.localScale = heartPrefab.transform.localScale;
            heart.previousZ = SpawnZ;
            heart.bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            heart.gameObject.SetActive(true);
            activeHearts.Add(heart);
        }

        private Pickup CreateHeart()
        {
            GameObject instance = Instantiate(heartPrefab);
            instance.name = "Pickup Star";
            instance.transform.SetParent(fieldRoot.transform, false);
            SetLayerRecursively(instance, MiningLayer);
            return new Pickup
            {
                gameObject = instance,
                transform = instance.transform,
                previousZ = SpawnZ,
                bobPhase = 0f
            };
        }

        private Asteroid CreateRock()
        {
            int index = UnityEngine.Random.Range(0, rockPrefabs.Length);
            GameObject source = rockPrefabs[index];
            GameObject instance = Instantiate(source);
            instance.name = "Asteroid";
            instance.transform.SetParent(fieldRoot.transform, false);
            instance.SetActive(true);
            SetLayerRecursively(instance, MiningLayer);

            Material material = rockMaterials != null && index < rockMaterials.Length
                ? rockMaterials[index]
                : null;
            if (material != null)
            {
                MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = material;
                }
            }

            // Normalize from the real rendered world bounds at unit scale. This is
            // robust to imported FBX hierarchies / unit scaling (the old per-mesh
            // localScale estimate broke for nested asteroid models, rendering them
            // tiny/huge — invisible — while the fixed collision radius still hit).
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            float baseScale = ComputeNormalizedScale(instance, index);

            return new Asteroid
            {
                gameObject = instance,
                transform = instance.transform,
                baseScale = baseScale,
                radius = 0.5f,
                previousZ = SpawnZ
            };
        }

        private float ComputeNormalizedScale(GameObject instance, int index)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderers[i].bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (found)
            {
                float maxDim = Mathf.Max(
                    bounds.size.x,
                    Mathf.Max(bounds.size.y, bounds.size.z));
                if (maxDim > 0.0001f)
                {
                    return DesiredRockSize / maxDim;
                }
            }

            return rockPrefabBaseScale != null && index < rockPrefabBaseScale.Length
                ? rockPrefabBaseScale[index]
                : DesiredRockSize;
        }

        private void RecycleRock(int activeIndex)
        {
            Asteroid rock = activeRocks[activeIndex];
            activeRocks.RemoveAt(activeIndex);
            rock.gameObject.SetActive(false);
            rockPool.Push(rock);
        }

        private void RecycleHeart(int activeIndex)
        {
            Pickup heart = activeHearts[activeIndex];
            activeHearts.RemoveAt(activeIndex);
            heart.gameObject.SetActive(false);
            heartPool.Push(heart);
        }

        private void ClearRocks()
        {
            for (int i = activeRocks.Count - 1; i >= 0; i--)
            {
                Asteroid rock = activeRocks[i];
                rock.gameObject.SetActive(false);
                rockPool.Push(rock);
            }

            activeRocks.Clear();
        }

        private void ClearHearts()
        {
            for (int i = activeHearts.Count - 1; i >= 0; i--)
            {
                Pickup heart = activeHearts[i];
                heart.gameObject.SetActive(false);
                heartPool.Push(heart);
            }

            activeHearts.Clear();
        }

        private void SetFieldActive(bool active)
        {
            if (fieldRoot != null)
            {
                fieldRoot.SetActive(active);
            }

            if (fieldCamera != null)
            {
                fieldCamera.enabled = active;
            }
        }

        private void EnsureMiningAudio()
        {
            if (engineAudioSource != null && boosterAudioSource != null)
            {
                return;
            }

            engineAudioClip = LoadFirstAudioClip(
                "Audio/engine",
                "Audio/rocket_engine",
                "Audio/RocketEngine");
            boosterAudioClip = LoadFirstAudioClip(
                "Audio/buster",
                "Audio/booster",
                "Audio/rocket_booster",
                "Audio/RocketBooster");

            engineAudioSource = CreateMiningAudioSource(
                "Mining Engine Audio",
                engineAudioClip,
                true,
                0.68f);
            boosterAudioSource = CreateMiningAudioSource(
                "Mining Booster Audio",
                boosterAudioClip,
                false,
                0.95f);
        }

        private static AudioClip LoadFirstAudioClip(params string[] resourcePaths)
        {
            for (int i = 0; i < resourcePaths.Length; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>(resourcePaths[i]);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private AudioSource CreateMiningAudioSource(
            string objectName,
            AudioClip clip,
            bool loop,
            float volume)
        {
            GameObject audioObject = new GameObject(objectName);
            audioObject.transform.SetParent(transform, false);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.clip = clip;
            return source;
        }

        private void UpdateMiningAudioState()
        {
            if (engineAudioSource == null)
            {
                return;
            }

            bool shouldPlayEngine =
                soundEnabled &&
                state == MiningState.Running &&
                !boosting &&
                engineAudioSource.clip != null;
            bool shouldPauseEngine =
                soundEnabled &&
                state == MiningState.Running &&
                boosting &&
                engineAudioSource.clip != null;

            if (shouldPlayEngine)
            {
                engineAudioSource.volume = 0.68f * sfxVolume;
                if (!engineAudioSource.isPlaying)
                {
                    if (engineAudioPausedForBoost)
                    {
                        engineAudioSource.UnPause();
                    }
                    else
                    {
                        engineAudioSource.Play();
                    }
                }

                engineAudioPausedForBoost = false;
            }
            else if (shouldPauseEngine)
            {
                if (engineAudioSource.isPlaying)
                {
                    engineAudioSource.Pause();
                    engineAudioPausedForBoost = true;
                }
            }
            else
            {
                engineAudioSource.Stop();
                engineAudioPausedForBoost = false;
            }

            if ((!soundEnabled || state != MiningState.Running) &&
                boosterAudioSource != null)
            {
                boosterAudioSource.Stop();
            }
        }

        private void PlayBoosterAudio()
        {
            if (!soundEnabled ||
                boosterAudioSource == null ||
                boosterAudioClip == null)
            {
                return;
            }

            boosterAudioSource.Stop();
            boosterAudioSource.pitch = 1f;
            boosterAudioSource.PlayOneShot(boosterAudioClip, 0.95f * sfxVolume);
        }

        // ----------------------------------------------------------------- UI build

        private void BuildBackdrop(RectTransform root)
        {
            GameObject veil = CreatePanel(
                "Backdrop Veil",
                root,
                new Color(0.004f, 0.01f, 0.03f, 1f));
            Stretch(veil.GetComponent<RectTransform>());
        }

        private void BuildHeader(RectTransform root)
        {
            Button exitButton = CreateButton("나가기", 20, root);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.035f, 0.9f);
            exitRect.anchorMax = new Vector2(0.25f, 0.965f);
            exitRect.offsetMin = Vector2.zero;
            exitRect.offsetMax = Vector2.zero;
            exitButton.onClick.AddListener(Close);

            Text title = CreateText(
                "우주선 비행",
                32,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                root);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.25f, 0.88f);
            titleRect.anchorMax = new Vector2(0.72f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.color = new Color(0.94f, 0.98f, 1f, 1f);

            attemptsText = CreateText(
                "오늘 남은 탐사 3/3",
                17,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                root);
            RectTransform attemptsRect = attemptsText.GetComponent<RectTransform>();
            attemptsRect.anchorMin = new Vector2(0.72f, 0.9f);
            attemptsRect.anchorMax = new Vector2(0.96f, 0.965f);
            attemptsRect.offsetMin = Vector2.zero;
            attemptsRect.offsetMax = Vector2.zero;
            attemptsText.color = new Color(1f, 0.82f, 0.38f, 1f);

            bestText = CreateText(
                "최고 기록 0 광년",
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                root);
            RectTransform bestRect = bestText.GetComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0.06f, 0.83f);
            bestRect.anchorMax = new Vector2(0.52f, 0.875f);
            bestRect.offsetMin = Vector2.zero;
            bestRect.offsetMax = Vector2.zero;
            bestText.color = new Color(1f, 0.82f, 0.38f, 1f);

            distanceText = CreateText(
                "비행 거리 0 광년",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                root);
            RectTransform distanceRect = distanceText.GetComponent<RectTransform>();
            distanceRect.anchorMin = new Vector2(0.5f, 0.83f);
            distanceRect.anchorMax = new Vector2(0.94f, 0.875f);
            distanceRect.offsetMin = Vector2.zero;
            distanceRect.offsetMax = Vector2.zero;
            distanceText.color = new Color(0.55f, 0.9f, 1f, 1f);
        }

        private void BuildHearts(RectTransform root)
        {
            GameObject heartsRoot = new GameObject(
                "Lives Hearts",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            heartsRoot.transform.SetParent(root, false);
            RectTransform heartsRect = heartsRoot.GetComponent<RectTransform>();
            heartsRect.anchorMin = new Vector2(0.66f, 0.765f);
            heartsRect.anchorMax = new Vector2(0.94f, 0.82f);
            heartsRect.offsetMin = new Vector2(-50f, -30f);
            heartsRect.offsetMax = new Vector2(-50f, -30f);

            HorizontalLayoutGroup layout =
                heartsRoot.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < heartIcons.Length; i++)
            {
                GameObject heartObject = new GameObject(
                    "남은 목숨 하트",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                heartObject.transform.SetParent(heartsRoot.transform, false);

                Image heart = heartObject.GetComponent<Image>();
                heart.sprite = generatedHeartIconSprite;
                heart.preserveAspect = true;
                heart.raycastTarget = false;

                LayoutElement layoutElement =
                    heartObject.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 52f;
                layoutElement.preferredHeight = 54f;
                heartIcons[i] = heart;
            }

            UpdateHeartsHud();
        }

        private void BuildPlayfield(RectTransform root)
        {
            GameObject frame = CreateFramedPanel(
                "Field Frame",
                root,
                new Color(0.006f, 0.02f, 0.05f, 1f),
                new Color(0.08f, 0.44f, 0.72f, 0.95f));
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.06f, 0.135f);
            frameRect.anchorMax = new Vector2(0.94f, 0.82f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;

            GameObject imageObject = new GameObject(
                "Field Render",
                typeof(RectTransform),
                typeof(RawImage));
            imageObject.transform.SetParent(frame.transform, false);
            fieldImage = imageObject.GetComponent<RawImage>();
            fieldImage.color = Color.white;
            fieldImage.texture = renderTexture;
            fieldImageRect = imageObject.GetComponent<RectTransform>();
            fieldImageRect.anchorMin = Vector2.zero;
            fieldImageRect.anchorMax = Vector2.one;
            fieldImageRect.offsetMin = new Vector2(6f, 6f);
            fieldImageRect.offsetMax = new Vector2(-6f, -6f);

            instructionText = CreateText(
                "드래그로 좌우 이동하며 운석을 피하세요",
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                root);
            RectTransform instructionRect = instructionText.GetComponent<RectTransform>();
            instructionRect.anchorMin = new Vector2(0.06f, 0.092f);
            instructionRect.anchorMax = new Vector2(0.94f, 0.13f);
            instructionRect.offsetMin = Vector2.zero;
            instructionRect.offsetMax = Vector2.zero;
            instructionText.color = new Color(0.7f, 0.85f, 1f, 1f);

            boostButton = CreateButton("부스터", 24, root);
            RectTransform boostRect = boostButton.GetComponent<RectTransform>();
            boostRect.anchorMin = new Vector2(0.32f, 0.155f);
            boostRect.anchorMax = new Vector2(0.68f, 0.225f);
            boostRect.offsetMin = Vector2.zero;
            boostRect.offsetMax = Vector2.zero;
            boostButtonText = boostButton.GetComponentInChildren<Text>();
            Image boostFrame = boostButton.image;
            if (boostFrame != null)
            {
                boostFrame.color = new Color(0.95f, 0.45f, 0.12f, 0.98f);
            }

            Image boostFill = boostButton.targetGraphic as Image;
            if (boostFill != null)
            {
                boostFill.color = new Color(0.22f, 0.08f, 0.02f, 0.99f);
            }

            if (boostButtonText != null)
            {
                boostButtonText.color = new Color(1f, 0.85f, 0.45f, 1f);
            }

            boostButton.onClick.AddListener(TryStartBoost);
            boostButton.gameObject.SetActive(false);
        }

        private void BuildStartOverlay(RectTransform root)
        {
            startOverlay = CreatePanel(
                "Mining Start Overlay",
                root,
                new Color(0.002f, 0.006f, 0.018f, 0.68f));
            RectTransform overlayRect = startOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.06f, 0.135f);
            overlayRect.anchorMax = new Vector2(0.94f, 0.82f);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject card = CreateFramedPanel(
                "Mining Start Card",
                startOverlay.transform,
                new Color(0.012f, 0.035f, 0.075f, 0.96f),
                new Color(0.1f, 0.58f, 0.9f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.14f, 0.36f);
            cardRect.anchorMax = new Vector2(0.86f, 0.64f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            Text hint = CreateText(
                "드래그해서 운석을 피하고 멀리 비행하세요.",
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.08f, 0.6f);
            hintRect.anchorMax = new Vector2(0.92f, 0.9f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            hint.color = new Color(0.82f, 0.95f, 1f, 1f);

            startButton = CreateButton("게임 시작", 30, card.transform);
            RectTransform startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.16f, 0.16f);
            startRect.anchorMax = new Vector2(0.84f, 0.5f);
            startRect.offsetMin = Vector2.zero;
            startRect.offsetMax = Vector2.zero;
            startButton.onClick.AddListener(StartRound);

            Image startFrame = startButton.image;
            if (startFrame != null)
            {
                startFrame.color = new Color(0.0f, 0.52f, 0.95f, 0.98f);
            }

            Text startText = startButton.GetComponentInChildren<Text>();
            if (startText != null)
            {
                startText.color = Color.white;
            }

            startOverlay.SetActive(false);
        }

        private void BuildResultOverlay(RectTransform root)
        {
            resultOverlay = CreatePanel(
                "Mining Result Overlay",
                root,
                new Color(0.002f, 0.006f, 0.018f, 0.86f));
            Stretch(resultOverlay.GetComponent<RectTransform>());

            GameObject dialog = CreateFramedPanel(
                "Mining Result Card",
                resultOverlay.transform,
                new Color(0.015f, 0.035f, 0.08f, 0.99f),
                new Color(0.16f, 0.58f, 0.88f, 1f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.1f, 0.25f);
            dialogRect.anchorMax = new Vector2(0.9f, 0.75f);
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;

            Text header = CreateText(
                "탐사 결과",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.12f, 0.87f);
            headerRect.anchorMax = new Vector2(0.88f, 0.96f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.color = new Color(0.5f, 0.86f, 1f, 1f);

            resultGradeText = CreateText(
                "좋음",
                44,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            RectTransform gradeRect = resultGradeText.GetComponent<RectTransform>();
            gradeRect.anchorMin = new Vector2(0.08f, 0.69f);
            gradeRect.anchorMax = new Vector2(0.92f, 0.86f);
            gradeRect.offsetMin = Vector2.zero;
            gradeRect.offsetMax = Vector2.zero;

            resultScoreText = CreateText(
                "비행 거리  0 광년",
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                dialog.transform);
            RectTransform scoreRect = resultScoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.06f, 0.59f);
            scoreRect.anchorMax = new Vector2(0.94f, 0.67f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;

            GameObject rewardPanel = CreateFramedPanel(
                "Reward Panel",
                dialog.transform,
                new Color(0.025f, 0.07f, 0.12f, 0.96f),
                new Color(0.1f, 0.4f, 0.62f, 0.9f));
            RectTransform rewardRect = rewardPanel.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0.08f, 0.4f);
            rewardRect.anchorMax = new Vector2(0.92f, 0.58f);
            rewardRect.offsetMin = Vector2.zero;
            rewardRect.offsetMax = Vector2.zero;

            Text rewardHeader = CreateText(
                "회수 보상",
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                rewardPanel.transform);
            RectTransform rewardHeaderRect = rewardHeader.GetComponent<RectTransform>();
            rewardHeaderRect.anchorMin = new Vector2(0.05f, 0.62f);
            rewardHeaderRect.anchorMax = new Vector2(0.95f, 0.95f);
            rewardHeaderRect.offsetMin = Vector2.zero;
            rewardHeaderRect.offsetMax = Vector2.zero;
            rewardHeader.color = new Color(1f, 0.82f, 0.38f, 1f);

            for (int i = 0; i < resultRewardIcons.Length; i++)
            {
                float slotMin = 0.06f + i * 0.46f;
                GameObject iconObject = new GameObject(
                    "Reward Icon " + i,
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(rewardPanel.transform, false);
                Image icon = iconObject.GetComponent<Image>();
                icon.preserveAspect = true;
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(slotMin, 0.08f);
                iconRect.anchorMax = new Vector2(slotMin + 0.13f, 0.62f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                resultRewardIcons[i] = icon;

                Text amount = CreateText(
                    string.Empty,
                    20,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    rewardPanel.transform);
                RectTransform amountRect = amount.GetComponent<RectTransform>();
                amountRect.anchorMin = new Vector2(slotMin + 0.14f, 0.08f);
                amountRect.anchorMax = new Vector2(slotMin + 0.44f, 0.62f);
                amountRect.offsetMin = Vector2.zero;
                amountRect.offsetMax = Vector2.zero;
                amount.color = new Color(0.92f, 0.97f, 1f, 1f);
                resultRewardTexts[i] = amount;
            }

            continueAdButton = CreateButton("광고 보고 이어서 진행하기", 21, dialog.transform);
            RectTransform continueRect = continueAdButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.08f, 0.23f);
            continueRect.anchorMax = new Vector2(0.92f, 0.37f);
            continueRect.offsetMin = Vector2.zero;
            continueRect.offsetMax = Vector2.zero;
            continueAdButtonText = continueAdButton.GetComponentInChildren<Text>();
            Image continueFrame = continueAdButton.image;
            if (continueFrame != null)
            {
                continueFrame.color = new Color(0.88f, 0.56f, 0.12f, 0.98f);
            }

            Image continueFill = continueAdButton.targetGraphic as Image;
            if (continueFill != null)
            {
                continueFill.color = new Color(0.15f, 0.09f, 0.018f, 0.99f);
            }

            if (continueAdButtonText != null)
            {
                continueAdButtonText.color = new Color(1f, 0.88f, 0.5f, 1f);
            }

            continueAdButton.onClick.AddListener(HandleContinueAdButton);

            replayButton = CreateButton("다시 탐사", 23, dialog.transform);
            RectTransform replayRect = replayButton.GetComponent<RectTransform>();
            replayRect.anchorMin = new Vector2(0.08f, 0.06f);
            replayRect.anchorMax = new Vector2(0.48f, 0.2f);
            replayRect.offsetMin = Vector2.zero;
            replayRect.offsetMax = Vector2.zero;
            replayButtonText = replayButton.GetComponentInChildren<Text>();
            replayButton.onClick.AddListener(HandleReplayButton);

            Button homeButton = CreateButton("메인으로", 23, dialog.transform);
            RectTransform homeRect = homeButton.GetComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(0.52f, 0.06f);
            homeRect.anchorMax = new Vector2(0.92f, 0.2f);
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;
            homeButton.onClick.AddListener(Close);

            resultOverlay.SetActive(false);
        }

        private void BuildNewRecordOverlay(RectTransform root)
        {
            newRecordOverlay = CreatePanel(
                "채굴 신기록 오버레이",
                root,
                new Color(0.002f, 0.006f, 0.018f, 0.9f));
            Stretch(newRecordOverlay.GetComponent<RectTransform>());

            GameObject card = CreateFramedPanel(
                "신기록 카드",
                newRecordOverlay.transform,
                new Color(0.02f, 0.045f, 0.09f, 0.99f),
                new Color(1f, 0.82f, 0.32f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.13f, 0.36f);
            cardRect.anchorMax = new Vector2(0.87f, 0.64f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            Text badge = CreateText(
                "신기록",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.08f, 0.78f);
            badgeRect.anchorMax = new Vector2(0.92f, 0.95f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;
            badge.color = new Color(1f, 0.86f, 0.4f, 1f);

            Text headline = CreateText(
                "최고 기록 갱신!",
                30,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform headlineRect = headline.GetComponent<RectTransform>();
            headlineRect.anchorMin = new Vector2(0.06f, 0.56f);
            headlineRect.anchorMax = new Vector2(0.94f, 0.78f);
            headlineRect.offsetMin = Vector2.zero;
            headlineRect.offsetMax = Vector2.zero;
            headline.color = new Color(1f, 0.95f, 0.8f, 1f);

            newRecordValueText = CreateText(
                "0 광년",
                40,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                card.transform);
            RectTransform valueRect = newRecordValueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.06f, 0.34f);
            valueRect.anchorMax = new Vector2(0.94f, 0.56f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            newRecordValueText.color = new Color(0.6f, 0.95f, 1f, 1f);

            Button confirmButton = CreateButton("확인", 26, card.transform);
            RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.22f, 0.08f);
            confirmRect.anchorMax = new Vector2(0.78f, 0.3f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirmButton.onClick.AddListener(() => SetNewRecordVisible(false));

            newRecordOverlay.SetActive(false);
        }

        // ----------------------------------------------------------------- UI state

        private void SetStartOverlayVisible(bool visible)
        {
            if (startOverlay != null && startOverlay.activeSelf != visible)
            {
                startOverlay.SetActive(visible);
            }

            if (startButton != null)
            {
                startButton.interactable =
                    visible &&
                    state == MiningState.Ready &&
                    remainingAttempts > 0;
            }
        }

        private void UpdateAttemptsText()
        {
            if (attemptsText == null)
            {
                return;
            }

            attemptsText.text = "오늘 남은 탐사 " +
                                Mathf.Clamp(remainingAttempts, 0, BaseDailyAttempts) +
                                "/" +
                                BaseDailyAttempts;
        }

        private void UpdateDistanceText()
        {
            if (distanceText == null)
            {
                return;
            }

            distanceText.text = "비행 거리 " + Mathf.RoundToInt(runDistance) + " 광년";
        }

        private void UpdateBestText()
        {
            if (bestText == null)
            {
                return;
            }

            bestText.text = "최고 기록 " + bestDistance + " 광년";
        }

        private void UpdateHeartsHud()
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] == null)
                {
                    continue;
                }

                heartIcons[i].gameObject.SetActive(i < lives);
            }
        }

        private void SaveBestDistance()
        {
            PlayerPrefs.SetInt(BestDistancePrefKey, bestDistance);
            PlayerPrefs.Save();
        }

        private void ShowNewRecordCelebration()
        {
            if (newRecordValueText != null)
            {
                newRecordValueText.text = bestDistance + " 광년";
            }

            SetNewRecordVisible(true);
        }

        private void SetNewRecordVisible(bool visible)
        {
            if (newRecordOverlay != null && newRecordOverlay.activeSelf != visible)
            {
                newRecordOverlay.SetActive(visible);
            }
        }

        private void UpdateReplayState()
        {
            if (replayButton == null)
            {
                return;
            }

            bool canReplay = remainingAttempts > 0;
            replayButton.interactable = true;
            if (replayButtonText != null)
            {
                replayButtonText.text = canReplay ? "다시 탐사" : "오늘 탐사 완료";
            }

            UpdateContinueState();
        }

        private void UpdateContinueState()
        {
            if (continueAdButton == null)
            {
                return;
            }

            bool canContinue = CanContinueCurrentRunWithAd();
            bool canRequestBonus = CanRequestBonusAttemptWithAd();
            bool shouldShow = canContinue || canRequestBonus;
            continueAdButton.gameObject.SetActive(shouldShow);
            bool canWatch = shouldShow && !rewardedAdBusy;
            continueAdButton.interactable = canWatch;
            if (continueAdButtonText != null)
            {
                continueAdButtonText.text = rewardedAdBusy
                    ? "광고 불러오는 중..."
                    : canContinue
                        ? "광고 보고 이어하기"
                        : "광고 보고 추가로 탐험하기";
            }
        }

        private bool CanContinueCurrentRunWithAd()
        {
            return canContinueCurrentRun &&
                   continuesUsedThisRun < MaxContinuesPerRun;
        }

        private bool CanRequestBonusAttemptWithAd()
        {
            return remainingAttempts <= 0 && remainingAdBonuses > 0;
        }

        private void UpdateRewardSlots(CurrencyAmount[] rewards)
        {
            for (int i = 0; i < resultRewardIcons.Length; i++)
            {
                CurrencyAmount reward = rewards != null && i < rewards.Length ? rewards[i] : null;
                bool visible = reward != null && reward.amount > 0;
                resultRewardIcons[i].gameObject.SetActive(visible);
                resultRewardTexts[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                resultRewardIcons[i].sprite = materialIconSprites[(int)reward.type];
                resultRewardTexts[i].text =
                    StarForgeCurrencyNames.GetDisplayName(reward.type) +
                    "\n<color=#FFD15C>+" +
                    reward.amount +
                    "</color>";
            }
        }

        private void LoadMaterialIcons()
        {
            for (int i = 0; i < materialIconSprites.Length; i++)
            {
                string path = "MaterialIcons/Material_" + (i + 1);
                materialIconSprites[i] = Resources.Load<Sprite>(path);
                if (materialIconSprites[i] != null)
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture == null)
                {
                    continue;
                }

                materialIconSprites[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                ownsMaterialIconSprite[i] = true;
            }
        }

        private static void GetGrade(float score, out string grade, out Color color)
        {
            if (score >= 0.995f)
            {
                grade = "완벽한 비행";
                color = new Color(1f, 0.84f, 0.3f, 1f);
                return;
            }

            if (score >= 0.9f)
            {
                grade = "매우 좋음";
                color = new Color(0.7f, 0.92f, 1f, 1f);
                return;
            }

            if (score >= 0.7f)
            {
                grade = "좋음";
                color = new Color(0.3f, 0.88f, 1f, 1f);
                return;
            }

            if (score >= 0.4f)
            {
                grade = "보통";
                color = new Color(1f, 0.7f, 0.28f, 1f);
                return;
            }

            grade = "짧은 비행";
            color = new Color(1f, 0.46f, 0.46f, 1f);
        }

        // ----------------------------------------------------------------- materials

        private Material CreateLitMaterial(
            Color baseColor,
            float metallic,
            float smoothness,
            Color emission,
            Texture albedo = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            else if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            if (albedo != null)
            {
                material.mainTexture = albedo;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", albedo);
                }
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private Material CreateUnlitMaterial(Color color, Texture texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private static void ConfigureTransparentPickupMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    new Color(1f, 0.72f, 0.08f, 1f) * 1.6f);
            }
        }

        // ----------------------------------------------------------------- helpers

        private GameObject CreateFramedPanel(
            string objectName,
            Transform parent,
            Color fillColor,
            Color frameColor)
        {
            GameObject frame = CreatePanel(objectName, parent, frameColor);
            Image frameImage = frame.GetComponent<Image>();
            frameImage.sprite = generatedChamferedSprite;
            frameImage.type = Image.Type.Sliced;
            GameObject fill = CreatePanel("Fill", frame.transform, fillColor);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = generatedChamferedSprite;
            fillImage.type = Image.Type.Sliced;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            return frame;
        }

        private Button CreateButton(string label, int fontSize, Transform parent)
        {
            GameObject buttonObject = CreateFramedPanel(
                label + " Button",
                parent,
                new Color(0.025f, 0.09f, 0.19f, 0.99f),
                new Color(0.1f, 0.48f, 0.78f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.transform.Find("Fill").GetComponent<Image>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.92f, 1f);
            colors.disabledColor = new Color(0.35f, 0.42f, 0.5f, 0.78f);
            button.colors = colors;

            Text text = CreateText(label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, buttonObject.transform);
            Stretch(text.GetComponent<RectTransform>());
            text.color = new Color(0.9f, 0.97f, 1f, 1f);
            text.raycastTarget = false;
            return button;
        }

        private Text CreateText(
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Transform parent)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static GameObject CreatePanel(string objectName, Transform parent, Color color)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static RectTransform CreateSafeAreaRoot(RectTransform root)
        {
            GameObject safeAreaObject = new GameObject(
                "Mining Safe Area",
                typeof(RectTransform),
                typeof(StarForgeSafeArea));
            safeAreaObject.transform.SetParent(root, false);
            RectTransform safeArea = safeAreaObject.GetComponent<RectTransform>();
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            return safeArea;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            Transform targetTransform = target.transform;
            for (int i = 0; i < targetTransform.childCount; i++)
            {
                SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
            }
        }

        private static Texture2D CreatePickupStarTexture(int size)
        {
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);
            texture.name = "StarForge Mining Pickup Star";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(0.5f, 0.5f);
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    Vector2 fromCenter = new Vector2(u, v) - center;
                    float angle = Mathf.Atan2(fromCenter.y, fromCenter.x);
                    float radius = fromCenter.magnitude;
                    float point = (Mathf.Cos(angle * 5f) + 1f) * 0.5f;
                    float starRadius = Mathf.Lerp(0.2f, 0.42f, point);
                    float edge = Mathf.Clamp01((starRadius - radius) * 42f);
                    float core = Mathf.Clamp01((0.18f - radius) * 16f);
                    Color color = Color.Lerp(
                        new Color(1f, 0.58f, 0.02f, 0f),
                        new Color(1f, 0.9f, 0.18f, 1f),
                        edge);
                    color += new Color(1f, 0.95f, 0.45f, 0f) * core * 0.55f;
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateSpaceBackdropTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "StarForge Mining Backdrop";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            System.Random random = new System.Random(73129);
            Vector2 nebulaCenter = new Vector2(width * 0.68f, height * 0.59f);
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float radial = 1f - Mathf.Clamp01(
                        Vector2.Distance(new Vector2(x, y), nebulaCenter) / (width * 0.78f));
                    float cloud = Mathf.Pow(radial, 2.2f) * (0.55f + Mathf.Sin(x * 0.034f + y * 0.018f) * 0.16f);
                    Color baseColor = Color.Lerp(
                        new Color(0.002f, 0.006f, 0.025f, 1f),
                        new Color(0.012f, 0.032f, 0.09f, 1f),
                        vertical);
                    baseColor += new Color(0.025f, 0.04f, 0.11f, 0f) * cloud;
                    texture.SetPixel(x, y, baseColor);
                }
            }

            for (int i = 0; i < 190; i++)
            {
                int x = random.Next(2, width - 2);
                int y = random.Next(2, height - 2);
                bool bright = random.NextDouble() > 0.82;
                Color star = bright
                    ? new Color(0.68f, 0.9f, 1f, 0.95f)
                    : new Color(0.28f, 0.52f, 0.82f, 0.65f);
                texture.SetPixel(x, y, star);
                if (bright)
                {
                    texture.SetPixel(x - 1, y, star * 0.45f);
                    texture.SetPixel(x + 1, y, star * 0.45f);
                    texture.SetPixel(x, y - 1, star * 0.45f);
                    texture.SetPixel(x, y + 1, star * 0.45f);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Sprite LoadHeartIconSprite()
        {
            Texture2D texture = Resources.Load<Texture2D>(HeartIconResourcePath);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite CreateHeartSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "남은 목숨 하트";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float px = (u - 0.5f) * 2.35f;
                    float py = (v - 0.46f) * 2.35f;
                    float value =
                        Mathf.Pow(px * px + py * py - 1f, 3f) -
                        px * px * py * py * py;
                    float alpha = Mathf.Clamp01(-value * 7f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite CreateRoundedSprite(int size, int cornerRadius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StarForge Mining Rounded";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed-distance rounded rectangle with a 1px anti-aliased edge.
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px < cornerRadius
                        ? cornerRadius - px
                        : (px > size - cornerRadius ? px - (size - cornerRadius) : 0f);
                    float dy = py < cornerRadius
                        ? cornerRadius - py
                        : (py > size - cornerRadius ? py - (size - cornerRadius) : 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01(cornerRadius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, coverage));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
        }
    }
}
