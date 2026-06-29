using System;
using System.Collections;
using StarForge.Core;
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
        private Material blackHoleCoreMaterial;
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
        private GameObject coronaObject;
        private Material coronaMaterial;
        private StarForgeBillboard coronaBillboard;
        private ParticleSystem sparkles;
        private GameObject pulsarJets;
        private StarForgeOrbitRotator pulsarRotator;
        private Material pulsarBeamMaterial;
        private GameObject magnetarArcs;
        private Material magnetarMaterial;
        private ParticleSystem novaShockwave;
        private GameObject photonRingObject;
        private Material photonRingMaterial;
        private StarForgeBillboard photonRingBillboard;
        private GameObject giantCoronaLayers;
        private readonly Material[] giantCoronaMaterials = new Material[3];
        private GameObject diffractionSpikes;
        private Material diffractionMaterial;
        private GameObject neutronEquator;
        private Material neutronEquatorMaterial;
        private StarForgeOrbitRotator neutronEquatorRotator;
        private ParticleSystem magnetarSparks;
        private GameObject novaNebula;
        private readonly Material[] novaNebulaMaterials = new Material[3];
        private GameObject quarkGrid;
        private Material quarkGridMaterial;
        private ParticleSystem quarkCompression;
        private ParticleSystem gravityInfall;
        private GameObject blackHoleLightInfall;
        private readonly GameObject[] blackHoleLightInfallLayers = new GameObject[5];
        private readonly Material[] blackHoleLightInfallMaterials = new Material[5];
        private GameObject blackHoleDopplerGlow;
        private Material dopplerBrightMaterial;
        private Material dopplerDimMaterial;
        private GameObject singularityLensing;
        private readonly Material[] singularityLensMaterials = new Material[3];
        private ParticleSystem stageArrivalBurst;

        private StarForgePlanetShape currentShape = StarForgePlanetShape.Default;
        private MeshFilter planetMeshFilter;
        private Mesh defaultPlanetMesh;
        private float trembleAmount;
        private Vector3 planetBasePosition;
        private Color lastBaseColor = Color.white;
        private StageVisualConfig lastStage;
        private bool surfaceImageBased;
        private bool surfaceHasBakedFace;
        private GameObject blackHoleFaceObject;
        private Material blackHoleFaceMaterial;
        private StarForgeBillboard blackHoleFaceBillboard;
        private bool blackHoleFaceActive;
        private ParticleSystem pulsarPulseRings;
        private GameObject premiumRimObject;
        private MeshFilter premiumRimMeshFilter;
        private Material premiumRimMaterial;
        private Color premiumRimBaseColor = Color.clear;
        private Color premiumRimSecondaryColor = Color.clear;
        private readonly GameObject[] premiumOrbitBands = new GameObject[3];
        private readonly Material[] premiumOrbitBandMaterials = new Material[3];
        private readonly Color[] premiumOrbitBandBaseColors = new Color[3];
        private readonly GameObject[] premiumEnergyArcs = new GameObject[4];
        private readonly Material[] premiumEnergyArcMaterials = new Material[4];
        private readonly Color[] premiumEnergyArcBaseColors = new Color[4];
        private readonly ParticleSystem[] premiumOrbitAuras = new ParticleSystem[3];
        private ParticleSystem premiumOutflow;
        private ParticleSystem premiumWisps;
        private ParticleSystem premiumAuraCloud;
        private ParticleSystem premiumStardust;
        private ParticleSystem premiumPulseRings;
        private float premiumRimBaseScale = 1.025f;

        private Color emissionBaseColor = Color.black;
        private Color haloBaseColor = Color.clear;
        private Color coronaBaseColor = Color.clear;
        private Color diffractionBaseColor = Color.clear;
        private Color neutronEquatorBaseColor = Color.clear;
        private Color pulsarBeamBaseColor = Color.clear;
        private Color magnetarBaseColor = Color.clear;
        private Color quarkGridBaseColor = Color.clear;
        private float planetLightBaseIntensity;
        private float pulseStrength = 0.05f;
        private float pulseSpeed = 1.4f;
        private float chargeBoost;
        private float decorScaleMultiplier = 1f;
        private Quaternion automaticRotation = Quaternion.identity;
        private Coroutine chargeRoutine;
        private Coroutine transitionRoutine;
        private GameObject externalBlackHoleObject;
        private bool externalBlackHoleActive;

        private void Awake()
        {
            EnsureCreated();
        }

        private void Update()
        {
            if (planetRoot != null)
            {
                if (blackHoleFaceActive || externalBlackHoleActive)
                {
                    planetRoot.localRotation = Quaternion.identity;
                }
                else if (currentShape == StarForgePlanetShape.Default)
                {
                    automaticRotation =
                        Quaternion.AngleAxis(
                            rotationSpeed * Time.deltaTime,
                            Vector3.up) *
                        automaticRotation;
                    automaticRotation *= Quaternion.AngleAxis(
                        rotationSpeed * 0.18f * Time.deltaTime,
                        Vector3.right);
                    planetRoot.localRotation = automaticRotation;
                }
                else
                {
                    // 하트/고양이: 얼굴이 보이도록 완전 회전 대신 부드러운 흔들림
                    float wobbleTime = Time.time * (0.35f + rotationSpeed * 0.012f);
                    planetRoot.localRotation =
                        Quaternion.Euler(
                            Mathf.Sin(wobbleTime * 0.8f) * 6f,
                            Mathf.Sin(wobbleTime) * 17f,
                            Mathf.Sin(wobbleTime * 0.6f + 1.3f) * 4f);
                }

                // 충전 중 부들부들 떨림
                float shake = Mathf.Max(trembleAmount, chargeBoost * chargeBoost);
                if (shake > 0.001f)
                {
                    float scaleFactor = planetRoot.localScale.x * 0.045f * shake;
                    float t = Time.time;
                    Vector3 jitter = new Vector3(
                        (Mathf.PerlinNoise(t * 23f, 0.31f) - 0.5f) * 2f,
                        (Mathf.PerlinNoise(0.73f, t * 27f) - 0.5f) * 2f,
                        0f) * scaleFactor;
                    planetRoot.localPosition = planetBasePosition + jitter;
                }
                else if (planetRoot.localPosition != planetBasePosition)
                {
                    planetRoot.localPosition = planetBasePosition;
                }
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

            if (coronaMaterial != null && coronaObject != null && coronaObject.activeSelf)
            {
                // 코로나는 본체보다 느리고 어긋난 위상으로 숨쉬듯 일렁임
                float coronaPulse = 1f + pulseStrength * 0.7f * Mathf.Sin(Time.time * pulseSpeed * 0.6f + 1.7f);
                Color coronaColor = coronaBaseColor;
                coronaColor.a = Mathf.Clamp01(coronaBaseColor.a * coronaPulse * (1f + chargeBoost * 0.6f));
                StarForgeVisualLibrary.SetMaterialColor(coronaMaterial, coronaColor);
            }

            UpdateHighTierDecor(pulse);
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

        public void StopChargePulse()
        {
            if (chargeRoutine != null)
            {
                StopCoroutine(chargeRoutine);
                chargeRoutine = null;
            }

            chargeBoost = 0f;
            trembleAmount = 0f;
            if (planetRoot != null)
            {
                planetRoot.localPosition = planetBasePosition;
            }
        }

        public void SetDecorScaleMultiplier(float multiplier)
        {
            decorScaleMultiplier = Mathf.Max(0.1f, multiplier);
            if (planetRoot != null && decorRoot != null)
            {
                decorRoot.localScale = Vector3.one * planetRoot.localScale.x * decorScaleMultiplier;
            }
        }

        /// <summary>충전 외 연출용 떨림 강도(0~1)를 직접 지정합니다.</summary>
        public void SetTremble(float amount)
        {
            trembleAmount = Mathf.Clamp01(amount);
        }

        /// <summary>행성 모양(기본/하트/고양이)을 적용합니다.</summary>
        public void SetShape(StarForgePlanetShape shape)
        {
            EnsureCreated();

            if (currentShape == shape)
            {
                return;
            }

            currentShape = shape;
            RefreshShapeGeometry();

            // 모양이 바뀌면 해당 모양의 텍스처로 표면을 다시 입힘
            if (lastStage != null)
            {
                appliedLevel = int.MinValue;
                ApplyStageInternal(lastStage, false, false);
            }

            EnforceShapePresentation();
        }

        public void ApplyPreviewAppearance(
            StarForgePlanetShape shape,
            StageVisualConfig stage)
        {
            EnsureCreated();

            currentShape = shape;
            appliedLevel = int.MinValue;
            RefreshShapeGeometry();
            ApplyStageInternal(stage, false, false);
            EnforceShapePresentation();
        }

        private void EnforceShapePresentation()
        {
            blackHoleFaceActive = false;
            if (blackHoleFaceObject != null)
            {
                blackHoleFaceObject.SetActive(false);
            }

            if (currentShape == StarForgePlanetShape.Cat && premiumRimObject != null)
            {
                premiumRimObject.SetActive(false);
            }
        }

        /// <summary>성공 시 성장 연출: 1.3^단계상승 만큼 부풀었다가 새 단계 크기로 정착합니다.</summary>
        public IEnumerator PlaySuccessGrowth(StageVisualConfig stage, int levelGain)
        {
            EnsureCreated();

            if (stage == null)
            {
                yield break;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            // 결과가 나온 순간 충전 떨림을 즉시 종료
            if (chargeRoutine != null)
            {
                StopCoroutine(chargeRoutine);
                chargeRoutine = null;
            }

            chargeBoost = 0f;
            trembleAmount = 0f;

            float fromScale = Mathf.Max(0.2f, planetRoot.localScale.x);
            float burstScale = fromScale * Mathf.Pow(1.3f, Mathf.Max(1, levelGain));
            float targetScale = Mathf.Max(0.2f, stage.scale);

            // 새 단계 비주얼을 입히되 스케일은 직접 연출
            ApplyStageInternal(stage, false, true);
            SetScale(fromScale);

            float growDuration = 0.38f + 0.05f * Mathf.Max(1, levelGain);
            float elapsed = 0f;
            while (elapsed < growDuration)
            {
                float normalized = elapsed / growDuration;
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                SetScale(Mathf.Lerp(fromScale, burstScale, eased));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScale(burstScale);
            yield return new WaitForSeconds(0.12f);

            const float settleDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                float normalized = Mathf.SmoothStep(0f, 1f, elapsed / settleDuration);
                SetScale(Mathf.Lerp(burstScale, targetScale, normalized));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScale(targetScale);
        }

        public void ApplyStage(StageVisualConfig stage)
        {
            ApplyStageInternal(stage, true, true);
        }

        public void ApplyStageImmediate(StageVisualConfig stage)
        {
            ApplyStageInternal(stage, false, false);
        }

        public void ApplyBlackHoleStage(int blackHoleLevel)
        {
            int clampedLevel = Mathf.Clamp(
                blackHoleLevel,
                StarForgeBlackHoleRules.MinLevel,
                StarForgeBlackHoleRules.MaxLevel);
            StageVisualConfig stage = new StageVisualConfig
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

            ApplyStageInternal(stage, true, true);
            EnsureExternalBlackHole();
            SetExternalBlackHoleActive(externalBlackHoleObject != null);
        }

        public IEnumerator PlayHighTierSuccessTransition(
            StageVisualConfig stage,
            float duration,
            Action onExplosion)
        {
            EnsureCreated();

            if (stage == null)
            {
                yield break;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (chargeRoutine != null)
            {
                StopCoroutine(chargeRoutine);
                chargeRoutine = null;
            }

            chargeBoost = 1f;

            float totalDuration = Mathf.Max(0.2f, duration);
            float compressionDuration = totalDuration * 0.7f;
            float expansionDuration = totalDuration - compressionDuration;
            float sourceScale = Mathf.Max(0.2f, planetRoot.localScale.x);
            float compressedScale = Mathf.Max(0.08f, sourceScale * 0.12f);
            float elapsed = 0f;

            while (elapsed < compressionDuration)
            {
                float normalized = elapsed / compressionDuration;
                float eased = normalized * normalized * normalized;
                float jitter = 1f + Mathf.Sin(normalized * Mathf.PI * 12f) * normalized * 0.018f;
                SetScale(Mathf.Lerp(sourceScale, compressedScale, eased) * jitter);
                chargeBoost = Mathf.Max(chargeBoost, normalized * 0.85f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScale(compressedScale);
            ApplyStageInternal(stage, false, true);

            float targetScale = Mathf.Max(0.2f, stage.scale);
            SetScale(targetScale * 0.1f);
            onExplosion?.Invoke();

            elapsed = 0f;
            while (elapsed < expansionDuration)
            {
                float normalized = elapsed / expansionDuration;
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                float overshoot = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.16f;
                SetScale(Mathf.Lerp(targetScale * 0.1f, targetScale, eased) * overshoot);
                chargeBoost = Mathf.Lerp(1f, 0f, normalized);

                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScale(targetScale);
            chargeBoost = 0f;
        }

        private void ApplyStageInternal(
            StageVisualConfig stage,
            bool animateScale,
            bool playArrival)
        {
            EnsureCreated();

            if (stage == null)
            {
                return;
            }

            SetExternalBlackHoleActive(false);

            if (stage.level == appliedLevel)
            {
                return;
            }

            bool firstApply = appliedLevel == int.MinValue;
            int previousLevel = appliedLevel;
            appliedLevel = stage.level;
            RefreshShapeGeometry();

            Color baseColor = ParseColor(stage.color, new Color(0.45f, 0.62f, 0.9f));
            lastBaseColor = baseColor;
            rotationSpeed = Mathf.Max(1f, stage.rotationSpeed);

            StarForgePlanetSurface surface = StarForgePlanetTextureFactory.Get(stage.level, baseColor, currentShape);
            ApplySurface(surface, stage, baseColor);
            ApplyDecor(surface.theme, stage, baseColor);
            ApplyLight(surface.theme, stage, baseColor);
            lastStage = stage;
            EnforceShapePresentation();

            float targetScale = Mathf.Max(0.2f, stage.scale);
            if (firstApply || !animateScale)
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

            if (playArrival && !firstApply && stage.level > previousLevel)
            {
                PlayStageArrival(stage.level, baseColor);
            }
        }

        private void ApplySurface(StarForgePlanetSurface surface, StageVisualConfig stage, Color baseColor)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            bool isBlackHole = surface.theme == StarForgePlanetTheme.BlackHole;
            Material bodyMaterial = isBlackHole ? blackHoleCoreMaterial : runtimeMaterial;
            planetRenderer.sharedMaterial = bodyMaterial;

            if (isBlackHole)
            {
                Color rimColor = stage.level >= 30
                    ? new Color(0.46f, 0.3f, 0.92f, 1f)
                    : stage.level >= 29
                        ? new Color(0.58f, 0.24f, 0.96f, 1f)
                        : new Color(0.4f, 0.16f, 0.84f, 1f);
                if (blackHoleCoreMaterial.HasProperty("_CoreColor"))
                {
                    blackHoleCoreMaterial.SetColor("_CoreColor", new Color(0.0005f, 0.0005f, 0.002f, 1f));
                    blackHoleCoreMaterial.SetColor("_RimColor", rimColor);
                    blackHoleCoreMaterial.SetFloat("_RimPower", stage.level >= 30 ? 7f : 5.5f);
                    blackHoleCoreMaterial.SetFloat("_RimStrength", stage.level >= 30 ? 0.08f : 0.12f);
                }

                surfaceImageBased = false;
                surfaceHasBakedFace = false;
                emissionBaseColor = Color.black;
                pulseStrength = stage.level >= 30 ? 0.04f : 0.08f;
                pulseSpeed = stage.level >= 30 ? 0.7f : 1.4f;
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
                case StarForgePlanetTheme.Star:
                    smoothness = stage.level >= 28
                        ? 0.32f + (stage.level - 28) * 0.08f
                        : 0.1f;
                    break;
                default: smoothness = 0.22f; break;
            }

            if (runtimeMaterial.HasProperty("_Smoothness"))
            {
                runtimeMaterial.SetFloat("_Smoothness", smoothness);
            }

            if (runtimeMaterial.HasProperty("_Metallic"))
            {
                runtimeMaterial.SetFloat(
                    "_Metallic",
                    stage.level >= 28 ? 0.08f + (stage.level - 28) * 0.025f : 0f);
            }

            runtimeMaterial.EnableKeyword("_EMISSION");

            surfaceImageBased = surface.isImageBased;
            surfaceHasBakedFace = surface.isImageBased && surface.hasBakedFace;

            Texture2D emissionTexture = surface.emissionMap;
            bool usesAmbientEmission =
                emissionTexture == null &&
                surface.isImageBased &&
                stage.level >= 1;
            if (usesAmbientEmission)
            {
                emissionTexture = surface.baseMap;
            }

            if (emissionTexture != null)
            {
                runtimeMaterial.SetTexture("_EmissionMap", emissionTexture);

                if (surface.isImageBased)
                {
                    // 이미지 텍스처: 원본의 페인팅 색감이 살도록 은은한 자체 발광
                    switch (surface.theme)
                    {
                        case StarForgePlanetTheme.Star:
                            emissionBaseColor = Color.white * GetImageStarEmission(stage.level);
                            break;
                        case StarForgePlanetTheme.Lava:
                            emissionBaseColor = Color.white * 2.1f;
                            break;
                        case StarForgePlanetTheme.BlackHole:
                            emissionBaseColor = Color.white * 1.2f;
                            break;
                        default:
                            emissionBaseColor = Color.white *
                                GetAmbientSurfaceEmission(surface.theme, stage.level);
                            break;
                    }
                }
                else
                {
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
            }
            else
            {
                runtimeMaterial.SetTexture("_EmissionMap", null);
                emissionBaseColor = Color.black;
                runtimeMaterial.DisableKeyword("_EMISSION");
            }

            switch (surface.theme)
            {
                case StarForgePlanetTheme.Star:
                    if (stage.level >= 28)
                    {
                        pulseStrength = currentShape == StarForgePlanetShape.Heart ? 0.09f : 0.045f;
                        pulseSpeed = currentShape == StarForgePlanetShape.Heart ? 4.4f : 1.35f;
                    }
                    else
                    {
                        pulseStrength = 0.18f;
                        pulseSpeed = 2.4f;
                    }
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

        private static float GetImageStarEmission(int level)
        {
            if (level >= 28)
            {
                return level == 28 ? 1.8f : level == 29 ? 2f : 0.68f;
            }

            if (level == 27)
            {
                return 2.2f;
            }

            if (level == 26)
            {
                return 1.8f;
            }

            if (level == 25)
            {
                return 1.5f;
            }

            if (level == 24)
            {
                return 1.25f;
            }

            if (level == 23)
            {
                return 1.15f;
            }

            if (level == 22)
            {
                return 0.5f;
            }

            return 1.3f + Mathf.Clamp(level - 16, 0, 5) * 0.06f;
        }

        private static float GetAmbientSurfaceEmission(StarForgePlanetTheme theme, int level)
        {
            switch (theme)
            {
                case StarForgePlanetTheme.Ice:
                    return 0.46f;
                case StarForgePlanetTheme.Ocean:
                case StarForgePlanetTheme.Life:
                    return 0.5f;
                case StarForgePlanetTheme.Gas:
                    return 0.48f + Mathf.Clamp(level - 13, 0, 2) * 0.035f;
                default:
                    switch (level)
                    {
                        case 1: return 1.6f;
                        case 2: return 1.8f;
                        case 3: return 1.3f;
                        case 4: return 2f;
                        case 7: return 1.8f;
                    }

                    return 0.34f + Mathf.Clamp(level, 1, 9) * 0.022f;
            }
        }

        private void ApplyDecor(StarForgePlanetTheme theme, StageVisualConfig stage, Color baseColor)
        {
            int level = stage.level;
            int tier = StarForgeVisualLibrary.GetLevelTier(level);
            bool isStar = theme == StarForgePlanetTheme.Star;
            bool isBlackHole = theme == StarForgePlanetTheme.BlackHole;
            bool isSurfaceOnlyHighTier = level >= 28;
            bool showRing = theme == StarForgePlanetTheme.Gas && level >= 14;
            bool showGiantCorona = level == 20 || level == 21;
            bool showDiffraction = level == 22;
            bool showNeutronEquator = level == 23 || level == 24;
            bool showNovaNebula = level == 26;
            bool showQuarkGrid = level == 27;

            if (isSurfaceOnlyHighTier)
            {
                Color highTierHalo = Color.Lerp(baseColor, Color.white, 0.28f);
                highTierHalo.a = 0.31f + (level - 28) * 0.05f;
                haloBaseColor = highTierHalo;
            }
            else
            {
                Color haloTint = Color.Lerp(baseColor, Color.white, level <= 15 ? 0.24f : 0.12f);
                haloBaseColor = isBlackHole
                    ? new Color(0.34f, 0.16f, 0.72f, 0.035f)
                    : new Color(
                        haloTint.r,
                        haloTint.g,
                        haloTint.b,
                        Mathf.Clamp(
                            0.2f + stage.emission * 0.16f,
                            0.2f,
                            0.68f));
            }

            haloBillboard.depthOffset = 0.3f * stage.scale;
            haloBillboard.transform.localScale = Vector3.one *
                (2.3f + tier * 0.17f + (isSurfaceOnlyHighTier ? 0.18f : 0f));

            // 코로나: 별 단계부터 본체를 감싸는 은은한 외광
            bool showCorona = isStar;
            coronaObject.SetActive(showCorona);
            if (showCorona)
            {
                Color coronaTint = isBlackHole
                    ? new Color(0.55f, 0.4f, 1f)
                    : Color.Lerp(baseColor, Color.white, 0.45f);
                float coronaAlpha = isSurfaceOnlyHighTier
                    ? 0.18f + (level - 28) * 0.04f
                    : isBlackHole
                        ? 0.1f
                        : Mathf.Clamp(
                            0.09f + stage.emission * 0.052f,
                            0.1f,
                            0.28f);
                coronaBaseColor = new Color(coronaTint.r, coronaTint.g, coronaTint.b, coronaAlpha);
                coronaBillboard.depthOffset = 0.42f * stage.scale;
                coronaBillboard.transform.localScale = Vector3.one *
                    (3.05f + tier * 0.26f + (isSurfaceOnlyHighTier ? 0.2f : 0f));
            }
            else
            {
                coronaBaseColor = Color.clear;
            }

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

            // 블랙홀 정면 데칼: 이미지 그대로, 본체는 회전하지 않음
            Texture2D faceTexture = null;
            blackHoleFaceActive = false;
            blackHoleFaceObject.SetActive(blackHoleFaceActive);
            if (blackHoleFaceActive)
            {
                if (blackHoleFaceMaterial.HasProperty("_BaseMap"))
                {
                    blackHoleFaceMaterial.SetTexture("_BaseMap", faceTexture);
                }

                if (blackHoleFaceMaterial.HasProperty("_MainTex"))
                {
                    blackHoleFaceMaterial.SetTexture("_MainTex", faceTexture);
                }

                float faceAlpha = currentShape == StarForgePlanetShape.Default
                    ? 0.14f
                    : currentShape == StarForgePlanetShape.Heart ? 0.22f : 0.38f;
                StarForgeVisualLibrary.SetMaterialColor(
                    blackHoleFaceMaterial,
                    new Color(1f, 1f, 1f, faceAlpha));
                blackHoleFaceBillboard.depthOffset = -0.55f;
                float faceScale = currentShape == StarForgePlanetShape.Cat
                    ? 1.9f
                    : currentShape == StarForgePlanetShape.Heart ? 1.72f : 1.55f;
                if (level == 29)
                {
                    faceScale *= 1.05f;
                }

                blackHoleFaceBillboard.transform.localScale = Vector3.one * faceScale;
            }

            diskObject.SetActive(false);
            if (isBlackHole)
            {
                // 강착원반: 주황 → 백열 → 청백, 데칼 모드에서는 천천히 도는 고리
                Color diskColor = level >= 30
                    ? new Color(0.65f, 0.78f, 1f, 0.85f)
                    : level >= 29
                        ? new Color(1f, 0.72f, 0.3f, 0.95f)
                        : new Color(1f, 0.55f, 0.16f, 0.95f);
                StarForgeVisualLibrary.SetMaterialColor(diskMaterial, diskColor);
                Color innerColor = level >= 30
                    ? new Color(0.82f, 0.92f, 1f, 1f)
                    : level >= 29
                        ? new Color(1f, 0.92f, 0.58f, 1f)
                        : new Color(0.92f, 0.72f, 1f, 1f);
                Color outerColor = level >= 30
                    ? new Color(0.12f, 0.2f, 0.55f, 1f)
                    : level >= 29
                        ? new Color(1f, 0.12f, 0.025f, 1f)
                        : new Color(0.28f, 0.04f, 0.66f, 1f);
                if (diskMaterial.HasProperty("_InnerColor"))
                {
                    diskMaterial.SetColor("_InnerColor", innerColor);
                    diskMaterial.SetColor("_OuterColor", outerColor);
                    diskMaterial.SetFloat("_FlowSpeed", level >= 30 ? -0.22f : level >= 29 ? 0.72f : 1.15f);
                    diskMaterial.SetFloat("_Turbulence", level >= 30 ? 0.45f : level >= 29 ? 1.15f : 0.85f);
                    diskMaterial.SetFloat("_Asymmetry", level >= 30 ? 0.12f : level >= 29 ? 0.58f : 0.24f);
                    diskMaterial.SetFloat("_Intensity", level >= 30 ? 0.42f : level >= 29 ? 1.45f : 1.05f);
                }

                diskRotator.degreesPerSecond = level >= 30
                    ? new Vector3(0f, 2f, 0f)
                    : level >= 29 ? new Vector3(0f, 7f, 0f) : new Vector3(0f, 12f, 0f);
                float diskScale = level >= 30 ? 1.12f : level >= 29 ? 1.52f : 1f;
                diskObject.transform.localScale = new Vector3(diskScale, 1f, diskScale);
            }

            // 광자 링: 블랙홀 가장자리를 두르는 가는 빛의 고리
            photonRingObject.SetActive(false);
            if (isBlackHole)
            {
                Color photonColor = level >= 30
                    ? new Color(0.78f, 0.88f, 1f, 0.85f)
                    : new Color(1f, 0.78f, 0.45f, 0.6f);
                StarForgeVisualLibrary.SetMaterialColor(photonRingMaterial, photonColor);
                photonRingBillboard.depthOffset = 0.05f;
                photonRingBillboard.transform.localScale = Vector3.one *
                    (level >= 30 ? 1.32f : level >= 29 ? 1.26f : 1.2f);
            }

            giantCoronaLayers.SetActive(showGiantCorona);
            if (showGiantCorona)
            {
                float intensity = level == 21 ? 1f : 0.7f;
                Color[] coronaPalette =
                {
                    Color.Lerp(baseColor, new Color(1f, 0.28f, 0.06f), 0.45f),
                    Color.Lerp(baseColor, new Color(1f, 0.72f, 0.18f), 0.6f),
                    Color.Lerp(baseColor, Color.white, level == 21 ? 0.55f : 0.25f)
                };

                for (int i = 0; i < giantCoronaMaterials.Length; i++)
                {
                    Color color = coronaPalette[i];
                    color.a = (0.16f - i * 0.025f) * intensity;
                    StarForgeVisualLibrary.SetMaterialColor(giantCoronaMaterials[i], color);
                }
            }

            diffractionSpikes.SetActive(showDiffraction);
            if (showDiffraction)
            {
                diffractionBaseColor = new Color(0.78f, 0.9f, 1f, 0.34f);
                StarForgeVisualLibrary.SetMaterialColor(diffractionMaterial, diffractionBaseColor);
            }
            else
            {
                diffractionBaseColor = Color.clear;
            }

            neutronEquator.SetActive(showNeutronEquator);
            if (showNeutronEquator)
            {
                neutronEquatorBaseColor = new Color(
                    Mathf.Lerp(baseColor.r, 1f, 0.65f),
                    Mathf.Lerp(baseColor.g, 1f, 0.65f),
                    Mathf.Lerp(baseColor.b, 1f, 0.65f),
                    level == 24 ? 0.55f : 0.38f);
                StarForgeVisualLibrary.SetMaterialColor(neutronEquatorMaterial, neutronEquatorBaseColor);
                neutronEquatorRotator.degreesPerSecond = new Vector3(8f, level == 24 ? 220f : 150f, 3f);
            }

            // 펄서: 자전축에서 어긋난 등대 광선
            pulsarJets.SetActive(level == 24);
            if (level == 24)
            {
                pulsarRotator.degreesPerSecond = new Vector3(0f, 140f, 0f);
                pulsarBeamBaseColor = Color.Lerp(baseColor, Color.white, 0.55f);
                pulsarBeamBaseColor.a = 0.35f;
                StarForgeVisualLibrary.SetMaterialColor(pulsarBeamMaterial, pulsarBeamBaseColor);
            }
            else
            {
                pulsarBeamBaseColor = Color.clear;
            }

            // 마그네타: 본체를 휘감는 보랏빛 자기장 고리
            magnetarArcs.SetActive(level == 25);
            if (level == 25)
            {
                magnetarBaseColor = Color.Lerp(baseColor, new Color(0.85f, 0.6f, 1f), 0.5f);
                magnetarBaseColor.a = 0.3f;
                StarForgeVisualLibrary.SetMaterialColor(magnetarMaterial, magnetarBaseColor);
                ParticleSystem.MainModule magnetarMain = magnetarSparks.main;
                magnetarMain.startColor = Color.Lerp(baseColor, Color.white, 0.45f);
                if (!magnetarSparks.isPlaying)
                {
                    magnetarSparks.Play();
                }
            }
            else
            {
                magnetarBaseColor = Color.clear;
                magnetarSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 초신성 잔해: 잔잔히 퍼져 나가는 충격파 링
            if (level == 26)
            {
                ParticleSystem.MainModule novaMain = novaShockwave.main;
                novaMain.startColor = new Color(1f, 0.62f, 0.75f, 0.5f);
                if (!novaShockwave.isPlaying)
                {
                    novaShockwave.Play();
                }
            }
            else
            {
                novaShockwave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            novaNebula.SetActive(showNovaNebula);
            if (showNovaNebula)
            {
                Color[] nebulaPalette =
                {
                    new Color(1f, 0.28f, 0.55f, 0.17f),
                    new Color(1f, 0.62f, 0.24f, 0.14f),
                    new Color(0.38f, 0.62f, 1f, 0.12f)
                };

                for (int i = 0; i < novaNebulaMaterials.Length; i++)
                {
                    StarForgeVisualLibrary.SetMaterialColor(novaNebulaMaterials[i], nebulaPalette[i]);
                }
            }

            quarkGrid.SetActive(showQuarkGrid);
            if (showQuarkGrid)
            {
                quarkGridBaseColor = new Color(0.58f, 0.4f, 1f, 0.36f);
                StarForgeVisualLibrary.SetMaterialColor(quarkGridMaterial, quarkGridBaseColor);
                ParticleSystem.MainModule compressionMain = quarkCompression.main;
                compressionMain.startColor = new Color(0.65f, 0.48f, 1f, 0.7f);
                if (!quarkCompression.isPlaying)
                {
                    quarkCompression.Play();
                }
            }
            else
            {
                quarkGridBaseColor = Color.clear;
                quarkCompression.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (isBlackHole)
            {
                ParticleSystem.MainModule infallMain = gravityInfall.main;
                Color[] infallPalette = level >= 30
                    ? new[]
                    {
                        new Color(0.58f, 0.2f, 0.96f, 1f),
                        new Color(1f, 0.32f, 0.06f, 1f),
                        new Color(1f, 0.78f, 0.12f, 1f),
                        new Color(0.16f, 0.48f, 1f, 1f),
                        Color.white
                    }
                    : level >= 29
                        ? new[]
                        {
                            new Color(1f, 0.32f, 0.06f, 1f),
                            new Color(0.58f, 0.2f, 0.96f, 1f),
                            new Color(1f, 0.78f, 0.12f, 1f)
                        }
                        : new[] { new Color(0.58f, 0.2f, 0.96f, 1f) };
                infallMain.startColor = new Color(1f, 1f, 1f, 0.3f);
                ParticleSystem.ColorOverLifetimeModule infallColor = gravityInfall.colorOverLifetime;
                Gradient infallGradient = new Gradient();
                int gradientColorCount = Mathf.Max(2, infallPalette.Length);
                GradientColorKey[] gradientColors = new GradientColorKey[gradientColorCount];
                for (int i = 0; i < gradientColorCount; i++)
                {
                    int paletteIndex = infallPalette.Length == 1
                        ? 0
                        : Mathf.RoundToInt(i * (infallPalette.Length - 1f) / (gradientColorCount - 1f));
                    gradientColors[i] = new GradientColorKey(
                        infallPalette[paletteIndex],
                        gradientColorCount == 1 ? 0f : (float)i / (gradientColorCount - 1));
                }

                infallGradient.SetKeys(
                    gradientColors,
                    new[]
                    {
                        new GradientAlphaKey(0.08f, 0f),
                        new GradientAlphaKey(0.7f, 0.3f),
                        new GradientAlphaKey(0.22f, 0.78f),
                        new GradientAlphaKey(0f, 1f)
                    });
                infallColor.color = infallGradient;
                ParticleSystem.EmissionModule infallEmission = gravityInfall.emission;
                infallEmission.rateOverTime = level >= 30 ? 3f : level >= 29 ? 2.5f : 2f;
                if (!gravityInfall.isPlaying)
                {
                    gravityInfall.Play();
                }

                blackHoleLightInfall.SetActive(true);
                float speed = level >= 30 ? 0.62f : level >= 29 ? 0.92f : 0.76f;
                float intensity = level >= 30 ? 0.08f : level >= 29 ? 0.1f : 0.14f;

                for (int i = 0; i < blackHoleLightInfallMaterials.Length; i++)
                {
                    Material material = blackHoleLightInfallMaterials[i];
                    bool isActiveLayer = i < infallPalette.Length;
                    if (blackHoleLightInfallLayers[i] != null)
                    {
                        blackHoleLightInfallLayers[i].SetActive(isActiveLayer);
                    }

                    if (!isActiveLayer || material == null || !material.HasProperty("_NearColor"))
                    {
                        continue;
                    }

                    Color layerColor = infallPalette[i];
                    material.SetColor("_NearColor", Color.Lerp(layerColor, Color.white, 0.16f));
                    material.SetColor("_FarColor", layerColor * 0.36f);
                    material.SetFloat("_Speed", speed * (1f + i * 0.23f));
                    material.SetFloat("_Twist", 7f + i * 1.8f);
                    material.SetFloat("_Arms", 5f + i);
                    material.SetFloat("_Intensity", intensity * (1f - i * 0.08f));
                    material.SetFloat("_InnerRadius", 0.408f - i * 0.014f);
                    material.SetFloat("_OuterRadius", 0.72f - i * 0.018f);
                }
            }
            else
            {
                gravityInfall.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                blackHoleLightInfall.SetActive(false);
            }

            blackHoleDopplerGlow.SetActive(false);
            singularityLensing.SetActive(false);

            // 펄서: 주변으로 퍼지는 펄스 링
            if (level == 24)
            {
                ParticleSystem.MainModule pulseMain = pulsarPulseRings.main;
                pulseMain.startColor = Color.Lerp(baseColor, Color.white, 0.5f);
                if (!pulsarPulseRings.isPlaying)
                {
                    pulsarPulseRings.Play();
                }
            }
            else
            {
                pulsarPulseRings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystem.MainModule flareMain = starFlares.main;
            if (isStar && !isSurfaceOnlyHighTier)
            {
                flareMain.startColor = Color.Lerp(baseColor, Color.white, 0.35f);
                flareMain.maxParticles = level >= 24 ? 84 : 64;
                flareMain.startSize = level >= 22
                    ? new ParticleSystem.MinMaxCurve(0.1f, 0.3f)
                    : new ParticleSystem.MinMaxCurve(0.075f, 0.22f);
                ParticleSystem.EmissionModule flareEmission = starFlares.emission;
                flareEmission.rateOverTime =
                    8f + Mathf.Clamp(level - 16, 0, 11) * 0.55f;
                if (!starFlares.isPlaying)
                {
                    starFlares.Play();
                }
            }
            else
            {
                starFlares.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 반짝임: 10강부터 살짝, 별 단계부터 본격적으로 빛 알갱이가 튐
            float sparkleRate = isBlackHole || isSurfaceOnlyHighTier
                ? 0f
                : level >= 16
                    ? 6.5f + (level - 16) * 0.7f + (level == 24 ? 5f : 0f)
                    : level >= 10
                        ? 3.6f + (level - 10) * 0.28f
                        : level >= 5
                            ? 1.8f + (level - 5) * 0.2f
                            : level >= 1 ? 0.9f + level * 0.16f : 0f;
            ParticleSystem.EmissionModule sparkleEmission = sparkles.emission;
            sparkleEmission.rateOverTime = sparkleRate;
            ParticleSystem.MainModule sparkleMain = sparkles.main;
            sparkleMain.maxParticles = level >= 16 ? 56 : 36;
            sparkleMain.startSize = level >= 16
                ? new ParticleSystem.MinMaxCurve(0.045f, 0.11f)
                : new ParticleSystem.MinMaxCurve(0.028f, 0.075f);
            sparkleMain.startColor = Color.Lerp(baseColor, Color.white, 0.75f);
            if (sparkleRate > 0f)
            {
                if (!sparkles.isPlaying)
                {
                    sparkles.Play();
                }
            }
            else
            {
                sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystem.MainModule dustMain = ambientDust.main;
            dustMain.maxParticles = 110;
            dustMain.startSize = level >= 16
                ? new ParticleSystem.MinMaxCurve(0.03f, 0.072f)
                : new ParticleSystem.MinMaxCurve(0.024f, 0.058f);
            Color dustTint = Color.Lerp(baseColor, Color.white, 0.36f);
            dustMain.startColor = new Color(dustTint.r, dustTint.g, dustTint.b, 0.52f);
            ParticleSystem.EmissionModule dustEmission = ambientDust.emission;
            dustEmission.rateOverTime = isBlackHole || isSurfaceOnlyHighTier
                ? 0f
                : 9f + level * 0.58f;
            if (!isBlackHole && !isSurfaceOnlyHighTier && !ambientDust.isPlaying)
            {
                ambientDust.Play();
            }
            else if (isBlackHole || isSurfaceOnlyHighTier)
            {
                ambientDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ConfigurePremiumDecor(stage);
        }

        private void ConfigurePremiumDecor(StageVisualConfig stage)
        {
            int level = stage.level;
            bool active = level >= 28;
            if (!active)
            {
                if (premiumRimObject != null)
                {
                    premiumRimObject.SetActive(false);
                }

                for (int i = 0; i < premiumOrbitAuras.Length; i++)
                {
                    if (premiumOrbitBands[i] != null)
                    {
                        premiumOrbitBands[i].SetActive(false);
                    }

                    premiumOrbitBandBaseColors[i] = Color.clear;
                    SetPremiumParticleActive(premiumOrbitAuras[i], false);
                }

                for (int i = 0; i < premiumEnergyArcs.Length; i++)
                {
                    if (premiumEnergyArcs[i] != null)
                    {
                        premiumEnergyArcs[i].SetActive(false);
                    }

                    premiumEnergyArcBaseColors[i] = Color.clear;
                }

                SetPremiumParticleActive(premiumOutflow, false);
                SetPremiumParticleActive(premiumWisps, false);
                SetPremiumParticleActive(premiumAuraCloud, false);
                SetPremiumParticleActive(premiumStardust, false);
                SetPremiumParticleActive(premiumPulseRings, false);

                premiumRimBaseColor = Color.clear;
                premiumRimSecondaryColor = Color.clear;
                return;
            }

            int levelIndex = Mathf.Clamp(level - 28, 0, 2);
            Color primary;
            Color secondary;
            Color accent;
            switch (level)
            {
                case 28:
                    primary = new Color(0.48f, 0.28f, 1f);
                    secondary = new Color(0.12f, 0.9f, 0.94f);
                    accent = new Color(0.84f, 0.78f, 1f);
                    break;
                case 29:
                    primary = new Color(0.16f, 0.5f, 1f);
                    secondary = new Color(1f, 0.7f, 0.2f);
                    accent = new Color(0.84f, 0.94f, 1f);
                    break;
                default:
                    primary = new Color(0.66f, 0.94f, 1f);
                    secondary = new Color(1f, 0.78f, 0.28f);
                    accent = new Color(1f, 0.98f, 0.88f);
                    break;
            }

            if (currentShape == StarForgePlanetShape.Heart)
            {
                primary = Color.Lerp(primary, new Color(1f, 0.35f, 0.68f), 0.58f);
                accent = Color.Lerp(accent, new Color(1f, 0.72f, 0.82f), 0.45f);
            }

            premiumRimBaseColor = primary;
            premiumRimSecondaryColor = secondary;
            bool showMeshRim = currentShape != StarForgePlanetShape.Cat;
            premiumRimBaseScale = 1.018f;
            premiumRimObject.SetActive(showMeshRim);
            premiumRimObject.transform.localScale = Vector3.one * premiumRimBaseScale;
            if (showMeshRim && premiumRimMaterial != null)
            {
                if (premiumRimMaterial.HasProperty("_Color"))
                {
                    premiumRimMaterial.SetColor("_Color", primary);
                }

                if (premiumRimMaterial.HasProperty("_SecondaryColor"))
                {
                    premiumRimMaterial.SetColor("_SecondaryColor", secondary);
                }

                if (premiumRimMaterial.HasProperty("_FresnelPower"))
                {
                    premiumRimMaterial.SetFloat(
                        "_FresnelPower",
                        currentShape == StarForgePlanetShape.Cat ? 5.8f : 4.4f + levelIndex * 0.35f);
                }

                if (premiumRimMaterial.HasProperty("_Intensity"))
                {
                    premiumRimMaterial.SetFloat(
                        "_Intensity",
                        currentShape == StarForgePlanetShape.Cat
                            ? 0.31f + levelIndex * 0.04f
                            : 0.36f + levelIndex * 0.065f);
                }

                if (premiumRimMaterial.HasProperty("_FlowSpeed"))
                {
                    premiumRimMaterial.SetFloat("_FlowSpeed", 0.26f + levelIndex * 0.12f);
                }

                if (premiumRimMaterial.HasProperty("_Pulse"))
                {
                    premiumRimMaterial.SetFloat("_Pulse", 1f);
                }

                if (!premiumRimMaterial.HasProperty("_Color"))
                {
                    StarForgeVisualLibrary.SetMaterialColor(
                        premiumRimMaterial,
                        new Color(primary.r, primary.g, primary.b, 0.14f));
                }
            }

            int activeOrbitCount = level - 27;
            int orbitParticleCap = level == 28 ? 24 : level == 29 ? 32 : 42;
            float orbitRate = level == 28 ? 8f : level == 29 ? 12f : 16f;
            for (int i = 0; i < premiumOrbitAuras.Length; i++)
            {
                bool bandActive = i < activeOrbitCount;
                if (premiumOrbitBands[i] != null)
                {
                    premiumOrbitBands[i].SetActive(bandActive);
                }

                if (bandActive)
                {
                    Color bandColor = i == 0
                        ? primary
                        : i == 1
                            ? secondary
                            : accent;
                    float bandAlpha =
                        (0.2f + levelIndex * 0.03f) *
                        (1f - i * 0.16f);
                    premiumOrbitBandBaseColors[i] = new Color(
                        bandColor.r,
                        bandColor.g,
                        bandColor.b,
                        bandAlpha);
                    StarForgeVisualLibrary.SetMaterialColor(
                        premiumOrbitBandMaterials[i],
                        premiumOrbitBandBaseColors[i]);

                    float shapeScale = currentShape == StarForgePlanetShape.Cat
                        ? 0.97f
                        : currentShape == StarForgePlanetShape.Heart
                            ? 1.035f
                            : 1f;
                    premiumOrbitBands[i].transform.localScale =
                        Vector3.one * shapeScale;
                }
                else
                {
                    premiumOrbitBandBaseColors[i] = Color.clear;
                }

                ParticleSystem system = premiumOrbitAuras[i];
                bool systemActive = i < activeOrbitCount;
                SetPremiumParticleActive(system, systemActive);
                if (!systemActive)
                {
                    continue;
                }

                ParticleSystem.MainModule main = system.main;
                main.maxParticles = orbitParticleCap;
                main.startLifetime = new ParticleSystem.MinMaxCurve(
                    1.3f + levelIndex * 0.12f,
                    2.15f + levelIndex * 0.18f);
                main.startSize = new ParticleSystem.MinMaxCurve(
                    0.022f + levelIndex * 0.003f,
                    0.048f + levelIndex * 0.006f);
                main.startColor = Color.Lerp(primary, accent, 0.16f);

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = orbitRate - i * 1.25f;

                ParticleSystem.ShapeModule shape = system.shape;
                shape.radius = 0.98f + levelIndex * 0.08f + i * 0.11f;

                ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
                Color orbitStart = i % 2 == 0 ? primary : secondary;
                Color orbitEnd = i == 2
                    ? Color.Lerp(accent, secondary, 0.5f)
                    : Color.Lerp(orbitStart, secondary, 0.38f);
                color.color = BuildPremiumGradient(
                    orbitStart,
                    orbitEnd,
                    0.34f + levelIndex * 0.035f);

                ParticleSystem.TrailModule trails = system.trails;
                trails.enabled = level >= 29;
                trails.ratio = level == 29 ? 0.28f : 0.4f;
                trails.colorOverLifetime = BuildPremiumGradient(
                    orbitStart,
                    orbitEnd,
                    0.32f + levelIndex * 0.03f);
                trails.colorOverTrail = BuildPremiumGradient(
                    orbitStart,
                    orbitEnd,
                    0.2f);
            }

            int activeArcCount = level - 26;
            for (int i = 0; i < premiumEnergyArcs.Length; i++)
            {
                bool arcActive = i < activeArcCount;
                GameObject arc = premiumEnergyArcs[i];
                if (arc != null)
                {
                    arc.SetActive(arcActive);
                }

                if (!arcActive)
                {
                    premiumEnergyArcBaseColors[i] = Color.clear;
                    continue;
                }

                Color arcColor = i % 3 == 0
                    ? primary
                    : i % 3 == 1
                        ? secondary
                        : accent;
                float arcAlpha =
                    (0.27f + levelIndex * 0.04f) *
                    (1f - i * 0.1f);
                premiumEnergyArcBaseColors[i] = new Color(
                    arcColor.r,
                    arcColor.g,
                    arcColor.b,
                    arcAlpha);
                StarForgeVisualLibrary.SetMaterialColor(
                    premiumEnergyArcMaterials[i],
                    premiumEnergyArcBaseColors[i]);

                float shapeScale = currentShape == StarForgePlanetShape.Cat
                    ? 0.96f
                    : currentShape == StarForgePlanetShape.Heart
                        ? 1.025f
                        : 1f;
                arc.transform.localScale = Vector3.one * shapeScale;
            }

            SetPremiumParticleActive(premiumOutflow, true);
            ParticleSystem.MainModule outflowMain = premiumOutflow.main;
            outflowMain.maxParticles = level == 28 ? 34 : level == 29 ? 44 : 56;
            outflowMain.startLifetime = new ParticleSystem.MinMaxCurve(
                0.72f,
                1.1f + levelIndex * 0.12f);
            outflowMain.startSize = new ParticleSystem.MinMaxCurve(
                0.02f + levelIndex * 0.003f,
                0.052f + levelIndex * 0.007f);
            outflowMain.startColor = Color.Lerp(primary, accent, 0.2f);
            ParticleSystem.EmissionModule outflowEmission = premiumOutflow.emission;
            outflowEmission.rateOverTime = level == 28 ? 10f : level == 29 ? 14f : 18f;
            ParticleSystem.ColorOverLifetimeModule outflowColor = premiumOutflow.colorOverLifetime;
            outflowColor.color = BuildPremiumGradient(
                Color.Lerp(primary, accent, 0.28f),
                secondary,
                0.31f + levelIndex * 0.025f);

            SetPremiumParticleActive(premiumWisps, true);
            ParticleSystem.MainModule wispMain = premiumWisps.main;
            wispMain.maxParticles = level == 28 ? 34 : level == 29 ? 46 : 60;
            wispMain.startLifetime = new ParticleSystem.MinMaxCurve(
                0.62f,
                1f + levelIndex * 0.12f);
            wispMain.startSize = new ParticleSystem.MinMaxCurve(
                0.017f + levelIndex * 0.002f,
                0.042f + levelIndex * 0.006f);
            wispMain.startColor = Color.Lerp(primary, secondary, 0.24f);
            ParticleSystem.EmissionModule wispEmission = premiumWisps.emission;
            wispEmission.rateOverTime = level == 28 ? 12f : level == 29 ? 17f : 22f;
            ParticleSystem.ColorOverLifetimeModule wispColor = premiumWisps.colorOverLifetime;
            wispColor.color = BuildPremiumGradient(
                Color.Lerp(primary, accent, 0.18f),
                secondary,
                0.29f + levelIndex * 0.03f);

            SetPremiumParticleActive(premiumAuraCloud, true);
            ParticleSystem.MainModule cloudMain = premiumAuraCloud.main;
            cloudMain.maxParticles = level == 28 ? 7 : level == 29 ? 9 : 12;
            cloudMain.startSize = new ParticleSystem.MinMaxCurve(
                0.11f + levelIndex * 0.015f,
                0.21f + levelIndex * 0.025f);
            cloudMain.startColor = Color.Lerp(primary, accent, 0.32f);
            ParticleSystem.EmissionModule cloudEmission = premiumAuraCloud.emission;
            cloudEmission.rateOverTime = level == 28 ? 1f : level == 29 ? 1.35f : 1.7f;
            ParticleSystem.ShapeModule cloudShape = premiumAuraCloud.shape;
            cloudShape.radius = 0.92f + levelIndex * 0.08f;
            ParticleSystem.ColorOverLifetimeModule cloudColor = premiumAuraCloud.colorOverLifetime;
            cloudColor.color = BuildPremiumGradient(
                Color.Lerp(primary, accent, 0.38f),
                secondary,
                0.06f + levelIndex * 0.012f);

            SetPremiumParticleActive(premiumStardust, true);
            ParticleSystem.MainModule dustMain = premiumStardust.main;
            dustMain.maxParticles = level == 28 ? 26 : level == 29 ? 34 : 42;
            dustMain.startSize = new ParticleSystem.MinMaxCurve(
                0.023f + levelIndex * 0.002f,
                0.058f + levelIndex * 0.005f);
            dustMain.startColor = Color.Lerp(primary, secondary, 0.3f);
            ParticleSystem.EmissionModule dustEmission = premiumStardust.emission;
            dustEmission.rateOverTime = level == 28 ? 3.5f : level == 29 ? 5f : 6.5f;
            ParticleSystem.ShapeModule dustShape = premiumStardust.shape;
            dustShape.radius = 1.42f + levelIndex * 0.12f;
            ParticleSystem.ColorOverLifetimeModule dustColor = premiumStardust.colorOverLifetime;
            dustColor.color = BuildPremiumGradient(
                Color.Lerp(primary, accent, 0.22f),
                secondary,
                0.48f);

            // The billboard ring texture breaks into screen-spanning line segments on portrait cameras.
            // Rim and outflow layers provide the same depth without obscuring the planet silhouette.
            SetPremiumParticleActive(premiumPulseRings, false);

            ConfigurePremiumFilaments(level, primary, secondary, accent);
        }

        private void ConfigurePremiumFilaments(
            int level,
            Color primary,
            Color secondary,
            Color accent)
        {
            // 블랙홀용 쿼드 필라멘트는 밝은 항성 표면에서 사각 경계가 드러난다.
            // 프리미엄 항성은 메시 림, 공전 오오라, 표면 방출만 사용한다.
            blackHoleLightInfall.SetActive(false);
        }

        private static void SetPremiumParticleActive(ParticleSystem system, bool active)
        {
            if (system == null)
            {
                return;
            }

            if (active)
            {
                if (!system.gameObject.activeSelf)
                {
                    system.gameObject.SetActive(true);
                }

                if (!system.isPlaying)
                {
                    system.Play();
                }
            }
            else
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (system.gameObject.activeSelf)
                {
                    system.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshShapeGeometry()
        {
            Mesh targetMesh = defaultPlanetMesh;
            if (currentShape == StarForgePlanetShape.Heart)
            {
                targetMesh = StarForgeVisualLibrary.GetHeartMesh();
            }
            else if (currentShape == StarForgePlanetShape.Cat)
            {
                // 전 레벨 고양이 모양은 동일한 머리 메시 사용 (귀/얼굴 없음)
                targetMesh = StarForgeVisualLibrary.GetCatHeadMesh();
            }

            planetMeshFilter.sharedMesh = targetMesh;
            if (premiumRimMeshFilter != null)
            {
                premiumRimMeshFilter.sharedMesh = targetMesh;
            }

            if (premiumOutflow != null)
            {
                ParticleSystem.ShapeModule shape = premiumOutflow.shape;
                shape.mesh = targetMesh;
            }

            if (premiumWisps != null)
            {
                ParticleSystem.ShapeModule shape = premiumWisps.shape;
                shape.mesh = targetMesh;
            }

            // 고양이 귀는 전 레벨에서 숨김 — 구체 + 텍스처로만 표현
            if (currentShape == StarForgePlanetShape.Cat)
            {
                if (premiumRimObject != null)
                {
                    premiumRimObject.SetActive(false);
                }
            }

            automaticRotation = Quaternion.identity;
            planetRoot.localRotation = Quaternion.identity;
        }

        private void ApplyLight(StarForgePlanetTheme theme, StageVisualConfig stage, Color baseColor)
        {
            if (planetLight == null)
            {
                return;
            }

            planetLight.color = theme == StarForgePlanetTheme.BlackHole
                ? new Color(0.42f, 0.18f, 0.78f)
                : Color.Lerp(baseColor, Color.white, stage.level >= 22 ? 0.35f : 0f);
            planetLightBaseIntensity = theme == StarForgePlanetTheme.BlackHole
                ? (stage.level >= 30 ? 0.12f : stage.level >= 29 ? 0.16f : 0.14f)
                : stage.level >= 28
                    ? 2.35f + (stage.level - 28) * 0.35f
                    : stage.level == 22
                        ? 2.8f
                        : stage.level == 23
                            ? 4.2f
                            : stage.level == 24
                                ? 4.6f
                                : 1.5f + stage.emission * 2f;
            planetLight.intensity = planetLightBaseIntensity;
            planetLight.range = 4.5f + stage.scale * 2.8f;
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

        private void EnsureExternalBlackHole()
        {
            if (externalBlackHoleObject != null || planetRoot == null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("BlackHole");
            if (prefab == null)
            {
                return;
            }

            externalBlackHoleObject = Instantiate(prefab, planetRoot, false);
            externalBlackHoleObject.name = "StarForge BlackHole Asset";
            externalBlackHoleObject.transform.localPosition = Vector3.zero;
            externalBlackHoleObject.transform.localRotation = Quaternion.identity;
            externalBlackHoleObject.transform.localScale = Vector3.one * 1.04f;

            Collider[] colliders =
                externalBlackHoleObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Destroy(colliders[i]);
            }

            externalBlackHoleObject.SetActive(false);
        }

        private void SetExternalBlackHoleActive(bool active)
        {
            externalBlackHoleActive = active && externalBlackHoleObject != null;
            if (externalBlackHoleObject != null)
            {
                externalBlackHoleObject.SetActive(externalBlackHoleActive);
            }

            if (planetRenderer != null)
            {
                planetRenderer.enabled = !externalBlackHoleActive;
            }

            if (decorRoot != null)
            {
                decorRoot.gameObject.SetActive(!externalBlackHoleActive);
            }

            if (blackHoleFaceObject != null)
            {
                blackHoleFaceActive = false;
                blackHoleFaceObject.SetActive(false);
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

            if (planetMeshFilter == null && planetRenderer != null)
            {
                planetMeshFilter = planetRenderer.GetComponent<MeshFilter>();
                Mesh sourceMesh = planetMeshFilter.sharedMesh;
                defaultPlanetMesh = Instantiate(sourceMesh);
                defaultPlanetMesh.name = "StarForge Runtime Sphere";
                planetMeshFilter.sharedMesh = defaultPlanetMesh;
                planetBasePosition = planetRoot.localPosition;
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

            if (blackHoleCoreMaterial == null)
            {
                blackHoleCoreMaterial = StarForgeVisualLibrary.CreateBlackHoleCoreMaterial();
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
                CreateCorona();
                CreateRing();
                CreateDisk();
                CreatePhotonRing();
                CreatePulsarJets();
                CreateMagnetarArcs();
                CreateGiantCoronaLayers();
                CreateDiffractionSpikes();
                CreateNeutronEquator();
                CreateMagnetarSparks();
                CreateNovaNebula();
                CreateQuarkGrid();
                CreateQuarkCompression();
                CreateGravityInfall();
                CreateBlackHoleLightInfall();
                CreateBlackHoleDopplerGlow();
                CreateSingularityLensing();
                CreateAmbientDust();
                CreateStarFlares();
                CreateSparkles();
                CreateNovaShockwave();
                CreateStageArrivalBurst();
                CreateBlackHoleFace();
                CreatePulsarPulseRings();
                CreatePremiumEffects();
            }

            if (blackHoleLightInfall == null)
            {
                CreateBlackHoleLightInfall();
            }

        }

        private void CreateBlackHoleFace()
        {
            blackHoleFaceObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blackHoleFaceObject.name = "Black Hole Face";
            Destroy(blackHoleFaceObject.GetComponent<Collider>());
            blackHoleFaceObject.transform.SetParent(decorRoot, false);
            blackHoleFaceObject.transform.localScale = Vector3.one * 2.35f;

            MeshRenderer faceRenderer = blackHoleFaceObject.GetComponent<MeshRenderer>();
            faceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            faceRenderer.receiveShadows = false;
            blackHoleFaceMaterial = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
            faceRenderer.sharedMaterial = blackHoleFaceMaterial;

            blackHoleFaceBillboard = blackHoleFaceObject.AddComponent<StarForgeBillboard>();
            blackHoleFaceObject.SetActive(false);
        }

        private void CreatePulsarPulseRings()
        {
            GameObject pulseObject = new GameObject("Pulsar Pulse Rings");
            pulseObject.transform.SetParent(decorRoot, false);
            pulsarPulseRings = pulseObject.AddComponent<ParticleSystem>();
            pulsarPulseRings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = pulsarPulseRings.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = 4.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 6;

            ParticleSystem.EmissionModule emission = pulsarPulseRings.emission;
            emission.rateOverTime = 1.7f;

            ParticleSystem.ShapeModule shape = pulsarPulseRings.shape;
            shape.enabled = false;

            // 중심에서 빠르게 퍼지는 전파 펄스
            AnimationCurve expand = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(1f, 1f));
            ParticleSystem.SizeOverLifetimeModule size = pulsarPulseRings.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, expand);

            ParticleSystem.ColorOverLifetimeModule color = pulsarPulseRings.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.32f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = pulsarPulseRings.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, true, StarForgeVisualLibrary.ShockwaveRingTexture);
        }

        private void CreatePremiumEffects()
        {
            CreatePremiumRim();
            CreatePremiumOrbitBands();
            CreatePremiumEnergyArcs();

            premiumOrbitAuras[0] = CreatePremiumOrbitAura(
                "Premium Orbit Aura A",
                new Vector3(18f, 8f, 14f),
                0.32f);
            premiumOrbitAuras[1] = CreatePremiumOrbitAura(
                "Premium Orbit Aura B",
                new Vector3(64f, -22f, -28f),
                -0.26f);
            premiumOrbitAuras[2] = CreatePremiumOrbitAura(
                "Premium Orbit Aura C",
                new Vector3(37f, 48f, 72f),
                0.2f);

            CreatePremiumOutflow();
            CreatePremiumWisps();
            CreatePremiumAuraCloud();
            CreatePremiumStardust();
            CreatePremiumPulseRings();
        }

        private void CreatePremiumRim()
        {
            premiumRimObject = new GameObject("Premium Near Rim");
            premiumRimObject.transform.SetParent(planetRoot, false);
            premiumRimObject.transform.localScale = Vector3.one * premiumRimBaseScale;

            premiumRimMeshFilter = premiumRimObject.AddComponent<MeshFilter>();
            premiumRimMeshFilter.sharedMesh = planetMeshFilter.sharedMesh;

            MeshRenderer renderer = premiumRimObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 4;
            premiumRimMaterial = StarForgeVisualLibrary.CreatePremiumRimMaterial();
            renderer.sharedMaterial = premiumRimMaterial;
            premiumRimObject.SetActive(false);
        }

        private void CreatePremiumOrbitBands()
        {
            Vector3[] rotations =
            {
                new Vector3(68f, 4f, 14f),
                new Vector3(53f, 42f, -22f),
                new Vector3(77f, -34f, 31f)
            };
            float[] radii = { 0.72f, 0.88f, 1.04f };
            Vector3[] speeds =
            {
                new Vector3(0f, 12f, 0f),
                new Vector3(0f, -8f, 0f),
                new Vector3(0f, 5f, 0f)
            };

            for (int i = 0; i < premiumOrbitBands.Length; i++)
            {
                GameObject band = new GameObject("Premium Orbit Band " + (i + 1));
                band.transform.SetParent(decorRoot, false);
                band.transform.localRotation = Quaternion.Euler(rotations[i]);

                MeshFilter filter = band.AddComponent<MeshFilter>();
                filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(
                    radii[i],
                    radii[i] + 0.024f + i * 0.004f,
                    128,
                    7f + i * 2f);

                MeshRenderer renderer = band.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 1 + i;
                Material material = StarForgeVisualLibrary.CreateParticleMaterial(
                    Color.clear,
                    true,
                    StarForgeVisualLibrary.PlanetRingTexture);
                renderer.sharedMaterial = material;

                StarForgeOrbitRotator rotator =
                    band.AddComponent<StarForgeOrbitRotator>();
                rotator.degreesPerSecond = speeds[i];

                premiumOrbitBands[i] = band;
                premiumOrbitBandMaterials[i] = material;
                band.SetActive(false);
            }
        }

        private void CreatePremiumEnergyArcs()
        {
            Vector3[] rotations =
            {
                new Vector3(74f, 18f, 7f),
                new Vector3(57f, -38f, -21f),
                new Vector3(82f, 51f, 28f),
                new Vector3(46f, -19f, 63f)
            };
            float[] radii = { 0.61f, 0.69f, 0.78f, 0.87f };
            float[] starts = { 18f, 176f, 272f, 92f };
            float[] spans = { 126f, 98f, 148f, 84f };
            Vector3[] speeds =
            {
                new Vector3(2f, 17f, 1f),
                new Vector3(-3f, -13f, 2f),
                new Vector3(1f, 10f, -2f),
                new Vector3(-2f, -8f, 3f)
            };

            for (int i = 0; i < premiumEnergyArcs.Length; i++)
            {
                GameObject arc = new GameObject("Premium Energy Arc " + (i + 1));
                arc.transform.SetParent(decorRoot, false);
                arc.transform.localRotation = Quaternion.Euler(rotations[i]);

                MeshFilter filter = arc.AddComponent<MeshFilter>();
                filter.sharedMesh = StarForgeVisualLibrary.CreateArcMesh(
                    radii[i],
                    radii[i] + 0.022f + i * 0.003f,
                    72,
                    starts[i],
                    spans[i],
                    3f + i);

                MeshRenderer renderer = arc.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 2 + i;
                Material material = StarForgeVisualLibrary.CreateParticleMaterial(
                    Color.clear,
                    true,
                    StarForgeVisualLibrary.PlanetRingTexture);
                if (material.HasProperty("_Cull"))
                {
                    material.SetInt("_Cull", 0);
                }

                renderer.sharedMaterial = material;

                StarForgeOrbitRotator rotator =
                    arc.AddComponent<StarForgeOrbitRotator>();
                rotator.degreesPerSecond = speeds[i];

                premiumEnergyArcs[i] = arc;
                premiumEnergyArcMaterials[i] = material;
                arc.SetActive(false);
            }
        }

        private ParticleSystem CreatePremiumOrbitAura(
            string objectName,
            Vector3 localRotation,
            float orbitalSpeed)
        {
            GameObject auraObject = new GameObject(objectName);
            auraObject.transform.SetParent(decorRoot, false);
            auraObject.transform.localRotation = Quaternion.Euler(localRotation);

            ParticleSystem system = auraObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.15f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.016f, 0.038f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 42;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.02f;
            shape.radiusThickness = 0.08f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(orbitalSpeed);

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
            noise.frequency = 0.38f;
            noise.scrollSpeed = 0.12f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.52f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.15f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(0.82f, 0.72f),
                    new Keyframe(1f, 0f)));

            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = false;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 0.58f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.38f),
                    new Keyframe(0.45f, 0.18f),
                    new Keyframe(1f, 0f)));

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 2;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            renderer.trailMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            auraObject.SetActive(false);
            return system;
        }

        private void CreatePremiumOutflow()
        {
            GameObject outflowObject = new GameObject("Premium Outer Emission");
            outflowObject.transform.SetParent(planetRoot, false);
            premiumOutflow = outflowObject.AddComponent<ParticleSystem>();
            premiumOutflow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = premiumOutflow.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.04f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 44;

            ParticleSystem.EmissionModule emission = premiumOutflow.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = premiumOutflow.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = planetMeshFilter.sharedMesh;
            shape.normalOffset = 0.012f;
            shape.randomDirectionAmount = 0.08f;

            ParticleSystem.NoiseModule noise = premiumOutflow.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            noise.frequency = 0.55f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule color = premiumOutflow.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.4f);

            ParticleSystem.SizeOverLifetimeModule size = premiumOutflow.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.22f, 1f),
                    new Keyframe(1f, 0.08f)));

            ParticleSystemRenderer renderer = premiumOutflow.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 3;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            outflowObject.SetActive(false);
        }

        private void CreatePremiumWisps()
        {
            GameObject wispObject = new GameObject("Premium Surface Wisps");
            wispObject.transform.SetParent(planetRoot, false);
            premiumWisps = wispObject.AddComponent<ParticleSystem>();
            premiumWisps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = premiumWisps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.032f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 44;

            ParticleSystem.EmissionModule emission = premiumWisps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = premiumWisps.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = planetMeshFilter.sharedMesh;
            shape.normalOffset = 0.018f;
            shape.randomDirectionAmount = 0.12f;

            ParticleSystem.NoiseModule noise = premiumWisps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            noise.frequency = 0.72f;
            noise.scrollSpeed = 0.24f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule color = premiumWisps.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.28f);

            ParticleSystem.SizeOverLifetimeModule size = premiumWisps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.18f),
                    new Keyframe(0.2f, 1f),
                    new Keyframe(0.72f, 0.62f),
                    new Keyframe(1f, 0f)));

            ParticleSystemRenderer renderer = premiumWisps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 3;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            wispObject.SetActive(false);
        }

        private void CreatePremiumAuraCloud()
        {
            GameObject cloudObject = new GameObject("Premium Outer Aura Cloud");
            cloudObject.transform.SetParent(decorRoot, false);
            premiumAuraCloud = cloudObject.AddComponent<ParticleSystem>();
            premiumAuraCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = premiumAuraCloud.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.018f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 14;

            ParticleSystem.EmissionModule emission = premiumAuraCloud.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = premiumAuraCloud.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.88f;
            shape.radiusThickness = 0.28f;

            ParticleSystem.VelocityOverLifetimeModule velocity =
                premiumAuraCloud.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(-0.035f, 0.045f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.025f, 0.03f);

            ParticleSystem.NoiseModule noise = premiumAuraCloud.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
            noise.frequency = 0.28f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule color = premiumAuraCloud.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.08f);

            ParticleSystem.SizeOverLifetimeModule size = premiumAuraCloud.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.3f, 1f),
                    new Keyframe(0.78f, 0.72f),
                    new Keyframe(1f, 0f)));

            ParticleSystemRenderer renderer = premiumAuraCloud.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 0;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            cloudObject.SetActive(false);
        }

        private void CreatePremiumStardust()
        {
            GameObject dustObject = new GameObject("Premium Sparse Stardust");
            dustObject.transform.SetParent(decorRoot, false);
            premiumStardust = dustObject.AddComponent<ParticleSystem>();
            premiumStardust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = premiumStardust.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.025f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.016f, 0.04f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 28;

            ParticleSystem.EmissionModule emission = premiumStardust.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = premiumStardust.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.48f;
            shape.radiusThickness = 0.22f;

            ParticleSystem.VelocityOverLifetimeModule velocity = premiumStardust.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(-0.06f, 0.08f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.04f, 0.05f);

            ParticleSystem.ColorOverLifetimeModule color = premiumStardust.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.7f);

            ParticleSystemRenderer renderer = premiumStardust.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 1;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            dustObject.SetActive(false);
        }

        private void CreatePremiumPulseRings()
        {
            GameObject ringObject = new GameObject("Premium Expansion Rings");
            ringObject.transform.SetParent(decorRoot, false);
            premiumPulseRings = ringObject.AddComponent<ParticleSystem>();
            premiumPulseRings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = premiumPulseRings.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.1f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.65f, 1.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 4;

            ParticleSystem.EmissionModule emission = premiumPulseRings.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = premiumPulseRings.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = premiumPulseRings.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.7f),
                    new Keyframe(1f, 1.85f)));

            ParticleSystem.ColorOverLifetimeModule color = premiumPulseRings.colorOverLifetime;
            color.enabled = true;
            color.color = BuildPremiumGradient(Color.white, Color.white, 0.18f);

            ParticleSystemRenderer renderer = premiumPulseRings.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 0;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white,
                true,
                StarForgeVisualLibrary.ShockwaveRingTexture);
            ringObject.SetActive(false);
        }

        private static Gradient BuildPremiumGradient(Color start, Color end, float peakAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(Color.Lerp(start, end, 0.55f), 0.5f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, 0.22f),
                    new GradientAlphaKey(peakAlpha * 0.72f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
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
            diskMaterial = StarForgeVisualLibrary.CreateBlackHoleDiskMaterial();
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
            dustObject.transform.localPosition = new Vector3(0f, 0f, 0.65f);
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
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.55f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

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

        private void CreateCorona()
        {
            GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Quad);
            corona.name = "Planet Corona";
            Destroy(corona.GetComponent<Collider>());
            corona.transform.SetParent(decorRoot, false);
            corona.transform.localScale = Vector3.one * 3.2f;

            MeshRenderer coronaRenderer = corona.GetComponent<MeshRenderer>();
            coronaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coronaRenderer.receiveShadows = false;
            coronaMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear, true, StarForgeVisualLibrary.SoftCircleTexture);
            coronaRenderer.sharedMaterial = coronaMaterial;

            coronaBillboard = corona.AddComponent<StarForgeBillboard>();
            coronaObject = corona;
            coronaObject.SetActive(false);
        }

        private void CreatePhotonRing()
        {
            photonRingObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            photonRingObject.name = "Photon Ring";
            Destroy(photonRingObject.GetComponent<Collider>());
            photonRingObject.transform.SetParent(decorRoot, false);
            photonRingObject.transform.localScale = Vector3.one * 1.6f;

            MeshRenderer ringRenderer = photonRingObject.GetComponent<MeshRenderer>();
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            photonRingMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear, true, StarForgeVisualLibrary.ShockwaveRingTexture);
            ringRenderer.sharedMaterial = photonRingMaterial;

            photonRingBillboard = photonRingObject.AddComponent<StarForgeBillboard>();
            photonRingObject.SetActive(false);
        }

        private void CreatePulsarJets()
        {
            pulsarJets = new GameObject("Pulsar Jets");
            pulsarJets.transform.SetParent(decorRoot, false);
            pulsarRotator = pulsarJets.AddComponent<StarForgeOrbitRotator>();

            GameObject beamAxis = new GameObject("Beam Axis");
            beamAxis.transform.SetParent(pulsarJets.transform, false);
            beamAxis.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);

            pulsarBeamMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                new Color(0.7f, 0.95f, 1f, 0.35f), true, StarForgeVisualLibrary.SoftCircleTexture);
            if (pulsarBeamMaterial.HasProperty("_Cull"))
            {
                pulsarBeamMaterial.SetInt("_Cull", 0);
            }

            for (int i = 0; i < 4; i++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
                beam.name = "Pulsar Beam " + i;
                Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(beamAxis.transform, false);

                float sign = i < 2 ? 1f : -1f;
                beam.transform.localPosition = new Vector3(0f, sign * 1.7f, 0f);
                beam.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 90f, 0f);
                beam.transform.localScale = new Vector3(0.22f, 2.6f, 1f);

                MeshRenderer beamRenderer = beam.GetComponent<MeshRenderer>();
                beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beamRenderer.receiveShadows = false;
                beamRenderer.sharedMaterial = pulsarBeamMaterial;
            }

            pulsarJets.SetActive(false);
        }

        private void CreateMagnetarArcs()
        {
            magnetarArcs = new GameObject("Magnetar Field");
            magnetarArcs.transform.SetParent(decorRoot, false);

            magnetarMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                new Color(0.75f, 0.5f, 1f, 0.3f), true, Texture2D.whiteTexture);
            if (magnetarMaterial.HasProperty("_Cull"))
            {
                magnetarMaterial.SetInt("_Cull", 0);
            }

            Vector3[] tilts =
            {
                new Vector3(78f, 0f, 0f),
                new Vector3(70f, 55f, 12f),
                new Vector3(64f, -48f, -8f)
            };

            for (int i = 0; i < tilts.Length; i++)
            {
                GameObject arc = new GameObject("Field Arc " + i);
                arc.transform.SetParent(magnetarArcs.transform, false);
                arc.transform.localRotation = Quaternion.Euler(tilts[i]);

                MeshFilter filter = arc.AddComponent<MeshFilter>();
                filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(1.16f, 1.22f, 64, 1f);

                MeshRenderer arcRenderer = arc.AddComponent<MeshRenderer>();
                arcRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arcRenderer.receiveShadows = false;
                arcRenderer.sharedMaterial = magnetarMaterial;
            }

            StarForgeOrbitRotator rotator = magnetarArcs.AddComponent<StarForgeOrbitRotator>();
            rotator.degreesPerSecond = new Vector3(8f, 18f, 6f);
            magnetarArcs.SetActive(false);
        }

        private void CreateSparkles()
        {
            GameObject sparkleObject = new GameObject("Star Sparkles");
            sparkleObject.transform.SetParent(decorRoot, false);
            sparkles = sparkleObject.AddComponent<ParticleSystem>();
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = sparkles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = sparkles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.62f;
            shape.radiusThickness = 0.05f;

            // 한 번 번쩍이고 사라지는 플래시 곡선
            AnimationCurve flash = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule size = sparkles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, flash);

            ParticleSystemRenderer renderer = sparkles.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void CreateNovaShockwave()
        {
            GameObject novaObject = new GameObject("Nova Shockwave");
            novaObject.transform.SetParent(decorRoot, false);
            novaShockwave = novaObject.AddComponent<ParticleSystem>();
            novaShockwave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = novaShockwave.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 2.4f;
            main.startSpeed = 0f;
            main.startSize = 4.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 4;

            ParticleSystem.EmissionModule emission = novaShockwave.emission;
            emission.rateOverTime = 0.5f;

            ParticleSystem.ShapeModule shape = novaShockwave.shape;
            shape.enabled = false;

            // 중심에서 천천히 퍼지며 옅어지는 링
            AnimationCurve expand = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(1f, 1f));
            ParticleSystem.SizeOverLifetimeModule size = novaShockwave.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, expand);

            ParticleSystem.ColorOverLifetimeModule color = novaShockwave.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0.25f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = novaShockwave.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, true, StarForgeVisualLibrary.ShockwaveRingTexture);
        }

        private void UpdateHighTierDecor(float pulse)
        {
            float time = Time.time;
            float pulsarSweep = Mathf.Pow(
                Mathf.Clamp01(Mathf.Sin(time * 5.4f) * 0.5f + 0.5f),
                5f);

            if (planetLight != null)
            {
                if (appliedLevel == 24)
                {
                    planetLight.intensity = planetLightBaseIntensity * (0.78f + pulsarSweep * 0.72f);
                }
                else if (appliedLevel >= 28)
                {
                    float lightPulse = 0.97f + 0.035f * Mathf.Sin(time * 1.45f);
                    if (currentShape == StarForgePlanetShape.Heart)
                    {
                        lightPulse += CalculateHeartbeat(time) * 0.08f;
                    }

                    planetLight.intensity = planetLightBaseIntensity * lightPulse;
                }
                else
                {
                    planetLight.intensity = planetLightBaseIntensity;
                }
            }

            if (diffractionSpikes != null && diffractionSpikes.activeSelf)
            {
                float flare = 0.72f + 0.28f * Mathf.Sin(time * 4.2f);
                Color color = diffractionBaseColor;
                color.a *= flare;
                StarForgeVisualLibrary.SetMaterialColor(diffractionMaterial, color);
                diffractionSpikes.transform.localScale = Vector3.one * (0.96f + flare * 0.07f);
            }

            if (neutronEquator != null && neutronEquator.activeSelf)
            {
                Color color = neutronEquatorBaseColor;
                color.a *= 0.78f + 0.22f * Mathf.Sin(time * 7.5f);
                StarForgeVisualLibrary.SetMaterialColor(neutronEquatorMaterial, color);
            }

            if (pulsarJets != null && pulsarJets.activeSelf)
            {
                Color color = pulsarBeamBaseColor;
                color.a *= 0.18f + pulsarSweep * 1.45f;
                StarForgeVisualLibrary.SetMaterialColor(pulsarBeamMaterial, color);
            }

            if (magnetarArcs != null && magnetarArcs.activeSelf)
            {
                float surge = 0.72f +
                    0.18f * Mathf.Sin(time * 8.5f) +
                    0.1f * Mathf.Sin(time * 19f + 0.8f);
                Color color = magnetarBaseColor;
                color.a *= Mathf.Clamp(surge, 0.45f, 1.15f);
                StarForgeVisualLibrary.SetMaterialColor(magnetarMaterial, color);
            }

            if (quarkGrid != null && quarkGrid.activeSelf)
            {
                float flicker = 0.58f +
                    0.25f * Mathf.Sin(time * 11f) +
                    0.17f * Mathf.Sin(time * 23f + 1.3f);
                Color color = quarkGridBaseColor;
                color.a *= Mathf.Clamp(flicker, 0.2f, 1f);
                StarForgeVisualLibrary.SetMaterialColor(quarkGridMaterial, color);
                quarkGrid.transform.localScale = Vector3.one * (0.96f + 0.05f * pulse);
            }

            if (photonRingObject != null && photonRingObject.activeSelf)
            {
                Color color = appliedLevel >= 30
                    ? new Color(0.78f, 0.88f, 1f, 0.85f)
                    : new Color(1f, 0.78f, 0.45f, 0.6f);
                color.a *= 0.82f + 0.18f * Mathf.Sin(time * 4.8f);
                StarForgeVisualLibrary.SetMaterialColor(photonRingMaterial, color);
            }

            if (singularityLensing != null && singularityLensing.activeSelf)
            {
                for (int i = 0; i < singularityLensMaterials.Length; i++)
                {
                    float irregularPulse = 0.55f +
                        0.25f * Mathf.Sin(time * (1.8f + i * 0.7f) + i * 1.9f) +
                        0.12f * Mathf.Sin(time * 7.3f + i);
                    Color color = new Color(
                        0.45f + i * 0.08f,
                        0.62f + i * 0.07f,
                        1f,
                        Mathf.Clamp(irregularPulse, 0.18f, 0.85f) * (0.24f - i * 0.04f));
                    StarForgeVisualLibrary.SetMaterialColor(singularityLensMaterials[i], color);
                }
            }

            if (premiumRimObject != null && premiumRimObject.activeSelf)
            {
                float heartbeat = currentShape == StarForgePlanetShape.Heart
                    ? CalculateHeartbeat(time)
                    : 0f;
                float breathing = 0.5f + 0.5f * Mathf.Sin(time * (1.1f + (appliedLevel - 28) * 0.14f));
                float rimPulse = 0.9f + breathing * 0.1f + heartbeat * 0.18f;
                float scalePulse = currentShape == StarForgePlanetShape.Heart
                    ? heartbeat * 0.018f
                    : breathing * 0.004f;
                premiumRimObject.transform.localScale =
                    Vector3.one * (premiumRimBaseScale + scalePulse);

                if (premiumRimMaterial != null && premiumRimMaterial.HasProperty("_Pulse"))
                {
                    premiumRimMaterial.SetFloat("_Pulse", rimPulse);
                }

                if (currentShape == StarForgePlanetShape.Heart && runtimeMaterial != null)
                {
                    float boost = 1f + chargeBoost * 1.4f;
                    runtimeMaterial.SetColor(
                        "_EmissionColor",
                        emissionBaseColor * (1f + heartbeat * 0.12f) * boost);
                }
            }

            for (int i = 0; i < premiumOrbitBands.Length; i++)
            {
                GameObject band = premiumOrbitBands[i];
                Material material = premiumOrbitBandMaterials[i];
                if (band == null || !band.activeSelf || material == null)
                {
                    continue;
                }

                float shimmer =
                    0.82f +
                    0.18f * Mathf.Sin(time * (0.72f + i * 0.17f) + i * 1.7f);
                if (currentShape == StarForgePlanetShape.Heart)
                {
                    shimmer += CalculateHeartbeat(time) * 0.12f;
                }

                Color color = premiumOrbitBandBaseColors[i];
                color.a *= shimmer;
                StarForgeVisualLibrary.SetMaterialColor(material, color);
            }

            for (int i = 0; i < premiumEnergyArcs.Length; i++)
            {
                GameObject arc = premiumEnergyArcs[i];
                Material material = premiumEnergyArcMaterials[i];
                if (arc == null || !arc.activeSelf || material == null)
                {
                    continue;
                }

                float flow =
                    0.72f +
                    0.28f * Mathf.Sin(time * (1.15f + i * 0.19f) + i * 2.1f);
                if (currentShape == StarForgePlanetShape.Heart)
                {
                    flow += CalculateHeartbeat(time) * 0.16f;
                }

                Color color = premiumEnergyArcBaseColors[i];
                color.a *= Mathf.Clamp(flow, 0.45f, 1.18f);
                StarForgeVisualLibrary.SetMaterialColor(material, color);
            }

        }

        private static float CalculateHeartbeat(float time)
        {
            float phase = Mathf.Repeat(time * 0.72f, 1f);
            float firstBeat = Mathf.Exp(-Mathf.Pow((phase - 0.12f) / 0.045f, 2f));
            float secondBeat = Mathf.Exp(-Mathf.Pow((phase - 0.28f) / 0.06f, 2f)) * 0.62f;
            return Mathf.Clamp01(firstBeat + secondBeat);
        }

        private void CreateGiantCoronaLayers()
        {
            giantCoronaLayers = new GameObject("Giant Star Corona Layers");
            giantCoronaLayers.transform.SetParent(decorRoot, false);

            Vector3[] rotations =
            {
                new Vector3(74f, 0f, 8f),
                new Vector3(62f, 42f, -12f),
                new Vector3(81f, -35f, 18f)
            };

            for (int i = 0; i < giantCoronaMaterials.Length; i++)
            {
                GameObject layer = new GameObject("Corona Layer " + i);
                layer.transform.SetParent(giantCoronaLayers.transform, false);
                layer.transform.localRotation = Quaternion.Euler(rotations[i]);

                MeshFilter filter = layer.AddComponent<MeshFilter>();
                float inner = 0.68f + i * 0.16f;
                filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(
                    inner,
                    inner + 0.08f + i * 0.025f,
                    72,
                    5f + i);

                MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                giantCoronaMaterials[i] = StarForgeVisualLibrary.CreateParticleMaterial(
                    Color.clear,
                    true,
                    StarForgeVisualLibrary.PlanetRingTexture);
                renderer.sharedMaterial = giantCoronaMaterials[i];

                StarForgeOrbitRotator rotator = layer.AddComponent<StarForgeOrbitRotator>();
                float direction = i % 2 == 0 ? 1f : -1f;
                rotator.degreesPerSecond = new Vector3(
                    direction * (4f + i * 2f),
                    direction * (18f + i * 11f),
                    direction * (3f + i));
            }

            giantCoronaLayers.SetActive(false);
        }

        private void CreateDiffractionSpikes()
        {
            diffractionSpikes = new GameObject("White Dwarf Diffraction");
            diffractionSpikes.transform.SetParent(decorRoot, false);
            StarForgeBillboard billboard = diffractionSpikes.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.46f;

            diffractionMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear,
                true,
                StarForgeVisualLibrary.SoftCircleTexture);

            for (int i = 0; i < 4; i++)
            {
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Quad);
                spike.name = "Diffraction Spike " + i;
                Destroy(spike.GetComponent<Collider>());
                spike.transform.SetParent(diffractionSpikes.transform, false);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                float length = i < 2 ? 4.8f : 3.2f;
                float width = i < 2 ? 0.055f : 0.035f;
                spike.transform.localScale = new Vector3(length, width, 1f);

                MeshRenderer renderer = spike.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = diffractionMaterial;
            }

            diffractionSpikes.SetActive(false);
        }

        private void CreateNeutronEquator()
        {
            neutronEquator = new GameObject("Neutron Equator");
            neutronEquator.transform.SetParent(decorRoot, false);
            neutronEquator.transform.localRotation = Quaternion.Euler(78f, 0f, 12f);

            MeshFilter filter = neutronEquator.AddComponent<MeshFilter>();
            filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(0.66f, 0.73f, 96, 8f);

            MeshRenderer renderer = neutronEquator.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            neutronEquatorMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear,
                true,
                StarForgeVisualLibrary.PlanetRingTexture);
            renderer.sharedMaterial = neutronEquatorMaterial;

            neutronEquatorRotator = neutronEquator.AddComponent<StarForgeOrbitRotator>();
            neutronEquator.SetActive(false);
        }

        private void CreateMagnetarSparks()
        {
            GameObject particleObject = new GameObject("Magnetar Charged Particles");
            particleObject.transform.SetParent(decorRoot, false);
            magnetarSparks = particleObject.AddComponent<ParticleSystem>();
            magnetarSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = magnetarSparks.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 90;

            ParticleSystem.EmissionModule emission = magnetarSparks.emission;
            emission.rateOverTime = 24f;

            ParticleSystem.ShapeModule shape = magnetarSparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.16f;
            shape.radiusThickness = 0.08f;

            ParticleSystem.VelocityOverLifetimeModule velocity = magnetarSparks.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0.35f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.8f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.2f);

            ParticleSystemRenderer renderer = magnetarSparks.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void CreateNovaNebula()
        {
            novaNebula = new GameObject("Supernova Nebula");
            novaNebula.transform.SetParent(decorRoot, false);
            StarForgeBillboard billboard = novaNebula.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.5f;

            Vector3[] offsets =
            {
                new Vector3(-0.18f, 0.08f, 0f),
                new Vector3(0.16f, -0.12f, 0f),
                new Vector3(0.02f, 0.2f, 0f)
            };
            Vector3[] scales =
            {
                new Vector3(3.7f, 2.35f, 1f),
                new Vector3(2.65f, 3.8f, 1f),
                new Vector3(4.2f, 1.8f, 1f)
            };

            for (int i = 0; i < novaNebulaMaterials.Length; i++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloud.name = "Nebula Cloud " + i;
                Destroy(cloud.GetComponent<Collider>());
                cloud.transform.SetParent(novaNebula.transform, false);
                cloud.transform.localPosition = offsets[i];
                cloud.transform.localRotation = Quaternion.Euler(0f, 0f, i * 57f);
                cloud.transform.localScale = scales[i];

                MeshRenderer renderer = cloud.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                novaNebulaMaterials[i] = StarForgeVisualLibrary.CreateParticleMaterial(
                    Color.clear,
                    true,
                    StarForgeVisualLibrary.SoftCircleTexture);
                renderer.sharedMaterial = novaNebulaMaterials[i];

                StarForgeOrbitRotator rotator = cloud.AddComponent<StarForgeOrbitRotator>();
                rotator.degreesPerSecond = new Vector3(0f, 0f, i % 2 == 0 ? 4f + i : -5f - i);
            }

            novaNebula.SetActive(false);
        }

        private void CreateQuarkGrid()
        {
            quarkGrid = new GameObject("Quark Energy Grid");
            quarkGrid.transform.SetParent(decorRoot, false);
            quarkGridMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.clear,
                true,
                Texture2D.whiteTexture);

            Vector3[] rotations =
            {
                new Vector3(90f, 0f, 0f),
                new Vector3(38f, 52f, 0f),
                new Vector3(42f, -48f, 30f)
            };

            for (int i = 0; i < rotations.Length; i++)
            {
                GameObject hex = new GameObject("Hex Grid " + i);
                hex.transform.SetParent(quarkGrid.transform, false);
                hex.transform.localRotation = Quaternion.Euler(rotations[i]);

                MeshFilter filter = hex.AddComponent<MeshFilter>();
                filter.sharedMesh = StarForgeVisualLibrary.CreateRingMesh(
                    0.84f + i * 0.08f,
                    0.87f + i * 0.08f,
                    6,
                    1f);

                MeshRenderer renderer = hex.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = quarkGridMaterial;
            }

            StarForgeOrbitRotator rotator = quarkGrid.AddComponent<StarForgeOrbitRotator>();
            rotator.degreesPerSecond = new Vector3(17f, 31f, 11f);
            quarkGrid.SetActive(false);
        }

        private void CreateQuarkCompression()
        {
            GameObject particleObject = new GameObject("Quark Compression");
            particleObject.transform.SetParent(decorRoot, false);
            quarkCompression = particleObject.AddComponent<ParticleSystem>();
            quarkCompression.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = quarkCompression.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(-0.75f, -0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 100;

            ParticleSystem.EmissionModule emission = quarkCompression.emission;
            emission.rateOverTime = 28f;

            ParticleSystem.ShapeModule shape = quarkCompression.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.45f;
            shape.radiusThickness = 0.08f;

            ParticleSystem.VelocityOverLifetimeModule velocity = quarkCompression.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.85f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystemRenderer renderer = quarkCompression.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void CreateGravityInfall()
        {
            GameObject particleObject = new GameObject("Gravity Infall");
            particleObject.transform.SetParent(decorRoot, false);
            gravityInfall = particleObject.AddComponent<ParticleSystem>();
            gravityInfall.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = gravityInfall.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(-1.15f, -0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 140;

            ParticleSystem.EmissionModule emission = gravityInfall.emission;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = gravityInfall.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2.1f;
            shape.radiusThickness = 0.12f;

            ParticleSystem.VelocityOverLifetimeModule velocity = gravityInfall.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.7f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystem.ColorOverLifetimeModule color = gravityInfall.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.05f, 0f),
                    new GradientAlphaKey(1f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = gravityInfall.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.25f),
                    new Keyframe(0.28f, 1f),
                    new Keyframe(0.78f, 0.58f),
                    new Keyframe(1f, 0.02f)));

            ParticleSystem.TrailModule trails = gravityInfall.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 0.82f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 0.9f, 1f, 0f));

            ParticleSystemRenderer renderer = gravityInfall.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.16f;
            renderer.lengthScale = 1.25f;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
            renderer.trailMaterial = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void CreateBlackHoleLightInfall()
        {
            blackHoleLightInfall = new GameObject("Black Hole Curved Light Infall");
            blackHoleLightInfall.transform.SetParent(decorRoot, false);
            StarForgeBillboard billboard = blackHoleLightInfall.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.16f;

            float[] scales = { 2.45f, 2.55f, 2.65f, 2.75f, 2.85f };
            float[] rotations = { 0f, 47f, -31f, 76f, -68f };
            float[] spinSpeeds = { 5.5f, -3.8f, 2.3f, -1.7f, 1.2f };

            for (int i = 0; i < blackHoleLightInfallMaterials.Length; i++)
            {
                GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Quad);
                layer.name = "Curved Infall Layer " + i;
                Destroy(layer.GetComponent<Collider>());
                layer.transform.SetParent(blackHoleLightInfall.transform, false);
                layer.transform.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
                layer.transform.localScale = Vector3.one * scales[i];
                blackHoleLightInfallLayers[i] = layer;

                MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                Material material = StarForgeVisualLibrary.CreateBlackHoleInfallMaterial();
                blackHoleLightInfallMaterials[i] = material;
                if (material.HasProperty("_Seed"))
                {
                    material.SetFloat("_Seed", 1.7f + i * 3.1f);
                }

                renderer.sharedMaterial = material;

                StarForgeOrbitRotator rotator = layer.AddComponent<StarForgeOrbitRotator>();
                rotator.degreesPerSecond = new Vector3(0f, 0f, spinSpeeds[i]);
            }

            blackHoleLightInfall.SetActive(false);
        }

        private void CreateBlackHoleDopplerGlow()
        {
            blackHoleDopplerGlow = new GameObject("Black Hole Doppler Glow");
            blackHoleDopplerGlow.transform.SetParent(decorRoot, false);
            StarForgeBillboard billboard = blackHoleDopplerGlow.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.34f;

            dopplerBrightMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                new Color(0.75f, 0.9f, 1f, 0.52f),
                true,
                StarForgeVisualLibrary.SoftCircleTexture);
            dopplerDimMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                new Color(1f, 0.28f, 0.1f, 0.18f),
                true,
                StarForgeVisualLibrary.SoftCircleTexture);

            for (int i = 0; i < 2; i++)
            {
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                glow.name = i == 0 ? "Approaching Bright Side" : "Receding Dim Side";
                Destroy(glow.GetComponent<Collider>());
                glow.transform.SetParent(blackHoleDopplerGlow.transform, false);
                glow.transform.localPosition = new Vector3(i == 0 ? -1.05f : 1.05f, 0f, 0f);
                glow.transform.localScale = i == 0
                    ? new Vector3(1.8f, 0.42f, 1f)
                    : new Vector3(1.35f, 0.3f, 1f);

                MeshRenderer renderer = glow.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = i == 0 ? dopplerBrightMaterial : dopplerDimMaterial;
            }

            blackHoleDopplerGlow.SetActive(false);
        }

        private void CreateSingularityLensing()
        {
            singularityLensing = new GameObject("Singularity Broken Lensing");
            singularityLensing.transform.SetParent(decorRoot, false);
            StarForgeBillboard billboard = singularityLensing.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.38f;

            Vector3[] scales =
            {
                new Vector3(2.1f, 1.7f, 1f),
                new Vector3(2.75f, 2.25f, 1f),
                new Vector3(3.55f, 2.8f, 1f)
            };

            for (int i = 0; i < singularityLensMaterials.Length; i++)
            {
                GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Quad);
                lens.name = "Broken Lensing Ring " + i;
                Destroy(lens.GetComponent<Collider>());
                lens.transform.SetParent(singularityLensing.transform, false);
                lens.transform.localPosition = new Vector3(
                    (i - 1) * 0.06f,
                    i % 2 == 0 ? 0.04f : -0.05f,
                    0f);
                lens.transform.localRotation = Quaternion.Euler(0f, 0f, i * 37f);
                lens.transform.localScale = scales[i];

                MeshRenderer renderer = lens.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                singularityLensMaterials[i] = StarForgeVisualLibrary.CreateParticleMaterial(
                    Color.clear,
                    true,
                    StarForgeVisualLibrary.ShockwaveRingTexture);
                renderer.sharedMaterial = singularityLensMaterials[i];

                StarForgeOrbitRotator rotator = lens.AddComponent<StarForgeOrbitRotator>();
                rotator.degreesPerSecond = new Vector3(0f, 0f, i % 2 == 0 ? 4f + i : -6f);
            }

            singularityLensing.SetActive(false);
        }

        private void CreateStageArrivalBurst()
        {
            GameObject particleObject = new GameObject("High Tier Stage Arrival");
            particleObject.transform.SetParent(decorRoot, false);
            stageArrivalBurst = particleObject.AddComponent<ParticleSystem>();
            stageArrivalBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = stageArrivalBurst.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 180;

            ParticleSystem.EmissionModule emission = stageArrivalBurst.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = stageArrivalBurst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule color = stageArrivalBurst.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.65f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = stageArrivalBurst.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 0.8f;
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
        }

        private void PlayStageArrival(int level, Color baseColor)
        {
            if (level < 20)
            {
                return;
            }

            Color burstColor;
            int burstCount;
            float speedMin;
            float speedMax;

            if (level <= 21)
            {
                burstColor = Color.Lerp(baseColor, new Color(1f, 0.75f, 0.18f), level == 21 ? 0.65f : 0.35f);
                burstCount = level == 21 ? 90 : 65;
                speedMin = 2f;
                speedMax = level == 21 ? 5.8f : 4.5f;
            }
            else if (level <= 24)
            {
                burstColor = new Color(0.72f, 0.92f, 1f);
                burstCount = level == 22 ? 45 : 70;
                speedMin = 1.6f;
                speedMax = 4.4f;
            }
            else if (level <= 27)
            {
                burstColor = level == 26
                    ? new Color(1f, 0.45f, 0.68f)
                    : new Color(0.7f, 0.45f, 1f);
                burstCount = level == 26 ? 120 : 85;
                speedMin = 2f;
                speedMax = level == 26 ? 7f : 5f;
            }
            else
            {
                burstColor = level >= 30
                    ? new Color(0.55f, 0.72f, 1f)
                    : level >= 29
                        ? new Color(1f, 0.68f, 0.28f)
                        : new Color(0.62f, 0.38f, 1f);
                burstCount = level >= 30 ? 35 : 70;
                speedMin = level >= 30 ? 0.6f : 1.2f;
                speedMax = level >= 30 ? 2.2f : 4.2f;
            }

            ParticleSystem.MainModule main = stageArrivalBurst.main;
            main.startColor = burstColor;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            stageArrivalBurst.Clear(true);
            stageArrivalBurst.Play(true);
            stageArrivalBurst.Emit(burstCount);

            switch (level)
            {
                case 20:
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.28f, 0.08f, 0.75f), 0.4f, 3.4f, 0.65f, 0f));
                    break;
                case 21:
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.62f, 0.12f, 0.85f), 0.35f, 4.4f, 0.7f, 0f));
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.95f, 0.6f, 0.65f), 0.5f, 3.5f, 0.55f, 0.12f));
                    break;
                case 22:
                    StartCoroutine(AnimateArrivalRing(new Color(0.85f, 0.94f, 1f, 0.9f), 4f, 0.55f, 0.55f, 0f));
                    break;
                case 23:
                    StartCoroutine(AnimateArrivalRing(new Color(0.55f, 0.92f, 1f, 0.75f), 3f, 0.7f, 0.5f, 0f));
                    break;
                case 24:
                    StartCoroutine(AnimateArrivalRing(new Color(0.55f, 0.95f, 1f, 0.85f), 0.45f, 3.4f, 0.45f, 0f));
                    StartCoroutine(AnimateArrivalRing(new Color(0.8f, 1f, 1f, 0.55f), 0.45f, 3.4f, 0.45f, 0.18f));
                    break;
                case 25:
                    StartCoroutine(AnimateArrivalRing(new Color(0.75f, 0.4f, 1f, 0.85f), 0.5f, 3.7f, 0.6f, 0f));
                    break;
                case 26:
                    novaShockwave.Emit(2);
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.4f, 0.65f, 0.9f), 0.4f, 5f, 0.75f, 0f));
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.75f, 0.3f, 0.7f), 0.5f, 4.2f, 0.65f, 0.1f));
                    break;
                case 27:
                    StartCoroutine(AnimateArrivalRing(new Color(0.65f, 0.42f, 1f, 0.85f), 4f, 0.65f, 0.6f, 0f));
                    break;
                case 28:
                    StartCoroutine(AnimateArrivalRing(new Color(0.5f, 0.25f, 1f, 0.8f), 4.5f, 0.45f, 0.7f, 0f));
                    break;
                case 29:
                    StartCoroutine(AnimateArrivalRing(new Color(1f, 0.65f, 0.2f, 0.8f), 0.55f, 5.5f, 0.8f, 0f));
                    break;
                case 30:
                    StartCoroutine(AnimateArrivalRing(new Color(0.55f, 0.72f, 1f, 0.9f), 5.5f, 0.3f, 0.8f, 0f));
                    StartCoroutine(AnimateArrivalRing(new Color(0.75f, 0.88f, 1f, 0.65f), 0.25f, 4.8f, 0.9f, 0.42f));
                    break;
            }
        }

        private IEnumerator AnimateArrivalRing(
            Color color,
            float startScale,
            float endScale,
            float duration,
            float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Stage Arrival Ring";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material material = StarForgeVisualLibrary.CreateParticleMaterial(
                color,
                true,
                StarForgeVisualLibrary.ShockwaveRingTexture);
            renderer.sharedMaterial = material;

            StarForgeBillboard billboard = ring.AddComponent<StarForgeBillboard>();
            billboard.depthOffset = 0.7f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                float scale = Mathf.Lerp(startScale, endScale, eased);
                float alpha = color.a * Mathf.Pow(1f - normalized, 1.5f);
                ring.transform.localScale = Vector3.one * scale;
                StarForgeVisualLibrary.SetMaterialColor(
                    material,
                    new Color(color.r, color.g, color.b, alpha));

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(ring);
            Destroy(material);
        }

        private static Color ParseColor(string html, Color fallback)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(html, out color) ? color : fallback;
        }
    }
}
