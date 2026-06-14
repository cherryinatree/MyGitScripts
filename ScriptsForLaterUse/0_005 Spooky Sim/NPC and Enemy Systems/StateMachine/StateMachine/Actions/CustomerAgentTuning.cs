using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerAgentTuning : MonoBehaviour
{
    private void Awake()
    {
        var a = GetComponent<NavMeshAgent>();
        a.autoRepath = true;
        a.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        a.avoidancePriority = Random.Range(30, 70);

        // Gives them personal space at the destination
        a.stoppingDistance = Mathf.Max(a.stoppingDistance, a.radius * 1.25f);
    }
}
