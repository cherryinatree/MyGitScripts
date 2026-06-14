using UnityEngine;

namespace Cherry.Cameras
{
    [DisallowMultipleComponent]
    public class SecurityCameraRig : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Camera cam;
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform pitchPivot;
        [Tooltip("Optional. If null, translation will be applied to pitchPivot.")]
        [SerializeField] private Transform slidePivot;

        [Header("Identity")]
        [SerializeField] private string cameraDisplayName = "Security Cam";

        [Header("Rotation Speeds")]
        [SerializeField] private float yawSpeed = 120f;   // degrees/sec
        [SerializeField] private float pitchSpeed = 90f;  // degrees/sec

        [Header("Yaw Limits")]
        [SerializeField] private bool limitYaw = true;

        [Tooltip("If true, yaw limits are centered around the camera's starting yaw (recommended).")]
        [SerializeField] private bool yawLimitsRelativeToStart = true;

        [Tooltip("Max degrees left/right from the center yaw when using relative limits.")]
        [SerializeField] private float yawRange = 70f; // +/- range

        [Tooltip("Used only if yawLimitsRelativeToStart is false.")]
        [SerializeField] private float minYaw = -90f;

        [Tooltip("Used only if yawLimitsRelativeToStart is false.")]
        [SerializeField] private float maxYaw = 90f;

        [Header("Pitch Limits")]
        [SerializeField] private bool limitPitch = true;

        [Tooltip("If true, pitch limits are centered around the camera's starting pitch.")]
        [SerializeField] private bool pitchLimitsRelativeToStart = true;

        [Tooltip("Used when pitchLimitsRelativeToStart is true (center +/-).")]
        [SerializeField] private float pitchRange = 35f;

        [Tooltip("Used only if pitchLimitsRelativeToStart is false.")]
        [SerializeField] private float minPitch = -35f;

        [Tooltip("Used only if pitchLimitsRelativeToStart is false.")]
        [SerializeField] private float maxPitch = 35f;

        [Header("Slide (Local X/Y)")]
        [SerializeField] private bool allowSlide = true;
        [SerializeField] private float slideSpeed = 0.6f; // units/sec
        [SerializeField] private Vector2 slideXLimits = new Vector2(-0.25f, 0.25f);
        [SerializeField] private Vector2 slideYLimits = new Vector2(-0.15f, 0.15f);

        [Header("Zoom (FOV)")]
        [SerializeField] private bool allowZoom = true;
        [SerializeField] private float zoomSpeed = 50f;   // fov units/sec
        [SerializeField] private float minFov = 20f;
        [SerializeField] private float maxFov = 70f;

        private float _yaw;          // stored as -180..180 relative angle when using relative limits
        private float _pitch;        // stored as -180..180 relative angle when using relative limits
        private Vector3 _slideLocal;
        private float _defaultFov;

        private float _yawCenter;    // starting yaw (normalized -180..180)
        private float _pitchCenter;  // starting pitch (normalized -180..180)

        public string DisplayName => cameraDisplayName;
        public Camera Camera => cam;

        private void Reset()
        {
            cam = GetComponentInChildren<Camera>();
        }

        private void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (yawPivot == null) yawPivot = transform;
            if (pitchPivot == null) pitchPivot = cam != null ? cam.transform : transform;
            if (slidePivot == null) slidePivot = pitchPivot;

            _defaultFov = cam != null ? cam.fieldOfView : 60f;

            // Capture starting orientation as "center"
            _yawCenter = NormalizeAngle(yawPivot.localEulerAngles.y);
            _pitchCenter = NormalizeAngle(pitchPivot.localEulerAngles.x);

            // Initialize current relative angles
            _yaw = 0f;
            _pitch = 0f;

            // Initialize slide
            _slideLocal = slidePivot != null ? slidePivot.localPosition : Vector3.zero;

            ApplyYaw();
            ApplyPitch();

