using UnityEngine;

public class TruckController : MonoBehaviour
{
    public float acceleration = 10f;
    public float turnSpeed = 50f;
    public float maxSpeed = 20f;
    public float brakeForce = 30f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 forwardMovement = Vector3.zero;
        float turn = 0f;

        // Acceleration
        if (Input.GetKey(KeyCode.W))
        {
            if (rb.linearVelocity.magnitude < maxSpeed)
                forwardMovement = transform.forward * acceleration;
        }

        // Reverse
        if (Input.GetKey(KeyCode.S))
        {
            if (rb.linearVelocity.magnitude < maxSpeed)
                forwardMovement = -transform.forward * acceleration * 0.5f; // slower reverse
        }

        // Turning
        if (Input.GetKey(KeyCode.A))
        {
            turn = -turnSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            turn = turnSpeed;
        }

        // Apply movement
        rb.AddForce(forwardMovement, ForceMode.Acceleration);

        // Apply turning
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            Quaternion turnOffset = Quaternion.Euler(0, turn * Time.fixedDeltaTime, 0);
            rb.MoveRotation(rb.rotation * turnOffset);
        }

        // Brake
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity *= 0.95f;
        }
    }
}