using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

public class CorePlayer : NetworkPlayer
{
    // Input values
    public Vector2 MoveInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool InteractInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool InteractPressedThisFrame { get; private set; }

    // Reference to PlayerInput component
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    void LateUpdate()
    {
        JumpPressedThisFrame = false;
        InteractPressedThisFrame = false;

        if(transform.position.y < -10)
        {
            transform.position = new Vector3(transform.position.x, 6, transform.position.z);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Any spawn-time logic common to server/client can go here
    }
    private void OnEnable()
    {
        // Subscribe to input actions
        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;

        playerInput.actions["Sprint"].performed += OnSprint;
        playerInput.actions["Sprint"].canceled += OnSprint;

        playerInput.actions["Interact"].performed += OnInteract;
        playerInput.actions["Interact"].canceled += OnInteract;

        playerInput.actions["Look"].performed += OnLook;
        playerInput.actions["Look"].canceled += OnLook;

        playerInput.actions["Jump"].performed += OnJump;
        playerInput.actions["Jump"].canceled += OnJump;
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled
        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;

        playerInput.actions["Sprint"].performed -= OnSprint;
        playerInput.actions["Sprint"].canceled -= OnSprint;

        playerInput.actions["Interact"].performed -= OnInteract;
        playerInput.actions["Interact"].canceled -= OnInteract;

        playerInput.actions["Look"].performed -= OnLook;
        playerInput.actions["Look"].canceled -= OnLook;

        playerInput.actions["Jump"].performed -= OnJump;
        playerInput.actions["Jump"].canceled -= OnJump;
    }

    // Handlers
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        SprintInput = context.ReadValueAsButton();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        InteractInput = context.ReadValueAsButton();
        if (context.performed)
            InteractPressedThisFrame = true;
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        JumpInput = context.ReadValueAsButton();
        if (context.performed)
            JumpPressedThisFrame = true; // Only true the frame button is pressed
    }


}
