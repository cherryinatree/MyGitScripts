using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private CoreEnemy core;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float acceleration = 10f;
    public float stopDrag = 5f;

    private Vector3 moveDirection;

    private void Start()
    {
        core = GetComponent<CoreEnemy>();
        rb = core.rb;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        MovePhysics();
    }

    public void SetMoveDirection(Vector3 dir)
    {
        moveDirection = dir.normalized;
    }

    // ✅ New function for states to call
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void MovePhysics()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 targetVelocity = moveDirection * moveSpeed;
            Vector3 velocityChange = targetVelocity - rb.linearVelocity;
            velocityChange.y = 0; // don’t cancel gravity

            rb.AddForce(velocityChange * acceleration, ForceMode.Acceleration);

            // Rotate toward movement direction
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 5f));
        }
        else
        {
            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, 0, Time.fixedDeltaTime * stopDrag),
                rb.linearVelocity.y,
                Mathf.Lerp(rb.linearVelocity.z, 0, Time.fixedDeltaTime * stopDrag)
            );
        }
    }
}
