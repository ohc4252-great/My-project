using System.Collections;
using StarForge.Core;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgeEffectController : MonoBehaviour
    {
        [Header("Optional Overrides (비워두면 자동 생성)")]
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem greatSuccessParticles;
        [SerializeField] private ParticleSystem failureParticles;
        [SerializeField] private ParticleSystem fractureParticles;
        [SerializeField] private ParticleSystem destroyedParticles;

        private ParticleSystem successBurst;
        private ParticleSystem successFountain;
        private ParticleSystem greatBurst;
        private ParticleSystem greatStreaks;
        private ParticleSystem failureSmoke;
        private ParticleSystem fractureSparks;
        private ParticleSystem fractureEmbers;
        private ParticleSystem destroyedCore;
        private ParticleSystem destroyedStreaks;
        private ParticleSystem destroyedDebris;
        private ParticleSystem destroyedSmoke;
        private ParticleSystem chargeGather;
        private ParticleSystem chargeDebris;
        private ParticleSystem impactSparks;

        private Light flashLight;
        private readonly Material[] shardTierMaterials = new Material[5];
        private Material[] rockPaletteMaterials;
        private Material shardTrailMaterial;
        private Coroutine flashRoutine;
        private bool built;

        public void EnsureCreated()
        {
            if (built)
            {
                return;
            }

            built = true;

            successBurst = CreateSystem("Success Burst", new Color(0.45f, 0.95f, 1f), true, 0.7f, 3.2f, 0.16f);

            successFountain = CreateSystem("Success Fountain", new Color(0.7f, 1f, 1f), true, 0.95f, 3.8f, 0.12f, -0.12f);
            successFountain.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.ShapeModule fountainShape = successFountain.shape;
            fountainShape.shapeType = ParticleSystemShapeType.Cone;
            fountainShape.angle = 24f;
            fountainShape.radius = 0.12f;

            greatBurst = CreateSystem("Great Success Burst", new Color(1f, 0.84f, 0.3f), true, 0.9f, 4.5f, 0.2f);

            greatStreaks = CreateSystem("Great Success Streaks", new Color(1f, 0.9f, 0.5f), true, 0.5f, 8f, 0.1f);
            MakeStretched(greatStreaks);

            failureSmoke = CreateSystem("Failure Smoke", new Color(0.5f, 0.5f, 0.55f, 0.7f), false, 1.3f, 0.8f, 0.5f, -0.04f);
            MakeGrowing(failureSmoke);

            fractureSparks = CreateSystem("Fracture Sparks", new Color(1f, 0.6f, 0.15f), true, 0.55f, 5.5f, 0.09f, 0.25f);
            MakeStretched(fractureSparks);

            fractureEmbers = CreateSystem("Fracture Embers", new Color(1f, 0.35f, 0.1f), true, 1.2f, 1.4f, 0.07f, 0.35f);

            destroyedCore = CreateSystem("Destroyed Core", new Color(1f, 0.55f, 0.2f), true, 0.8f, 6.5f, 0.3f);

            destroyedStreaks = CreateSystem("Destroyed Streaks", new Color(1f, 0.8f, 0.45f), true, 0.6f, 11f, 0.12f);
            MakeStretched(destroyedStreaks);

            destroyedDebris = CreateSystem("Destroyed Debris", new Color(0.45f, 0.38f, 0.32f), false, 1.5f, 5f, 0.14f, 0.55f);
            ConfigureDebris(destroyedDebris);

            destroyedSmoke = CreateSystem("Destroyed Smoke", new Color(0.35f, 0.3f, 0.3f, 0.6f), false, 1.8f, 1.6f, 0.7f, -0.05f);
            MakeGrowing(destroyedSmoke);

            impactSparks = CreateSystem("Impact Sparks", new Color(1f, 0.95f, 0.8f), true, 0.3f, 2f, 0.07f);

            chargeGather = CreateSystem("Charge Gather", new Color(0.6f, 0.85f, 1f), true, 0.55f, -3.4f, 0.07f);
            ParticleSystem.MainModule gatherMain = chargeGather.main;
            gatherMain.loop = true;
            gatherMain.startLifetime = 0.55f;
            ParticleSystem.EmissionModule gatherEmission = chargeGather.emission;
            gatherEmission.rateOverTime = 90f;
            ParticleSystem.ShapeModule gatherShape = chargeGather.shape;
            gatherShape.shapeType = ParticleSystemShapeType.Sphere;
            gatherShape.radius = 2f;
            gatherShape.radiusThickness = 0f;
            ParticleSystem.VelocityOverLifetimeModule gatherVelocity = chargeGather.velocityOverLifetime;
            gatherVelocity.enabled = true;
            gatherVelocity.orbitalY = new ParticleSystem.MinMaxCurve(0.4f);
            chargeGather.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            chargeDebris = CreateSystem("Charge Debris", new Color(0.62f, 0.55f, 0.45f), false, 0.8f, -3.2f, 0.12f);
            ParticleSystem.MainModule debrisMain = chargeDebris.main;
            debrisMain.loop = true;
            debrisMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            debrisMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.EmissionModule debrisEmission = chargeDebris.emission;
            debrisEmission.rateOverTime = 24f;
            ParticleSystem.ShapeModule debrisShape = chargeDebris.shape;
            debrisShape.shapeType = ParticleSystemShapeType.Sphere;
            debrisShape.radius = 2.1f;
            debrisShape.radiusThickness = 0f;
            ParticleSystem.VelocityOverLifetimeModule debrisVelocity = chargeDebris.velocityOverLifetime;
            debrisVelocity.enabled = true;
            debrisVelocity.orbitalY = new ParticleSystem.MinMaxCurve(0.6f);
            ParticleSystem.RotationOverLifetimeModule debrisRotation = chargeDebris.rotationOverLifetime;
            debrisRotation.enabled = true;
            debrisRotation.z = new ParticleSystem.MinMaxCurve(-7f, 7f);
            ParticleSystemRenderer debrisRenderer = chargeDebris.GetComponent<ParticleSystemRenderer>();
            debrisRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            debrisRenderer.SetMeshes(StarForgeVisualLibrary.GetRockMeshes());
            debrisRenderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, false, Texture2D.whiteTexture);
            chargeDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            GameObject lightObject = new GameObject("Effect Flash Light");
            lightObject.transform.SetParent(transform, false);
            flashLight = lightObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 14f;
            flashLight.intensity = 0f;
            flashLight.shadows = LightShadows.None;

            EnsureShardMaterials();
        }

        public IEnumerator PlayShardFly(Transform target, int shardCount, float duration, int level = 0)
        {
            EnsureCreated();

            if (target == null)
            {
                yield break;
            }

            int tier = StarForgeVisualLibrary.GetLevelTier(level);
            Color gatherColor;
            Color trailColor;
            Color lightColor;
            Color debrisColor;

            switch (tier)
            {
                case 1:
                    gatherColor = new Color(0.85f, 0.78f, 0.6f);
                    trailColor = new Color(0.85f, 0.7f, 0.5f, 0.7f);
                    lightColor = new Color(1f, 0.85f, 0.65f);
                    debrisColor = new Color(0.6f, 0.52f, 0.42f);
                    break;
                case 2:
                    gatherColor = new Color(1f, 0.8f, 0.35f);
                    trailColor = new Color(1f, 0.82f, 0.4f, 0.8f);
                    lightColor = new Color(1f, 0.8f, 0.4f);
                    debrisColor = new Color(0.95f, 0.7f, 0.3f);
                    break;
                case 3:
                    gatherColor = new Color(0.65f, 0.4f, 1f);
                    trailColor = new Color(0.6f, 0.35f, 1f, 0.8f);
                    lightColor = new Color(0.65f, 0.45f, 1f);
                    debrisColor = new Color(0.32f, 0.24f, 0.48f);
                    break;
                case 4:
                    gatherColor = new Color(0.7f, 0.9f, 1f);
                    trailColor = new Color(0.8f, 0.95f, 1f, 0.85f);
                    lightColor = new Color(0.85f, 0.95f, 1f);
                    debrisColor = new Color(0.75f, 0.85f, 1f);
                    break;
                default:
                    gatherColor = new Color(0.6f, 0.85f, 1f);
                    trailColor = new Color(1f, 0.8f, 0.5f, 0.7f);
                    lightColor = new Color(0.75f, 0.9f, 1f);
                    debrisColor = new Color(0.62f, 0.55f, 0.45f);
                    break;
            }

            ParticleSystem.MainModule gatherMain = chargeGather.main;
            gatherMain.startColor = gatherColor;
            ParticleSystem.EmissionModule gatherEmission = chargeGather.emission;
            gatherEmission.rateOverTime = 70f + tier * 35f;
            ParticleSystem.VelocityOverLifetimeModule gatherVelocity = chargeGather.velocityOverLifetime;
            gatherVelocity.orbitalY = new ParticleSystem.MinMaxCurve(0.4f + tier * 0.7f);
            chargeGather.transform.position = target.position;
            chargeGather.Play();

            Camera effectCamera = Camera.main;
            float debrisRate = 0f;
            float debrisSizeMin = 0f;
            float debrisSizeMax = 0f;
            float debrisAccumulator = 1f;
            Color debrisColorMin = debrisColor;
            Color debrisColorMax = debrisColor;

            bool useRockDebris = tier >= 1;
            if (useRockDebris)
            {
                ParticleSystem.MainModule debrisMain = chargeDebris.main;
                debrisColorMin = tier <= 1 ? new Color(0.12f, 0.11f, 0.11f) : debrisColor;
                debrisColorMax = tier <= 1 ? new Color(0.5f, 0.38f, 0.26f) : debrisColor;
                debrisSizeMin = 0.06f + tier * 0.02f;
                debrisSizeMax = 0.12f + tier * 0.03f;
                debrisRate = 12f + tier * 9f;
                debrisMain.startColor = new ParticleSystem.MinMaxGradient(debrisColorMin, debrisColorMax);
                debrisMain.startSize = new ParticleSystem.MinMaxCurve(debrisSizeMin, debrisSizeMax);
                ParticleSystem.EmissionModule debrisEmission = chargeDebris.emission;
                debrisEmission.rateOverTime = 0f;
                ParticleSystem.VelocityOverLifetimeModule debrisVelocity = chargeDebris.velocityOverLifetime;
                debrisVelocity.orbitalY = new ParticleSystem.MinMaxCurve(0.15f + tier * 0.1f);
                chargeDebris.transform.position = target.position;
                chargeDebris.Clear(true);
                chargeDebris.Play();
            }

            ParticleSystem.MainModule impactMain = impactSparks.main;
            impactMain.startColor = gatherColor;

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            flashLight.color = lightColor;
            flashLight.transform.position = target.position + new Vector3(0f, 0.2f, -1.4f);
            float lightPeak = 1.4f + tier * 0.5f;

            Material shardTierMaterial = GetShardTierMaterial(tier);
            float shardScaleBoost = 1f + tier * 0.12f;

            int count = Mathf.Max(1, shardCount);
            float stagger = duration * 0.45f;
            float flight = Mathf.Max(0.12f, duration - stagger);

            GameObject[] shards = new GameObject[count];
            Vector3[] starts = new Vector3[count];
            Vector3[] controls = new Vector3[count];
            Vector3[] offsets = new Vector3[count];
            Vector3[] tumbles = new Vector3[count];
            float[] delays = new float[count];
            bool[] arrived = new bool[count];

            for (int i = 0; i < count; i++)
            {
                shards[i] = new GameObject("Flying Meteor Fragment", typeof(MeshFilter), typeof(MeshRenderer));
                shards[i].GetComponent<MeshFilter>().sharedMesh = StarForgeVisualLibrary.GetRandomRockMesh();
                shards[i].transform.localScale = Vector3.one * Random.Range(0.09f, 0.18f) * shardScaleBoost;
                shards[i].transform.rotation = Random.rotation;
                tumbles[i] = Random.onUnitSphere * Random.Range(90f, 280f);

                Renderer shardRenderer = shards[i].GetComponent<Renderer>();
                if (shardRenderer != null)
                {
                    shardRenderer.sharedMaterial = tier <= 1 ? GetRockPaletteMaterial() : shardTierMaterial;
                    shardRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                if (tier >= 2)
                {
                    TrailRenderer trail = shards[i].AddComponent<TrailRenderer>();
                    trail.time = 0.1f;
                    trail.startWidth = 0.028f;
                    trail.endWidth = 0f;
                    trail.minVertexDistance = 0.03f;
                    trail.numCapVertices = 2;
                    trail.sharedMaterial = shardTrailMaterial;
                    trail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * 0.45f);
                    trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                    trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                starts[i] = GetOffscreenStart(effectCamera, target.position, 0.16f);
                Vector3 mid = (starts[i] + target.position) * 0.5f;
                controls[i] = mid + Random.insideUnitSphere * 1.2f;
                offsets[i] = Random.insideUnitSphere * 0.16f;
                delays[i] = count > 1 ? (i / (float)(count - 1)) * stagger : 0f;
                shards[i].transform.position = starts[i];
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                flashLight.intensity = Mathf.Lerp(0.2f, lightPeak, elapsed / duration);

                if (useRockDebris)
                {
                    debrisAccumulator += debrisRate * Time.deltaTime;
                    int debrisCount = Mathf.FloorToInt(debrisAccumulator);
                    debrisAccumulator -= debrisCount;

                    for (int i = 0; i < debrisCount; i++)
                    {
                        EmitOffscreenDebris(
                            chargeDebris,
                            effectCamera,
                            target.position,
                            debrisColorMin,
                            debrisColorMax,
                            debrisSizeMin,
                            debrisSizeMax);
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (shards[i] == null)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01((elapsed - delays[i]) / flight);
                    float eased = t * t;
                    Vector3 end = target.position + offsets[i];
                    Vector3 position = EvaluateBezier(starts[i], controls[i], end, eased);

                    if (t >= 1f && !arrived[i])
                    {
                        arrived[i] = true;
                        impactSparks.transform.position = end;
                        EmitNow(impactSparks, 3);
                        Destroy(shards[i]);
                    }
                    else
                    {
                        shards[i].transform.position = position;
                        shards[i].transform.Rotate(tumbles[i] * Time.deltaTime, Space.Self);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (shards[i] != null)
                {
                    Destroy(shards[i]);
                }
            }

            chargeGather.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            chargeDebris.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            flashLight.intensity = 0f;
        }

        public void PlayResult(StarForgeResultKind resultKind, Vector3 position, float intensity = 1f)
        {
            EnsureCreated();

            float power = Mathf.Max(0.7f, intensity);

            switch (resultKind)
            {
                case StarForgeResultKind.Success:
                    if (successParticles != null)
                    {
                        PlayAt(successParticles, position);
                        break;
                    }

                    PlaySuccess(position, power);
                    break;
                case StarForgeResultKind.GreatSuccess:
                    if (greatSuccessParticles != null)
                    {
                        PlayAt(greatSuccessParticles, position);
                        break;
                    }

                    PlayGreatSuccess(position, power);
                    break;
                case StarForgeResultKind.Failure:
                    if (failureParticles != null)
                    {
                        PlayAt(failureParticles, position);
                        break;
                    }

                    PlayFailure(position, power);
                    break;
                case StarForgeResultKind.Fracture:
                    if (fractureParticles != null)
                    {
                        PlayAt(fractureParticles, position);
                        break;
                    }

                    PlayFracture(position, power);
                    break;
                case StarForgeResultKind.Destroyed:
                    if (destroyedParticles != null)
                    {
                        PlayAt(destroyedParticles, position);
                        break;
                    }

                    PlayDestroyed(position, power);
                    break;
            }
        }

        private void PlaySuccess(Vector3 position, float power)
        {
            EmitAt(successBurst, position, Mathf.RoundToInt(34f * power));
            EmitAt(successFountain, position, Mathf.RoundToInt(20f * power));
            StartCoroutine(ShockwaveRoutine(position, new Color(0.5f, 0.95f, 1f, 0.85f), 2.6f * power, 0.5f, 0f));
            Flash(position, new Color(0.55f, 0.95f, 1f), 2.6f, 0.4f);
        }

        private void PlayGreatSuccess(Vector3 position, float power)
        {
            EmitAt(greatBurst, position, Mathf.RoundToInt(70f * power));
            EmitAt(greatStreaks, position, Mathf.RoundToInt(30f * power));
            EmitAt(successFountain, position, 26);
            StartCoroutine(ShockwaveRoutine(position, new Color(1f, 0.85f, 0.4f, 0.9f), 3.6f * power, 0.6f, 0f));
            StartCoroutine(ShockwaveRoutine(position, new Color(1f, 1f, 0.9f, 0.7f), 4.2f * power, 0.7f, 0.12f));
            Flash(position, new Color(1f, 0.85f, 0.45f), 4.5f, 0.65f);
        }

        private void PlayFailure(Vector3 position, float power)
        {
            EmitAt(failureSmoke, position, Mathf.RoundToInt(12f * Mathf.Min(power, 1.4f)));
        }

        private void PlayFracture(Vector3 position, float power)
        {
            EmitAt(fractureSparks, position, Mathf.RoundToInt(26f * power));
            EmitAt(fractureEmbers, position, 14);
            Flash(position, new Color(1f, 0.4f, 0.15f), 2f, 0.3f);
        }

        private void PlayDestroyed(Vector3 position, float power)
        {
            EmitAt(destroyedCore, position, Mathf.RoundToInt(70f * power));
            EmitAt(destroyedStreaks, position, Mathf.RoundToInt(36f * power));
            EmitAt(destroyedDebris, position, 20);
            EmitAt(destroyedSmoke, position, 18);
            StartCoroutine(ShockwaveRoutine(position, new Color(1f, 0.6f, 0.25f, 0.95f), 4.5f * power, 0.65f, 0f));
            StartCoroutine(ShockwaveRoutine(position, new Color(1f, 0.9f, 0.7f, 0.7f), 3f * power, 0.4f, 0.06f));
            Flash(position, new Color(1f, 0.75f, 0.5f), 6.5f, 0.75f);
        }

        private ParticleSystem CreateSystem(
            string objectName,
            Color color,
            bool additive,
            float lifetime,
            float speed,
            float size,
            float gravity = 0f)
        {
            GameObject particleObject = new GameObject(objectName);
            particleObject.transform.SetParent(transform, false);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = speed >= 0f
                ? new ParticleSystem.MinMaxCurve(speed * 0.55f, speed)
                : new ParticleSystem.MinMaxCurve(speed, speed * 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 512;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, additive);

            return particles;
        }

        private static void MakeStretched(ParticleSystem particles)
        {
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 1.2f;
        }

        private static void MakeGrowing(ParticleSystem particles)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
        }

        private void ConfigureDebris(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            ParticleSystem.TrailModule trails = particles.trails;
            trails.enabled = true;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.3f);
            trails.dieWithParticles = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.4f);
            trails.inheritParticleColor = false;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.SetMeshes(StarForgeVisualLibrary.GetRockMeshes());
            renderer.material = StarForgeVisualLibrary.CreateParticleMaterial(
                Color.white, false, Texture2D.whiteTexture);
            renderer.trailMaterial = StarForgeVisualLibrary.CreateParticleMaterial(
                new Color(1f, 0.6f, 0.25f, 0.8f), true);
        }

        private static void EmitAt(ParticleSystem particles, Vector3 position, int count)
        {
            if (particles == null || count <= 0)
            {
                return;
            }

            particles.transform.position = position;
            EmitNow(particles, count);
        }

        private static void EmitNow(ParticleSystem particles, int count)
        {
            if (!particles.isPlaying)
            {
                particles.Play();
            }

            particles.Emit(count);
        }

        private static void PlayAt(ParticleSystem particles, Vector3 position)
        {
            if (particles == null)
            {
                return;
            }

            particles.transform.position = position;
            particles.Clear(true);
            particles.Play(true);
        }

        private void Flash(Vector3 position, Color color, float peak, float duration)
        {
            flashLight.transform.position = position + new Vector3(0f, 0.2f, -1.4f);
            flashLight.color = color;

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(peak, duration));
        }

        private IEnumerator FlashRoutine(float peak, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float envelope = normalized < 0.15f
                    ? normalized / 0.15f
                    : Mathf.Pow(1f - (normalized - 0.15f) / 0.85f, 2f);
                flashLight.intensity = peak * envelope;

                elapsed += Time.deltaTime;
                yield return null;
            }

            flashLight.intensity = 0f;
            flashRoutine = null;
        }

        private IEnumerator ShockwaveRoutine(Vector3 position, Color color, float maxScale, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Shockwave";
            Destroy(quad.GetComponent<Collider>());

            MeshRenderer quadRenderer = quad.GetComponent<MeshRenderer>();
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            Material material = StarForgeVisualLibrary.CreateParticleMaterial(
                color, true, StarForgeVisualLibrary.ShockwaveRingTexture);
            quadRenderer.material = material;

            Camera mainCamera = Camera.main;
            Vector3 towardCamera = mainCamera != null ? -mainCamera.transform.forward : Vector3.back;
            quad.transform.position = position + towardCamera * 0.8f;
            quad.AddComponent<StarForgeBillboard>();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float scale = Mathf.Lerp(0.3f, maxScale, 1f - Mathf.Pow(1f - normalized, 3f));
                float alpha = color.a * Mathf.Pow(1f - normalized, 1.6f);

                quad.transform.localScale = Vector3.one * scale;
                StarForgeVisualLibrary.SetMaterialColor(material, new Color(color.r, color.g, color.b, alpha));

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(quad);
            Destroy(material);
        }

        private static Vector3 EvaluateBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private static Vector3 GetOffscreenStart(Camera camera, Vector3 targetPosition, float viewportMargin)
        {
            if (camera == null)
            {
                Vector2 fallbackDirection = Random.insideUnitCircle.normalized;
                if (fallbackDirection.sqrMagnitude < 0.01f)
                {
                    fallbackDirection = Vector2.down;
                }

                return targetPosition + new Vector3(fallbackDirection.x, fallbackDirection.y, 0f) * 6f;
            }

            float depth = Vector3.Dot(targetPosition - camera.transform.position, camera.transform.forward);
            depth = Mathf.Max(depth, camera.nearClipPlane + 0.1f);

            Vector2 viewportPosition;
            switch (Random.Range(0, 4))
            {
                case 0:
                    viewportPosition = new Vector2(-viewportMargin, Random.Range(0f, 1f));
                    break;
                case 1:
                    viewportPosition = new Vector2(1f + viewportMargin, Random.Range(0f, 1f));
                    break;
                case 2:
                    viewportPosition = new Vector2(Random.Range(0f, 1f), -viewportMargin);
                    break;
                default:
                    viewportPosition = new Vector2(Random.Range(0f, 1f), 1f + viewportMargin);
                    break;
            }

            return camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
        }

        private static void EmitOffscreenDebris(
            ParticleSystem particles,
            Camera camera,
            Vector3 targetPosition,
            Color colorMin,
            Color colorMax,
            float sizeMin,
            float sizeMax)
        {
            float lifetime = Random.Range(0.55f, 0.85f);
            Vector3 start = GetOffscreenStart(camera, targetPosition, 0.12f);
            Vector3 end = targetPosition + Random.insideUnitSphere * 0.12f;
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = start,
                velocity = (end - start) / lifetime,
                startLifetime = lifetime,
                startSize = Random.Range(sizeMin, sizeMax),
                startColor = Color.Lerp(colorMin, colorMax, Random.value)
            };

            particles.Emit(emitParams, 1);
        }

        private void EnsureShardMaterials()
        {
            if (shardTrailMaterial == null)
            {
                shardTrailMaterial = StarForgeVisualLibrary.CreateParticleMaterial(Color.white, true);
            }
        }

        private Material GetRockPaletteMaterial()
        {
            if (rockPaletteMaterials == null)
            {
                Color[] paletteColors =
                {
                    new Color(0.07f, 0.07f, 0.08f),
                    new Color(0.14f, 0.13f, 0.13f),
                    new Color(0.28f, 0.28f, 0.3f),
                    new Color(0.42f, 0.41f, 0.4f),
                    new Color(0.34f, 0.26f, 0.19f),
                    new Color(0.48f, 0.37f, 0.26f)
                };

                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                rockPaletteMaterials = new Material[paletteColors.Length];
                for (int i = 0; i < paletteColors.Length; i++)
                {
                    Material material = new Material(shader);
                    material.name = "StarForge Rock Palette " + i;
                    material.color = paletteColors[i];
                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", 0.12f);
                    }

                    rockPaletteMaterials[i] = material;
                }
            }

            return rockPaletteMaterials[Random.Range(0, rockPaletteMaterials.Length)];
        }

        private Material GetShardTierMaterial(int tier)
        {
            tier = Mathf.Clamp(tier, 0, shardTierMaterials.Length - 1);
            if (shardTierMaterials[tier] != null)
            {
                return shardTierMaterials[tier];
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = "StarForge Shard Tier " + tier;

            Color baseColor;
            Color emissionColor = Color.black;

            switch (tier)
            {
                case 1:
                    baseColor = new Color(0.55f, 0.48f, 0.4f);
                    break;
                case 2:
                    baseColor = new Color(0.9f, 0.72f, 0.4f);
                    emissionColor = new Color(1f, 0.65f, 0.2f) * 1.6f;
                    break;
                case 3:
                    baseColor = new Color(0.16f, 0.13f, 0.22f);
                    emissionColor = new Color(0.5f, 0.28f, 1f) * 1.4f;
                    break;
                case 4:
                    baseColor = new Color(0.85f, 0.9f, 1f);
                    emissionColor = new Color(0.55f, 0.75f, 1f) * 2.2f;
                    break;
                default:
                    baseColor = new Color(0.8f, 0.72f, 0.56f);
                    break;
            }

            material.color = baseColor;
            if (emissionColor != Color.black)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            shardTierMaterials[tier] = material;
            return material;
        }
    }
}
