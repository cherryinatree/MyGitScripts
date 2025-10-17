using UnityEngine;
using System.Collections.Generic;

public class MonsterPerception : MonoBehaviour
{
    // ---- Vision ----
    [Header("Vision Cone")]
    [Tooltip("Maximum vision distance for cone checks")]
    public float visionRange = 15f;

    [Tooltip("Full cone angle in degrees (e.g., 60 means ±30° from forward)")]
    [Range(1f, 179f)]
    public float visionAngle = 60f;

    [Tooltip("Layers that BLOCK vision (e.g. environment). These will stop LOS checks.")]
    public LayerMask visionMask;

    [Tooltip("Layers that CONTAIN potential targets (player, NPCs, etc.). Used for overlap checks.")]
    public LayerMask targetMask;

    [Tooltip("Where the monster 'sees' from. If null, uses this.transform.")]
    public Transform eyePoint;

    // ---- Rays ----
    [Header("Raycast Sampling (18 rays)")]
    [Tooltip("How far each of the 18 sample rays should check")]
    public float rayRange = 10f;

    [Tooltip("Use this to filter what the 18 sample rays can hit (usually environment/obstacles)")]
    public LayerMask sampleRayMask;

    // Outputs (parallel arrays)
    public float[] RayDistances { get; private set; }   // length 18
    public GameObject[] RayHitObjects { get; private set; } // length 18

    // ---- Hearing ----
    [Header("Hearing")]
    public float hearingRange = 10f;

    // ---- Targets ----
    [Header("Targets")]
    [Tooltip("Direct reference to the player transform")]
    public Transform player;

    [Tooltip("Optional: NPC tag for quick filtering (leave empty to ignore tag check)")]
    public string npcTag = "NPC";

    // ---- Detection / Awareness ----
    [Header("Awareness")]
    [Tooltip("Awareness score builds when hearing/seeing, decays otherwise")]
    public float awareness = 0f;

    public float awarenessIncrease = 25f;
    public float awarenessDecrease = 5f;
    public float awarenessThreshold = 100f;

    public bool CanSeePlayer { get; private set; }
    public bool CanHearPlayer { get; private set; }
    public bool PlayerInConeVisible { get; private set; }
    public bool NPCInConeVisible { get; private set; }

    public bool IsPlayerSpotted => awareness >= awarenessThreshold;

    // Internals
    private const int RAY_COUNT = 18;
    private Vector3[] _rayDirs; // cached (world-space per frame)
    private Transform _eye;     // cached eye transform


    //may need to remove
  [HideInInspector] public Transform target;


    void Awake()
    {
        _eye = eyePoint != null ? eyePoint : transform;

        if (RayDistances == null || RayDistances.Length != RAY_COUNT)
            RayDistances = new float[RAY_COUNT];

        if (RayHitObjects == null || RayHitObjects.Length != RAY_COUNT)
            RayHitObjects = new GameObject[RAY_COUNT];

        _rayDirs = new Vector3[RAY_COUNT];
    }

    public void Start()
    {
        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (_eye == null) _eye = eyePoint != null ? eyePoint : transform;

        BuildRayDirections();  // compute 18 directions in world space
        CastSampleRays();      // fill distances + hit objects

        // Perception
        PlayerInConeVisible = CheckTargetInConeAndLOS(player);
        NPCInConeVisible = CheckAnyNPCInConeAndLOS();

        // Legacy simple flags (for compatibility). You can remove if you don’t need both.
        CanSeePlayer = PlayerInConeVisible;
        CheckHearing();

        // Awareness build/decay
        if (PlayerInConeVisible || CanHearPlayer)
            awareness += awarenessIncrease * Time.deltaTime;
        else
            awareness -= awarenessDecrease * Time.deltaTime;

        awareness = Mathf.Clamp(awareness, 0f, awarenessThreshold);


    }

    // ---------------------------
    // Vision helpers
    // ---------------------------

