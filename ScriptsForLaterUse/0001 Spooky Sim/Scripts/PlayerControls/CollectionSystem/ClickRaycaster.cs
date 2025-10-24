using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[AddComponentMenu("Gameplay/Click Raycaster (Hold + Travel)")]
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class ClickRaycaster : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [Tooltip("Bind to your Button action named 'Click'. Press = start, Release = stop.")]
    [SerializeField] private InputActionReference clickAction;

    [Header("Ray Origin & Aim")]
    [Tooltip("If assigned, the beam starts here; otherwise it starts at the camera position.")]
    [SerializeField] private Transform originOverride;
    [Tooltip("Camera that defines the aim direction (forward). If null, tries Camera on this object, then Camera.main.")]
    [SerializeField] private Camera cam;
    [SerializeField, Min(0.1f)] private float maxDistance = 100f;
    [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Ignore;
    [Tooltip("If true and nothing is hit, the beam still draws out to maxDistance.")]
    [SerializeField] private bool drawOnMiss = true;

    [Header("Beam Animation")]
    [SerializeField, Min(0f)] private float extendDuration = 0.08f;   // time for tip to reach target
    [SerializeField, Min(0f)] private float retractDuration = 0.06f;  // time to retract on release
    [SerializeField] private bool retractOnRelease = true;

    [Header("Beam Visuals")]
    [SerializeField, Min(0.0005f)] private float beamWidth = 0.01f;
    [Tooltip("Optional unlit material (Built-in/URP/HDRP) for the LineRenderer.")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private Color missColor = Color.red;

    [Header("Events")]
    public UnityEvent onAnyHit;
    [Serializable] public class GameObjectEvent : UnityEvent<GameObject> { }
    [Serializable] public class RaycastHitEvent : UnityEvent<RaycastHit> { }
    public GameObjectEvent onHitObject;
    public RaycastHitEvent onHitInfo;
    public UnityEvent onMiss;

    // ---- internals ----
    private LineRenderer lr;
    private bool isHeld;
    private float animT;                 // 0..1 for extend/retract
    private float targetLength;          // current desired length
    private float currentLength;         // what we’re actually drawing (animating toward target)
    private Collider lastHit;            // for change-detection

    Timer timer;
    float delay = 0.5f;

    private void Awake()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        lr = GetComponent<LineRenderer>();
        lr.enabled = false;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.widthMultiplier = beamWidth;
        if (lineMaterial) lr.material = lineMaterial;
        timer = new Timer(delay);
    }

    private void OnEnable()
    {
        if (clickAction == null || clickAction.action == null)
        {
            Debug.LogError($"{name}: ClickRaycasterHold needs an InputActionReference assigned to 'clickAction'.");
            return;
        }

        // Start/hold on press, stop on release
        clickAction.action.started += OnClickStartedOrPerformed;
        clickAction.action.performed += OnClickStartedOrPerformed;
        clickAction.action.canceled += OnClickCanceled;
        clickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.started -= OnClickStartedOrPerformed;
            clickAction.action.performed -= OnClickStartedOrPerformed;
            clickAction.action.canceled -= OnClickCanceled;
            clickAction.action.Disable();
        }
        lr.enabled = false;
        isHeld = false;
    }

    private void OnClickStartedOrPerformed(InputAction.CallbackContext _)
    {
        if (cam == null) return;

        isHeld = true;
        animT = 0f;
        currentLength = 0f;
        lr.enabled = true;
        lr.widthMultiplier = beamWidth;
        lastHit = null;
    }

    private void OnClickCanceled(InputAction.CallbackContext _)
    {
        isHeld = false;
        if (!retractOnRelease)
        {
            lr.enabled = false;
        }
        // if retractOnRelease == true, Update() will animate back to 0 then hide
    }

    private void Update()
    {
        if (!lr.enabled && !isHeld) return;
        if (cam == null) return;

        // Build ray from camera forward, origin can be overridden (e.g., a muzzle)
        Vector3 origin = originOverride ? originOverride.position : cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // Determine target end
        bool hitSomething = Physics.Raycast(new Ray(origin, dir), out RaycastHit hit, maxDistance, hitMask, queryTriggers);
        Vector3 targetEnd = hitSomething ? hit.point : origin + dir * maxDistance;
        targetLength = (hitSomething || drawOnMiss) ? Vector3.Distance(origin, targetEnd) : 0f;

        // Animate length
        float dt = Time.deltaTime;

        if (isHeld)
        {
            // Extend toward full target length
            if (extendDuration > 0f)
            {
                animT = Mathf.Clamp01(animT + dt / extendDuration);
                currentLength = Mathf.Lerp(0f, targetLength, animT);
            }
            else
            {
                animT = 1f;
                currentLength = targetLength;
            }
        }
        else
        {
            // Released: retract if desired
            if (retractOnRelease)
            {
                if (retractDuration > 0f)
                {
                    animT = Mathf.Clamp01(animT - dt / retractDuration);
                    currentLength = Mathf.Lerp(0f, targetLength, animT);
                }
                else
                {
                    currentLength = 0f;
                    animT = 0f;
                }

                if (animT <= 0.0001f)
                {
                    lr.enabled = false;
                    return;
                }
            }
            else
            {
                // Not retracting—just hide
                lr.enabled = false;
                return;
            }
        }

        // Pick color
        lr.material.SetColor("_EmissionColor", hitSomething ? hitColor : missColor);

        // Final end position = origin + dir * currentLength
        Vector3 end = origin + dir * currentLength;

        // Draw
        lr.SetPosition(0, origin);
        lr.SetPosition(1, end);

        // Fire events (on change only to avoid spamming)
        if (isHeld)
        {
            if (hitSomething)
            {
                if (lastHit != hit.collider)
                {
                    lastHit = hit.collider;
                    onAnyHit?.Invoke();
                    onHitObject?.Invoke(hit.collider.gameObject);
                    onHitInfo?.Invoke(hit);

                    //if (timer.ClockTick())
                    // {
                    //  timer.RestartTimer();
                    //}
                }
                GetComponent<BeamHitRouter>()?.HandleHit(hit);
            }
            else
            {
                if (lastHit != null)
                {
                    lastHit = null;
                    onMiss?.Invoke();
                }
            }
        }
    }
}
