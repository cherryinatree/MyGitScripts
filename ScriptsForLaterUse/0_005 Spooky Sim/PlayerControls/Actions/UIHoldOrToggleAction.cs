using Cherry.Combat;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.UI;
using System.Collections;
using Cherry.Cameras;


/// <summary>
/// Shows a UI while Tab is held, or via a sticky CapsLock toggle.
/// Adds: slide in/out + an activation image that fades out logarithmically.
/// </summary>
[DisallowMultipleComponent]
public class UIHoldOrToggleAction : PlayerAction
{
    [Header("Target")]
    [Tooltip("UI root object (typically a panel under a Canvas).")]
    [SerializeField] public GameObject uiGroup;

    [Header("Behavior")]
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool startStickyOn = false;
    [SerializeField] private bool resetStickyOnContextExit = false; 
    [Header("Optional: Camera Monitor Integration")]
    [SerializeField] private CameraMonitorController cameraMonitor;


    // Internal state
    private bool _sticky;
    private bool _currentShown; // desired state

    private GhostVisionToggleHDRP _ghostVisionToggle;

    [Header("Battery")]
    public BatteryConsumer batteryConsumer; // set drainPerSecond in inspector (optional)

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;

    [Header("UI Motion")]
    [Tooltip("How long it takes the UI to slide in/out.")]
    [SerializeField] private float slideDuration = 0.25f;

    [Tooltip("Offset from the UI's on-screen anchoredPosition to its hidden (off-screen) position.")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -900f);