    // Returns true if THIS target is inside the cone and line-of-sight is clear
    private bool CheckTargetInConeAndLOS(Transform target)
    {
        if (target == null) return false;

        Vector3 toTarget = target.position - _eye.position;
        float dist = toTarget.magnitude;
        if (dist > visionRange) return false;

        Vector3 dir = toTarget.normalized;
        float angle = Vector3.Angle(_eye.forward, dir);
        if (angle > visionAngle * 0.5f) return false;

        // LOS check: if RAY hits something in visionMask before the target, target is occluded
        if (Physics.Raycast(_eye.position, dir, out RaycastHit hit, dist, visionMask, QueryTriggerInteraction.Ignore))
        {
            // We hit a blocker before target
            return false;
        }

        return true;
    }

    // Overlap sphere for possible NPCs, then cone + LOS check
    private bool CheckAnyNPCInConeAndLOS()
    {
        // Find possible targets in range
        Collider[] nearby = Physics.OverlapSphere(_eye.position, visionRange, targetMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < nearby.Length; i++)
        {
            Transform t = nearby[i].transform;
            if (t == player) continue; // don’t double-count the player

            if (!string.IsNullOrEmpty(npcTag))
            {
                if (!t.CompareTag(npcTag)) continue;
            }

            if (CheckTargetInConeAndLOS(t))
                return true;
        }
        return false;
    }

    private void CheckHearing()
    {
        if (player == null)
        {
            CanHearPlayer = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        CanHearPlayer = dist <= hearingRange;
    }

    // ---------------------------
    // 18 ray directions (world)
    // ---------------------------
    //
    // Index map for readability:
    // 0..7   : 8 horizontal (every 45° starting at forward, clockwise on Y)
    // 8..11  : 4 diagonals DOWN (forward, right, back, left) at ~45° down
    // 12..15 : 4 diagonals UP   (forward, right, back, left) at ~45° up
    // 16     : straight DOWN
    // 17     : straight UP

    private void BuildRayDirections()
    {
        // Basis
        Vector3 f = _eye.forward;
        Vector3 r = _eye.right;
        Vector3 u = _eye.up;

        // Project forward/right to horizontal plane to keep horizontal rays flat
        Vector3 fH = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
        Vector3 rH = Vector3.ProjectOnPlane(r, Vector3.up).normalized;
        if (fH.sqrMagnitude < 0.0001f) fH = Vector3.forward;
        if (rH.sqrMagnitude < 0.0001f) rH = Vector3.right;

        // 8 horizontal at 45° increments around Y (starting at forward)
        // Directions: F, F+R, R, B+R, B, B+L, L, F+L
        _rayDirs[0] = (fH).normalized;
        _rayDirs[1] = (fH + rH).normalized;
        _rayDirs[2] = (rH).normalized;
        _rayDirs[3] = (-fH + rH).normalized;
        _rayDirs[4] = (-fH).normalized;
        _rayDirs[5] = (-fH - rH).normalized;
        _rayDirs[6] = (-rH).normalized;
        _rayDirs[7] = (fH - rH).normalized;

        // 4 diagonals DOWN (~45°)
        float diag = Mathf.Sqrt(0.5f); // ~0.707
        _rayDirs[8] = (fH * diag + (-u) * diag).normalized;      // forward-down
        _rayDirs[9] = (rH * diag + (-u) * diag).normalized;     // right-down
        _rayDirs[10] = (-fH * diag + (-u) * diag).normalized;     // back-down
        _rayDirs[11] = (-rH * diag + (-u) * diag).normalized;     // left-down

        // 4 diagonals UP (~45°)
        _rayDirs[12] = (fH * diag + (u) * diag).normalized;      // forward-up
        _rayDirs[13] = (rH * diag + (u) * diag).normalized;     // right-up
        _rayDirs[14] = (-fH * diag + (u) * diag).normalized;     // back-up
        _rayDirs[15] = (-rH * diag + (u) * diag).normalized;     // left-up

        // Straight down/up
        _rayDirs[16] = (-u).normalized;
        _rayDirs[17] = (u).normalized;
    }

    private void CastSampleRays()
    {
        Vector3 origin = _eye.position;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            Ray ray = new Ray(origin, _rayDirs[i]);
            if (Physics.Raycast(ray, out RaycastHit hit, rayRange, sampleRayMask, QueryTriggerInteraction.Ignore))
            {
                RayDistances[i] = hit.distance;
                RayHitObjects[i] = hit.collider.gameObject;
            }
            else
            {
                RayDistances[i] = rayRange;
                RayHitObjects[i] = null;
            }
        }
    }

