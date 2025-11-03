using UnityEngine;
using UnityEngine.InputSystem; // <-- new Input System namespace

/// <summary>
/// RB jump with coyote, buffered input, double jump (re-press), fall multiplier,
/// jump-cut (short hop), and fall-speed clamp. Drop on same object as CorePlayer.
/// </summary>
public class JumpAction : PlayerAction
{
    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 7.8f;        // upward m/s applied on jump
    [SerializeField] private bool allowDoubleJump = true;
    [Tooltip("Require releasing the jump button before allowing double jump.")]
    [SerializeField] private bool requireRepressForDouble = true;

    [Header("Timing Quality")]
    [SerializeField] private float coyoteTime = 0.12f;  // after leaving ground
    [SerializeField] private float jumpBufferTime = 0.12f;  // press before landing

    [Header("Gravity Shaping")]
    [Tooltip("Extra gravity when rising and player RELEASES jump (short hop).")]
    [SerializeField] private float jumpCutMultiplier = 2.2f;
    [Tooltip("Extra gravity when falling (snappier fall).")]
    [SerializeField] private float fallMultiplier = 2.6f;
    [Tooltip("Clamp downward speed to avoid floaty terminal fall.")]
    [SerializeField] private float maxFallSpeed = -45f;

    [Header("Misc")]
    [Tooltip("Zero downward velocity before applying jump.")]
    [SerializeField] private bool clearDownwardBeforeJump = true;

    private Rigidbody _rb;

    // Input/state
    private bool _jumpHeld;
    private bool _jumpReleasedSinceLastJump = true; // to enforce re-press for double
    private int _jumpsUsed;                        // 0 = none, 1 = used first, 2 = used double

    // Timers
    private float _coyoteTimer;
    private float _bufferTimer;



    protected void Start()
    {
        base.Awake();
        _rb = core.Body;
    }

    protected override void Subscribe(CorePlayer c)
    {
        c.OnJumpStarted += OnJumpPressed;
        c.OnJumpCanceled += OnJumpReleased;
        BindContinuousInputs(true); // to tick timers via Update
    }

    protected override void Unsubscribe(CorePlayer c)
    {
        c.OnJumpStarted -= OnJumpPressed;
        c.OnJumpCanceled -= OnJumpReleased;
        BindContinuousInputs(false);
    }

    private void Update()
    {
        // Ground tracking for coyote + reset jumps
        if (core.IsGrounded)
        {
            _coyoteTimer = coyoteTime;
            _jumpsUsed = 0;
            if (!_jumpHeld) _jumpReleasedSinceLastJump = true; // allow re-press logic to reset on ground
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        // Handle buffered jump
        if (_bufferTimer > 0f)
        {
            _bufferTimer -= Time.deltaTime;
            TryConsumeBufferedJump();
        }
    }

    private void FixedUpdate()
    {
        // Per-character gravity shaping (add on top of RB gravity)
        Vector3 v = _rb.linearVelocity;

        if (v.y < 0f)
        {
            // Falling: stronger gravity
            v.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            if (v.y < maxFallSpeed) v.y = maxFallSpeed;
        }
        else if (v.y > 0f && !_jumpHeld)
        {
            // Rising but jump released: short hop
            v.y += Physics.gravity.y * (jumpCutMultiplier - 1f) * Time.fixedDeltaTime;
        }

        _rb.linearVelocity = v;
    }

    private void OnJumpPressed()
    {
        if (!IsContextAllowed()) return;

        _jumpHeld = true;
        _bufferTimer = jumpBufferTime; // remember the press shortly
        TryConsumeBufferedJump();
    }

    private void OnJumpReleased()
    {
        _jumpHeld = false;
        // We only care about the flag for allowing the double jump
        _jumpReleasedSinceLastJump = true;
    }

    private void TryConsumeBufferedJump()
    {
        // If we can jump now, do it and clear buffer
        if (CanGroundOrCoyoteJump())
        {
            PerformJump();
            _bufferTimer = 0f;
            return;
        }

        // Airborne double jump logic
        if (CanDoubleJump())
        {
            PerformJump();
            _bufferTimer = 0f;
        }
    }

    private bool CanGroundOrCoyoteJump()
    {
        // Either grounded right now or inside coyote window, AND we have a buffered press
        if (_bufferTimer <= 0f) return false;

        bool eligible = core.IsGrounded || _coyoteTimer > 0f;
        return eligible;
    }

    private bool CanDoubleJump()
    {
        if (!allowDoubleJump) return false;
        if (_bufferTimer <= 0f) return false;
        if (_jumpsUsed == 0) return false; // you must have jumped once already
        if (_jumpsUsed >= 2) return false; // no more than 2 jumps total

        // Require re-press (prevents "holding" from auto-firing double)
        if (requireRepressForDouble && !_jumpReleasedSinceLastJump) return false;

        return true;
    }

    private void PerformJump()
    {
        Vector3 v = _rb.linearVelocity;

        if (clearDownwardBeforeJump && v.y < 0f)
            v.y = 0f; // snappier jump out of a fall

        // Set exact upward speed for crisp takeoff (more reliable than AddForce alone)
        v.y = jumpSpeed;
        _rb.linearVelocity = v;

        _jumpsUsed = Mathf.Min(2, _jumpsUsed + 1);
        _coyoteTimer = 0f;
        _bufferTimer = 0f;
        _jumpReleasedSinceLastJump = false;
    }
}
