using UnityEngine;
using UnityEngine.Events;

public class WaitForChange : CombatAction
{

    public UnityEvent onArrivedAtItem;
    private SaleSystem saleSystem;
    

    public override void OnEnterState()
    {
        base.OnEnterState();
        saleSystem = GetComponent<CustomerShopping>().AssignedCheckOutLine.GetComponent<SaleSystem>();
    }

    public override void PerformAction()
    {
        if (saleSystem.GetTotalPrice() <= 0f)
        {
            onArrivedAtItem.Invoke();
        }
    }
}