    [Tooltip("Ease curve for sliding. 0..1 time -> 0..1 progress.")]
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("If true, also fades the whole UI group in/out during the slide.")]
    [SerializeField] private bool fadeUIGroup = true;

    [Header("Activation Image")]
    [Tooltip("Optional image (or any UI object) shown when UI activates, then fades out.")]
    [SerializeField] private GameObject activationImageObject;

    [Tooltip("Seconds for the activation image to fade out.")]
    [SerializeField] private float activationFadeSeconds = 1.5f;

    [Tooltip("Log strength for fade curve. Higher = faster drop early, slower near the end.")]
    [Min(0.01f)]
    [SerializeField] private float logStrength = 9f;

    // Cached UI components
    private RectTransform _uiRect;
    private CanvasGroup _uiCanvasGroup;
    private Vector2 _shownPos;
    private Vector2 _hiddenPos;

    private CanvasGroup _activationCanvasGroup;

    private Coroutine _transitionRoutine;
    private Coroutine _activationFadeRoutine;

    private bool _posInitialized;
    private Vector2 _baseShownPos;


    // ------------ PlayerAction lifecycle ------------
    protected override void Awake()
    {
        base.Awake();
        if (cameraMonitor == null)
            cameraMonitor = FindFirstObjectByType<CameraMonitorController>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        batteryConsumer = GetComponent<BatteryConsumer>();
        _ghostVisionToggle = GetComponent<GhostVisionToggleHDRP>();

        if (uiGroup == null)
            uiGroup = GameObject.Find("UIPanel");

        CacheUIBits(); 

        if (_uiRect != null && !_posInitialized)
        {
            _baseShownPos = _uiRect.anchoredPosition;   // your real "on-screen" position
            _posInitialized = true;
        }

        _shownPos = _baseShownPos;
        _hiddenPos = _baseShownPos + hiddenOffset;

        _sticky = startStickyOn;
        ForceHiddenImmediate();


        _sticky = startStickyOn;

        // Start hidden
        ForceHiddenImmediate();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateVisibilityImmediate();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RequestVisible(false, immediate: true);
    }

    protected override void Subscribe(CorePlayer c) { /* no-op */ }
    protected override void Unsubscribe(CorePlayer c) { /* no-op */ }

    protected override void OnContextChanged(string newContext)
    {
        if (!IsContextAllowed())
        {
            if (resetStickyOnContextExit) _sticky = false;
            RequestVisible(false);
        }
        else
        {
            UpdateVisibilityImmediate();
        }
    }

    // ------------ Runtime logic ------------
    private void Update()
    {
        // Battery gate (if you have one)
        if (batteryConsumer != null && !batteryConsumer.CanUse())
        {
            if (_currentShown) RequestVisible(false);
            return;
        }

        bool contextOK = IsContextAllowed();
        var kb = Keyboard.current;

        bool hold = kb != null && kb.tabKey.isPressed;

        if (kb != null && kb.capsLockKey.wasPressedThisFrame)
            _sticky = !_sticky;

        bool shouldShow = contextOK && (hold || _sticky);

        if (shouldShow != _currentShown)
            RequestVisible(shouldShow);
    }

    public void UpdateVisibilityImmediate()
    {
        var kb = Keyboard.current;
        bool hold = kb != null && kb.tabKey.isPressed;
        bool shouldShow = IsContextAllowed() && (hold || _sticky);
        RequestVisible(shouldShow, immediate: true);
    }

    public void SetSticky(bool on)
    {
        _sticky = on;
        UpdateVisibilityImmediate();
    }

    public bool IsVisible => _currentShown;

    // ------------ Core show/hide ------------
    private void RequestVisible(bool show, bool immediate = false)
    {
        // If trying to show but battery is dead, bail
        if (show && batteryConsumer != null && !batteryConsumer.CanUse())
            return;

        _currentShown = show;

        if (immediate)
        {
            if (_transitionRoutine != null) { StopCoroutine(_transitionRoutine); _transitionRoutine = null; }
            ApplyVisibleImmediate(show);
            return;
        }

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(show ? ShowRoutine() : HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        CacheUIBits();
        if (uiGroup == null || _uiRect == null) yield break;

        // Activate first so it can render while sliding in
        uiGroup.SetActive(true); 
        if (cameraMonitor != null) cameraMonitor.NotifyHudOpened();


        if (manageCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        if (_ghostVisionToggle != null)
            _ghostVisionToggle.ToggleGhostVision(true);

        // Activation image (log fade)
        StartActivationFade();

        // Prep start state
        _uiRect.anchoredPosition = _hiddenPos;
        if (_uiCanvasGroup != null)
        {
            _uiCanvasGroup.blocksRaycasts = true;
            _uiCanvasGroup.interactable = true;
            if (fadeUIGroup) _uiCanvasGroup.alpha = 0f;
            else _uiCanvasGroup.alpha = 1f;
        }

        float dur = Mathf.Max(0.01f, slideDuration);
        float t = 0f;

        while (t < dur)
        {
            float u = t / dur;
            float eased = slideEase != null ? slideEase.Evaluate(u) : u;

            _uiRect.anchoredPosition = Vector2.LerpUnclamped(_hiddenPos, _shownPos, eased);

            if (_uiCanvasGroup != null && fadeUIGroup)
                _uiCanvasGroup.alpha = Mathf.LerpUnclamped(0f, 1f, eased);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _uiRect.anchoredPosition = _shownPos;
        if (_uiCanvasGroup != null) _uiCanvasGroup.alpha = 1f;

        _transitionRoutine = null;
    }

    private IEnumerator HideRoutine()
    {
        CacheUIBits();
        if (uiGroup == null || _uiRect == null) yield break;
        if (cameraMonitor != null) cameraMonitor.NotifyHudClosed();

        // During hide, block interaction if desired
        if (_uiCanvasGroup != null)
        {
            _uiCanvasGroup.blocksRaycasts = false;
            _uiCanvasGroup.interactable = false;
        }

        float dur = Mathf.Max(0.01f, slideDuration);
        float t = 0f;

        Vector2 startPos = _uiRect.anchoredPosition;

        float startAlpha = (_uiCanvasGroup != null) ? _uiCanvasGroup.alpha : 1f;

        while (t < dur)
        {
            float u = t / dur;
            float eased = slideEase != null ? slideEase.Evaluate(u) : u;

            _uiRect.anchoredPosition = Vector2.LerpUnclamped(startPos, _hiddenPos, eased);

            if (_uiCanvasGroup != null && fadeUIGroup)
                _uiCanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, eased);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Finalize hidden
        if (_ghostVisionToggle != null)
            _ghostVisionToggle.ToggleGhostVision(false);

        if (manageCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (_uiCanvasGroup != null)
        {
            _uiCanvasGroup.alpha = 0f;
            _uiCanvasGroup.blocksRaycasts = false;
            _uiCanvasGroup.interactable = false;
        }

        _uiRect.anchoredPosition = _hiddenPos;
        uiGroup.SetActive(false);

        _transitionRoutine = null;
    }

    private void ApplyVisibleImmediate(bool show)
    {
        CacheUIBits();

        if (show)
        {
            if (uiGroup == null) return;

            uiGroup.SetActive(true);
            if (cameraMonitor != null) cameraMonitor.NotifyHudOpened();

            if (audioSource != null && openSound != null)
                audioSource.PlayOneShot(openSound);

            if (_ghostVisionToggle != null)
                _ghostVisionToggle.ToggleGhostVision(true);

            StartActivationFade();

            if (_uiRect != null) _uiRect.anchoredPosition = _shownPos;
            if (_uiCanvasGroup != null)
            {
                _uiCanvasGroup.alpha = 1f;
                _uiCanvasGroup.blocksRaycasts = true;
                _uiCanvasGroup.interactable = true;
            }

            if (manageCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        else
        {
            if (cameraMonitor != null) cameraMonitor.NotifyHudClosed();

            if (_ghostVisionToggle != null)
                _ghostVisionToggle.ToggleGhostVision(false);

            if (manageCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (uiGroup != null)
                uiGroup.SetActive(false);

            if (_uiRect != null) _uiRect.anchoredPosition = _hiddenPos;
            if (_uiCanvasGroup != null)
            {
                _uiCanvasGroup.alpha = 0f;
                _uiCanvasGroup.blocksRaycasts = false;
                _uiCanvasGroup.interactable = false;
            }
        }
    }

    private void ForceHiddenImmediate()
    {
        if (cameraMonitor != null) cameraMonitor.NotifyHudClosed();

        CacheUIBits();

        if (_uiRect != null) _uiRect.anchoredPosition = _hiddenPos;
        if (_uiCanvasGroup != null)
        {
            _uiCanvasGroup.alpha = 0f;
            _uiCanvasGroup.blocksRaycasts = false;
            _uiCanvasGroup.interactable = false;
        }

        if (uiGroup != null) uiGroup.SetActive(false);

        if (manageCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    private void CacheUIBits()
    {
        if (uiGroup == null) return;

        _uiRect = uiGroup.GetComponent<RectTransform>();

        _uiCanvasGroup = uiGroup.GetComponent<CanvasGroup>();
        if (_uiCanvasGroup == null) _uiCanvasGroup = uiGroup.AddComponent<CanvasGroup>();

        // Only initialize base shown position ONCE
        if (_uiRect != null && !_posInitialized)
        {
            _baseShownPos = _uiRect.anchoredPosition;
            _posInitialized = true;
        }

        if (_posInitialized)
        {
            _shownPos = _baseShownPos;
            _hiddenPos = _baseShownPos + hiddenOffset;
        }

        if (activationImageObject != null && _activationCanvasGroup == null)
        {
            _activationCanvasGroup = activationImageObject.GetComponent<CanvasGroup>();
            if (_activationCanvasGroup == null) _activationCanvasGroup = activationImageObject.AddComponent<CanvasGroup>();
        }
    }


    // ------------ Activation image fade ------------
    private void StartActivationFade()
    {
        if (activationImageObject == null) return;

        CacheUIBits();
        if (_activationCanvasGroup == null) return;

        activationImageObject.SetActive(true);
        _activationCanvasGroup.alpha = 1f;

        if (_activationFadeRoutine != null) StopCoroutine(_activationFadeRoutine);
        _activationFadeRoutine = StartCoroutine(ActivationFadeRoutine());
    }

    private IEnumerator ActivationFadeRoutine()
    {
        float dur = Mathf.Max(0.01f, activationFadeSeconds);
        float t = 0f;

        // alpha(u) = 1 - log(1 + s*u) / log(1 + s)
        float denom = Mathf.Log10(1f + logStrength);

        while (t < dur)
        {
            float u = t / dur;

            float a = 1f - (Mathf.Log10(1f + logStrength * u) / denom);
            _activationCanvasGroup.alpha = Mathf.Clamp01(a);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _activationCanvasGroup.alpha = 0f;
        activationImageObject.SetActive(false);
        _activationFadeRoutine = null;
    }
}
