using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cherry.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    [AddComponentMenu("Cherry/Character/Cherry Character Controller (Rigidbody)")]
    public class CherryCharacterController : MonoBehaviour
    {
        public enum PerspectiveMode { FirstPerson, ThirdPerson }

        [Header("General")]
        public bool controllerPaused = false;

        [Tooltip("Main camera used for look + interaction + third person orbit.")]
        public Camera playerCamera;

        public PerspectiveMode perspective = PerspectiveMode.FirstPerson;

        [Tooltip("Lock & hide mouse cursor.")]
        public bool lockAndHideCursor = true;

        [Tooltip("If true, will fallback to legacy Input.* if Input System devices are missing.")]
        public bool allowLegacyInputFallback = true;

        [Tooltip("Prints input + velocity once per second for debugging.")]
        public bool debugLogMovement = false;

        [Tooltip("Layers considered solid ground for grounding.")]
        public LayerMask groundMask = ~0;

        [Tooltip("Layers considered for camera obstruction (third-person).")]
        public LayerMask cameraObstructionMask = ~0;

        [Header("Input")]
        public bool enableInput = true;

#if ENABLE_INPUT_SYSTEM
        public Key sprintKey = Key.LeftShift;
        public Key crouchKey = Key.LeftCtrl;
        public Key jumpKey = Key.Space;
        public Key interactKey = Key.E;
        public Key switchPerspectiveKey = Key.Q;

        // Legacy fallback keys (used only if Keyboard.current is null)
        public KeyCode sprintKeyLegacy = KeyCode.LeftShift;
        public KeyCode crouchKeyLegacy = KeyCode.LeftControl;
        public KeyCode jumpKeyLegacy = KeyCode.Space;
        public KeyCode interactKeyLegacy = KeyCode.E;
        public KeyCode switchPerspectiveKeyLegacy = KeyCode.Q;
#else
        public KeyCode sprintKeyLegacy = KeyCode.LeftShift;
        public KeyCode crouchKeyLegacy = KeyCode.LeftControl;
        public KeyCode jumpKeyLegacy = KeyCode.Space;
        public KeyCode interactKeyLegacy = KeyCode.E;
        public KeyCode switchPerspectiveKeyLegacy = KeyCode.Q;
#endif

        [Tooltip("Toggle sprint instead of hold.")]
        public bool toggleSprint = false;

        [Tooltip("Toggle crouch instead of hold.")]
        public bool toggleCrouch = false;

        [Header("Camera")]
        public bool enableCameraControl = true;

        [Range(0.1f, 25f)]
        public float mouseSensitivity = 8f;

        [Range(0.1f, 30f)]
        public float lookSmoothing = 6f;

        [Range(30f, 180f)]
        public float pitchClamp = 170f;

        public float standingEyeHeight = 0.8f;
        public float crouchingEyeHeight = 0.25f;

        [Header("Third Person")]
        public float thirdPersonDistance = 4.5f;
        public float thirdPersonMinDistance = 0.15f;
        public float thirdPersonMaxDistance = 8f;
        public float thirdPersonZoomSensitivity = 5f;

        public bool rotateBodyToMoveDirection = true;

        [Header("Movement")]
        public float walkSpeed = 4.0f;
        public float sprintSpeed = 6.5f;
        public float crouchSpeed = 2.5f;

        public float acceleration = 30f;
        public float deceleration = 35f;

        [Range(0f, 1f)]
        public float airControl = 0.35f;

        public float jumpVelocity = 6.5f;
        public bool holdJump = false;

        public float slopeLimit = 70f;
        public float stickToGroundVelocity = 2.0f;

        [Header("Crouch")]
        public float standingCapsuleHeight = 2.0f;
        public float crouchingCapsuleHeight = 1.2f;
        public float stanceLerpSpeed = 10f;

        [Header("Grounding")]
        public float groundCheckRadius = 0.28f;
        public float groundCheckDistance = 0.55f;

        [Header("Moving Platforms")]
        public bool enableMovingPlatforms = true;
        public bool ignorePlatformVerticalVelocity = false;

        [Header("Interaction")]
        public float interactRange = 4f;
        public LayerMask interactMask = ~0;

        public event Action OnFootstep;

        private Rigidbody _rb;
        private CapsuleCollider _capsule;

        private Vector2 _moveInput;
        private Vector2 _lookInputRaw;
        private Vector2 _lookInputSmoothed;
        private Vector2 _lookVelRef;

        private bool _jumpHeld;
        private bool _jumpPressedThisFrame;

        private bool _sprintHeld;
        private bool _sprintPressedThisFrame;

        private bool _crouchHeld;
        private bool _crouchPressedThisFrame;

        private bool _interactPressedThisFrame;
        private bool _switchPerspectivePressedThisFrame;

        private bool _sprintToggled;
        private bool _crouchToggled;

        private bool _isGrounded;
        private float _groundAngle;
        private Vector3 _groundNormal = Vector3.up;
        private Rigidbody _groundRb;

        private float _yaw;
        private float _pitch;

        private bool _isCrouching;
        private float _currentEyeHeight;

        private Rigidbody _activePlatformRb;
        private Transform _activePlatformRoot;
        private Vector3 _activePlatformLastPos;
        private Vector3 _activePlatformVelocity;

        private float _debugNextTime;

        private void Reset()
        {
            // Sensible defaults when you add the component
            playerCamera = Camera.main;
            standingCapsuleHeight = 2f;
            crouchingCapsuleHeight = 1.2f;
            standingEyeHeight = 0.85f;
            crouchingEyeHeight = 0.35f;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.isKinematic = false;

            // Init capsule shape safely (prevents “half underground” lockups)
            _capsule.height = Mathf.Max(0.5f, standingCapsuleHeight);
            _capsule.center = Vector3.up * (_capsule.height * 0.5f);

            _currentEyeHeight = standingEyeHeight;

            if (playerCamera == null) playerCamera = Camera.main;

            Vector3 e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = 0f;
        }

        private void Start()
        {
            ApplyCursorState();
            ApplyCamera();
        }

        private void OnValidate()
        {
            standingCapsuleHeight = Mathf.Max(standingCapsuleHeight, 0.5f);
            crouchingCapsuleHeight = Mathf.Clamp(crouchingCapsuleHeight, 0.3f, standingCapsuleHeight);
            groundCheckRadius = Mathf.Max(0.05f, groundCheckRadius);
            groundCheckDistance = Mathf.Max(0.05f, groundCheckDistance);
            thirdPersonMaxDistance = Mathf.Max(thirdPersonMinDistance, thirdPersonMaxDistance);
        }

        private void Update()
        {
            if (!enableInput || controllerPaused)
            {
                ClearOneFrameInputs();
                return;
            }

            ReadInputs();
            HandleToggles();
            HandlePerspectiveSwitch();
            HandleInteraction();

            if (enableCameraControl)
            {
                UpdateLook();
                UpdateThirdPersonZoom();
            }
        }

        private void FixedUpdate()
        {
            if (controllerPaused) return;

            UpdateGrounding();
            UpdatePlatformVelocity();
            UpdateStance();

            ApplyMovement();
            ApplyJump();

            if (debugLogMovement && Time.time >= _debugNextTime)
            {
                _debugNextTime = Time.time + 1f;
                Vector3 platformVel = GetPlatformVelocity();
                Vector3 rel = _rb.linearVelocity - platformVel;
                Debug.Log($"[CherryCharacterController] move={_moveInput} grounded={_isGrounded} v={_rb.linearVelocity} rel={rel}");
            }
        }

        private void LateUpdate()
        {
            if (controllerPaused) return;
            if (!enableCameraControl) return;

            ApplyCamera();
        }

        public void SetActivePlatform(Rigidbody platformRb, Transform platformRoot)
        {
            _activePlatformRb = platformRb;
            _activePlatformRoot = platformRoot;
            if (_activePlatformRoot != null) _activePlatformLastPos = _activePlatformRoot.position;
            _activePlatformVelocity = Vector3.zero;
        }

        public void ClearActivePlatform(Transform platformRoot)
        {
            if (_activePlatformRoot != platformRoot) return;
            _activePlatformRb = null;
            _activePlatformRoot = null;
            _activePlatformVelocity = Vector3.zero;
        }

        private void ReadInputs()
        {
            _moveInput = Vector2.zero;
            _lookInputRaw = Vector2.zero;

            _jumpHeld = false;
            _jumpPressedThisFrame = false;

            _sprintHeld = false;
            _sprintPressedThisFrame = false;

            _crouchHeld = false;
            _crouchPressedThisFrame = false;

            _interactPressedThisFrame = false;
            _switchPerspectivePressedThisFrame = false;

#if ENABLE_INPUT_SYSTEM
            bool hasKB = Keyboard.current != null;
            bool hasMouse = Mouse.current != null;

            if (hasKB)
            {
                float x = 0f;
                float y = 0f;

                // Allow opposing keys to cancel out (better than ?: chain)
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.wKey.isPressed) y += 1f;

                _moveInput = new Vector2(x, y);

                if (sprintKey != Key.None)
                {
                    var kc = Keyboard.current[sprintKey];
                    _sprintHeld = kc != null && kc.isPressed;
                    _sprintPressedThisFrame = kc != null && kc.wasPressedThisFrame;
                }

                if (crouchKey != Key.None)
                {
                    var kc = Keyboard.current[crouchKey];
                    _crouchHeld = kc != null && kc.isPressed;
                    _crouchPressedThisFrame = kc != null && kc.wasPressedThisFrame;
                }

                if (jumpKey != Key.None)
                {
                    var kc = Keyboard.current[jumpKey];
                    _jumpHeld = kc != null && kc.isPressed;
                    _jumpPressedThisFrame = kc != null && kc.wasPressedThisFrame;
                }

                if (interactKey != Key.None)
                {
                    var kc = Keyboard.current[interactKey];
                    _interactPressedThisFrame = kc != null && kc.wasPressedThisFrame;
                }

                if (switchPerspectiveKey != Key.None)
                {
                    var kc = Keyboard.current[switchPerspectiveKey];
                    _switchPerspectivePressedThisFrame = kc != null && kc.wasPressedThisFrame;
                }
            }

            if (hasMouse)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                _lookInputRaw = d / 50f;
            }

            // If input system devices are missing, fallback to legacy
            if (allowLegacyInputFallback && (!hasKB || !hasMouse))
            {
                ReadLegacyFallback();
            }
