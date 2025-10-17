using UnityEngine;

/// <summary>
/// Terrorize behavior: stalks or flees the player, plays creepy sounds when unseen,
/// can "blink" behind the player with VFX/SFX, lingers/peeks at corners,
/// and remembers its own last seen player position.
/// </summary>
public class TerrorizeAction : CombatAction
{
    [Header("References")]
    public Transform player;
    public Transform playerEye;
    public LayerMask losBlockersMask = ~0;
    public LayerMask blinkBlockersMask = ~0;
    public MonsterPerception perception;
    public CoreEnemy core;
    public EnemyMovement movement;
    public EnemyAnimatorController anim;
    public EnemySoundController sfx;

    [Header("Player Vision Approximation")]
    [Range(10f, 180f)] public float playerFOV = 90f;
    public float playerVisionRange = 30f;

    [Header("General Stalking")]
    public float desiredSpeed = 1.25f;
    [Range(0.05f, 1f)] public float steerResponsiveness = 0.35f;
    public float thinkInterval = 0.5f;

    [Header("Behavior Toggles")]
    public bool enableCreepySounds = true;
    public bool enableTeleportBehindPlayer = true;
    public bool avoidWallsDuringStalk = true;
    public bool pursueWhenSeen = true;
    public bool fleeWhenLookedAt = false;
    public bool fleeWhenLit = false;
    public bool enableCornerPeek = true;

    [Header("Creepy Sounds")]
    public float creepyInterval = 10f;

    [Header("Blink Behind Player")]
    public float blinkDistance = 3.05f;
    public float blinkCooldown = 8f;
    public float groundSnapMax = 6f;
    public GameObject blinkVFXPrefab;
    public AudioClip blinkSfx;
    public float blinkVFXLifetime = 2f;

    [Header("Wall Avoidance (Patrol-style)")]
    public float avoidThreshold = 1.5f;
    public float hallwaySideDistance = 1.25f;
    [Range(0f, 1f)] public float hallwayCenterBias = 0.35f;

    [Header("Small Obstacle Jump (optional)")]
    public bool allowSmallObstacleJump = true;
    public float smallObstacleThreshold = 1.0f;
    public float maxJumpHeight = 1.25f;

    [Header("Corner Peek Behavior")]
    public float peekPause = 1.1f;
    public float peekSideStep = 0.6f;
    public float peekCooldown = 5f;
    public AudioClip peekSfx;
    public string peekAnimTrigger = "peek";

    [HideInInspector] public bool IsLit = false;

    private float nextThinkTime;
    private float nextCreepyTime;
    private float nextBlinkTime;
    private float nextPeekTime;
    private Vector3 desiredDir;
    private Vector3 lastKnownPlayerPos;
    private bool isPeeking;
    private float peekUntilTime;

    // Ray indices
    private const int R_FWD = 0;
    private const int R_RIGHT = 2;
    private const int R_LEFT = 6;
    private const int R_FWD_DOWN = 8;
    private const int R_DOWN = 16;
    private const int R_FWD_UP = 12;
    private const int R_UP = 17;

    protected override void Awake()
    {
        base.Awake();
        if (!core) core = GetComponentInParent<CoreEnemy>();
        if (!movement) movement = GetComponentInParent<EnemyMovement>();
        if (!anim) anim = GetComponentInParent<EnemyAnimatorController>();
        if (!sfx) sfx = GetComponentInParent<EnemySoundController>();
        if (!perception) perception = GetComponentInParent<MonsterPerception>();
        if (!player && perception) player = perception.player;
        if (!playerEye && player) playerEye = player;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        nextThinkTime = Time.time;
        nextCreepyTime = Time.time + Mathf.Max(2f, creepyInterval);
        nextBlinkTime = Time.time + 2f;
        nextPeekTime = Time.time + 2f;
        desiredDir = Flatten(transform.forward);
        if (player) lastKnownPlayerPos = player.position;
        anim?.PlayWalk(true);
        isPeeking = false;
    }

    public override void PerformAction()
    {
        if (!core || !movement || !player) return;

        // Update last known player pos
        Vector3 toPlayer = player.position - core.transform.position; toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f) lastKnownPlayerPos = player.position;

        if (!isPeeking && Time.time >= nextThinkTime)
        {
            bool playerSeesUs = PlayerCanSeeMe();
            bool unseen = !playerSeesUs;

            if (enableCreepySounds && unseen && Time.time >= nextCreepyTime)
            {
                sfx?.PlayIdle();
                nextCreepyTime = Time.time + creepyInterval;
            }

            if (enableTeleportBehindPlayer && unseen && Time.time >= nextBlinkTime)
                TryBlinkBehindPlayer();

            if (enableCornerPeek && unseen && Time.time >= nextPeekTime)
            {
                if (TryCornerPeek())
                {
                    isPeeking = true;
                    peekUntilTime = Time.time + peekPause;
                    nextPeekTime = Time.time + Mathf.Max(peekCooldown, peekPause + 0.5f);
                }
            }

            bool shouldFlee = (fleeWhenLookedAt && playerSeesUs) || (fleeWhenLit && IsLit);
            bool canPursue = (pursueWhenSeen || unseen);

            if (shouldFlee) desiredDir = ComputeSteerDir(awayFromPlayer: true);
            else if (canPursue) desiredDir = ComputeSteerDir(awayFromPlayer: false);
            else desiredDir = Vector3.zero;

            nextThinkTime = Time.time + Mathf.Max(0.1f, thinkInterval);
        }

