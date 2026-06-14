using UnityEngine;

/// TEMP motor: character controller mover for local feel.
/// Later: replace with PredictedMotor + NGO reconciliation.
[RequireComponent(typeof(CharacterController))]
public class SimpleMotor : MonoBehaviour, IMotor
{
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8.5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float gravity = -20f;
    [SerializeField] Transform cameraPivot;

    CharacterController _cc;
    Vector2 _move, _look;
    float _vy;
    bool _sprinting;

    void Awake() => _cc = GetComponent<CharacterController>();

    public void SetMoveInput(Vector2 move) => _move = Vector2.ClampMagnitude(move, 1f);
    public void SetLookInput(Vector2 look) => _look = look;
    public void SetSprinting(bool sprint) => _sprinting = sprint;

    public void Jump()
    {
        if (_cc.isGrounded) _vy = jumpForce;
    }

    void Update()
    {
        // rotate camera pivot
        if (cameraPivot)
        {
            var yaw = cameraPivot.parent; // assume pivot under a parent we can yaw
            yaw.Rotate(0f, _look.x, 0f, Space.World);
            cameraPivot.Rotate(-_look.y, 0f, 0f, Space.Self);
        }

        // move relative to yaw
        var forward = cameraPivot ? cameraPivot.parent.forward : Vector3.forward;
        var right = cameraPivot ? cameraPivot.parent.right : Vector3.right;

        var desired = (right * _move.x + forward * _move.y).normalized;
        float spd = _sprinting ? sprintSpeed : walkSpeed;

        if (_cc.isGrounded && _vy < 0f) _vy = -2f;
        _vy += gravity * Time.deltaTime;

        var vel = desired * spd + Vector3.up * _vy;
        _cc.Move(vel * Time.deltaTime);

        // face move direction
        if (desired.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desired, Vector3.up), 12f * Time.deltaTime);
    }
}
