using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;




public class GoToItem : CombatAction
{

    public UnityEvent onArrivedAtItem;

    private CustomerStoreManager customerStoreManager;
    private CustomerShopping customerShopping;
    private Vector3 targetPosition;
    private NavMeshAgent navMeshAgent;

    public float turnSpeed = 12f; // higher = snappier
                                  // Start is called once before the first execution of Update after the MonoBehaviour is created
    private bool hasInitializedShopping = false;

    public override void OnEnterState()
    {
        base.OnEnterState();
        if (customerStoreManager == null) customerStoreManager = FindFirstObjectByType<CustomerStoreManager>();
        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.Warp(transform.position);

        if (customerShopping != null)
        {
            if (!hasInitializedShopping)
            {
                customerShopping.InitializeShoppingList();
                hasInitializedShopping = true;
            }
            targetPosition = customerShopping.GetNextItemLocation();
            if(targetPosition == Vector3.zero)
            {
                Destroy(gameObject);
                return;
                //targetPosition = transform.position;
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
        if (dir.sqrMagnitude < 0.1f)
        {
            onArrivedAtItem?.Invoke();
            return;
        }

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);

    }
}
