using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cherry.Player
{
    [AddComponentMenu("Cherry/Player/Zero-G Thruster Controller")]
    [DisallowMultipleComponent]
    public class ZeroGThrusterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform cameraTransform;

        [Header("Enable Condition")]
        [Tooltip("Controller becomes active when Rigidbody.useGravity == false.")]
        [SerializeField] private bool activeWhenGravityOff = true;

        [Header("Thruster Tuning")]
        [Tooltip("Acceleration (m/s^2) for WASD thrust relative to camera direction.")]
        [SerializeField] private float lateralAcceleration = 12f;

        [Tooltip("Acceleration (m/s^2) for Space/Ctrl (up/down) thrust.")]
        [SerializeField] private float verticalAcceleration = 12f;

        [Tooltip("Max speed cap. 0 disables cap.")]
        [SerializeField] private float maxSpeed = 10f;

        [Tooltip("If true, applies damping when no input to help the player stop drifting.")]
        [SerializeField] private bool dampWhenNoInput = true;

        [Tooltip("How quickly velocity damps when no input. (bigger = stops faster)")]
        [SerializeField] private float noInputDamping = 2.0f;

        [Header("Axis / Up Direction")]
        [Tooltip("If true, up/down uses world up (Vector3.up). If false, uses this transform's up.")]
        [SerializeField] private bool useWorldUp = true;

        [Header("Look / Turn (Yaw)")]
        [Tooltip("Mouse X sensitivity in degrees per second (scaled by Time.deltaTime).")]
        [SerializeField] private float yawSensitivity = 180f;

        [Tooltip("If true, only rotate while Zero-G is active.")]
        [SerializeField] private bool rotateOnlyWhileZeroG = true;

        [Tooltip("If true, locks cursor while Zero-G is active.")]
        [SerializeField] private bool lockCursorWhileZeroG = true;

        [Header("Optional: Override Drag While Zero-G")]
        [SerializeField] private bool overrideDragWhileZeroG = true;
        [SerializeField] private float zeroGDrag = 0.05f;
        [SerializeField] private float zeroGAngularDrag = 0.05f;

        [Header("Disable These Scripts While Zero-G Is Active")]
        [Tooltip("Put your normal movement / look / gravity-based controller scripts here.")]
        [SerializeField] private List<Behaviour> disableWhileZeroG = new();

        // internal
        private bool _isActive;
        private readonly Dictionary<Behaviour, bool> _prevEnabled = new();

        private float _prevDrag;
        private float _prevAngularDrag;

        private float _yawInputThisFrame; // cached from Update -> applied in FixedUpdate

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
            var cam = Camera.main;
            cameraTransform = cam ? cam.transform : null;
        }

        private void Awake()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
            if (!cameraTransform)
            {
                var cam = Camera.main;
                cameraTransform = cam ? cam.transform : null;
            }

            if (rb)
            {
                _prevDrag = rb.linearDamping;
                _prevAngularDrag = rb.angularDamping;
            }

            RefreshActiveState(force: true);
        }

        private void OnEnable()
        {
            RefreshActiveState(force: true);
        }

        private void OnDisable()
        {
            SetActive(false);
        }

        private void Update()
        {
            RefreshActiveState(force: false);

            // Cache yaw input each frame; apply it in FixedUpdate via MoveRotation.
            if (!_isActive && rotateOnlyWhileZeroG)
            {
                _yawInputThisFrame = 0f;
                return;
            }

            _yawInputThisFrame = ReadMouseX();
        }

        private void FixedUpdate()
        {
            if (!_isActive || rb == null) return;

            // --- Yaw rotation (turn body left/right) ---
            if (Mathf.Abs(_yawInputThisFrame) > 0.0001f)
            {
                float dt = Time.fixedDeltaTime;

                Vector3 upAxis = useWorldUp ? Vector3.up : transform.up;

                // degrees this fixed step
                float yawDegrees = _yawInputThisFrame * yawSensitivity * dt;

                Quaternion delta = Quaternion.AngleAxis(yawDegrees, upAxis);
                rb.MoveRotation(delta * rb.rotation);
            }

            // --- Thruster movement ---
            ReadMoveInput(out float moveX, out float moveY, out float upDown);

            Vector3 up = useWorldUp ? Vector3.up : transform.up;

            // Camera-relative lateral axes projected onto plane so WASD doesn't fight Space/Ctrl
            Vector3 camFwd = cameraTransform ? cameraTransform.forward : transform.forward;
            Vector3 camRight = cameraTransform ? cameraTransform.right : transform.right;

            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(camFwd, up);
            if (forwardOnPlane.sqrMagnitude < 0.0001f) forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, up);
            forwardOnPlane.Normalize();

            Vector3 rightOnPlane = Vector3.ProjectOnPlane(camRight, up);
            if (rightOnPlane.sqrMagnitude < 0.0001f) rightOnPlane = Vector3.ProjectOnPlane(transform.right, up);
            rightOnPlane.Normalize();

            Vector3 accel =
                (rightOnPlane * moveX + forwardOnPlane * moveY) * lateralAcceleration +
                (up * upDown) * verticalAcceleration;

            bool hasInput = (Mathf.Abs(moveX) > 0.001f) || (Mathf.Abs(moveY) > 0.001f) || (Mathf.Abs(upDown) > 0.001f);

            // Optional speed cap
            if (maxSpeed > 0.01f)
            {
                Vector3 v = rb.linearVelocity;
                float speed = v.magnitude;

                if (speed > maxSpeed && accel.sqrMagnitude > 0.0001f)
                {
                    Vector3 vDir = v / speed;
                    float pushInVelDir = Vector3.Dot(accel, vDir);
                    if (pushInVelDir > 0f)
                        accel -= vDir * pushInVelDir;
                }
            }

            if (accel.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(accel, ForceMode.Acceleration);
            }
            else if (dampWhenNoInput && !hasInput)
            {
                rb.AddForce(-rb.linearVelocity * noInputDamping, ForceMode.Acceleration);
            }
        }

        private void RefreshActiveState(bool force)
        {
            if (rb == null) return;

            bool shouldBeActive = !activeWhenGravityOff || (rb.useGravity == false);

            if (!force && shouldBeActive == _isActive) return;

            SetActive(shouldBeActive);
        }

        private void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;

            if (lockCursorWhileZeroG)
            {
                if (_isActive)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (rb != null && overrideDragWhileZeroG)
            {
                if (_isActive)
                {
                    _prevDrag = rb.linearDamping;
                    _prevAngularDrag = rb.angularDamping;
                    rb.linearDamping = zeroGDrag;
                    rb.angularDamping = zeroGAngularDrag;
                }
                else
                {
                    rb.linearDamping = _prevDrag;
                    rb.angularDamping = _prevAngularDrag;
                }
            }

            // Disable/restore other scripts
            if (_isActive)
            {
                _prevEnabled.Clear();
                foreach (var b in disableWhileZeroG)
                {
                    if (!b) continue;
                    _prevEnabled[b] = b.enabled;
                    b.enabled = false;
                }
            }
            else
            {
                foreach (var kvp in _prevEnabled)
                {
                    if (kvp.Key) kvp.Key.enabled = kvp.Value;
                }
                _prevEnabled.Clear();
            }
        }

        private static int BoolToAxis(bool pos, bool neg) => (pos ? 1 : 0) + (neg ? -1 : 0);

        private void ReadMoveInput(out float moveX, out float moveY, out float upDown)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                moveY = BoolToAxis(kb.wKey.isPressed, kb.sKey.isPressed);
                moveX = BoolToAxis(kb.dKey.isPressed, kb.aKey.isPressed);

                bool up = kb.spaceKey.isPressed;
                bool down = (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
                upDown = BoolToAxis(up, down);
                return;
            }
#endif
            moveY = BoolToAxis(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));
            moveX = BoolToAxis(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
            upDown = BoolToAxis(Input.GetKey(KeyCode.Space),
                                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        }

        private float ReadMouseX()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.delta.ReadValue().x; // pixels this frame
#endif
            return Input.GetAxisRaw("Mouse X");
        }
    }
}