#else
            ReadLegacyFallback();
#endif

            _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);
        }

        private void ReadLegacyFallback()
        {
            // Movement
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;

            _moveInput = new Vector2(x, y);

            // Look
            _lookInputRaw = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

#if ENABLE_INPUT_SYSTEM
            _sprintHeld = Input.GetKey(sprintKeyLegacy);
            _sprintPressedThisFrame = Input.GetKeyDown(sprintKeyLegacy);

            _crouchHeld = Input.GetKey(crouchKeyLegacy);
            _crouchPressedThisFrame = Input.GetKeyDown(crouchKeyLegacy);

            _jumpHeld = Input.GetKey(jumpKeyLegacy);
            _jumpPressedThisFrame = Input.GetKeyDown(jumpKeyLegacy);

            _interactPressedThisFrame = Input.GetKeyDown(interactKeyLegacy);
            _switchPerspectivePressedThisFrame = Input.GetKeyDown(switchPerspectiveKeyLegacy);
#else
            _sprintHeld = Input.GetKey(sprintKeyLegacy);
            _sprintPressedThisFrame = Input.GetKeyDown(sprintKeyLegacy);

            _crouchHeld = Input.GetKey(crouchKeyLegacy);
            _crouchPressedThisFrame = Input.GetKeyDown(crouchKeyLegacy);

            _jumpHeld = Input.GetKey(jumpKeyLegacy);
            _jumpPressedThisFrame = Input.GetKeyDown(jumpKeyLegacy);

            _interactPressedThisFrame = Input.GetKeyDown(interactKeyLegacy);
            _switchPerspectivePressedThisFrame = Input.GetKeyDown(switchPerspectiveKeyLegacy);
#endif
        }

        private void HandleToggles()
        {
            // FIX: toggles must use "pressed this frame", not "held"
            if (toggleSprint)
            {
                if (_sprintPressedThisFrame) _sprintToggled = !_sprintToggled;
            }
            else
            {
                _sprintToggled = _sprintHeld;
            }

            if (toggleCrouch)
            {
                if (_crouchPressedThisFrame) _crouchToggled = !_crouchToggled;
            }
            else
            {
                _crouchToggled = _crouchHeld;
            }

            _isCrouching = _crouchToggled;
        }

        private void HandlePerspectiveSwitch()
        {
            if (!_switchPerspectivePressedThisFrame) return;
            perspective = perspective == PerspectiveMode.FirstPerson ? PerspectiveMode.ThirdPerson : PerspectiveMode.FirstPerson;
        }

        private void HandleInteraction()
        {
            if (!_interactPressedThisFrame) return;
            TryInteract();
        }

        private void ClearOneFrameInputs()
        {
            _jumpPressedThisFrame = false;
            _sprintPressedThisFrame = false;
            _crouchPressedThisFrame = false;
            _interactPressedThisFrame = false;
            _switchPerspectivePressedThisFrame = false;
        }

        private void UpdateLook()
        {
            _lookInputSmoothed = Vector2.SmoothDamp(_lookInputSmoothed, _lookInputRaw, ref _lookVelRef,
                1f / Mathf.Max(lookSmoothing, 0.0001f));

            float sens = mouseSensitivity * 5f;

            _yaw += _lookInputSmoothed.x * sens;
            _pitch -= _lookInputSmoothed.y * sens;

            float half = pitchClamp * 0.5f;
            _pitch = Mathf.Clamp(_pitch, -half, half);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void UpdateThirdPersonZoom()
        {
            if (perspective != PerspectiveMode.ThirdPerson) return;

            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) scroll = Mouse.current.scroll.y.ReadValue() / 1000f;
#else
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
            if (Mathf.Abs(scroll) < 0.0001f) return;

            thirdPersonDistance = Mathf.Clamp(
                thirdPersonDistance - scroll * (thirdPersonZoomSensitivity * 2f),
                thirdPersonMinDistance,
                thirdPersonMaxDistance
            );
        }

        private void ApplyCamera()
        {
            if (playerCamera == null) return;

            Vector3 pivot = transform.position + Vector3.up * _currentEyeHeight;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

            if (perspective == PerspectiveMode.FirstPerson)
            {
                playerCamera.transform.position = pivot;
                playerCamera.transform.rotation = rot;
            }
            else
            {
                Vector3 desiredPos = pivot - (rot * Vector3.forward * thirdPersonDistance);
                Vector3 finalPos = ResolveThirdPersonObstruction(pivot, desiredPos);

                playerCamera.transform.position = finalPos;
                playerCamera.transform.rotation = rot;
            }
        }

        private Vector3 ResolveThirdPersonObstruction(Vector3 pivot, Vector3 desiredPos)
        {
            Vector3 dir = desiredPos - pivot;
            float dist = dir.magnitude;
            if (dist <= 0.001f) return desiredPos;

            dir /= dist;

            const float camRadius = 0.25f;
            if (Physics.SphereCast(pivot, camRadius, dir, out RaycastHit hit, dist,
                    cameraObstructionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0.05f, hit.distance * 0.9f);
                return pivot + dir * safeDist;
            }

            return desiredPos;
        }

        private void ApplyCursorState()
        {
            if (!lockAndHideCursor) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UpdateGrounding()
        {
            _isGrounded = false;
            _groundRb = null;
            _groundNormal = Vector3.up;
            _groundAngle = 0f;

            Vector3 origin = transform.position + Vector3.up * 0.1f;

            if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                _groundNormal = hit.normal;
                _groundAngle = Vector3.Angle(_groundNormal, Vector3.up);

                if (_groundAngle <= slopeLimit)
                {
                    _isGrounded = true;
                    _groundRb = hit.rigidbody;
                }
            }
        }

        private void UpdatePlatformVelocity()
        {
            _activePlatformVelocity = Vector3.zero;

            if (!enableMovingPlatforms) return;

            if (_activePlatformRoot != null)
            {
                Vector3 pos = _activePlatformRoot.position;
                Vector3 deltaVel = (pos - _activePlatformLastPos) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
                _activePlatformLastPos = pos;

                if (_activePlatformRb != null && _activePlatformRb.linearVelocity.sqrMagnitude > 0.0001f)
                    _activePlatformVelocity = _activePlatformRb.linearVelocity;
                else
                    _activePlatformVelocity = deltaVel;
            }
            else if (_isGrounded && _groundRb != null)
            {
                _activePlatformVelocity = _groundRb.linearVelocity;
            }

            if (ignorePlatformVerticalVelocity)
                _activePlatformVelocity = Vector3.ProjectOnPlane(_activePlatformVelocity, Vector3.up);
        }

        private Vector3 GetPlatformVelocity()
        {
            if (!enableMovingPlatforms) return Vector3.zero;
            return _isGrounded ? _activePlatformVelocity : Vector3.zero;
        }

        private void UpdateStance()
        {
            float targetHeight = _isCrouching ? crouchingCapsuleHeight : standingCapsuleHeight;
            float targetEye = _isCrouching ? crouchingEyeHeight : standingEyeHeight;

            _capsule.height = Mathf.MoveTowards(_capsule.height, targetHeight, stanceLerpSpeed * Time.fixedDeltaTime);
            _capsule.center = Vector3.up * (_capsule.height * 0.5f);

            _currentEyeHeight = Mathf.MoveTowards(_currentEyeHeight, targetEye, stanceLerpSpeed * Time.fixedDeltaTime);
        }

        private float GetTargetSpeed()
        {
            if (_isCrouching) return crouchSpeed;
            if (_sprintToggled && _moveInput.y > 0.1f) return sprintSpeed;
            return walkSpeed;
        }

        private Vector3 GetMoveDirectionWorld()
        {
            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 forward = yawRot * Vector3.forward;
            Vector3 right = yawRot * Vector3.right;

            Vector3 dir = forward * _moveInput.y + right * _moveInput.x;
            return Vector3.ClampMagnitude(dir, 1f);
        }

        private void ApplyMovement()
        {
            Vector3 platformVel = GetPlatformVelocity();

            float targetSpeed = GetTargetSpeed();
            Vector3 desiredDir = GetMoveDirectionWorld();

            Vector3 v = _rb.linearVelocity;
            Vector3 relV = v - platformVel;

            Vector3 relHoriz = new Vector3(relV.x, 0f, relV.z);
            float relY = relV.y;

            bool hasInput = desiredDir.sqrMagnitude > 0.0001f;

            float accel = _isGrounded ? acceleration : (acceleration * airControl);
            float decel = _isGrounded ? deceleration : (deceleration * airControl);

            Vector3 targetRelHoriz = desiredDir * targetSpeed;

            Vector3 newRelHoriz = hasInput
                ? Vector3.MoveTowards(relHoriz, targetRelHoriz, accel * Time.fixedDeltaTime)
                : Vector3.MoveTowards(relHoriz, Vector3.zero, decel * Time.fixedDeltaTime);

            if (_isGrounded && relY <= 0.01f)
                relY = -stickToGroundVelocity;

            Vector3 newRelV = new Vector3(newRelHoriz.x, relY, newRelHoriz.z);
            _rb.linearVelocity = newRelV + platformVel;

            if (perspective == PerspectiveMode.ThirdPerson && rotateBodyToMoveDirection && _isGrounded)
            {
                if (newRelHoriz.sqrMagnitude > 0.04f)
                {
                    float targetYaw = Mathf.Atan2(newRelHoriz.x, newRelHoriz.z) * Mathf.Rad2Deg;
                    Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.fixedDeltaTime);
                    _yaw = transform.eulerAngles.y;
                }
            }
        }

        private void ApplyJump()
        {
            bool wantsJump = holdJump ? _jumpHeld : _jumpPressedThisFrame;
            if (!wantsJump) return;
            if (!_isGrounded) return;
            if (_isCrouching) return;

            Vector3 platformVel = GetPlatformVelocity();

            Vector3 v = _rb.linearVelocity;
            Vector3 relV = v - platformVel;

            relV.y = jumpVelocity;
            _rb.linearVelocity = relV + platformVel;

            _jumpPressedThisFrame = false;
        }

        public bool TryInteract()
        {
            if (playerCamera == null) return false;

            Ray r = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(r, 0.25f, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null) return interactable.Interact();
            }
            return false;
        }
    }

    public interface IInteractable
    {
        bool Interact();
    }
}
