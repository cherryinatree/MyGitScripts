using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections; // <-- add this


public class WorldHintPanel : MonoBehaviour
{
    public enum AnchorMode { InFrontOfCamera, AnchorToTarget }
    public enum BillboardMode { FaceCameraFull, YawOnly, None }

    [Header("Anchoring")]
    public AnchorMode mode = AnchorMode.InFrontOfCamera;
    public Transform target;                // used in AnchorToTarget
    public Camera cam;                      // assign at runtime if null
    public float inFrontDistance = 1.2f;    // for InFrontOfCamera
    public Vector3 targetLocalOffset = new Vector3(0.0f, 0.5f, 0.0f);
    public float followLerp = 12f;          // smoother follow

    [Header("Billboarding")]
    public BillboardMode billboard = BillboardMode.YawOnly;

    [Header("Visibility & Clamping")]
    public bool clampToView = true;         // if offscreen, gently bias toward in front
    public float clampStrength = 0.6f;      // 0..1, how strongly to bias toward front
    public bool protectFromOcclusion = true;
    public LayerMask occlusionMask = ~0;
    public float occlusionPadding = 0.05f;

    [Header("Distance/Scale/Fade")]
    public bool scaleByDistance = true;
    public Vector2 scaleRangeMeters = new Vector2(0.8f, 6f); // map to min/max scale
    public Vector2 scaleMinMax = new Vector2(0.8f, 1.25f);
    public bool fadeByDistance = true;
    public Vector2 fadeRangeMeters = new Vector2(0.6f, 8f);  // alpha 1 → 0 across this
    public CanvasGroup group;               // assign
    public float appearFadeSpeed = 10f;
    public float disappearFadeSpeed = 10f;

    [Header("Content")]
    public TextMeshProUGUI label;           // assign
    public UnityEngine.UI.Image icon;       // optional

    [Header("Connector (optional)")]
    public LineRenderer connector;          // optional
    public bool drawConnector = false;
    public Vector3 connectorTargetOffset = Vector3.zero;

    // runtime
    Vector3 _vel;
    float _targetAlpha = 1f;
    Vector3 _desiredPos;