        if (isPeeking)
        {
            movement.SetMoveDirection(Vector3.zero);
            if (Time.time >= peekUntilTime) isPeeking = false;
            return;
        }

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            anim?.PlayWalk(true);
            movement.SetMoveDirection(desiredDir.normalized * desiredSpeed);
        }
        else
        {
            anim?.PlayIdle();
            movement.SetMoveDirection(Vector3.zero);
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        movement?.SetMoveDirection(Vector3.zero);
        anim?.PlayIdle();
        isPeeking = false;
    }

    // --- Helpers ---
    private bool PlayerCanSeeMe()
    {
        if (!player) return false;
        Transform eye = playerEye ? playerEye : player;

        Vector3 toMe = core.transform.position - eye.position;
        float dist = toMe.magnitude;
        if (dist > playerVisionRange) return false;

        Vector3 dir = toMe.normalized;
        if (Vector3.Angle(eye.forward, dir) > playerFOV * 0.5f) return false;

        return !Physics.Raycast(eye.position, dir, dist, losBlockersMask, QueryTriggerInteraction.Ignore);
    }

    private void TryBlinkBehindPlayer()
    {
        if (!player) return;
        Vector3 back = -Flatten(player.forward);
        Vector3 target = player.position + back * blinkDistance;

        Vector3 start = player.position + Vector3.up * 0.25f;
        Vector3 dir = (target - start).normalized;
        float len = Vector3.Distance(target, start);

        if (!Physics.Raycast(start, dir, len, blinkBlockersMask, QueryTriggerInteraction.Ignore))
        {
            SpawnVfxAt(core.rb.position);
            Vector3 snapOrigin = target + Vector3.up * 3f;
            if (Physics.Raycast(snapOrigin, Vector3.down, out RaycastHit hit, groundSnapMax, blinkBlockersMask, QueryTriggerInteraction.Ignore))
                core.rb.position = hit.point;
            else core.rb.position = target;
            SpawnVfxAt(core.rb.position);

            //if (blinkSfx != null && sfx != null) sfx.PlayOneShot(blinkSfx);
            //else sfx?.PlayIdle();

            nextBlinkTime = Time.time + Mathf.Max(2f, blinkCooldown);
        }
        else
            nextBlinkTime = Time.time + Mathf.Max(2f, blinkCooldown * 0.5f);
    }

    private void SpawnVfxAt(Vector3 pos)
    {
        if (!blinkVFXPrefab) return;
        var go = GameObject.Instantiate(blinkVFXPrefab, pos, Quaternion.identity);
        if (blinkVFXLifetime > 0f) GameObject.Destroy(go, blinkVFXLifetime);
    }

    private bool TryCornerPeek()
    {
        if (!perception || perception.RayDistances == null) return false;
        var d = perception.RayDistances;

        bool fwdBlocked = d[R_FWD] <= avoidThreshold;
        bool rightOpen = d[R_RIGHT] > d[R_LEFT] + 0.4f;
        bool leftOpen = d[R_LEFT] > d[R_RIGHT] + 0.4f;

        if (fwdBlocked && (rightOpen || leftOpen))
        {
           // if (!string.IsNullOrEmpty(peekAnimTrigger)) anim?.PlayTrigger(peekAnimTrigger);
            //if (peekSfx != null && sfx != null) sfx.PlayOneShot(peekSfx);

            if (peekSideStep > 0.05f)
            {
                Vector3 side = rightOpen ? Flatten(core.transform.right) : -Flatten(core.transform.right);
                desiredDir = side;
                movement.SetMoveDirection(desiredDir.normalized * desiredSpeed);
            }
            return true;
        }
        return false;
    }
    private Vector3 ComputeSteerDir(bool awayFromPlayer)
    {
        // Base intent: toward or away from the (last) player position on the XZ plane
        Vector3 targetPos = player ? player.position : lastKnownPlayerPos;
        Vector3 forward = Flatten(core.transform.forward);
        Vector3 right = Flatten(core.transform.right);

        Vector3 intent = Flatten(awayFromPlayer
            ? (core.transform.position - targetPos)
            : (targetPos - core.transform.position));

        if (intent.sqrMagnitude < 0.0001f)
            intent = forward;

        // If we’re not using avoidance or we don’t have valid rays, just go with intent
        if (!avoidWallsDuringStalk || perception == null || perception.RayDistances == null || perception.RayDistances.Length < 18)
            return intent;

        var d = perception.RayDistances;

        // Read the key rays (indices must match your MonsterPerception layout)
        float fwdDist = d[R_FWD];     // forward
        float leftDist = d[R_LEFT];    // left
        float rightDist = d[R_RIGHT];   // right

        // 1) Hallway centering: if both sides are close, bias back toward the center line
        bool leftClose = leftDist <= hallwaySideDistance;
        bool rightClose = rightDist <= hallwaySideDistance;

        Vector3 desired = intent;
        if (leftClose && rightClose)
        {
            // Positive when there’s more room on the right, negative if more on the left
            float sideBias = Mathf.Clamp((rightDist - leftDist), -1f, 1f);
            desired = (intent + right * sideBias * hallwayCenterBias).normalized;
        }

        // 2) Forward avoidance: if forward clearance is tight, pick the side with more room
        if (fwdDist <= avoidThreshold)
        {
            // Side-step toward the more open side (smoothly)
            desired = (rightDist > leftDist)
                ? Vector3.Slerp(desired, right, steerResponsiveness).normalized
                : Vector3.Slerp(desired, -right, steerResponsiveness).normalized;

            // Optional hop over small curb/step if enabled
           // if (allowSmallObstacleJump)
                //TrySmallObstacleJump(d);
        }

        // 3) Smoothly blend from our current desire toward the new desired direction
        Vector3 from = (desiredDir.sqrMagnitude < 0.0001f) ? intent : desiredDir;
        Vector3 smoothed = Vector3.Slerp(from, desired, Mathf.Clamp01(steerResponsiveness)).normalized;

        return smoothed;
    }
    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
    }


}
