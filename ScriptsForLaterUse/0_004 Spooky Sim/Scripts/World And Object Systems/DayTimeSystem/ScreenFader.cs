using System.Collections;
using UnityEngine;

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Screen Fader")]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool blockRaycastsWhenVisible = true;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _routine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
                UpdateBlocks();
            }
        }

        public void SetInstant(float alpha)
        {
            if (canvasGroup == null) return;
            if (_routine != null) StopCoroutine(_routine);
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            UpdateBlocks();
        }

        public void FadeTo(float alpha, float duration)
        {
            if (canvasGroup == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine(Mathf.Clamp01(alpha), Mathf.Max(0.0001f, duration)));
        }

        public IEnumerator FadeToRoutine(float alpha, float duration)
        {
            if (canvasGroup == null) yield break;
            if (_routine != null) StopCoroutine(_routine);
            yield return FadeRoutine(Mathf.Clamp01(alpha), Mathf.Max(0.0001f, duration));
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += UnityEngine.Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float k = ease != null ? ease.Evaluate(u) : u;
                canvasGroup.alpha = Mathf.Lerp(start, target, k);
                UpdateBlocks();
                yield return null;
            }

            canvasGroup.alpha = target;
            UpdateBlocks();
            _routine = null;
        }

        private void UpdateBlocks()
        {
            if (canvasGroup == null) return;
            if (!blockRaycastsWhenVisible) return;

            bool visible = canvasGroup.alpha > 0.001f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = !visible;
        }
    }
}