    // ---------------------------
    // Gizmos
    // ---------------------------
    private void OnDrawGizmosSelected()
    {
        Transform eye = (eyePoint != null) ? eyePoint : transform;

        // Draw vision cone (approximation with lines)
        Gizmos.color = Color.yellow;
        DrawConeGizmo(eye.position, eye.forward, visionAngle, visionRange, 20);

        // If not playing, need dirs; if playing, they’re built in Update already
        if (!Application.isPlaying)
        {
            // Build simple dirs from current transform so scene view shows something meaningful
            Transform t = eye;
            Vector3 f = t.forward;
            Vector3 r = t.right;
            Vector3 u = t.up;
            Vector3 fH = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
            Vector3 rH = Vector3.ProjectOnPlane(r, Vector3.up).normalized;
            if (fH.sqrMagnitude < 0.0001f) fH = Vector3.forward;
            if (rH.sqrMagnitude < 0.0001f) rH = Vector3.right;

            Vector3[] preview = new Vector3[RAY_COUNT];
            preview[0] = (fH).normalized;
            preview[1] = (fH + rH).normalized;
            preview[2] = (rH).normalized;
            preview[3] = (-fH + rH).normalized;
            preview[4] = (-fH).normalized;
            preview[5] = (-fH - rH).normalized;
            preview[6] = (-rH).normalized;
            preview[7] = (fH - rH).normalized;
            float diag = Mathf.Sqrt(0.5f);
            preview[8] = (fH * diag + (-u) * diag).normalized;
            preview[9] = (rH * diag + (-u) * diag).normalized;
            preview[10] = (-fH * diag + (-u) * diag).normalized;
            preview[11] = (-rH * diag + (-u) * diag).normalized;
            preview[12] = (fH * diag + (u) * diag).normalized;
            preview[13] = (rH * diag + (u) * diag).normalized;
            preview[14] = (-fH * diag + (u) * diag).normalized;
            preview[15] = (-rH * diag + (u) * diag).normalized;
            preview[16] = (-u).normalized;
            preview[17] = (u).normalized;

            // draw
            for (int i = 0; i < RAY_COUNT; i++)
            {
                Gizmos.color = (i < 8) ? Color.cyan : (i < 12 ? Color.blue : (i < 16 ? Color.magenta : Color.green));
                Gizmos.DrawLine(eye.position, eye.position + preview[i] * rayRange);
            }
        }
        else
        {
            // Use current rays/distances
            for (int i = 0; i < RAY_COUNT; i++)
            {
                Color c = (i < 8) ? Color.cyan : (i < 12 ? Color.blue : (i < 16 ? Color.magenta : Color.green));
                Gizmos.color = c;

                Vector3 dir = (_rayDirs != null && _rayDirs.Length == RAY_COUNT) ? _rayDirs[i] : Vector3.forward;
                float dist = (RayDistances != null && RayDistances.Length == RAY_COUNT) ? Mathf.Min(RayDistances[i], rayRange) : rayRange;

                Gizmos.DrawLine(eye.position, eye.position + dir * dist);
            }
        }
    }

    private void DrawConeGizmo(Vector3 origin, Vector3 forward, float angleDeg, float range, int segments)
    {
        // Draw “disc” ring at range and connect to origin for a visual cone feel
        angleDeg = Mathf.Clamp(angleDeg, 1f, 179f);
        float half = angleDeg * 0.5f;

        // Build a right/forward basis on the horizontal plane for the ring
        Vector3 up = Vector3.up;
        Vector3 fH = Vector3.ProjectOnPlane(forward, up).normalized;
        if (fH.sqrMagnitude < 0.0001f) fH = forward.normalized;

        Vector3 right = Vector3.Cross(up, fH).normalized;
        Vector3 left = -right;

        // Draw multiple spokes around the cone edge on the horizontal ring
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float t = (segments == 0) ? 0f : (i / (float)segments);
            float yaw = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;

            Vector3 dir = (Quaternion.AngleAxis(Mathf.Rad2Deg * yaw, up) * fH);
            Vector3 edge = Quaternion.AngleAxis(-half, right) * dir; // tilt down a bit to hint cone depth (purely visual)
            Vector3 point = origin + dir * range;

            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
            // Spokes
            Gizmos.DrawLine(origin, point);
        }
    }
}

