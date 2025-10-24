using UnityEngine;

public class JumpPad : MonoBehaviour
{
    public LayerMask collideLayer; // Layer to identify the player
    public float jumpForce = 10f; // Adjust the force as needed
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == collideLayer)
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply an upward force to the player
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z); // Adjust the Y value for jump height
            }
        }
    }
}
