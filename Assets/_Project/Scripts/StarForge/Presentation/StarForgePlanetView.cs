using System.Collections;
using StarForge.Data;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgePlanetView : MonoBehaviour
    {
        [SerializeField] private Transform planetRoot;
        [SerializeField] private Renderer planetRenderer;
        [SerializeField] private Light planetLight;

        private Material runtimeMaterial;
        private float rotationSpeed;
        private int appliedLevel = int.MinValue;

        private Transform decorRoot;
        private Material haloMaterial;
        private StarForgeBillboard haloBillboard;
        private GameObject ringObject;
        private Material ringMaterial;
        private GameObject diskObject;
        private Material diskMaterial;
        private StarForgeOrbitRotator diskRotator;
        private ParticleSystem ambientDust;
        private ParticleSystem starFlares;

        private Color emissionBaseColor = Color.black;
        private Color haloBaseColor = Color.clear;
        private float pulseStrength = 0.05f;
        private float pulseSpeed = 1.4f;
        private float chargeBoost;
        private float decorScaleMultiplier = 1f;
        private Coroutine chargeRoutine;
        private Coroutine transitionRoutine;

        private void Awake()
        {
            EnsureCreated();
        }

        private void Update()
        {
            if (planetRoot != null)
            {
                planetRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                planetRoot.Rotate(Vector3.right, rotationSpeed * 0.18f * Time.deltaTime, Space.Self);
            }

            float pulse = 1f + pulseStrength * Mathf.Sin(Time.time * pulseSpeed);
            float boost = 1f + chargeBoost * 1.4f;

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetColor("_EmissionColor", emissionBaseColor * pulse * boost);
            }

            if (haloMaterial != null)
            {
                Color haloColor = haloBaseColor;
                haloColor.a = Mathf.Clamp01(haloBaseColor.a * pulse * (1f + chargeBoost * 0.9f));
                StarForgeVisualLibrary.SetMaterialColor(haloMaterial, haloColor);
            }
        }

        public Transform Target
        {
            get
            {
                EnsureCreated();
                return planetRoot;
            }
        }

        public void PlayChargePulse(float duration)
        {
            EnsureCreated();

            if (chargeRoutine != null)
            {
                StopCoroutine(chargeRoutine);
            }

            chargeRoutine = StartCoroutine(ChargeRoutine(duration));
        }

        public void SetDecorScaleMultiplier(float multiplier)
        {
            decorScaleMultiplier = Mathf.Max(0.1f, multiplier);
            if (planetRoot != null && decorRoot != null)
            {
                decorRoot.localScale = Vector3.one * planetRoot.localScale.x * decorScaleMultiplier;
            }
        }

        public void ApplyStage(StageVisualConfig stage)
        {
            EnsureCreated();

            if (stage == null || stage.level == appliedLevel)
            {
                return;
            }

            bool firstApply = appliedLevel == int.MinValue;
            appliedLevel = stage.level;

            Color baseColor = ParseColor(stage.color, new Color(0.45f, 0.62f, 0.9f));
            rotationSpeed = Mathf.Max(1f, stage.rotationSpeed);

            StarForgePlanetSurface surface = StarForgePlanetTextureFactory.Get(stage.level, baseColor);
            ApplySurface(surface, stage, baseColor);
            ApplyDecor(surface.theme, stage, baseColor);
            ApplyLight(surface.theme, stage, baseColor);

            float targetScale = Mathf.Max(0.2f, stage.scale);
            if (firstApply)
            {
                SetScale(targetScale);
            }
            else
            {
                if (transitionRoutine != null)
                {
                    StopCoroutine(transitionRoutine);
                }

                transitionRoutine = StartCoroutine(ScaleTransition(targetScale));
            }
        }

        private void ApplySurface(StarForgePlanetSurface surface, StageVisualConfig stage, Color baseColor)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            runtimeMaterial.SetTexture("_BaseMap", surface.baseMap);
            if (runtimeMaterial.HasProperty("_MainTex"))
            {
                runtimeMaterial.SetTexture("_MainTex", surface.baseMap);
            }

            StarForgeVisualLibrary.SetMaterialColor(runtimeMaterial, Color.white);

            float smoothness;
            switch (surface.theme)
            {
                case StarForgePlanetTheme.Ice: smoothness = 0.55f; break;
                case StarForgePlanetTheme.Ocean: smoothness = 0.6f; break;
                case StarForgePlanetTheme.Life: smoothness = 0.4f; break;
                case StarForgePlanetTheme.Gas: smoothness = 0.35f; break;
                case StarForgePlanetTheme.Lava: smoothness = 0.3f; break;
                case StarForgePlanetTheme.Star: smoothness = 0.1f; break;
                default: smoothness = 0.22f; break;
            }

            if (runtimeMaterial.HasProperty("_Smoothness"))
            {
                runtimeMaterial.SetFloat("_Smoothness", smoothness);
            }

            if (runtimeMaterial.HasProperty("_Metallic"))
            {
                runtimeMaterial.SetFloat("_Metallic", 0f);
            }

            runtimeMaterial.EnableKeyword("_EMISSION");

            if (surface.emissionMap != null)
            {
                runtimeMaterial.SetTexture("_EmissionMap", surface.emissionMap);

                switch (surface.theme)
                {
                    case StarForgePlanetTheme.Star:
                        emissionBaseColor = Color.white * (1.1f + stage.emission);
                        break;
                    case StarForgePlanetTheme.Lava:
                        emissionBaseColor = Color.white * 2f;
                        break;
                    default:
                        emissionBaseColor = Color.white * 1.3f;
                        break;
                }
            }
            else
            {
                runtimeMaterial.SetTexture("_EmissionMap", null);
                emissionBaseColor = baseColor * (stage.emission * 0.55f);
            }

            switch (surface.theme)
            {
                case StarForgePlanetTheme.Star:
                    pulseStrength = 0.18f;
                    pulseSpeed = 2.4f;
                    break;
                case StarForgePlanetTheme.Lava:
                    pulseStrength = 0.1f;
                    pulseSpeed = 1.6f;
                    break;
                case StarForgePlanetTheme.BlackHole:
                    pulseStrength = 0.25f;
                    pulseSpeed = 3f;
                    break;
                default:
                    pulseStrength = 0.05f;
                    pulseSpeed = 1.4f;
                    break;
            }
        }

        private void ApplyDecor(StarForgePlanetTheme theme, StageVisualConfig stage, Color baseColor)
        {
            bool isStar = theme == StarForgePlanetTheme.Star;
            bool isBlackHole = theme == StarForgePlanetTheme.BlackHole;
            bool showRing = theme == StarForgePlanetTheme.Gas && stage.level >= 14;

            haloBaseColor = isBlackHole
                ? new Color(0.5f, 0.32f, 1f, 0.55f)
                : new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp(0.16f + stage.emission * 0.15f, 0.16f, 0.6f));
            haloBillboard.depthOffset = 0.3f * stage.scale;

            ringObject.SetActive(showRing);
            if (showRing)
            {
                Color ringColor = new Color(
                    Mathf.Clamp01(baseColor.r * 1.2f + 0.1f),
                    Mathf.Clamp01(baseColor.g * 1.2f + 0.1f),
                    Mathf.Clamp01(baseColor.b * 1.2f + 0.1f),
                    0.55f);
                StarForgeVisualLibrary.SetMaterialColor(ringMaterial, ringColor);
            }

            diskObject.SetActive(isBlackHole);
            if (isBlackHole)
            {
                StarForgeVisualLibrary.SetMaterialColor(diskMaterial, new Color(1f, 0.55f, 0.16f, 0.95f));
                diskRotator.degreesPerSecond = new Vector3(0f, 40f + stage.level * 4f, 0f);
            }

            ParticleSystem.MainModule flareMain = starFlares.main;
            if (isStar)
            {
                flareMain.startColor = Color.Lerp(baseColor, Color.white, 0.35f);
                if (!starFlares.isPlaying)
                {
                    starFlares.Play();
                }
            }
            else
            {
                starFlares.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystem.MainModule dustMain = ambientDust.main;
            dustMain.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            ParticleSystem.EmissionModule dustEmission = ambientDust.emission;
            dustEmission.rateOverTime = 8f + stage.level * 0.5f;
            if (!ambientDust.isPlaying)
            {
                ambientDust.Play();
            }
        }

        private void ApplyLight(StarForgePlanetTheme theme, StageVisualConfig stage, Color baseColor)
        {
            if (planetLight == null)
            {
                return;
            }

            planetLight.color = theme == StarForgePlanetTheme.BlackHole
                ? new Color(1f, 0.6f, 0.25f)
                : baseColor;
            planetLight.intensity = 1.2f + stage.emission * 1.8f;
            planetLight.range = 4f + stage.scale * 2.5f;
        }

        private IEnumerator ScaleTransition(float targetScale)
        {
            float fromScale = planetRoot.localScale.x;
            bool growing = targetScale > fromScale;
            const float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float pop = growing ? 1f + 0.1f * Mathf.Sin(t * Mathf.PI) : 1f;
                SetScale(Mathf.Lerp(fromScale, targetScale, t) * pop);
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScale(targetScale);
            transitionRoutine = null;
        }

        private IEnumerator ChargeRoutine(float duration)
        {
            float ramp = Mathf.Max(0.05f, duration);
            float elapsed = 0f;

            while (elapsed < ramp)
            {
                chargeBoost = elapsed / ramp;
                elapsed += Time.deltaTime;
                yield return null;
            }

            chargeBoost = 1f;

            const float decay = 0.3f;
            elapsed = 0f;
            while (elapsed < decay)
            {
                chargeBoost = 1f - elapsed / decay;
                elapsed += Time.deltaTime;
                yield return null;
            }

            chargeBoost = 0f;
            chargeRoutine = null;
        }

        private void SetScale(float scale)
        {
            planetRoot.localScale = Vector3.one * scale;
            if (decorRoot != null)
            {
                decorRoot.localScale = Vector3.one * scale * decorScaleMultiplier;
            }
        }

        private void EnsureCreated()
        {
            if (planetRoot == null)
            {
                GameObject root = new GameObject("Star Body");
                root.transform.SetParent(transform, false);
                planetRoot = root.transform;
            }

            if (planetRenderer == null)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Runtime Planet Sphere";
                sphere.transform.SetParent(planetRoot, false);
                planetRenderer = sphere.GetComponent<Renderer>();
            }

            if (runtimeMaterial == null && planetRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                runtimeMaterial = new Material(shader);
                runtimeMaterial.name = "StarForge Runtime Planet";
                planetRenderer.sharedMaterial = runtimeMaterial;
            }

            if (planetLight == null)
            {
                GameObject lightObject = new GameObject("Planet Glow Light");
                lightObject.transform.SetParent(planetRoot, false);
                lightObject.transform.localPosition = new Vector3(0f, 0f, -1.5f);
                planetLight = lightObject.AddComponent<Light>();
                planetLight.type = LightType.Point;
            }

            if (decorRoot == null)
            {
                GameObject decorObject = new GameObject("Planet Decor");
                decorObject.transform.SetParent(transform, false);
                decorRoot = decorObject.transform;

                CreateHalo();
                CreateRing();
                CreateDisk();
                CreateAmbientDust();
                CreateStarFlares();
            }
        }

        private void CreateHalo()
        {
            GameObject haloObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            haloObject.name = "Planet Halo";
            Destroy(haloObject.GetComponent<Collider>());
            haloObject.transform.SetParent(decorRoot, false);
            haloObject.transform.localScale = Vector3.one * 2.3f;

            MeshRenderer haloRenderer = haloObject.GetComponent<MeshRenderer>();
            haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear, true, StarForgeVisualLibrary.SoftCircleTexture);
            haloRenderer.sharedMaterial = haloMaterial;

            haloBillboard = haloObject.AddComponent<StarForgeBillboard>();
        }

        private void CreateRing()
        {
            ringObject = new GameObject("Planet Ring");
            ringObject.transform.SetParent(decorRoot, false);
            ringObject.transform.localRotation = Quaternion.Euler(62f, 0f, 16f);

            MeshFilter filter = ringObject.AddComponent<MeshFilter>();
            filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(0.72f, 1.42f, 96, 6f);

            MeshRenderer renderer = ringObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ringMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, false, StarForgeVisualLibrary.PlanetRingTexture);
            if (ringMaterial.HasProperty("_Cull"))
            {
                ringMaterial.SetInt("_Cull", 0);
            }

            renderer.sharedMaterial = ringMaterial;
            ringObject.SetActive(false);
        }

        private void CreateDisk()
        {
            diskObject = new GameObject("Accretion Disk");
            diskObject.transform.SetParent(decorRoot, false);
            diskObject.transform.localRotation = Quaternion.Euler(64f, 0f, 8f);

            MeshFilter filter = diskObject.AddComponent<MeshFilter>();
            filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(0.56f, 1.5f, 96, 6f);

            MeshRenderer renderer = diskObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            diskMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, true, StarForgeVisualLibrary.PlanetRingTexture);
            if (diskMaterial.HasProperty("_Cull"))
            {
                diskMaterial.SetInt("_Cull", 0);
            }

            renderer.sharedMaterial = diskMaterial;

            diskRotator = diskObject.AddComponent<StarForgeOrbitRotator>();
            diskObject.SetActive(false);
        }

        private void CreateAmbientDust()
        {
            GameObject dustObject = new GameObject("Ambient Dust");
            dustObject.transform.SetParent(decorRoot, false);
            ambientDust = dustObject.AddComponent<ParticleSystem>();
            ambientDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ambientDust.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = ambientDust.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = ambientDust.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 1.25f;
            shape.donutRadius = 0.12f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ambientDust.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.55f);

            ParticleSystem.ColorOverLifetimeModule color = ambientDust.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = ambientDust.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void CreateStarFlares()
        {
            GameObject flareObject = new GameObject("Star Flares");
            flareObject.transform.SetParent(decorRoot, false);
            starFlares = flareObject.AddComponent<ParticleSystem>();
            starFlares.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = starFlares.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 100;

            ParticleSystem.EmissionModule emission = starFlares.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = starFlares.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.56f;
            shape.radiusThickness = 0f;

            ParticleSystem.SizeOverLifetimeModule size = starFlares.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            ParticleSystem.ColorOverLifetimeModule color = starFlares.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.5f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = starFlares.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private static Color ParseColor(string html, Color fallback)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(html, out color) ? color : fallback;
        }
    }
}
