using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class GoToLine : CombatAction
{

    public UnityEvent onArrivedAtItem;

    private CustomerStoreManager customerStoreManager;
    private CustomerShopping customerShopping;
    private Vector3 targetPosition;
    private NavMeshAgent navMeshAgent;

    public float turnSpeed = 12f; // higher = snappier
                                  // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnEnterState()
    {
        base.OnEnterState();
  
        customerStoreManager = FindFirstObjectByType<CustomerStoreManager>();
        customerShopping = GetComponent<CustomerShopping>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        customerShopping.AssignedCheckOutLine = customerStoreManager.GetRandomCheckoutLine();

        if (customerShopping != null && customerShopping.AssignedCheckOutLine != null)
        {
            if (!customerShopping.AssignedCheckOutLine.IsLineFull())
            {
                targetPosition = customerShopping.AssignedCheckOutLine.GetFirstAvailableSpot(gameObject);
            }
        }

        if (targetPosition != null && navMeshAgent != null)
        {
            navMeshAgent.SetDestination(targetPosition);
        }
    }

    public override void PerformAction()
    {    // Prefer desiredVelocity; fallback to steeringTarget when slowing near destination
        Vector3 dir = navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f
            ? navMeshAgent.desiredVelocity
            : (navMeshAgent.steeringTarget - transform.position);

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            onArrivedAtItem?.Invoke();
            return;
        }

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);

    }
}
