using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cherry.Cameras
{
    [DisallowMultipleComponent]
    public class SecurityMonitorFX : MonoBehaviour
    {
        [Header("Master")]
        [SerializeField] private CanvasGroup overlayGroup; // optional
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip switchStaticClip;
        [Range(0f, 1f)][SerializeField] private float staticVolume = 0.65f;
        [SerializeField] private Vector2 staticPitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] private bool playStaticOnBurst = true;


        [Header("Feed (optional jitter)")]
        [SerializeField] private RectTransform feedRect;   // the RawImage rect that shows the RenderTexture
        [SerializeField] private float burstJitterPixels = 8f;
        [SerializeField] private float burstScale = 1.02f;

        [Header("Scanlines")]
        [SerializeField] private RawImage scanlines;
        [SerializeField] private CanvasGroup scanlinesGroup;
        [Range(0f, 1f)][SerializeField] private float scanlineAlpha = 0.12f;
        [SerializeField] private Vector2 scanlineTiling = new Vector2(1f, 6f);
        [SerializeField] private float scanlineScrollSpeed = 0.15f;

        [Header("Noise")]
        [SerializeField] private RawImage noise;
        [SerializeField] private CanvasGroup noiseGroup;
        [Range(0f, 1f)][SerializeField] private float noiseBaseAlpha = 0.05f;
        [Range(0f, 1f)][SerializeField] private float noiseFlicker = 0.08f;
        [SerializeField] private Vector2 noiseTiling = new Vector2(2.2f, 2.2f);

        [Header("REC")]
        [SerializeField] private TMP_Text recText;
        [SerializeField] private Image recDot;
        [SerializeField] private float recBlinkHz = 1.5f;

        [Header("Timestamp")]
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private string timeFormat = "HH:mm:ss";

        [Header("Auto-generate textures (optional)")]
        [SerializeField] private bool autoGenerateTextures = true;
        [SerializeField] private int noiseTexSize = 64;

        private bool _enabled;
        private float _t;
        private Rect _scanUV = new Rect(0, 0, 1, 1);
        private Rect _noiseUV = new Rect(0, 0, 1, 1);

        private Vector2 _feedBasePos;
        private Vector3 _feedBaseScale;

        private Coroutine _burstRoutine;

        private Texture2D _generatedNoise;
        private Texture2D _generatedScanlines;

        private void Awake()
        {
            if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();
            if (overlayGroup == null) overlayGroup = gameObject.AddComponent<CanvasGroup>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;


            if (feedRect != null)
            {
                _feedBasePos = feedRect.anchoredPosition;
                _feedBaseScale = feedRect.localScale;
            }

            if (autoGenerateTextures)
            {
                EnsureTextures();
            }

            // Default off until camera mode enables it
            SetEnabled(false);
        }

        private void OnDestroy()
        {
            if (_generatedNoise != null) Destroy(_generatedNoise);
            if (_generatedScanlines != null) Destroy(_generatedScanlines);
        }

        private void EnsureTextures()
        {
            if (scanlines != null && scanlines.texture == null)
            {
                _generatedScanlines = GenerateScanlinesTexture();
                scanlines.texture = _generatedScanlines;
            }

            if (noise != null && noise.texture == null)
            {
                _generatedNoise = GenerateNoiseTexture(noiseTexSize);
                noise.texture = _generatedNoise;
            }
        }

        public void SetEnabled(bool on)
        {
            _enabled = on;

            if (overlayGroup != null)
            {
                overlayGroup.alpha = on ? 1f : 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            if (!on)
            {
                if (scanlinesGroup != null) scanlinesGroup.alpha = 0f;
                if (noiseGroup != null) noiseGroup.alpha = 0f;
                if (recText != null) recText.enabled = false;
                if (recDot != null) recDot.enabled = false;
                if (timestampText != null) timestampText.enabled = false;

                ResetFeedTransform();
            }
            else
            {
                if (scanlinesGroup != null) scanlinesGroup.alpha = scanlineAlpha;
                if (noiseGroup != null) noiseGroup.alpha = noiseBaseAlpha;
                if (recText != null) recText.enabled = true;
                if (recDot != null) recDot.enabled = true;
                if (timestampText != null) timestampText.enabled = true;
            }
        }

        private void Update()
        {
            if (!_enabled) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _t += dt;

            UpdateScanlines(dt);
            UpdateNoise(dt);
            UpdateRec(dt);
            UpdateTimestamp();
        }

        private void UpdateScanlines(float dt)
        {
            if (scanlines == null) return;

            _scanUV.width = Mathf.Max(0.001f, scanlineTiling.x);
            _scanUV.height = Mathf.Max(0.001f, scanlineTiling.y);

            _scanUV.y += scanlineScrollSpeed * dt;
            scanlines.uvRect = _scanUV;

            if (scanlinesGroup != null) scanlinesGroup.alpha = scanlineAlpha;
        }

        private void UpdateNoise(float dt)
        {
            if (noise == null) return;

            _noiseUV.width = Mathf.Max(0.001f, noiseTiling.x);
            _noiseUV.height = Mathf.Max(0.001f, noiseTiling.y);

            // jitter UV offsets to feel like moving noise
            _noiseUV.x = Mathf.Repeat(_noiseUV.x + (Random.value * 0.08f), 1f);
            _noiseUV.y = Mathf.Repeat(_noiseUV.y + (Random.value * 0.08f), 1f);
            noise.uvRect = _noiseUV;

            if (noiseGroup != null)
            {
                // small flicker around base alpha
                float flick = (Random.value - 0.5f) * 2f * noiseFlicker;
                noiseGroup.alpha = Mathf.Clamp01(noiseBaseAlpha + flick);
            }
        }

        private void UpdateRec(float dt)
        {
            float pulse = Mathf.Sin(_t * Mathf.PI * 2f * recBlinkHz);
            bool on = pulse > 0f;

            if (recText != null) recText.enabled = on;
            if (recDot != null) recDot.enabled = on;
        }

        private void UpdateTimestamp()
        {
            if (timestampText == null) return;
            timestampText.text = System.DateTime.Now.ToString(timeFormat);
        }

        public void StaticBurst(float duration = 0.15f, float peakAlpha = 0.9f)
        {
            if (!_enabled) return;

            PlayStaticSwitchSfx(); // <-- add this

            if (_burstRoutine != null) StopCoroutine(_burstRoutine);
            _burstRoutine = StartCoroutine(StaticBurstRoutine(duration, peakAlpha));
        }
        private void PlayStaticSwitchSfx()
        {
            if (!playStaticOnBurst) return;
            if (audioSource == null || switchStaticClip == null) return;

            audioSource.pitch = Random.Range(staticPitchRange.x, staticPitchRange.y);
            audioSource.PlayOneShot(switchStaticClip, staticVolume);
        }

        private IEnumerator StaticBurstRoutine(float duration, float peakAlpha)
        {
            float t = 0f;
            float startAlpha = noiseGroup != null ? noiseGroup.alpha : noiseBaseAlpha;

            while (t < duration)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));

                // quick spike then settle
                float spike = Mathf.Sin(u * Mathf.PI);
                float a = Mathf.Lerp(startAlpha, peakAlpha, spike);

                if (noiseGroup != null) noiseGroup.alpha = a;

                // tiny feed jitter + scale bump
                if (feedRect != null)
                {
                    Vector2 j = Random.insideUnitCircle * burstJitterPixels * spike;
                    feedRect.anchoredPosition = _feedBasePos + j;
                    feedRect.localScale = Vector3.Lerp(_feedBaseScale, _feedBaseScale * burstScale, spike);
                }

                t += dt;
                yield return null;
            }

            if (noiseGroup != null) noiseGroup.alpha = noiseBaseAlpha;
            ResetFeedTransform();
            _burstRoutine = null;
        }

        private void ResetFeedTransform()
        {
            if (feedRect == null) return;
            feedRect.anchoredPosition = _feedBasePos;
            feedRect.localScale = _feedBaseScale;
        }

        private Texture2D GenerateNoiseTexture(int size)
        {
            size = Mathf.Clamp(size, 16, 256);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "GeneratedNoise";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private Texture2D GenerateScanlinesTexture()
        {
            // A tiny repeating pattern: 1 bright row, 1 dim row, rest transparent-ish
            int w = 2;
            int h = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "GeneratedScanlines";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;

            Color32 bright = new Color32(255, 255, 255, 255);
            Color32 dim = new Color32(255, 255, 255, 90);
            Color32 clear = new Color32(255, 255, 255, 0);

            // Row 0 bright, row 1 dim, row 2 clear, row 3 clear
            for (int y = 0; y < h; y++)
            {
                Color32 c = y == 0 ? bright : (y == 1 ? dim : clear);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply(false, false);
            return tex;
        }
    }
}
