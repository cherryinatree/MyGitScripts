using System.Collections;
using UnityEngine;

public class AppearInFrontSpawner : MonoBehaviour
{
    public enum RotationMode { KeepPrefabRotation, MatchPlayerForward, LookAtPlayer }

    [Header("Targets")]
    [Tooltip("Whose 'front' are we using? Typically the Player root or Camera.")]
    public Transform player;
    [Tooltip("The object to instantiate.")]
    public GameObject prefab;

    [Header("Placement (relative to player)")]
    [Tooltip("Forward distance in front of the player.")]
    public float forwardDistance = 2f;
    [Tooltip("Sideways offset (+ right, - left).")]
    public float lateralOffset = 0f;
    [Tooltip("Vertical offset (+ up, - down).")]
    public float heightOffset = 0f;
    [Tooltip("If true, snap down to ground using a raycast.")]
    public bool snapToGround = false;
    public LayerMask groundMask = ~0;         // default: everything
    public float groundRayLength = 5f;        // how far to search downwards
    public float groundSurfaceOffset = 0.02f; // keep slightly above ground

    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.MatchPlayerForward;

    [Header("Parenting")]
    [Tooltip("If true, the spawned object will become a child of the player.")]
    public bool parentToPlayer = false;

    [Header("Timing")]
    [Tooltip("If > 0, spawning waits this many seconds.")]
    public float spawnDelay = 0f;
    [Tooltip("If true, the spawned object is destroyed after lifetime seconds.")]
    public bool autoDespawn = true;
    [Tooltip("Lifetime for auto-despawn (seconds).")]
    public float lifetime = 3f;

    [Header("Play Mode Test")]
    public KeyCode testKey = KeyCode.None;

    void Reset()
    {
        player = Camera.main ? Camera.main.transform : transform;
    }

    void Update()
    {
       /* if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
        {
            Trigger(); // test with current inspector settings
        }*/
    }

    /// <summary>
    /// Public entry point (uses current inspector settings).
    /// </summary>
    public void Trigger()
    {
        Trigger(spawnDelay, autoDespawn ? lifetime : 0f, autoDespawn);
    }

    /// <summary>
    /// Flexible trigger you can call from other scripts/UnityEvents.
    /// delaySeconds: delay before spawn; lifetimeSeconds: how long before despawn (0 or negative = no despawn);
    /// doAutoDespawn: override whether to despawn automatically.
    /// </summary>
    public void Trigger(float delaySeconds, float lifetimeSeconds, bool doAutoDespawn)
    {
        if (prefab == null || player == null)
        {
            Debug.LogWarning("[AppearInFrontSpawner] Missing prefab or player.");
            return;
        }
        StartCoroutine(SpawnRoutine(delaySeconds, lifetimeSeconds, doAutoDespawn));
    }

    private IEnumerator SpawnRoutine(float delaySeconds, float lifetimeSeconds, bool doAutoDespawn)
    {
        Debug.Log("Spawn");
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // Compute spawn pose
        ComputeSpawnPose(out Vector3 pos, out Quaternion rot);

        // Spawn
        GameObject go = Instantiate(prefab, pos, rot);
        if (parentToPlayer && player != null)
        {
            // Keep world transform when parenting
            go.transform.SetParent(player, true);
        }

        // Optional despawn
        if (doAutoDespawn && lifetimeSeconds > 0f)
        {
            Destroy(go, lifetimeSeconds);
        }
    }

    private void ComputeSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        // Base offsets in player's local axes
        Vector3 forward = player.forward;
        Vector3 right = player.right;
        Vector3 up = player.up;

        Vector3 basePos = player.position
                        + forward * forwardDistance
                        + right * lateralOffset
                        + up * heightOffset;

        // Optional ground snap
        if (snapToGround)
        {
            Vector3 rayStart = basePos + Vector3.up * (groundRayLength * 0.5f);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                basePos = hit.point + Vector3.up * groundSurfaceOffset;
            }
        }

        position = basePos;

        // Rotation
        rotation = rotationMode switch
        {
            RotationMode.KeepPrefabRotation => Quaternion.identity * prefab.transform.rotation,
            RotationMode.MatchPlayerForward => Quaternion.LookRotation(player.forward, Vector3.up),
            RotationMode.LookAtPlayer => Quaternion.LookRotation((player.position - basePos).normalized, Vector3.up),
            _ => Quaternion.identity
        };
    }

}
