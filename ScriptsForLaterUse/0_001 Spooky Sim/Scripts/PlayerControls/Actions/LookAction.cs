using UnityEngine;

public class LookAction : PlayerAction
{
    [Header("Targets")]
    [Tooltip("Left/Right rotation target. Use your player root (the CharacterController object).")]
    [SerializeField] private Transform yawTarget;
    [Tooltip("Up/Down pivot (a child, e.g., CameraRig). Optional; leave null to skip pitch.")]
    [SerializeField] private Transform pitchTarget;

    [Header("Sensitivity")]
    [Tooltip("Degrees per mouse pixel.")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [Tooltip("Degrees per second at full stick deflection.")]
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private bool invertY = false;

    [Header("Pitch Clamp")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnEnable = true;
    [SerializeField] private CursorLockMode lockMode = CursorLockMode.Locked;
    [SerializeField] private bool hideCursor = true;

    // Internal state (do NOT re-read Euler angles each frame)
    private float _yaw;   // world yaw in degrees
    private float _pitch; // local pitch in degrees

    protected void Start()
    {
        base.Awake();

        if (!yawTarget) yawTarget = core.transform; // safe default
        if (!pitchTarget) // pitch optional, try to find a camera rig
        {
            var cam = Camera.main;
            if (cam && cam.transform.parent) pitchTarget = cam.transform.parent;
        }

        // Initialize ONCE from current transforms (avoid continuous reads)
        _yaw = yawTarget ? yawTarget.rotation.eulerAngles.y : 0f;

        if (pitchTarget)
        {
            float px = pitchTarget.localRotation.eulerAngles.x;
            _pitch = (px > 180f) ? px - 360f : px; // convert to signed range
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }
    }

    protected override void Subscribe(CorePlayer c) => BindContinuousInputs(true);
    protected override void Unsubscribe(CorePlayer c) => BindContinuousInputs(false);

    protected override void OnEnable()
    {
        base.OnEnable();
        if (lockCursorOnEnable) { Cursor.lockState = lockMode; Cursor.visible = !hideCursor; }
    }

    protected override void OnLookContinuous(Vector2 look)
    {
        if (!IsContextAllowed() || yawTarget == null) return;

        // Decide scaling: mouse is delta-per-frame; stick is rate-per-second
        bool isMouse = false;
        var pi = core.PlayerInput;
        if (pi != null && !string.IsNullOrEmpty(pi.currentControlScheme))
            isMouse = pi.currentControlScheme.Contains("Mouse");

        float dx = isMouse ? look.x * mouseSensitivity
                           : look.x * stickSensitivity * Time.deltaTime;

        float dy = isMouse ? look.y * mouseSensitivity
                           : look.y * stickSensitivity * Time.deltaTime;

        if (invertY) dy = -dy;

        // Accumulate yaw/pitch
        _yaw += dx;
        if (pitchTarget)
        {
            _pitch -= dy;                  // positive Y looks up
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Apply directly (no smoothing)
        yawTarget.rotation = Quaternion.Euler(0f, _yaw, 0f);
        if (pitchTarget) pitchTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
