using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Movement wrapper over NavMeshAgent so states don't touch the agent directly.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavMovement : MonoBehaviour
{
    public NavMeshAgent Agent { get; private set; }

    [Header("Defaults")]
    public float defaultSpeed = 3.5f;
    public float defaultAngularSpeed = 720f;   // deg/s
    public float defaultAccel = 12f;
    public float stoppingDistance = 0.7f;

    [Header("Off-Mesh Links")]
    public bool handleOffMeshLinks = true;
    public float jumpDuration = 0.35f;
    public float jumpHeight = 0.8f;
    public string jumpTrigger = "jump";   // optional animator trigger
    public EnemyAnimatorController animatorController;

    private Coroutine _linkRoutine;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.updateRotation = false;   // we rotate manually -> smoother + matches your rig
        Agent.speed = defaultSpeed;
        Agent.angularSpeed = defaultAngularSpeed;
        Agent.acceleration = defaultAccel;
        Agent.stoppingDistance = stoppingDistance;
        if (!animatorController) animatorController = GetComponentInParent<EnemyAnimatorController>();
    }

    private void Update()
    {
        // Manual rotation toward velocity when moving
        Vector3 vel = Agent.desiredVelocity;
        vel.y = 0f;
        if (vel.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(vel.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, Agent.angularSpeed * Time.deltaTime);
        }

        // Off-mesh links
        if (handleOffMeshLinks && Agent.isOnOffMeshLink && _linkRoutine == null)
        {
            _linkRoutine = StartCoroutine(HandleOffMeshLink());
        }
    }

    public void SetMoveSpeed(float speed)
    {
        Agent.speed = Mathf.Max(0f, speed);
    }

    public void SetStoppingDistance(float dist)
    {
        Agent.stoppingDistance = Mathf.Max(0f, dist);
    }

    public void SetDestination(Vector3 worldPos)
    {
        Agent.isStopped = false;
        Agent.SetDestination(worldPos);
    }

    public void Stop(bool clearPath = false)
    {
        Agent.isStopped = true;
        if (clearPath) Agent.ResetPath();
    }

    public bool ReachedDestination(float extraTolerance = 0f)
    {
        if (Agent.pathPending) return false;
        if (Agent.remainingDistance <= Agent.stoppingDistance + extraTolerance)
            return !Agent.hasPath || Agent.velocity.sqrMagnitude < 0.01f;
        return false;
    }

    public bool HasValidPath => Agent.hasPath && Agent.pathStatus == NavMeshPathStatus.PathComplete;

    private IEnumerator HandleOffMeshLink()
    {
        var linkData = Agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        Vector3 end = linkData.endPos;
        end.y = start.y; // we’ll add arc manually

        animatorController?.PlayTrigger(jumpTrigger);

        float t = 0f;
        Agent.updatePosition = false; // we’ll move manually during the jump
        Vector3 center = (start + end) * 0.5f + Vector3.up * jumpHeight;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, jumpDuration);
            // Quadratic Bezier curve
            Vector3 p1 = Vector3.Lerp(start, center, t);
            Vector3 p2 = Vector3.Lerp(center, end, t);
            Vector3 pos = Vector3.Lerp(p1, p2, t);
            transform.position = pos;
            yield return null;
        }

        Agent.updatePosition = true;
        Agent.CompleteOffMeshLink();
        _linkRoutine = null;
    }
}
