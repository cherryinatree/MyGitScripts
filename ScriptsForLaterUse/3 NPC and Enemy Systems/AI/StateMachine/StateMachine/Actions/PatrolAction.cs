using UnityEngine;

/// <summary>
/// Minimal patrol: continuously run using physics movement.
/// ObstacleAvoidance steers the direction to keep running without getting stuck.
/// </summary>
public class PatrolAction : CombatAction
{
    [Header("Run Settings")]
    public float runSpeed = 3.0f;                 // world-units/s target speed
    [Range(0.05f, 1f)] public float blend = 0.35f; // smooth between last & new steering

    [Header("Modules")]
    private CoreEnemy core;
    private EnemyMovement movement;
    private EnemyAnimatorController anim;        // optional
    private EnemySoundController sfx;            // optional
    public ObstacleAvoidance avoidance;          // required for steering

    private Vector3 _currentDir;

    protected override void Awake()
    {
        base.Awake();
        core = GetComponentInParent<CoreEnemy>();
        movement = GetComponentInParent<EnemyMovement>();
        anim = GetComponentInParent<EnemyAnimatorController>();
        sfx = GetComponentInParent<EnemySoundController>();
        if (!avoidance) avoidance = GetComponentInParent<ObstacleAvoidance>();
    }
    public override void OnEnterState()
    {
        base.OnEnterState();
        if (core == null || movement == null) return;

        _currentDir = Flatten(core.transform.forward);

        // ✅ Tell movement what speed to use
        movement.SetMoveSpeed(runSpeed);

        anim?.PlayWalk(true);
    }


    public override void PerformAction()
    {
        if (core == null || movement == null) return;

        // Base intent = keep running forward relative to body facing
        Vector3 intent = Flatten(core.transform.forward);

        // Steer using avoidance
        Vector3 steered = (avoidance != null) ? avoidance.GetSteeredDirection(intent) : intent;

        // Smooth between last direction and new (helps reduce jitter)
        _currentDir = Vector3.Slerp(_currentDir, steered, Mathf.Clamp01(blend)).normalized;

        // Drive physics movement
        movement.SetMoveDirection(_currentDir);

        // Optional hooks
        anim?.PlayWalk(true);
        // sfx?.PlayStep(); // if you’re driving steps by time; better via animation events
    }

    public override void OnExitState()
    {
        base.OnExitState();
        movement?.SetMoveDirection(Vector3.zero);
        anim?.PlayIdle();
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
    }
}