            Deactivate();
        }

        public void Activate(RenderTexture sharedTarget)
        {
            if (cam == null) return;
            cam.targetTexture = sharedTarget;
            cam.enabled = true;
        }

        public void Deactivate()
        {
            if (cam == null) return;
            cam.enabled = false;
            cam.targetTexture = null;
        }

        public void ApplyControl(Vector2 look, Vector2 move, float zoom, float dt)
        {
            // look: (x = yaw, y = pitch) typically from mouse delta / stick

            if (yawPivot != null)
            {
                _yaw += look.x * yawSpeed * dt;

                if (limitYaw)
                {
                    if (yawLimitsRelativeToStart)
                    {
                        _yaw = Mathf.Clamp(_yaw, -Mathf.Abs(yawRange), Mathf.Abs(yawRange));
                    }
                    else
                    {
                        // Absolute clamp in local space
                        float absYaw = NormalizeAngle(_yawCenter + _yaw);
                        absYaw = ClampAngle(absYaw, minYaw, maxYaw);
                        _yaw = NormalizeAngle(absYaw - _yawCenter);
                    }
                }

                ApplyYaw();
            }

            if (pitchPivot != null)
            {
                _pitch -= look.y * pitchSpeed * dt; // invert so mouse up looks up

                if (limitPitch)
                {
                    if (pitchLimitsRelativeToStart)
                    {
                        _pitch = Mathf.Clamp(_pitch, -Mathf.Abs(pitchRange), Mathf.Abs(pitchRange));
                    }
                    else
                    {
                        float absPitch = NormalizeAngle(_pitchCenter + _pitch);
                        absPitch = Mathf.Clamp(absPitch, minPitch, maxPitch);
                        _pitch = NormalizeAngle(absPitch - _pitchCenter);
                    }
                }

                ApplyPitch();
            }

            // move: (x = left/right, y = up/down)
            if (allowSlide && slidePivot != null)
            {
                _slideLocal += new Vector3(move.x, move.y, 0f) * slideSpeed * dt;
                _slideLocal.x = Mathf.Clamp(_slideLocal.x, slideXLimits.x, slideXLimits.y);
                _slideLocal.y = Mathf.Clamp(_slideLocal.y, slideYLimits.x, slideYLimits.y);
                slidePivot.localPosition = _slideLocal;
            }

            // zoom: usually scroll Y
            if (allowZoom && cam != null)
            {
                if (Mathf.Abs(zoom) > 0.0001f)
                {
                    cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - zoom * zoomSpeed * dt, minFov, maxFov);
                }
            }
        }

        public void ResetView()
        {
            _yaw = 0f;
            _pitch = 0f;
            _slideLocal = Vector3.zero;

            ApplyYaw();
            ApplyPitch();

            if (slidePivot != null) slidePivot.localPosition = Vector3.zero;
            if (cam != null) cam.fieldOfView = _defaultFov;
        }

        private void ApplyYaw()
        {
            if (yawPivot == null) return;
            float absYaw = _yawCenter + _yaw;
            yawPivot.localRotation = Quaternion.Euler(0f, absYaw, 0f);
        }

        private void ApplyPitch()
        {
            if (pitchPivot == null) return;
            float absPitch = _pitchCenter + _pitch;
            pitchPivot.localRotation = Quaternion.Euler(absPitch, 0f, 0f);
        }

        // Convert 0..360 to -180..180
        private static float NormalizeAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f) degrees -= 360f;
            if (degrees < -180f) degrees += 360f;
            return degrees;
        }

        // Clamp an angle that might wrap around 180/-180.
        // Assumes min/max are given in -180..180 and min < max, typical for yaw clamps like -90..90.
        private static float ClampAngle(float angle, float min, float max)
        {
            angle = NormalizeAngle(angle);
            min = NormalizeAngle(min);
            max = NormalizeAngle(max);

            // Simple case: range does not cross wrap boundary
            if (min <= max)
                return Mathf.Clamp(angle, min, max);

            // If it did cross boundary (rare), clamp to whichever side is closer
            // Example: min=170, max=-170 means allowed near the wrap.
            bool inRange = angle >= min || angle <= max;
            if (inRange) return angle;

            float distToMin = Mathf.Abs(NormalizeAngle(angle - min));
            float distToMax = Mathf.Abs(NormalizeAngle(angle - max));
            return distToMin < distToMax ? min : max;
        }
    }
}
