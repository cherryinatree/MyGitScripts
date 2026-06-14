using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PutItemsOnCounter : CombatAction
{

    public UnityEvent onArrivedAtItem;
    private CustomerShopping customerShopping;
    private CheckOutCounter checkOutCounter;

    public override void OnEnterState()
    {
        base.OnEnterState();
        customerShopping = GetComponent<CustomerShopping>();
        checkOutCounter = customerShopping.AssignedCheckOutLine.gameObject.GetComponent<CheckOutCounter>();
        transform.LookAt(new Vector3(transform.position.x, checkOutCounter.transform.position.y, transform.position.z));

        foreach (Carryable item in customerShopping.itemsCarried)
        {
            item.gameObject.SetActive(true);
            checkOutCounter.PlaceItemInBaggingArea(item);
            ItemReadyForCheckout itemReady = item.gameObject.GetComponent<ItemReadyForCheckout>();
            if (itemReady != null)
            {
                itemReady.GetReadyToScan();
            }
        }
        
    }

    public override void PerformAction()
    {
        if(checkOutCounter.allItemsScanned)
        {
            onArrivedAtItem.Invoke();
        }
    }
}
