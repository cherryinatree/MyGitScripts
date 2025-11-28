using UnityEngine;

/// <summary>
/// Reusable obstacle avoidance steering:
/// - If MonsterPerception is assigned, uses its horizontal rays (fwd/right/left).
/// - Otherwise, does local sphere/ray casts from this transform.
/// Returns a steered direction that pushes away from nearby walls/obstacles,
/// centers in hallways, and deflects when forward space is tight.
/// </summary>
public class ObstacleAvoidance : MonoBehaviour
{
    [Header("Perception (optional)")]
    public MonsterPerception perception;    // if present, will use its rays
    public bool preferPerception = true;

    [Header("Local Casts (fallback)")]
    public LayerMask obstacleMask = ~0;
    public float castOriginHeight = 0.9f;
    public float sphereRadius = 0.3f;
    public float forwardCheckDistance = 2.0f;
    public float sideCheckDistance = 1.6f;

    [Header("Clearance Thresholds")]
    public float minForwardClearance = 1.2f; // if forward space < this, steer away
    public float minSideClearance = 0.9f;    // keep this distance from side walls

    [Header("Steering")]
    [Range(0f, 1f)] public float steerStrength = 0.5f; // how strongly to deflect
    [Range(0f, 1f)] public float hallwayCenterBias = 0.35f; // if both sides close, bias center

    // Perception ray indices (must match your MonsterPerception layout)
    private const int R_FWD = 0;
    private const int R_RIGHT = 2;
    private const int R_LEFT = 6;

    // Debug
    private Vector3 _lastInput, _lastOutput;

    /// <summary>
    /// Provide a desired (XZ) direction; get a steered (XZ) direction back.
    /// </summary>
    public Vector3 GetSteeredDirection(Vector3 desiredDir)
    {
        _lastInput = desiredDir;
        if (desiredDir.sqrMagnitude < 0.0001f) return desiredDir;

        Vector3 fwd = Flatten(desiredDir);
        Vector3 right = Flatten(transform.right);
        Vector3 left = -right;

        float fwdDist, rightDist, leftDist;
        if (preferPerception && perception != null && perception.RayDistances != null && perception.RayDistances.Length >= 8)
        {
            fwdDist = perception.RayDistances[R_FWD];
            rightDist = perception.RayDistances[R_RIGHT];
            leftDist = perception.RayDistances[R_LEFT];
        }
        else
        {
            // Fallback sphere/ray casts
            Vector3 origin = transform.position + Vector3.up * castOriginHeight;
            fwdDist = SphereCheck(origin, fwd, forwardCheckDistance);
            rightDist = SphereCheck(origin, right, sideCheckDistance);
            leftDist = SphereCheck(origin, left, sideCheckDistance);
        }

        // Start with the intent
        Vector3 steered = fwd;

        // Hallway centering: if both sides are “near”, bias toward the middle
        bool leftClose = leftDist <= minSideClearance;
        bool rightClose = rightDist <= minSideClearance;
        if (leftClose && rightClose)
        {
            float sideBias = Mathf.Clamp((rightDist - leftDist), -1f, 1f);
            steered = (steered + right * sideBias * hallwayCenterBias).normalized;
        }

        // Keep away from side walls
        if (leftDist <= minSideClearance)
            steered = Vector3.Slerp(steered, right, steerStrength).normalized;
        if (rightDist <= minSideClearance)
            steered = Vector3.Slerp(steered, left, steerStrength).normalized;

        // If forward is too tight, choose the roomier side
        if (fwdDist <= minForwardClearance)
        {
            if (rightDist > leftDist)
                steered = Vector3.Slerp(steered, right, Mathf.Max(steerStrength, 0.5f)).normalized;
            else
                steered = Vector3.Slerp(steered, left, Mathf.Max(steerStrength, 0.5f)).normalized;
        }

        _lastOutput = steered;
        return steered;
    }

    private float SphereCheck(Vector3 origin, Vector3 dir, float maxDist)
    {
        // Returns distance to obstacle, or maxDist if clear
        if (Physics.SphereCast(origin, sphereRadius, dir, out RaycastHit hit, maxDist, obstacleMask, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return maxDist;
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize last input/output and casts (fallback mode)
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position + Vector3.up * castOriginHeight;

        // Input & Output
        if (_lastInput.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Flatten(_lastInput) * 1.5f);
        }
        if (_lastOutput.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Flatten(_lastOutput) * 1.5f);
        }

        // Fallback probes
        if (!(preferPerception && perception != null))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos + Flatten(transform.forward) * forwardCheckDistance, sphereRadius);
            Gizmos.DrawWireSphere(pos + Flatten(transform.right) * sideCheckDistance, sphereRadius);
            Gizmos.DrawWireSphere(pos + -Flatten(transform.right) * sideCheckDistance, sphereRadius);
            Gizmos.DrawRay(pos, Flatten(transform.forward) * forwardCheckDistance);
            Gizmos.DrawRay(pos, Flatten(transform.right) * sideCheckDistance);
            Gizmos.DrawRay(pos, -Flatten(transform.right) * sideCheckDistance);
        }
    }
}
