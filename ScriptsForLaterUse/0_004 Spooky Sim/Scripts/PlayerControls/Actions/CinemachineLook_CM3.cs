using UnityEngine;
using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
#endif

/// <summary>
/// Drives Cinemachine 3 Pan/Tilt from input (mouse delta or right stick).
/// - If yawMode = CinemachinePanAxis: camera yaw is stored in PanAxis.Value.
/// - If yawMode = RotateYawTransform: yaw rotates a provided transform (typical FPS body yaw),
///   while only pitch uses TiltAxis.
/// </summary>
[DefaultExecutionOrder(-10)]
public class CinemachineLook_CM3 : MonoBehaviour
{
    public enum YawMode
    {
        CinemachinePanAxis,
        RotateYawTransform
    }

    [Header("Cinemachine")]
    [SerializeField] private CinemachinePanTilt panTilt;

    [Header("Yaw")]
    [SerializeField] private YawMode yawMode = YawMode.CinemachinePanAxis;
    [Tooltip("Used when YawMode = RotateYawTransform (e.g., your player body root).")]
    [SerializeField] private Transform yawTransform;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Vector2 Look action (Mouse delta / Right stick). Optional if you use PlayerInput Send Messages -> OnLook().")]
    [SerializeField] private InputActionReference lookAction;
#endif

    [Header("Tuning")]
    [SerializeField] private float sensitivity = 0.12f; // degrees per input unit
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool noTurning = false;


    [Tooltip("OFF for mouse delta, ON for sticks (usually).")]
    [SerializeField] private bool multiplyByDeltaTime = false;

    [Header("Pitch Clamp (degrees)")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    // Internal state (degrees)
    private float _yaw;
    private float _pitch;

    // If you use PlayerInput (Send Messages), it can feed here
    private Vector2 _lookFromMessages;

    private void Reset()
    {
        panTilt = GetComponentInChildren<CinemachinePanTilt>(true);
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (lookAction != null) lookAction.action.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (lookAction != null) lookAction.action.Disable();
#endif
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (panTilt != null)
        {
            _yaw = panTilt.PanAxis.Value;
            _pitch = panTilt.TiltAxis.Value;
        }
    }

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Optional: If using PlayerInput with Behavior = Send Messages,
    /// name your action "Look" and it will call OnLook automatically.
    /// </summary>
    public void OnLook(InputValue value)
    {
        _lookFromMessages = value.Get<Vector2>();
    }
#endif

    private void Update()
    {
        if (panTilt == null)
            return;

        Vector2 look = ReadLook();
        if (look == Vector2.zero)
            return;

        float dtMul = multiplyByDeltaTime ? Time.deltaTime : 1f;

        float dx = look.x * sensitivity * dtMul;
        float dy = look.y * sensitivity * dtMul;

        if (noTurning)
        {
            dx = 0f;
        }

        // Typical FPS: mouse up => look up (negative pitch)
        dy = invertY ? dy : -dy;

        // --- Yaw ---
        if (yawMode == YawMode.CinemachinePanAxis)
        {
            _yaw = ApplyAxisDelta(_yaw, dx, panTilt.PanAxis);
            panTilt.PanAxis.Value = _yaw;
        }
        else // RotateYawTransform
        {
            _yaw += dx;
            if (yawTransform != null)
                yawTransform.localRotation = Quaternion.Euler(0f, _yaw, 0f);

            // Keep CM pan at its center (prevents double-yaw)
            panTilt.PanAxis.Value = panTilt.PanAxis.Center;
        }

        // --- Pitch ---
        _pitch += dy;

        // Clamp to BOTH your clamp and Cinemachine axis range
        Vector2 axisRange = panTilt.TiltAxis.Range;
        float lo = Mathf.Max(minPitch, axisRange.x);
        float hi = Mathf.Min(maxPitch, axisRange.y);
        _pitch = Mathf.Clamp(_pitch, lo, hi);

        panTilt.TiltAxis.Value = _pitch;

        // If we were fed by Send Messages, consume per-frame
        _lookFromMessages = Vector2.zero;
    }

    private Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        if (lookAction != null)
            return lookAction.action.ReadValue<Vector2>();
        return _lookFromMessages;
#else
        // Legacy fallback (won't work if old input is disabled)
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
    }

    private static float ApplyAxisDelta(float current, float delta, InputAxis axis)
    {
        float next = current + delta;

        // Wrap or clamp based on axis settings
        float min = axis.Range.x;
        float max = axis.Range.y;

        if (axis.Wrap)
        {
            float range = max - min;
            if (range <= 0.0001f) return min;

            next = (next - min) % range;
            if (next < 0) next += range;
            return next + min;
        }

        return Mathf.Clamp(next, min, max);
    }
}
