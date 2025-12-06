using Cherry.Combat;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

public class GhostVisionToggleHDRP : PlayerAction
{
    [Header("Input (New Input System optional)")]
    [SerializeField] private InputActionReference toggleAction; // bind to keyboard/t (Press)

    [Header("Custom Passes")]
    [Tooltip("The Custom Pass Volume that has the FullScreen pass using MAT_GhostVision, disabled by default.")]
    [SerializeField] private CustomPassVolume ghostVisionPassVolume;

    [Tooltip("The material used by the FullScreen pass (same one assigned in the volume).")]
    [SerializeField] private Material ghostVisionMaterial; // SG_GhostVision_Fullscreen instance

    [Tooltip("Optional: the same volume also contains a Draw Renderers pass for the 'Ghost' layer.")]
    [SerializeField] private bool enableGhostXray = true;

    [Header("Blend")]
    [SerializeField, Min(0f)] private float blendSeconds = 0.35f;
    [SerializeField] private string strengthProperty = "_Strength";

    [Header("Ghost Layer Reveal (Camera Culling)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask ghostLayer; // create "Ghost" layer and assign

    public bool IsOn { get; private set; }

    [Header("Battery")]
    public BatteryConsumer batteryConsumer; // set drainPerSecond in inspector
    public float batteryConsumptionPerSecond = 1f;

    Coroutine _fade;
    int _strengthID;

    protected override void Awake()
    {
        base.Awake();

        if (batteryConsumer == null)
            batteryConsumer = GetComponent<BatteryConsumer>();

        if (!targetCamera) targetCamera = Camera.main;
        _strengthID = Shader.PropertyToID(strengthProperty);

        if (ghostVisionMaterial) ghostVisionMaterial.SetFloat(_strengthID, 0f);
        if (ghostVisionPassVolume) ghostVisionPassVolume.gameObject.SetActive(false);

        // Hide ghosts by default
        SetGhostLayerVisible(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (toggleAction)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnTogglePerformed;
        }
    }

    protected override void OnDisable()
    {
        if (toggleAction)
        {
            toggleAction.action.performed -= OnTogglePerformed;
            toggleAction.action.Disable();
        }
        base.OnDisable();
    }

    // PlayerAction requirements (not used here)
    protected override void Subscribe(CorePlayer c) { }
    protected override void Unsubscribe(CorePlayer c) { }

    void Update()
    {
        // Fallback if no InputActionReference assigned
        if (!toggleAction && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            Toggle();

        if(!batteryConsumer.CanUse() && IsOn)
        {
            ToggleGhostVision(false);
        }
    }

    void OnTogglePerformed(InputAction.CallbackContext _) => Toggle();

    public void Toggle()
    {
        //SetGhostVision(!IsOn);
    }

    public void ToggleGhostVision(bool toggleOn)
    {
        SetGhostVision(toggleOn);
    }

    public void SetGhostVision(bool enable)
    {
        if (!IsOn)
        {
            if (!batteryConsumer.CanUse()) return; // no battery, can't turn on
        }
        if (IsOn == enable) return;
        IsOn = enable;

        if (!IsOn)
            batteryConsumer.StopDrain("GhostVision");
        else
        batteryConsumer.StartDrain("GhostVision", batteryConsumptionPerSecond);

        if (ghostVisionPassVolume && ghostVisionMaterial)
            ghostVisionPassVolume.gameObject.SetActive(true); // ensure active while we fade
        if (!gameObject.activeSelf) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeStrength(IsOn));

        SetGhostLayerVisible(IsOn && enableGhostXray);
    }

    IEnumerator FadeStrength(bool toOn)
    {
        if (ghostVisionMaterial == null)
        {
            yield break;
        }

        float start = ghostVisionMaterial.GetFloat(_strengthID);
        float end = toOn ? 1f : 0f;
        float t = 0f;

        while (t < blendSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = blendSeconds > 0f ? Mathf.SmoothStep(0f, 1f, t / blendSeconds) : 1f;
            float v = Mathf.Lerp(start, end, k);
            ghostVisionMaterial.SetFloat(_strengthID, v);
            yield return null;
        }

        ghostVisionMaterial.SetFloat(_strengthID, end);

        // Turn the volume off when fully faded out to save cost
        if (!toOn && ghostVisionPassVolume)
            ghostVisionPassVolume.gameObject.SetActive(false);

        _fade = null;
    }

    void SetGhostLayerVisible(bool show)
    {
        if (!targetCamera) return;
        int mask = targetCamera.cullingMask;
        int ghosts = ghostLayer.value;
        if (show) mask |= ghosts;
        else mask &= ~ghosts;
        targetCamera.cullingMask = mask;
    }
}
