using StarForge.Core;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgeAudioController : MonoBehaviour
    {
        private const int PremiumAudioStartLevel = 28;
        private const float PremiumLowPassCutoff = 2400f;
        private const float LowTierSuccessHighPassCutoff = 360f;
        private const float LowTierSuccessVolumeMultiplier = 1.38f;
        private const float LowTierSuccessPitch = 1.08f;
        private const float LowTierGreatSuccessPitch = 1.11f;
        private const float ChargeVolumeMultiplier = 1.45f;
        private const float MainBgmVolume = 0.42f;
        private const float MiningBgmVolume = 0.48f;
        private static bool startupMainBgmSuppressed;

        private AudioSource mainBgmSource;
        private AudioSource miningBgmSource;
        private AudioSource chargeSource;
        private AudioSource resultSource;
        private AudioLowPassFilter chargeLowPass;
        private AudioLowPassFilter resultLowPass;
        private AudioHighPassFilter resultPresenceHighPass;

        private AudioClip[] chargeClips;
        private AudioClip[] successClips;
        private AudioClip successFallback;
        private AudioClip crackClip;
        private AudioClip failureClip;
        private AudioClip breakClip;
        private bool built;
        private bool soundEnabled = true;
        private bool miningModeActive;
        private bool enhancementCinematicActive;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private float enhancementAudioMuteUntil;

        public static void SetStartupMainBgmSuppressed(bool suppressed)
        {
            startupMainBgmSuppressed = suppressed;
            StarForgeAudioController[] controllers =
                FindObjectsByType<StarForgeAudioController>(
                    FindObjectsInactive.Exclude);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                {
                    controllers[i].RefreshBgmVolumes();
                }
            }
        }

        public bool SoundEnabled
        {
            get { return soundEnabled; }
            set
            {
                soundEnabled = value;
                if (!soundEnabled)
                {
                    StopCharge();
                    if (resultSource != null)
                    {
                        resultSource.Stop();
                    }

                    enhancementAudioMuteUntil = 0f;
                    enhancementCinematicActive = false;
                }

                RefreshBgmVolumes();
            }
        }

        private void Update()
        {
            if (!built)
            {
                return;
            }

            RefreshBgmVolumes();
        }

        public void EnsureCreated()
        {
            if (built)
            {
                return;
            }

            built = true;

            mainBgmSource = CreateLoopingSource(
                "Main BGM",
                LoadFirstAudioClip("Audio/main", "Audio/Main"));
            miningBgmSource = CreateLoopingSource(
                "Mining BGM",
                LoadFirstAudioClip("Audio/game", "Audio/Game"));

            GameObject chargeObject = new GameObject("Charge Audio");
            chargeObject.transform.SetParent(transform, false);
            chargeSource = chargeObject.AddComponent<AudioSource>();
            chargeSource.playOnAwake = false;
            chargeSource.loop = false;
            chargeSource.spatialBlend = 0f;

            GameObject resultObject = new GameObject("Result Audio");
            resultObject.transform.SetParent(transform, false);
            resultSource = resultObject.AddComponent<AudioSource>();
            resultSource.playOnAwake = false;
            resultSource.loop = false;
            resultSource.spatialBlend = 0f;

            // AudioLowPassFilter는 클립 없는 소스에 미리 붙이면 경고가 나고,
            // 한 GameObject에 두 개를 붙일 수도 없으므로 프리미엄 단계에서 필요할 때 지연 생성한다.

            chargeClips = new[]
            {
                Resources.Load<AudioClip>("Audio/charge_1_10"),
                Resources.Load<AudioClip>("Audio/charge_10_20"),
                Resources.Load<AudioClip>("Audio/charge_20_28"),
                Resources.Load<AudioClip>("Audio/charge_28_29"),
                Resources.Load<AudioClip>("Audio/charge_29_30")
            };

            successClips = new[]
            {
                Resources.Load<AudioClip>("Audio/success_1_10"),
                Resources.Load<AudioClip>("Audio/success_10_20"),
                Resources.Load<AudioClip>("Audio/success_20_28"),
                Resources.Load<AudioClip>("Audio/success_28_30"),
                Resources.Load<AudioClip>("Audio/success_28_30")
            };

            successFallback = Resources.Load<AudioClip>("Audio/success");
            crackClip = Resources.Load<AudioClip>("Audio/crack");
            failureClip = Resources.Load<AudioClip>("Audio/failure");
            breakClip = Resources.Load<AudioClip>("Audio/break");

            RefreshBgmVolumes();
        }

        public void SetMiningModeActive(bool active)
        {
            EnsureCreated();
            if (active)
            {
                RestartLoop(miningBgmSource);
            }
            else
            {
                StopLoop(miningBgmSource);
            }

            miningModeActive = active;
            RefreshBgmVolumes();
        }

        public void SetEnhancementCinematicActive(bool active)
        {
            EnsureCreated();
            enhancementCinematicActive = active;
            RefreshBgmVolumes();
        }

        public void SetVolumes(float backgroundVolume, float effectsVolume)
        {
            bgmVolume = Mathf.Clamp01(backgroundVolume);
            sfxVolume = Mathf.Clamp01(effectsVolume);

            if (chargeSource != null)
            {
                chargeSource.volume = sfxVolume;
            }

            if (resultSource != null)
            {
                resultSource.volume = sfxVolume;
            }

            RefreshBgmVolumes();
        }

        public float GetChargeDuration(int level, float fallback)
        {
            EnsureCreated();

            AudioClip clip = chargeClips[StarForgeVisualLibrary.GetLevelTier(level)];
            return clip != null ? clip.length : fallback;
        }

        public void PlayCharge(int level)
        {
            EnsureCreated();

            if (!soundEnabled)
            {
                return;
            }

            AudioClip clip = chargeClips[StarForgeVisualLibrary.GetLevelTier(level)];
            if (clip == null)
            {
                return;
            }

            chargeSource.Stop();
            chargeSource.pitch = 1f;
            chargeSource.volume = sfxVolume;
            chargeSource.clip = clip;
            SetPremiumFilter(chargeSource, ref chargeLowPass, level >= PremiumAudioStartLevel);
            chargeSource.Play();
            chargeSource.PlayOneShot(clip, ChargeVolumeMultiplier - 1f);
            TrackEnhancementAudio(clip, chargeSource.pitch);
        }

        public void StopCharge()
        {
            if (chargeSource != null)
            {
                chargeSource.Stop();
            }

            RefreshBgmVolumes();
        }

        public void PlayResult(StarForgeResultKind kind, int attemptLevel)
        {
            EnsureCreated();

            if (!soundEnabled)
            {
                return;
            }

            switch (kind)
            {
                case StarForgeResultKind.Success:
                case StarForgeResultKind.GreatSuccess:
                {
                    int tier = StarForgeVisualLibrary.GetLevelTier(attemptLevel);
                    AudioClip clip = successClips[tier];
                    if (clip == null)
                    {
                        clip = successFallback;
                    }

                    bool isGreat = kind == StarForgeResultKind.GreatSuccess;
                    bool usePremiumFilter = attemptLevel >= PremiumAudioStartLevel;
                    bool useLowTierPresenceBoost = tier == 0;
                    SetPremiumFilter(resultSource, ref resultLowPass, usePremiumFilter);
                    SetResultPresenceBoost(useLowTierPresenceBoost);
                    float volume = isGreat ? 1f : 0.9f;
                    float pitch = isGreat && !usePremiumFilter ? 1.05f : 1f;
                    if (useLowTierPresenceBoost)
                    {
                        volume *= LowTierSuccessVolumeMultiplier;
                        pitch = isGreat
                            ? LowTierGreatSuccessPitch
                            : LowTierSuccessPitch;
                    }

                    PlayOneShot(
                        clip,
                        volume,
                        pitch);
                    break;
                }
                case StarForgeResultKind.Fracture:
                    SetPremiumFilter(resultSource, ref resultLowPass, false);
                    SetResultPresenceBoost(false);
                    PlayOneShot(crackClip, 0.6f, 1f);
                    break;
                case StarForgeResultKind.Destroyed:
                    SetPremiumFilter(resultSource, ref resultLowPass, false);
                    SetResultPresenceBoost(false);
                    PlayOneShot(breakClip, 1f, 1f);
                    break;
                case StarForgeResultKind.Failure:
                    SetPremiumFilter(resultSource, ref resultLowPass, false);
                    SetResultPresenceBoost(false);
                    PlayOneShot(failureClip != null ? failureClip : crackClip, 1f, 1f);
                    break;
            }
        }

        private void SetResultPresenceBoost(bool enabled)
        {
            if (resultPresenceHighPass == null)
            {
                if (!enabled)
                {
                    return;
                }

                resultPresenceHighPass =
                    resultSource.GetComponent<AudioHighPassFilter>();
                if (resultPresenceHighPass == null)
                {
                    resultPresenceHighPass =
                        resultSource.gameObject.AddComponent<AudioHighPassFilter>();
                }
            }

            resultPresenceHighPass.cutoffFrequency = LowTierSuccessHighPassCutoff;
            resultPresenceHighPass.highpassResonanceQ = 1.15f;
            resultPresenceHighPass.enabled = enabled;
        }

        /// <summary>프리미엄(28강+) 단계에서만 저역 통과 필터를 지연 생성/활성화한다. GameObject당 하나만 존재.</summary>
        private static void SetPremiumFilter(
            AudioSource source,
            ref AudioLowPassFilter filter,
            bool enabled)
        {
            if (filter == null)
            {
                if (!enabled)
                {
                    return;
                }

                filter = source.GetComponent<AudioLowPassFilter>();
                if (filter == null)
                {
                    filter = source.gameObject.AddComponent<AudioLowPassFilter>();
                }
            }

            filter.cutoffFrequency = PremiumLowPassCutoff;
            filter.lowpassResonanceQ = 1f;
            filter.enabled = enabled;
        }

        private void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            resultSource.pitch = pitch;
            resultSource.volume = sfxVolume;
            resultSource.PlayOneShot(clip, volume);
            TrackEnhancementAudio(clip, pitch);
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

        private AudioSource CreateLoopingSource(string objectName, AudioClip clip)
        {
            GameObject sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.clip = clip;
            if (clip != null)
            {
                source.Play();
            }

            return source;
        }

        private void TrackEnhancementAudio(AudioClip clip, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            float duration = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
            enhancementAudioMuteUntil =
                Mathf.Max(enhancementAudioMuteUntil, Time.unscaledTime + duration);
            RefreshBgmVolumes();
        }

        private void RefreshBgmVolumes()
        {
            bool enhancementBgmMuted = IsEnhancementBgmMuted();
            float mainVolume = enhancementBgmMuted
                ? 0f
                : MainBgmVolume * bgmVolume;
            float miningVolume = enhancementBgmMuted
                ? 0f
                : MiningBgmVolume * bgmVolume;

            SetLoopVolume(
                mainBgmSource,
                soundEnabled && !miningModeActive && !startupMainBgmSuppressed,
                mainVolume,
                startupMainBgmSuppressed);
            SetLoopVolume(
                miningBgmSource,
                soundEnabled && miningModeActive,
                miningVolume,
                true);
        }

        private bool IsEnhancementBgmMuted()
        {
            if (enhancementCinematicActive)
            {
                return true;
            }

            if (enhancementAudioMuteUntil <= 0f)
            {
                return false;
            }

            if (Time.unscaledTime < enhancementAudioMuteUntil)
            {
                return true;
            }

            enhancementAudioMuteUntil = 0f;
            return false;
        }

        private static void SetLoopVolume(
            AudioSource source,
            bool shouldPlay,
            float activeVolume,
            bool stopWhenInactive)
        {
            if (source == null)
            {
                return;
            }

            source.volume = shouldPlay ? activeVolume : 0f;
            if (source.clip == null)
            {
                return;
            }

            if (shouldPlay)
            {
                if (!source.isPlaying)
                {
                    source.Play();
                }
            }
            else if (stopWhenInactive && source.isPlaying)
            {
                source.Stop();
            }
        }

        private static void RestartLoop(AudioSource source)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            source.Stop();
            source.time = 0f;
            source.Play();
        }

        private static void StopLoop(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
        }
    }
}
