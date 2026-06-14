using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Intruder))]
[RequireComponent(typeof(Health))]
public class IntruderAI : MonoBehaviour
{
    public enum Mode { Fight, Flee, Mixed }
    public Mode Behavior = Mode.Mixed;

    public float DetectRange = 15f;
    public float AttackRange = 1.6f;
    public float AttackCooldown = 1.2f;
    public float AttackHitDelay = 0.35f;
    public float Damage = 15f;

    public float FleeDistance = 8f;
    public float FleeRepathInterval = 0.4f;

    public Animator Anim;
    public string AttackTrigger = "Attack";

    private NavMeshAgent _agent;
    private Health _health;
    private float _nextAttack;
    private float _nextFleeRepath;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_health.IsDead) return;

        var target = FindNearestRobot();
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > DetectRange) return;

        bool shouldFlee =
            Behavior == Mode.Flee ||
            (Behavior == Mode.Mixed && _health.CurrentHealth <= _health.MaxHealth * 0.35f);

        if (shouldFlee)
        {
            if (Time.time >= _nextFleeRepath)
            {
                _nextFleeRepath = Time.time + FleeRepathInterval;
                Vector3 away = (transform.position - target.position).normalized;
                Vector3 fleePos = transform.position + away * FleeDistance;

                if (NavMesh.SamplePosition(fleePos, out var hit, 6f, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);
            }
            return;
        }

        // Fight mode
        _agent.SetDestination(target.position);

        if (dist <= AttackRange && Time.time >= _nextAttack)
        {
            _nextAttack = Time.time + AttackCooldown;
            StartCoroutine(AttackRoutine(target.gameObject));
        }
    }

    private System.Collections.IEnumerator AttackRoutine(GameObject robot)
    {
        if (Anim != null) Anim.SetTrigger(AttackTrigger);
        yield return new WaitForSeconds(AttackHitDelay);

        var h = robot.GetComponentInParent<Health>();
        if (h != null && !h.IsDead)
            h.TakeDamage(Damage, gameObject);
    }

    private Transform FindNearestRobot()
    {
        RobotMaster best = null;
        float bestDist = float.PositiveInfinity;

        var robots = FindObjectsByType<RobotMaster>(FindObjectsSortMode.None);
        for (int i = 0; i < robots.Length; i++)
        {
            var r = robots[i];
            if (r == null) continue;

            // Prefer security robots
            float bias = (r.Role == RobotMaster.RobotRole.Security) ? 0f : 3f;

            float d = Vector3.Distance(transform.position, r.transform.position) + bias;
            if (d < bestDist)
            {
                bestDist = d;
                best = r;
            }
        }

        return best != null ? best.transform : null;
    }
}
