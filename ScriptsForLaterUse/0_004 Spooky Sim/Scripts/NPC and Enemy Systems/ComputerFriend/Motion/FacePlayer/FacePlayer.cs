using UnityEngine;

/// <summary>
/// Make this character smoothly face the player (or any target).
/// - Works even if the player is temporarily deactivated (will re-acquire).
/// - Optionally locks to yaw only so the NPC doesn't tilt up/down.
/// - Can aim at the player's camera/head via Target Aim Override.
/// </summary>
[DisallowMultipleComponent]
public class FacePlayer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Optional explicit target. Leave empty to auto-find by tag.")]
    public Transform target;

    [Tooltip("Auto-find the player by tag if Target is not assigned or is inactive.")]
    public bool findPlayerByTag = true;

    [Tooltip("Tag used when auto-finding the player.")]
    public string playerTag = "Player";

    [Tooltip("How often we try to (re)acquire the player target, in seconds.")]
    public float reacquireInterval = 0.5f;

    [Header("Rotation Behavior")]
    [Tooltip("Rotate only around Y (yaw). Keeps NPC upright.")]
    public bool onlyYaw = true;

    [Tooltip("Degrees per second to turn.")]
    public float turnSpeedDegPerSec = 360f;

    [Tooltip("If > 0, only face the player when within this distance (meters). 0 = always.")]
    public float maxDistance = 0f;

    [Header("Aim Point")]
    [Tooltip("If set, we aim here (e.g., player's head/camera).")]
    public Transform targetAimOverride;

    [Tooltip("If no override, we add this offset to the target (approximate head height).")]
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Tooltip("Rotate this transform. Defaults to self.")]
    public Transform pivot;

    [Header("Physics (Optional)")]
    [Tooltip("Use Rigidbody.MoveRotation if this object has a Rigidbody you want to drive.")]
    public bool useRigidbodyMoveRotation = false;

    private Rigidbody _rb;
    private float _nextFindTime;

    void Awake()
    {
        if (!pivot) pivot = transform;
        if (useRigidbodyMoveRotation)
            _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        TryFindTarget(immediate: true);
    }

    void Update()
    {
        // Reacquire if missing or inactive
        if ((target == null || !target.gameObject.activeInHierarchy) && Time.unscaledTime >= _nextFindTime)
            TryFindTarget(immediate: false);

        if (target == null) return;

        if (maxDistance > 0f)
        {
            float sq = (pivot.position - target.position).sqrMagnitude;
            if (sq > maxDistance * maxDistance) return;
        }

        Vector3 aimPoint = GetAimPoint();
        Vector3 dir = aimPoint - pivot.position;

        if (onlyYaw)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Quaternion next = Quaternion.RotateTowards(pivot.rotation, desired, turnSpeedDegPerSec * Time.deltaTime);

        if (useRigidbodyMoveRotation && _rb != null)
            _rb.MoveRotation(next);
        else
            pivot.rotation = next;
    }

    /// <summary>Compute where we aim on the target.</summary>
    private Vector3 GetAimPoint()
    {
        if (targetAimOverride)
            return targetAimOverride.position;

        // If no override, use target + offset in target's local space (so it follows head height even if target scales).
        return target.TransformPoint(targetOffset);
    }

    /// <summary>Manually set the target at runtime (e.g., from another script).</summary>
    public void SetTarget(Transform t, Transform aimOverride = null)
    {
        target = t;
        if (aimOverride) targetAimOverride = aimOverride;
    }

    private void TryFindTarget(bool immediate)
    {
        if (!findPlayerByTag) return;

        if (!immediate && Time.unscaledTime < _nextFindTime)
            return;

        _nextFindTime = Time.unscaledTime + reacquireInterval;

        GameObject go = GameObject.FindWithTag(playerTag);
        if (go)
        {
            target = go.transform;

            // Bonus: if the player has a camera (even if inactive during cutscenes), aim at that for nicer eye contact.
            Camera cam = go.GetComponentInChildren<Camera>(true);
            if (cam) targetAimOverride = cam.transform;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform p = pivot ? pivot : transform;
        if (p == null) return;

        Gizmos.color = Color.cyan;
        if (target != null)
            Gizmos.DrawLine(p.position, GetAimPoint());

        if (maxDistance > 0f)
            Gizmos.DrawWireSphere(p.position, maxDistance);
    }
#endif
}
