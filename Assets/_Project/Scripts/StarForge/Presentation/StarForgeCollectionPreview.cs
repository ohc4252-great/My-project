using System;
using System.Collections;
using StarForge.Core;
using StarForge.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarForge.Presentation
{
    public sealed class StarForgeCollectionPreview : MonoBehaviour
    {
        private const int PreviewLayer = 30;
        private const int BaseTextureSize = 720;
        private const int MaximumTextureSize = 1600;
        private const float MaximumTransitionDuration = 5f;
        private const float WorldScaleExponent = 0.3f;
        private const float CameraOrbitDegreesPerPixel = 0.22f;
        private const float CameraOrbitPitchLimit = 55f;

        private Camera previewCamera;
        private Transform worldRoot;
        private ParticleSystem starfield;
        private ParticleSystem nebulaField;
        private Material starfieldMaterial;
        private Material nebulaMaterial;
        private RenderTexture renderTexture;
        private Coroutine transitionRoutine;
        private GameObject[] planetAnchors;
        private StarForgePlanetView[] planetViews;
        private StarForgePlanetShape[] planetShapes;
        private bool[] planetAppearanceApplied;
        private Vector3[] cameraPositions;
        private float[] cameraDistances;
        private StarForgeBalance builtBalance;
        private int builtMaxLevel;
        private int visibleMaxLevel;
        private int currentLevel;
        private int transitionTargetLevel;
        private Action transitionOnComplete;
        private StarForgePlanetShape currentShape = StarForgePlanetShape.Default;
        private float cameraOrbitYaw;
        private float cameraOrbitPitch;
        private int textureWidth;
        private int textureHeight;

        public Texture OutputTexture
        {
            get
            {
                EnsureCreated();
                return renderTexture;
            }
        }

        public bool IsTransitioning
        {
            get { return transitionRoutine != null; }
        }

        public void SetViewportSize(float width, float height)
        {
            EnsureCreated();
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            float aspect = width / height;
            int targetWidth;
            int targetHeight;
            if (aspect <= 1f)
            {
                targetWidth = BaseTextureSize;
                targetHeight = Mathf.Clamp(
                    Mathf.RoundToInt(BaseTextureSize / aspect),
                    BaseTextureSize,
                    MaximumTextureSize);
            }
            else
            {
                targetHeight = BaseTextureSize;
                targetWidth = Mathf.Clamp(
                    Mathf.RoundToInt(BaseTextureSize * aspect),
                    BaseTextureSize,
                    MaximumTextureSize);
            }

            if (targetWidth == textureWidth && targetHeight == textureHeight)
            {
                return;
            }

            CreateRenderTexture(targetWidth, targetHeight);
        }

        public void Show(
            StarForgeBalance balance,
            int maxLevel,
            int startLevel,
            StarForgePlanetShape shape)
        {
            if (balance == null)
            {
                return;
            }

            EnsureCreated();
            StopTransition();
            gameObject.SetActive(true);
            EnsureWorld(balance, maxLevel);
            currentLevel = Mathf.Clamp(startLevel, 0, visibleMaxLevel);
            SetShape(shape);
            EnsurePlanet(currentLevel);
            ShowOnlyCurrentPlanet();
            ApplyCameraOrbit(currentLevel);
            previewCamera.enabled = true;
        }

        public void SetShape(StarForgePlanetShape shape)
        {
            EnsureCreated();
            bool shapeChanged = currentShape != shape;
            currentShape = shape;
            if (planetViews == null)
            {
                return;
            }

            EnsurePlanet(currentLevel);
            if (shapeChanged && transitionRoutine != null)
            {
                EnsurePlanet(transitionTargetLevel);
            }
        }

        public void ResetCameraOrbit()
        {
            cameraOrbitYaw = 0f;
            cameraOrbitPitch = 0f;
        }

        public void OrbitCurrentCamera(Vector2 dragDelta)
        {
            if (previewCamera == null ||
                cameraPositions == null ||
                currentLevel < 0 ||
                currentLevel >= cameraPositions.Length)
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
            ApplyCameraOrbit(currentLevel);
        }

        public void TransitionTo(
            int targetLevel,
            float duration,
            Action onComplete)
        {
            EnsureCreated();
            if (transitionRoutine != null ||
                cameraPositions == null ||
                targetLevel < 0 ||
                targetLevel >= cameraPositions.Length ||
                targetLevel > visibleMaxLevel)
            {
                return;
            }

            EnsurePlanet(currentLevel);
            EnsurePlanet(targetLevel);
            SetPlanetActive(currentLevel, true);
            SetPlanetActive(targetLevel, true);
            transitionTargetLevel = targetLevel;
            transitionOnComplete = onComplete;
            transitionRoutine = StartCoroutine(
                TransitionRoutine(
                    targetLevel,
                    Mathf.Clamp(duration, 0.35f, MaximumTransitionDuration)));
        }

        public bool CompleteTransitionImmediately()
        {
            if (transitionRoutine == null ||
                cameraPositions == null ||
                transitionTargetLevel < 0 ||
                transitionTargetLevel >= cameraPositions.Length)
            {
                return false;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            currentLevel = transitionTargetLevel;
            ApplyCameraOrbit(currentLevel);
            ShowOnlyCurrentPlanet();

            Action onComplete = transitionOnComplete;
            transitionOnComplete = null;
            onComplete?.Invoke();
            return true;
        }

        public void Hide()
        {
            StopTransition();
            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            gameObject.SetActive(false);
        }

        public static float GetDiameterMultiplier(int lowerLevel, int upperLevel)
        {
            if (lowerLevel == 29 && upperLevel == 30)
            {
                return 100f;
            }

            if (lowerLevel == 28 && upperLevel == 29)
            {
                return 10f;
            }

            if (upperLevel >= 25)
            {
                return 5f;
            }

            if (upperLevel >= 20)
            {
                return 3f;
            }

            if (upperLevel >= 10)
            {
                return 2f;
            }

            return 1.5f;
        }

        public static float GetTransitionDuration(float diameterMultiplier)
        {
            float duration =
                0.65f + Mathf.Log(Mathf.Max(1f, diameterMultiplier), 2f) * 0.66f;
            return Mathf.Clamp(duration, 0.65f, MaximumTransitionDuration);
        }

        public static double GetCumulativeDiameter(int level)
        {
            double diameter = 1d;
            for (int current = 2; current <= level; current++)
            {
                diameter *= GetDiameterMultiplier(current - 1, current);
            }

            return diameter;
        }

        private IEnumerator TransitionRoutine(
            int targetLevel,
            float duration)
        {
            Vector3 sourceTarget = GetCameraOrbitTarget(currentLevel);
            Vector3 targetTarget = GetCameraOrbitTarget(targetLevel);
            float sourceDistance = cameraDistances[currentLevel];
            float targetDistance = cameraDistances[targetLevel];
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float eased =
                    normalized * normalized * (3f - 2f * normalized);
                Vector3 orbitTarget = Vector3.Lerp(
                    sourceTarget,
                    targetTarget,
                    eased);
                orbitTarget.y +=
                    Mathf.Sin(normalized * Mathf.PI * 2f) *
                    Mathf.Min(
                        0.3f,
                        Mathf.Abs(targetTarget.x - sourceTarget.x) *
                        0.004f);
                ApplyCameraOrbit(
                    orbitTarget,
                    Mathf.Lerp(sourceDistance, targetDistance, eased));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            currentLevel = targetLevel;
            ApplyCameraOrbit(currentLevel);
            ShowOnlyCurrentPlanet();
            transitionRoutine = null;
            Action onComplete = transitionOnComplete;
            transitionOnComplete = null;
            onComplete?.Invoke();
        }

        private void ApplyCameraOrbit(int level)
        {
            ApplyCameraOrbit(
                GetCameraOrbitTarget(level),
                cameraDistances[level]);
        }

        private void ApplyCameraOrbit(
            Vector3 orbitTarget,
            float distance)
        {
            Quaternion orbitRotation = Quaternion.Euler(
                cameraOrbitPitch,
                cameraOrbitYaw,
                0f);
            previewCamera.transform.localPosition =
                orbitTarget +
                orbitRotation * Vector3.back * distance;
            previewCamera.transform.localRotation = orbitRotation;
        }

        private Vector3 GetCameraOrbitTarget(int level)
        {
            Vector3 defaultPosition = cameraPositions[level];
            return new Vector3(
                defaultPosition.x,
                defaultPosition.y,
                0f);
        }

        private void EnsureWorld(StarForgeBalance balance, int maxLevel)
        {
            if (worldRoot != null &&
                builtBalance == balance &&
                builtMaxLevel == balance.maxLevel)
            {
                SetVisibleMaxLevel(maxLevel);
                return;
            }

            DestroyWorld();
            builtBalance = balance;
            builtMaxLevel = balance.maxLevel;
            planetAnchors = new GameObject[builtMaxLevel + 1];
            planetViews = new StarForgePlanetView[builtMaxLevel + 1];
            planetShapes = new StarForgePlanetShape[builtMaxLevel + 1];
            planetAppearanceApplied = new bool[builtMaxLevel + 1];
            cameraPositions = new Vector3[builtMaxLevel + 1];
            cameraDistances = new float[builtMaxLevel + 1];

            GameObject worldObject = new GameObject("Collection Planet Line");
            worldObject.transform.SetParent(transform, false);
            worldRoot = worldObject.transform;

            double cumulativeDiameter = 1d;
            float previousX = 0f;
            float previousRadius = 0f;

            for (int level = 0; level <= builtMaxLevel; level++)
            {
                if (level > 1)
                {
                    cumulativeDiameter *= GetDiameterMultiplier(level - 1, level);
                }

                StageVisualConfig stage = balance.GetStage(level);
                float worldScale = Mathf.Pow(
                    Mathf.Max(1f, (float)cumulativeDiameter),
                    WorldScaleExponent);
                float visualRadius = GetVisualRadius(stage, worldScale);
                float x = level == 0
                    ? 0f
                    : previousX +
                      previousRadius +
                      visualRadius +
                      GetPlanetGap(previousRadius, visualRadius);

                GameObject anchorObject = new GameObject(
                    "Collection Planet " + level + " Anchor");
                anchorObject.transform.SetParent(worldRoot, false);
                anchorObject.transform.localPosition = new Vector3(x, 0f, 0f);
                anchorObject.transform.localScale = Vector3.one * worldScale;
                planetAnchors[level] = anchorObject;

                float cameraDistance = GetCameraDistance(visualRadius);
                cameraDistances[level] = cameraDistance;
                cameraPositions[level] =
                    new Vector3(x, 0.05f, -cameraDistance);

                previousX = x;
                previousRadius = visualRadius;
            }

            CreateSpaceEnvironment();
            SetVisibleMaxLevel(maxLevel);
            SetLayerRecursively(worldObject, PreviewLayer);
        }

        private void SetVisibleMaxLevel(int maxLevel)
        {
            visibleMaxLevel = Mathf.Clamp(maxLevel, 0, builtMaxLevel);
            if (planetAnchors == null)
            {
                return;
            }

            for (int level = 0; level < planetAnchors.Length; level++)
            {
                if (planetAnchors[level] != null)
                {
                    if (level > visibleMaxLevel)
                    {
                        planetAnchors[level].SetActive(false);
                    }
                }
            }
        }

        private void EnsurePlanet(int level)
        {
            if (builtBalance == null ||
                planetAnchors == null ||
                planetViews == null ||
                level < 0 ||
                level >= planetViews.Length ||
                planetAnchors[level] == null)
            {
                return;
            }

            if (planetViews[level] == null)
            {
                GameObject planetObject = new GameObject(
                    "Collection Planet " + level);
                planetObject.transform.SetParent(
                    planetAnchors[level].transform,
                    false);
                StarForgePlanetView planet =
                    planetObject.AddComponent<StarForgePlanetView>();
                planetViews[level] = planet;
                planet.SetDecorScaleMultiplier(1f);
            }

            if (!planetAppearanceApplied[level] ||
                planetShapes[level] != currentShape)
            {
                planetViews[level].ApplyPreviewAppearance(
                    currentShape,
                    builtBalance.GetStage(level));
                planetShapes[level] = currentShape;
                planetAppearanceApplied[level] = true;
            }

            SetLayerRecursively(
                planetViews[level].gameObject,
                PreviewLayer);
        }

        private void SetPlanetActive(int level, bool active)
        {
            if (planetAnchors == null ||
                level < 0 ||
                level >= planetAnchors.Length ||
                planetAnchors[level] == null)
            {
                return;
            }

            planetAnchors[level].SetActive(
                active && level <= visibleMaxLevel);
        }

        private void ShowOnlyCurrentPlanet()
        {
            if (planetAnchors == null)
            {
                return;
            }

            for (int level = 0; level < planetAnchors.Length; level++)
            {
                SetPlanetActive(level, level == currentLevel);
            }
        }

        private void DestroyWorld()
        {
            if (worldRoot != null)
            {
                worldRoot.gameObject.SetActive(false);
                Destroy(worldRoot.gameObject);
                worldRoot = null;
            }

            planetAnchors = null;
            planetViews = null;
            planetShapes = null;
            planetAppearanceApplied = null;
            cameraPositions = null;
            cameraDistances = null;
            starfield = null;
            nebulaField = null;

            DestroyMaterial(ref starfieldMaterial);
            DestroyMaterial(ref nebulaMaterial);
        }

        private void CreateSpaceEnvironment()
        {
            GameObject starObject = new GameObject("Collection Deep Starfield");
            starObject.transform.SetParent(worldRoot, false);
            starfield = starObject.AddComponent<ParticleSystem>();
            ConfigureStaticParticles(
                starfield,
                1800,
                new Color(0.72f, 0.86f, 1f, 0.95f),
                0.018f,
                0.09f,
                true);
            EmitStarfield();

            GameObject nebulaObject = new GameObject("Collection Nebula Energy");
            nebulaObject.transform.SetParent(worldRoot, false);
            nebulaField = nebulaObject.AddComponent<ParticleSystem>();
            ConfigureStaticParticles(
                nebulaField,
                260,
                new Color(0.28f, 0.34f, 1f, 0.1f),
                6f,
                42f,
                true);
            EmitNebula();
        }

        private void ConfigureStaticParticles(
            ParticleSystem particles,
            int maxParticles,
            Color tint,
            float minimumSize,
            float maximumSize,
            bool additive)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 10000f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(
                minimumSize,
                maximumSize);
            main.maxParticles = maxParticles;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material material = StarForgeVisualLibrary.CreateParticleMaterial(
                tint,
                additive);
            renderer.material = material;

            if (particles == starfield)
            {
                starfieldMaterial = material;
            }
            else
            {
                nebulaMaterial = material;
            }
        }

        private void EmitStarfield()
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(73129);

            for (int level = 0; level <= builtMaxLevel; level++)
            {
                float distance = cameraDistances[level];
                float horizontalRange = Mathf.Max(16f, distance * 0.48f);
                float verticalRange = Mathf.Max(28f, distance * 0.4f);
                float depthRange = Mathf.Max(180f, distance * 0.55f);
                float sizeScale = Mathf.Max(1f, distance * 0.012f);

                for (int i = 0; i < 52; i++)
                {
                    ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                    emit.position = new Vector3(
                        cameraPositions[level].x +
                        UnityEngine.Random.Range(-horizontalRange, horizontalRange),
                        UnityEngine.Random.Range(-verticalRange, verticalRange),
                        UnityEngine.Random.Range(8f, depthRange));
                    emit.startSize =
                        UnityEngine.Random.Range(0.018f, 0.09f) * sizeScale;
                    emit.startColor = Color.Lerp(
                        new Color(0.4f, 0.62f, 1f, 0.55f),
                        Color.white,
                        UnityEngine.Random.value);
                    emit.startLifetime = 10000f;
                    starfield.Emit(emit, 1);
                }
            }

            UnityEngine.Random.state = previousState;
        }

        private void EmitNebula()
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(41357);

            for (int level = 0; level <= builtMaxLevel; level++)
            {
                float distance = cameraDistances[level];
                float horizontalRange = Mathf.Max(12f, distance * 0.42f);
                float verticalRange = Mathf.Max(20f, distance * 0.32f);
                float depthRange = Mathf.Max(150f, distance * 0.45f);
                float sizeScale = Mathf.Max(1f, distance * 0.002f);

                for (int i = 0; i < 7; i++)
                {
                    float hue = UnityEngine.Random.value;
                    Color color = hue < 0.34f
                        ? new Color(0.18f, 0.42f, 1f, 0.045f)
                        : hue < 0.68f
                            ? new Color(0.55f, 0.2f, 1f, 0.04f)
                            : new Color(0.1f, 0.85f, 0.88f, 0.035f);
                    ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                    emit.position = new Vector3(
                        cameraPositions[level].x +
                        UnityEngine.Random.Range(-horizontalRange, horizontalRange),
                        UnityEngine.Random.Range(-verticalRange, verticalRange),
                        UnityEngine.Random.Range(25f, depthRange));
                    emit.startSize =
                        UnityEngine.Random.Range(8f, 42f) * sizeScale;
                    emit.startColor = color;
                    emit.startLifetime = 10000f;
                    nebulaField.Emit(emit, 1);
                }
            }

            UnityEngine.Random.state = previousState;
        }

        private void StopTransition()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            transitionOnComplete = null;
        }

        private void OnDestroy()
        {
            DestroyWorld();

            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            Destroy(renderTexture);
        }

        private void EnsureCreated()
        {
            if (previewCamera != null)
            {
                return;
            }

            transform.position = new Vector3(1000f, 0f, 0f);

            CreateRenderTexture(BaseTextureSize, 1280);

            GameObject cameraObject = new GameObject("Collection Space Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localRotation = Quaternion.identity;
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.0015f, 0.003f, 0.012f, 1f);
            previewCamera.fieldOfView = 42f;
            previewCamera.nearClipPlane = 0.05f;
            previewCamera.farClipPlane = 10000000f;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.targetTexture = renderTexture;
            previewCamera.allowHDR = true;
            previewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            previewCamera.enabled = false;

            GameObject lightObject = new GameObject("Collection Space Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(32f, -28f, 0f);
            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.35f;
            keyLight.cullingMask = 1 << PreviewLayer;

            SetLayerRecursively(gameObject, PreviewLayer);
        }

        private void CreateRenderTexture(int width, int height)
        {
            bool cameraWasEnabled = previewCamera != null && previewCamera.enabled;
            if (previewCamera != null)
            {
                previewCamera.targetTexture = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            textureWidth = width;
            textureHeight = height;
            renderTexture = new RenderTexture(
                textureWidth,
                textureHeight,
                24,
                RenderTextureFormat.ARGB32);
            renderTexture.name =
                "StarForge Collection Space " + textureWidth + "x" + textureHeight;
            renderTexture.antiAliasing = 2;
            renderTexture.Create();

            if (previewCamera != null)
            {
                previewCamera.targetTexture = renderTexture;
                previewCamera.enabled = cameraWasEnabled;
            }
        }

        private static float GetVisualRadius(
            StageVisualConfig stage,
            float worldScale)
        {
            float decorExtent = stage.level >= 28
                ? 2.8f
                : stage.level >= 25
                    ? 2.25f
                    : stage.level >= 20
                        ? 1.9f
                        : stage.level >= 16
                            ? 1.65f
                            : 1.15f;
            return Mathf.Max(0.5f, stage.scale * worldScale * decorExtent);
        }

        private static float GetCameraDistance(float visualRadius)
        {
            float halfFieldOfView = 42f * 0.5f * Mathf.Deg2Rad;
            return Mathf.Max(
                2.2f,
                visualRadius / Mathf.Tan(halfFieldOfView) * 1.08f);
        }

        private static float GetPlanetGap(float previousRadius, float currentRadius)
        {
            return Mathf.Clamp(
                Mathf.Sqrt(Mathf.Max(0.1f, previousRadius * currentRadius)) * 0.85f,
                3.5f,
                120f);
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            Destroy(material);
            material = null;
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
    }
}
