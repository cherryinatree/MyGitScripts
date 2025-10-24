using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Points")]
    public Transform spawnPoint;  // where the enemy appears (behind the corner)
    public Transform lookPoint;   // where the enemy steps out to look at the player
    public Transform hidePoint;   // where the enemy retreats to (behind the wall)

    [Header("NavMesh")]
    [Tooltip("How far to search for a nearby NavMesh position from the target point.")]
    public float sampleRadius = 2f;

    private NavMeshAgent agent;
    private bool hasAppeared;
    public bool DisableOnHide = true;
    private bool retreat = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Start hidden as you wanted
        gameObject.SetActive(false);
    }


    private void Update()
    {
        Debug.Log("EnemyController Update: " + Vector3.Distance(transform.position, hidePoint.position));

        if (retreat && Vector3.Distance(transform.position, hidePoint.position) < 2f)
        {
            Debug.Log("EnemyController Update: Deactivate");
            gameObject.SetActive(false);
        }   
    }


    /// <summary>
    /// Safely warps the agent to a NavMesh position near a given transform.
    /// </summary>
    private bool PlaceOnNavMeshNear(Transform t)
    {
        agent = GetComponent<NavMeshAgent>();
        if (t == null)
        {
            Debug.LogWarning("EnemyController: target Transform is null.");
            return false;
        }

        if (NavMesh.SamplePosition(t.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(hit.position);        // instantly place on the mesh
            agent.ResetPath();               // clear any stale path
            return true;
        }

        Debug.LogWarning($"EnemyController: No NavMesh found near '{t.name}'. " +
                         $"Move this point onto the NavMesh or increase sampleRadius.");
        return false;
    }

    public void Appear()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Ensure we’re on the NavMesh before starting movement
        if (!PlaceOnNavMeshNear(spawnPoint != null ? spawnPoint : hidePoint))
            return;

        agent.isStopped = false;
        agent.SetDestination(lookPoint.position);
        hasAppeared = true;
    }

    public void Retreat()
    {
        if (!gameObject.activeSelf || !hasAppeared) return;

        // Make sure we’re currently on the NavMesh (in case something moved us)
        if (!PlaceOnNavMeshNear(transform))
            return;

        agent.isStopped = false;
        agent.SetDestination(hidePoint.position);
        retreat = true;
    }
}
