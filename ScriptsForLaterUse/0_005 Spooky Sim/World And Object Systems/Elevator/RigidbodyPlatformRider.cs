using UnityEngine;

[AddComponentMenu("Cherry/World/Rigidbody Platform Rider")]
public class RigidbodyPlatformRider : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody playerRb;

    [Header("Grounding")]
    [Tooltip("How 'upward' a contact normal must be to count as standing on top. 0.5 ~ 60 degrees.")]
    [Range(0f, 1f)]
    [SerializeField] private float groundedNormalThreshold = 0.5f;

    [Header("Behavior")]
    [Tooltip("If true, uses platform Rigidbody if present; otherwise uses transform delta.")]
    [SerializeField] private bool preferPlatformRigidbody = true;

    private Transform _platformTf;
    private Rigidbody _platformRb;

    private Vector3 _lastPlatformPos;
    private Quaternion _lastPlatformRot;

    private void Awake()
    {
        if (playerRb == null) playerRb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_platformTf == null) return;

        // Compute how far the platform moved since last FixedUpdate
        Vector3 platformPos = _platformTf.position;

        Vector3 deltaPos;
        if (preferPlatformRigidbody && _platformRb != null)
        {
            // Kinematic RB moved with MovePosition has a meaningful velocity
            deltaPos = _platformRb.linearVelocity * Time.fixedDeltaTime;
        }
        else
        {
            deltaPos = platformPos - _lastPlatformPos;
        }

        // Apply the delta to the player AFTER your controller has done its movement.
        if (deltaPos.sqrMagnitude > 0f)
        {
            playerRb.MovePosition(playerRb.position + deltaPos);
        }

        _lastPlatformPos = platformPos;
        _lastPlatformRot = _platformTf.rotation;
    }

    private void OnCollisionStay(Collision collision)
    {
        // Find a "ground-like" contact (standing on top)
        for (int i = 0; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (Vector3.Dot(c.normal, Vector3.up) >= groundedNormalThreshold)
            {
                SetPlatform(collision.rigidbody, collision.transform);
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // If we leave the platform we're tracking, clear it
        if (_platformTf != null && collision.transform == _platformTf)
        {
            ClearPlatform();
        }
    }

    private void SetPlatform(Rigidbody rb, Transform tf)
    {
        if (_platformTf == tf) return;

        _platformTf = tf;
        _platformRb = rb;

        _lastPlatformPos = _platformTf.position;
        _lastPlatformRot = _platformTf.rotation;
    }

    private void ClearPlatform()
    {
        _platformTf = null;
        _platformRb = null;
    }
}

