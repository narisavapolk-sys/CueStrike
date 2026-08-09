using System;
using System.Collections;
using UnityEngine;

namespace CueStrike.UI
{
    /// <summary>
    /// AAA UI Animation system for CueStrike VR.
    /// No external dependencies. VR-optimized (90fps, no motion sickness).
    /// </summary>
    public class CueStrikeUIAnimations : MonoBehaviour
    {
        #region Settings
        [Header("Performance")]
        [SerializeField] private int _maxConcurrentAnimations = 8;
        [SerializeField] private bool _useUnscaledTime = true;

        private int _activeAnimations = 0;
        #endregion

        #region Fade
        public void FadeIn(Transform target, float duration, Action onComplete = null)
        {
            StartCoroutine(FadeCoroutine(target, 0f, 1f, duration, onComplete));
        }

        public void FadeOut(Transform target, float duration, Action onComplete = null)
        {
            StartCoroutine(FadeCoroutine(target, 1f, 0f, duration, onComplete));
        }

        private IEnumerator FadeCoroutine(Transform target, float from, float to, float duration, Action onComplete)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) { onComplete?.Invoke(); yield break; }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
            _activeAnimations--;
            onComplete?.Invoke();
        }
        #endregion

        #region Scale
        public void ScaleIn(Transform target, float duration, AnimationCurve curve = null, Action onComplete = null)
        {
            if (target == null) return;
            target.localScale = Vector3.zero;
            StartCoroutine(ScaleCoroutine(target, Vector3.zero, Vector3.one, duration, curve, onComplete));
        }

        public void ScaleOut(Transform target, float duration, AnimationCurve curve = null, Action onComplete = null)
        {
            StartCoroutine(ScaleCoroutine(target, target.localScale, Vector3.zero, duration, curve, onComplete));
        }

        public void ScaleBounce(Transform target, float duration)
        {
            StartCoroutine(BounceCoroutine(target, duration));
        }

        private IEnumerator ScaleCoroutine(Transform target, Vector3 from, Vector3 to, float duration, AnimationCurve curve, Action onComplete)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) { onComplete?.Invoke(); yield break; }

            float elapsed = 0f;
            AnimationCurve animCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = animCurve.Evaluate(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            target.localScale = to;
            _activeAnimations--;
            onComplete?.Invoke();
        }

        private IEnumerator BounceCoroutine(Transform target, float duration)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = elapsed / duration;
                // Damped sine wave for bounce
                float bounce = 1f + Mathf.Sin(t * Mathf.PI * 4f) * Mathf.Exp(-t * 4f) * 0.3f;
                target.localScale = originalScale * bounce;
                yield return null;
            }

            target.localScale = originalScale;
            _activeAnimations--;
        }
        #endregion

        #region Slide
        public void SlideIn(Transform target, Vector3 fromOffset, float duration, Action onComplete = null)
        {
            if (target == null) return;
            Vector3 to = target.localPosition;
            target.localPosition = to + fromOffset;
            StartCoroutine(SlideCoroutine(target, target.localPosition, to, duration, onComplete));
        }

        public void SlideOut(Transform target, Vector3 toOffset, float duration, Action onComplete = null)
        {
            if (target == null) return;
            Vector3 from = target.localPosition;
            StartCoroutine(SlideCoroutine(target, from, from + toOffset, duration, onComplete));
        }

        private IEnumerator SlideCoroutine(Transform target, Vector3 from, Vector3 to, float duration, Action onComplete)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) { onComplete?.Invoke(); yield break; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                target.localPosition = Vector3.Lerp(from, to, t);
                yield return null;
            }

            target.localPosition = to;
            _activeAnimations--;
            onComplete?.Invoke();
        }
        #endregion

        #region Shake
        public void Shake(Transform target, float intensity, float duration)
        {
            StartCoroutine(ShakeCoroutine(target, intensity, duration));
        }

        private IEnumerator ShakeCoroutine(Transform target, float intensity, float duration)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) yield break;

            Vector3 originalPos = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float damper = 1f - (elapsed / duration);
                float x = Mathf.Sin(elapsed * 50f) * intensity * damper;
                float y = Mathf.Cos(elapsed * 45f) * intensity * damper;
                target.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            target.localPosition = originalPos;
            _activeAnimations--;
        }
        #endregion

        #region Pulse
        public void Pulse(Transform target, float scaleAmount, float duration)
        {
            StartCoroutine(PulseCoroutine(target, scaleAmount, duration));
        }

        private IEnumerator PulseCoroutine(Transform target, float scaleAmount, float duration)
        {
            if (!CanStartAnimation()) yield break;
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale * scaleAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.PingPong(elapsed / duration * 2f, 1f);
                target.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            target.localScale = originalScale;
            _activeAnimations--;
        }
        #endregion

        #region Utility
        private bool CanStartAnimation()
        {
            if (_activeAnimations >= _maxConcurrentAnimations)
            {
                Debug.LogWarning("[CueStrikeUIAnimations] Max concurrent animations reached. Skipping.");
                return false;
            }
            _activeAnimations++;
            return true;
        }

        private float GetDeltaTime()
        {
            return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        #endregion

        #region Bounce (alias called by ChinesePoolScoreboard)
        public void Bounce(Transform target, float duration)
        {
            ScaleBounce(target, duration);
        }
        #endregion
    }
}