    Coroutine _hideRoutine;  // <-- add
    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!group) group = GetComponentInChildren<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();
    }

    void LateUpdate()
    {
        if (!cam) return;

        // Compute desired anchor position
        Vector3 anchorPos;
        if (mode == AnchorMode.InFrontOfCamera)
        {
            anchorPos = cam.transform.position + cam.transform.forward * inFrontDistance;
        }
        else
        {
            if (!target)
            {
                // fallback: go in front
                anchorPos = cam.transform.position + cam.transform.forward * inFrontDistance;
            }
            else
            {
                anchorPos = target.TransformPoint(targetLocalOffset);
            }
        }

        // Visibility/occlusion adjustment
        Vector3 toAnchor = anchorPos - cam.transform.position;
        float anchorDist = Mathf.Max(0.001f, toAnchor.magnitude);
        Vector3 anchorDir = toAnchor / anchorDist;

        // Clamp behind camera or too off-axis → bias toward in-front
        if (clampToView)
        {
            Vector3 fwd = cam.transform.forward;
            float facing = Vector3.Dot(fwd, anchorDir); // < 0 means behind
            if (facing < 0.05f)
            {
                anchorPos = Vector3.Lerp(anchorPos, cam.transform.position + fwd * inFrontDistance, clampStrength);
            }
            else
            {
                // optional gentle bias toward a forward arc
                Vector3 onArc = cam.transform.position + fwd * anchorDist;
                anchorPos = Vector3.Lerp(anchorPos, onArc, clampStrength * 0.25f);
            }
        }

        // Occlusion: pull toward camera if something blocks LOS
        if (protectFromOcclusion)
        {
            Vector3 rayDir = (anchorPos - cam.transform.position);
            float rayDist = rayDir.magnitude;
            if (rayDist > 0.001f)
            {
                rayDir /= rayDist;
                if (Physics.Raycast(cam.transform.position, rayDir, out RaycastHit hit, rayDist, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    anchorPos = hit.point - rayDir * occlusionPadding;
                }
            }
        }

        // Smooth follow
        _desiredPos = anchorPos;
        transform.position = Vector3.Lerp(transform.position, _desiredPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        // Billboard
        ApplyBillboard();

        // Scale & Fade by distance
        if (scaleByDistance)
        {
            float t = Mathf.InverseLerp(scaleRangeMeters.x, scaleRangeMeters.y, (transform.position - cam.transform.position).magnitude);
            float s = Mathf.Lerp(scaleMinMax.x, scaleMinMax.y, t);
            transform.localScale = Vector3.one * s;
        }

        if (fadeByDistance)
        {
            float t = Mathf.InverseLerp(fadeRangeMeters.y, fadeRangeMeters.x, (transform.position - cam.transform.position).magnitude);
            _targetAlpha = Mathf.Clamp01(t);
        }

        // Smooth alpha
        float speed = (_targetAlpha > group.alpha) ? appearFadeSpeed : disappearFadeSpeed;
        group.alpha = Mathf.MoveTowards(group.alpha, _targetAlpha, speed * Time.deltaTime);
        group.interactable = group.blocksRaycasts = (group.alpha > 0.95f);

        // Connector (optional)
        if (drawConnector && connector)
        {
            connector.enabled = true;
            connector.positionCount = 2;
            connector.SetPosition(0, transform.position);
            Vector3 tether = (target ? target.position + connectorTargetOffset : transform.position);
            connector.SetPosition(1, tether);
        }
        else if (connector)
        {
            connector.enabled = false;
        }
    }


    // ----- NEW: timed hide API -----
    public void HideAfter(float seconds)
    {
        CancelHide();
        if (seconds <= 0f) { Hide(); return; }
        _hideRoutine = StartCoroutine(HideAfterCo(seconds));
    }

    public void CancelHide()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }

    IEnumerator HideAfterCo(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            // If panel got disabled/destroyed, stop quietly
            if (!isActiveAndEnabled) yield break;
            t += Time.deltaTime;
            yield return null;
        }
        Hide();
        _hideRoutine = null;
    }

    public void Show() { CancelHide(); SetVisible(true); }   // <-- small tweak
    public void SetVisible(bool v) { _targetAlpha = v ? 1f : 0f; }

    void ApplyBillboard()
    {
        if (billboard == BillboardMode.None) return;

        Vector3 toCam = cam.transform.position - transform.position;
        toCam.y = (billboard == BillboardMode.YawOnly) ? 0f : toCam.y;

        if (toCam.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(-toCam.normalized, Vector3.up); // face camera
            transform.rotation = look;
        }
    }

    // --------- Public API ---------

    public void SetText(string text) { if (label) label.text = text; }
    public void SetIcon(Sprite s, bool show = true)
    {
        if (!icon) return;
        icon.sprite = s;
        icon.enabled = show && s != null;
    }

   // public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
    //public void SetVisible(bool v) => _targetAlpha = v ? 1f : 0f;

    public void ConfigureInFront(Camera camera, float distanceMeters, string text)
    {
        CancelHide(); // <-- ensure fresh timer each show
        if (cam == null)
        {
            cam = camera ? camera : Camera.main;
        }
        mode = AnchorMode.InFrontOfCamera;
        inFrontDistance = distanceMeters;
        SetText(text);
        Show();
    }

    public void ConfigureNextTo(Camera camera, Transform worldTarget, Vector3 localOffset, string text)
    {
        CancelHide(); // <-- ensure fresh timer each show
        if (cam == null)
        {
            cam = camera ? camera : Camera.main;
        }
        mode = AnchorMode.AnchorToTarget;
        target = worldTarget;
        targetLocalOffset = localOffset;
        SetText(text);
        Show();
    }

    // Convenience: from an InputActionReference, resolve a display string like "Press E"
    public void SetActionPrompt(string prefix, InputActionReference actionRef)
    {
        CancelHide(); // <-- ensure fresh timer each show
        if (!label || actionRef == null || actionRef.action == null) return;
        var opts = InputBinding.DisplayStringOptions.DontIncludeInteractions | InputBinding.DisplayStringOptions.DontOmitDevice;
        string bind = actionRef.action.GetBindingDisplayString(opts);
        label.text = string.IsNullOrEmpty(prefix) ? bind : $"{prefix} {bind}";
    }
}
