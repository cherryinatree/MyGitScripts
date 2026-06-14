using System.Collections;
using TMPro;
using UnityEngine;

namespace Cherry.UI
{
    public class DeathScreenUI : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeToBlackSeconds = 0.8f;
        [SerializeField] private float fadeFromBlackSeconds = 0.6f;

        [Header("Text")]
        [SerializeField] private TMP_Text youDiedText;
        [SerializeField] private float youDiedHoldSeconds = 1.2f;

        private void Awake()
        {
            if (youDiedText != null) youDiedText.gameObject.SetActive(false);
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
                fadeGroup.interactable = false;
            }
        }

        public IEnumerator PlayDeathSequence()
        {
            // Fade to black
            yield return Fade(1f, fadeToBlackSeconds);

            // Show text
            if (youDiedText != null)
            {
                youDiedText.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(youDiedHoldSeconds);
                youDiedText.gameObject.SetActive(false);
            }
        }

        public IEnumerator FadeInFromBlack()
        {
            yield return Fade(0f, fadeFromBlackSeconds);
        }

        private IEnumerator Fade(float targetAlpha, float seconds)
        {
            if (fadeGroup == null) yield break;

            fadeGroup.blocksRaycasts = true;

            float start = fadeGroup.alpha;
            float t = 0f;

            seconds = Mathf.Max(0.01f, seconds);

            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = targetAlpha > 0.001f;
        }
    }
}
