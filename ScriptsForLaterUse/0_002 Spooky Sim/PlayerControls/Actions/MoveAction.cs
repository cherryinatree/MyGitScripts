using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Rigidbody locomotion that NEVER rotates the player.
/// Camera-relative input, slope projection, momentum-friendly braking,
/// and a short landing grace to keep speed on touchdown.
/// </summary>
public class MoveAction : PlayerAction
{
    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Acceleration (m/s^2)")]
    [SerializeField] private float accelGround = 45f;
    [SerializeField] private float accelAir = 18f;
    [SerializeField] private float brakeGround = 20f;   // only when no input & grounded

    [Header("Landing Feel")]
    [Tooltip("Skip braking for this long after becoming grounded.")]
    [SerializeField] private float landingGraceTime = 0.12f;

    [Header("Input")]
    [SerializeField] private bool normalizeInput = true;
    [SerializeField, Range(0f, 0.25f)] private float inputDeadzone = 0.05f;

    [Header("Camera (optional)")]
    [SerializeField] private Transform cameraTransform; // if null, uses Camera.main

    [Header("Grounding")]
    [SerializeField] private float groundProbeLength = 0.35f;
    [SerializeField] private LayerMask groundMask = ~0;

    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private Vector2 _moveInput;
    private Vector3 _desiredDir;   // world-space planar direction
    private bool _wasGrounded;
    private float _landingGraceTimer;

    // --- PlayerAction wiring ---
    protected void Start()
    {
        base.Awake();
        _rb = core.Body;
        _capsule = core.Capsule;
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    protected override void Subscribe(CorePlayer c) => BindContinuousInputs(true);
    protected override void Unsubscribe(CorePlayer c) => BindContinuousInputs(false);

    protected override void OnMoveContinuous(Vector2 move)
    {
        var v = normalizeInput ? Vector2.ClampMagnitude(move, 1f) : move;
        _moveInput = (v.magnitude < inputDeadzone) ? Vector2.zero : v;
    }

    private void FixedUpdate()
    {
  if (!core || _rb == null) return;
      cameraTransform = transform;

      // Camera-relative orthonormal basis (right = up × forward)
      Vector3 up = Vector3.up;
      Vector3 camFwd = transform ? transform.forward : Vector3.forward;
      camFwd = Vector3.ProjectOnPlane(camFwd, up).normalized;
      if (camFwd.sqrMagnitude < 0.0001f) camFwd = Vector3.forward;
      Vector3 camRight = Vector3.Cross(up, camFwd).normalized;

      // Desired planar direction from input
      _desiredDir = (_moveInput.sqrMagnitude > 0f)
          ? (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized
          : Vector3.zero;

      // Ground state & normal
      bool grounded = core.IsGrounded;
      Vector3 groundNormal = Vector3.up;
      if (grounded && TryGetGroundNormal(out var n)) groundNormal = n;

      // Project desired dir onto ground plane for smooth slopes
      if (grounded && _desiredDir != Vector3.zero)
          _desiredDir = Vector3.ProjectOnPlane(_desiredDir, groundNormal).normalized;

      // Landing grace
      if (grounded && !_wasGrounded) _landingGraceTimer = landingGraceTime;
      if (_landingGraceTimer > 0f) _landingGraceTimer -= Time.fixedDeltaTime;

      // Speeds and planar steering
      float targetSpeed = (_desiredDir == Vector3.zero) ? 0f :
          (core.IsSprinting ? sprintSpeed : walkSpeed);

      Vector3 v = _rb.linearVelocity;
      Vector3 vPlanar = new Vector3(v.x, 0f, v.z);
      Vector3 targetPlanar = _desiredDir * targetSpeed;

      if (_desiredDir != Vector3.zero)
      {
          float maxStep = (grounded ? accelGround : accelAir) * Time.fixedDeltaTime;
          Vector3 delta = targetPlanar - vPlanar;
          Vector3 step = Vector3.ClampMagnitude(delta, maxStep);
          _rb.AddForce(step / Time.fixedDeltaTime, ForceMode.Acceleration);
      }
      else if (grounded && _landingGraceTimer <= 0f && vPlanar.sqrMagnitude > 1e-6f)
      {
          float maxBrake = brakeGround * Time.fixedDeltaTime;
          Vector3 desired = Vector3.MoveTowards(vPlanar, Vector3.zero, maxBrake);
          Vector3 change = desired - vPlanar;
          _rb.AddForce(change / Time.fixedDeltaTime, ForceMode.Acceleration);
      }

      // Absolutely NO rotation here — look script owns rotation.
      _wasGrounded = grounded;
}

    private bool TryGetGroundNormal(out Vector3 normal)
    {
        if (_capsule == null) { normal = Vector3.up; return false; }

        var center = core.transform.position + _capsule.center;
        float bottomY = center.y - (_capsule.height * 0.5f) + _capsule.radius;
        Vector3 origin = new(center.x, bottomY + 0.02f, center.z);

        if (Physics.SphereCast(origin, Mathf.Max(0.01f, _capsule.radius * 0.95f),
                               Vector3.down, out var hit, groundProbeLength, groundMask,
                               QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }
        normal = Vector3.up;
        return false;
    }
}
