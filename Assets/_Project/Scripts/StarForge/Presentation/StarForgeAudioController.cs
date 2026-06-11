using StarForge.Core;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgeAudioController : MonoBehaviour
    {
        private AudioSource chargeSource;
        private AudioSource resultSource;

        private AudioClip[] chargeClips;
        private AudioClip[] successClips;
        private AudioClip successFallback;
        private AudioClip crackClip;
        private AudioClip breakClip;
        private bool built;
        private bool soundEnabled = true;

        public bool SoundEnabled
        {
            get { return soundEnabled; }
            set
            {
                soundEnabled = value;
                if (!soundEnabled)
                {
                    StopCharge();
                }
            }
        }

        public void EnsureCreated()
        {
            if (built)
            {
                return;
            }

            built = true;

            chargeSource = gameObject.AddComponent<AudioSource>();
            chargeSource.playOnAwake = false;
            chargeSource.loop = false;
            chargeSource.spatialBlend = 0f;

            resultSource = gameObject.AddComponent<AudioSource>();
            resultSource.playOnAwake = false;
            resultSource.loop = false;
            resultSource.spatialBlend = 0f;

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
            breakClip = Resources.Load<AudioClip>("Audio/break");
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
            chargeSource.volume = 1f;
            chargeSource.clip = clip;
            chargeSource.Play();
        }

        public void StopCharge()
        {
            if (chargeSource != null)
            {
                chargeSource.Stop();
            }
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
                    AudioClip clip = successClips[StarForgeVisualLibrary.GetLevelTier(attemptLevel)];
                    if (clip == null)
                    {
                        clip = successFallback;
                    }

                    bool isGreat = kind == StarForgeResultKind.GreatSuccess;
                    PlayOneShot(clip, isGreat ? 1f : 0.9f, isGreat ? 1.05f : 1f);
                    break;
                }
                case StarForgeResultKind.Fracture:
                    PlayOneShot(crackClip, 0.6f, 1f);
                    break;
                case StarForgeResultKind.Destroyed:
                    PlayOneShot(breakClip, 1f, 1f);
                    break;
                case StarForgeResultKind.Failure:
                    PlayOneShot(crackClip, 0.21f, 0.78f);
                    break;
            }
        }

        private void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            resultSource.pitch = pitch;
            resultSource.PlayOneShot(clip, volume);
        }
    }
}
