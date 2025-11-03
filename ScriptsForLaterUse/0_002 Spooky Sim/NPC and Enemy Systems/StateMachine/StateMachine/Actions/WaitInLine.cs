using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class WaitInLine : CombatAction
{

    public UnityEvent onArrivedAtItem;

    private int myLinePositionIndex = -1;
    private CustomerShopping customerShopping;
    private NavMeshAgent navMeshAgent;
    private Vector3 targetPosition;

    public override void OnEnterState()
    {
        base.OnEnterState();
        customerShopping = GetComponent<CustomerShopping>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (customerShopping != null)
        {
            myLinePositionIndex = customerShopping.AssignedCheckOutLine.GetCustomerPositionInLine(gameObject);
        }
    }

    public override void PerformAction()
    {
        // Do nothing, just wait in line
        if (customerShopping != null)
        {
            if (MovingUpInLine())
            {
                if (myLinePositionIndex == 0)
                {
                    onArrivedAtItem?.Invoke();
                }

                if (myLinePositionIndex > 0)
                {
                    if(customerShopping.AssignedCheckOutLine.IsNextSpotAvailable(gameObject))
                    {
                        targetPosition = customerShopping.AssignedCheckOutLine.MoveUpInLine(gameObject);
                        myLinePositionIndex = customerShopping.AssignedCheckOutLine.GetCustomerPositionInLine(gameObject);
                        navMeshAgent.SetDestination(targetPosition);
                    }
                }

            }
        }
    }

    private bool MovingUpInLine()
    {
        Vector3 dir = navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f
          ? navMeshAgent.desiredVelocity
          : (navMeshAgent.steeringTarget - transform.position);

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            onArrivedAtItem?.Invoke();
            return true;
        }

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 15 * Time.deltaTime);

        return false;
    }
}
