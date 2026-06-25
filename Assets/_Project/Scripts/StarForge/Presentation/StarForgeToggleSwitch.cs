using UnityEngine;
using UnityEngine.UI;

namespace StarForge.Presentation
{
    /// <summary>
    /// Drives an iOS-style sliding switch for a <see cref="Toggle"/>.
    /// Instead of showing/hiding the knob (the default <c>Toggle.graphic</c>
    /// behaviour) the knob stays visible and slides left/right while its colour
    /// fades between an "off" grey and the "on" accent colour.
    /// The component simply observes <see cref="Toggle.isOn"/> every frame so it
    /// reacts to both user taps and <see cref="Toggle.SetIsOnWithoutNotify"/>.
    /// </summary>
    public sealed class StarForgeToggleSwitch : MonoBehaviour
    {
        private const float AnimationSpeed = 8f;

        private Toggle toggle;
        private RectTransform knob;
        private Image knobImage;
        private Image trackImage;

        private Vector2 offAnchorMin;
        private Vector2 offAnchorMax;
        private Vector2 onAnchorMin;
        private Vector2 onAnchorMax;

        private Color knobOffColor;
        private Color knobOnColor;
        private Color trackOffColor;
        private Color trackOnColor;

        private float progress;

        public void Configure(
            Toggle toggle,
            RectTransform knob,
            Image knobImage,
            Image trackImage,
            Vector2 offAnchorMin,
            Vector2 offAnchorMax,
            Vector2 onAnchorMin,
            Vector2 onAnchorMax,
            Color knobOffColor,
            Color knobOnColor,
            Color trackOffColor,
            Color trackOnColor)
        {
            this.toggle = toggle;
            this.knob = knob;
            this.knobImage = knobImage;
            this.trackImage = trackImage;
            this.offAnchorMin = offAnchorMin;
            this.offAnchorMax = offAnchorMax;
            this.onAnchorMin = onAnchorMin;
            this.onAnchorMax = onAnchorMax;
            this.knobOffColor = knobOffColor;
            this.knobOnColor = knobOnColor;
            this.trackOffColor = trackOffColor;
            this.trackOnColor = trackOnColor;

            progress = IsOn ? 1f : 0f;
            Apply(progress);
        }

        private bool IsOn => toggle != null && toggle.isOn;

        private void OnEnable()
        {
            // Snap to the current state when the panel is (re)shown so the
            // switch never slides from a stale position on open.
            if (toggle == null)
            {
                return;
            }

            progress = IsOn ? 1f : 0f;
            Apply(progress);
        }

        private void Update()
        {
            float target = IsOn ? 1f : 0f;
            if (!Mathf.Approximately(progress, target))
            {
                progress = Mathf.MoveTowards(
                    progress,
                    target,
                    Time.unscaledDeltaTime * AnimationSpeed);
                Apply(progress);
            }
        }

        private void Apply(float t)
        {
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (knob != null)
            {
                knob.anchorMin = Vector2.LerpUnclamped(offAnchorMin, onAnchorMin, eased);
                knob.anchorMax = Vector2.LerpUnclamped(offAnchorMax, onAnchorMax, eased);
            }

            if (knobImage != null)
            {
                knobImage.color = Color.Lerp(knobOffColor, knobOnColor, eased);
            }

            if (trackImage != null)
            {
                trackImage.color = Color.Lerp(trackOffColor, trackOnColor, eased);
            }
        }
    }
}
