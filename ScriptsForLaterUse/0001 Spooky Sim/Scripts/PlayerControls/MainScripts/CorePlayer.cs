using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CorePlayer : MonoBehaviour
{
    public PlayerInput PlayerInput { get; private set; }
    public Rigidbody Body { get; private set; }
    public CapsuleCollider Capsule { get; private set; }

    // ---- Contexts ----
    private readonly Stack<string> _ctx = new();
    public string CurrentContext => _ctx.Count > 0 ? _ctx.Peek() : "Gameplay";
    public event Action<string> OnContextChanged;
    public void PushContext(string ctx) { if (!string.IsNullOrWhiteSpace(ctx)) { _ctx.Push(ctx); OnContextChanged?.Invoke(CurrentContext); } }
    public void PopContext() { if (_ctx.Count > 0) { _ctx.Pop(); OnContextChanged?.Invoke(CurrentContext); } }

    // ---- Input state -> broadcast each frame ----
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _sprintHeld;
    public event Action<Vector2> OnMove;
    public event Action<Vector2> OnLook;

    public event Action OnJumpStarted;
    public event Action OnJumpCanceled;

    public event Action OnAttackStarted;
    public event Action OnAttackCanceled;
    // Optional others
    public event Action OnInteractStarted; public event Action OnInteractCanceled;

    public event Action InteractPressed; // pick / place / box-load / shelf-unload
    public event Action DropPressedStarted;     // drop the held item
    public event Action DropPressedCanceled;     // drop the held item


    // ---- Ground check (physics) ----
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundProbeExtra = 0.08f; // probe a little below feet
    [SerializeField] private float groundRadiusInset = 0.02f; // shrink radius slightly

    public bool IsGrounded
    {
        get
        {
            // Sphere cast from capsule bottom
            var center = transform.position + Capsule.center;
            float bottomY = center.y - (Capsule.height * 0.5f) + Capsule.radius;
            Vector3 origin = new Vector3(center.x, bottomY + 0.01f, center.z);
            float radius = Mathf.Max(0.01f, Capsule.radius - groundRadiusInset);
            return Physics.SphereCast(origin, radius, Vector3.down, out _, groundProbeExtra, groundMask, QueryTriggerInteraction.Ignore);
        }
    }

    // Sprint access for actions
    public bool IsSprinting => _sprintHeld;

    // ---- Input Actions ----
    private InputAction _aMove, _aLook, _aSprint, _aJump, _aInteract, _aDrop, _aAttack;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        Body = GetComponent<Rigidbody>();
        Capsule = GetComponent<CapsuleCollider>();
        if (_ctx.Count == 0) _ctx.Push("Gameplay");

        // RB defaults for a player controller
        Body.interpolation = RigidbodyInterpolation.Interpolate;
        Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY; // yaw only
        Body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var a = PlayerInput.actions;
        _aMove = a["Move"];
        _aLook = a["Look"];
        _aSprint = a["Sprint"];
        _aJump = a["Jump"];
        _aInteract = a["Interact"];
        _aDrop = a["Drop"];
        _aAttack = a["Click"];
    }

    private void Start()
    {
        //Body.freezeRotation = true; // ensure rotation is frozen after physics init
    }

    private void OnEnable()
    {
        PlayerInput.actions?.Enable();

        if (_aMove != null) { _aMove.performed += c => _moveInput = c.ReadValue<Vector2>(); _aMove.canceled += c => _moveInput = Vector2.zero; }
        if (_aLook != null) { _aLook.performed += c => _lookInput = c.ReadValue<Vector2>(); _aLook.canceled += c => _lookInput = Vector2.zero; }
        if (_aSprint != null) { _aSprint.performed += _ => _sprintHeld = true; _aSprint.canceled += _ => _sprintHeld = false; }
        if (_aJump != null) { _aJump.started += _ => OnJumpStarted?.Invoke(); _aJump.canceled += _ => OnJumpCanceled?.Invoke(); }
        if (_aInteract != null) { _aInteract.started += _ => OnInteractStarted?.Invoke(); _aInteract.canceled += _ => OnInteractCanceled?.Invoke(); }
        if (_aDrop != null) { _aDrop.started += _ => DropPressedStarted?.Invoke(); _aDrop.canceled += _ => DropPressedCanceled?.Invoke(); }
        if (_aAttack != null) { _aAttack.started += _ => OnAttackStarted?.Invoke(); _aAttack.canceled += _ => OnAttackCanceled?.Invoke(); }
    }

    private void OnDisable()
    {
        PlayerInput.actions?.Disable();
    }

    private void Update()
    {
        //OnMove?.Invoke(_moveInput);
        OnLook?.Invoke(_lookInput);

    }

    // Jump helpers for actions
    public void SetVerticalVelocity(float vy)
    {
       // var v = Body.linearVelocity; v.y = vy; Body.linearVelocity = v;
    }
    public void AddJumpImpulse(float jumpSpeed)
    {
        // Convert desired initial upward speed to impulse: J = m * Δv
        Body.AddForce(Vector3.up * Body.mass * jumpSpeed, ForceMode.Impulse);
    }
    public void CutJump(float cutMultiplier = 0.5f)
    {
        if (Body.linearVelocity.y > 0f)
        {
            var v = Body.linearVelocity;
            v.y *= Mathf.Clamp01(cutMultiplier);
            Body.linearVelocity = v;
        }
    }
}
