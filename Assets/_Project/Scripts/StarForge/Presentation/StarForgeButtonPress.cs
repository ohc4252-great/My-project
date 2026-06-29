using UnityEngine;
using UnityEngine.EventSystems;

namespace StarForge.Presentation
{
    /// <summary>
    /// Universal tactile press feedback: shrinks a UI element slightly while it is
    /// held and springs it back on release, so taps read clearly even when a
    /// button's color transition is subtle. Uses unscaled time so it works while
    /// the game is paused. Scaling localScale does not affect UI layout, so this is
    /// safe to drop on any button (layout-grouped or anchored).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StarForgeButtonPress :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private const float PressedScale = 0.92f;
        private const float ReturnSpeed = 14f;

        private RectTransform target;
        private Vector3 baseScale = Vector3.one;
        private float currentScale = 1f;
        private float targetScale = 1f;
        private bool baseCaptured;

        private void Awake()
        {
            target = transform as RectTransform;
            CaptureBaseScale();
        }

        private void OnEnable()
        {
            CaptureBaseScale();
            currentScale = 1f;
            targetScale = 1f;
            ApplyScale();
        }

        private void CaptureBaseScale()
        {
            if (!baseCaptured && target != null)
            {
                baseScale = target.localScale;
                baseCaptured = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = PressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = 1f;
        }

        private void OnDisable()
        {
            currentScale = 1f;
            targetScale = 1f;
            ApplyScale();
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (!Mathf.Approximately(currentScale, targetScale))
            {
                currentScale = Mathf.MoveTowards(
                    currentScale,
                    targetScale,
                    ReturnSpeed * Time.unscaledDeltaTime);
                ApplyScale();
            }
        }

        private void ApplyScale()
        {
            if (target != null)
            {
                target.localScale = baseScale * currentScale;
            }
        }
    }
}
