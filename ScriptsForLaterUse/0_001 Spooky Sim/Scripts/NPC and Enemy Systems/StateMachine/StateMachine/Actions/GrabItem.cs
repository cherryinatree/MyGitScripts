using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;

public class GrabItem : CombatAction
{

    public UnityEvent GoToLine;
    public UnityEvent GrabAnotherItem;

    CustomerShopping customerShopping;
    int currentItemCount = 0;

    private NavMeshAgent navMeshAgent;

    private GameObject ShelfFixture;

    public override void OnEnterState()
    {
        base.OnEnterState();
        customerShopping = GetComponent<CustomerShopping>();
        currentItemCount = customerShopping.itemsCarried.Count;
        if (customerShopping.shoppingList.Count > 0)
        {
            ShelfFixture = customerShopping.shoppingList[0].GetFixtureParent().gameObject;
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false; // Disable automatic rotation
    }


    public override void PerformAction()
    {
        FaceDirection();
        if (customerShopping.shoppingList.Count != 0)
        {
            if (customerShopping.itemsCarried.Count == currentItemCount)
            {
                customerShopping.PutItemInBag();
            }
            else if (customerShopping.shoppingList.Count > 0)
            {
                GrabAnotherItem?.Invoke();
            }
        }else
        {
            GoToLine?.Invoke();
        }
    }

    private void FaceDirection()
    {
        if (ShelfFixture != null)
        {
            // Calculate the direction to the target
            Vector3 directionToTarget = ShelfFixture.transform.position - transform.position;
            directionToTarget.y = 0; // Keep rotation horizontal

            if (directionToTarget != Vector3.zero)
            {
                // Create a target rotation
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                // Smoothly rotate towards the target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15 * Time.deltaTime);
            }
        }
    }
}
