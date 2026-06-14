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

    public event Action OnFpressedStarted;
    public event Action OnFpressedCanceled;

    public event Action OnCpressedStarted;
    public event Action OnCpressedCanceled;
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
    private InputAction _aMove, _aLook, _aSprint, _aJump, _aInteract, _aDrop, _aAttack, _aFpressed, _aCpressed;

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
        _aFpressed = a["Fpressed"];
        _aCpressed = a["Cpressed"];
    }
    private void OnEnable()
    {
        if (_aSprint != null) { _aSprint.performed += OnSprintPerformed; _aSprint.canceled += OnSprintCanceled; }
        if (_aJump != null) { _aJump.started += OnJumpStartedCb; _aJump.canceled += OnJumpCanceledCb; }
        if (_aInteract != null) { _aInteract.started += OnInteractStartedCb; _aInteract.canceled += OnInteractCanceledCb; }
        if (_aDrop != null) { _aDrop.started += OnDropStartedCb; _aDrop.canceled += OnDropCanceledCb; }
        if (_aAttack != null) { _aAttack.started += OnAttackStartedCb; _aAttack.canceled += OnAttackCanceledCb; }
        if (_aFpressed != null) { _aFpressed.started += OnFStartedCb; _aFpressed.canceled += OnFCanceledCb; }
        if (_aCpressed != null) { _aCpressed.started += OnCStartedCb; _aCpressed.canceled += OnCCanceledCb; }
    }

    private void OnDisable()
    {
        if (_aSprint != null) { _aSprint.performed -= OnSprintPerformed; _aSprint.canceled -= OnSprintCanceled; }
        if (_aJump != null) { _aJump.started -= OnJumpStartedCb; _aJump.canceled -= OnJumpCanceledCb; }
        if (_aInteract != null) { _aInteract.started -= OnInteractStartedCb; _aInteract.canceled -= OnInteractCanceledCb; }
        if (_aDrop != null) { _aDrop.started -= OnDropStartedCb; _aDrop.canceled -= OnDropCanceledCb; }
        if (_aAttack != null) { _aAttack.started -= OnAttackStartedCb; _aAttack.canceled -= OnAttackCanceledCb; }
        if (_aFpressed != null) { _aFpressed.started -= OnFStartedCb; _aFpressed.canceled -= OnFCanceledCb; }
        if (_aCpressed != null) { _aCpressed.started -= OnCStartedCb; _aCpressed.canceled -= OnCCanceledCb; }
    }

    private void Update()
    {
        _moveInput = _aMove != null ? _aMove.ReadValue<Vector2>() : Vector2.zero;
        _lookInput = _aLook != null ? _aLook.ReadValue<Vector2>() : Vector2.zero;

        OnMove?.Invoke(_moveInput);
        OnLook?.Invoke(_lookInput);
    }

    // --- named callbacks ---
    private void OnSprintPerformed(InputAction.CallbackContext _) => _sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext _) => _sprintHeld = false;

    private void OnJumpStartedCb(InputAction.CallbackContext _) => OnJumpStarted?.Invoke();
    private void OnJumpCanceledCb(InputAction.CallbackContext _) => OnJumpCanceled?.Invoke();

    private void OnInteractStartedCb(InputAction.CallbackContext _) => OnInteractStarted?.Invoke();
    private void OnInteractCanceledCb(InputAction.CallbackContext _) => OnInteractCanceled?.Invoke();

    private void OnDropStartedCb(InputAction.CallbackContext _) => DropPressedStarted?.Invoke();
    private void OnDropCanceledCb(InputAction.CallbackContext _) => DropPressedCanceled?.Invoke();

    private void OnAttackStartedCb(InputAction.CallbackContext _) => OnAttackStarted?.Invoke();
    private void OnAttackCanceledCb(InputAction.CallbackContext _) => OnAttackCanceled?.Invoke();

    private void OnFStartedCb(InputAction.CallbackContext _) => OnFpressedStarted?.Invoke();
    private void OnFCanceledCb(InputAction.CallbackContext _) => OnFpressedCanceled?.Invoke();

    private void OnCStartedCb(InputAction.CallbackContext _) => OnCpressedStarted?.Invoke();
    private void OnCCanceledCb(InputAction.CallbackContext _) => OnCpressedCanceled?.Invoke();


}
