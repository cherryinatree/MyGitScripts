using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Isolated player movement: walk + sprint only.
/// - No jumping
/// - No look/rotation
/// - No dependencies on other scripts
/// - Uses CharacterController
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MoveAction : MonoBehaviour
{

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.0f;

    [Header("Smoothing")]
    [Tooltip("How fast movement speed changes. 0 = instant.")]
    [SerializeField] private float acceleration = 12f;

    [Header("Optional Grounding")]
    [Tooltip("If true, applies simple gravity so you stay on slopes/ground. No jump.")]
    [SerializeField] private bool useGravity = false;
    [SerializeField] private float gravity = -9.81f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System (Optional)")]
    [Tooltip("Vector2 Move action (WASD/Left Stick).")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("Button Sprint action (Hold).")]
    [SerializeField] private InputActionReference sprintAction;
#endif

    private CharacterController controller;
    private float currentSpeed;
    private float verticalVelocity; // only used if useGravity = true

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null) moveAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null) moveAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
#endif
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        bool sprintHeld = ReadSprintInput();

        // Build movement direction in local space (relative to player forward/right)
        Vector3 moveDir = (transform.right * input.x + transform.forward * input.y);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;

        // Smooth toward target speed
        if (acceleration <= 0f)
            currentSpeed = targetSpeed;
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        Vector3 velocity = moveDir * currentSpeed;

        // Optional simple gravity to keep grounded (no jump)
        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f; // tiny downward push to stick to ground
            verticalVelocity += gravity * Time.deltaTime;
            velocity.y = verticalVelocity;
        }
        else
        {
            velocity.y = 0f;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null)
            return moveAction.action.ReadValue<Vector2>();
#endif
        // Fallback: keyboard WASD / arrows
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        return new Vector2(x, y);
    }

    private bool ReadSprintInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (sprintAction != null)
            return sprintAction.action.IsPressed();
#endif
        // Fallback: left shift
        return Input.GetKey(KeyCode.LeftShift);
    }
}