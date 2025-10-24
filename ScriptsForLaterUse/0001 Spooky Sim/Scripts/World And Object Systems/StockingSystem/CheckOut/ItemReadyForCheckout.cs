using UnityEditor.SceneManagement;
using UnityEngine;

public class ItemReadyForCheckout : Interactable
{

    private Carryable carryable;
    private SellItemHolder sellItemHolder;
    private CheckOutCounter checkOutCounter;

    private bool scanned = false;

    private void Start()
    {
        carryable = GetComponent<Carryable>();
        sellItemHolder = GetComponent<SellItemHolder>();
    }

    public void SetCheckOutCounter(CheckOutCounter counter)
    {
        checkOutCounter = counter;
    }

    public override bool CanInteract(GameObject interactor)
    {
        return carryable != null && !carryable.IsHeldOrDocked;
    }

    public override void Interact(GameObject interactor)
    {
        Debug.Log("ItemReadyForCheckout: Interact called.");
        //ScanMe();
    }

    public void ScanMe()
    {
        if (scanned) return;
        checkOutCounter.ScanItem(carryable);
        scanned = true;
    }

    public void GetReadyToScan()
    {

        carryable.SetOnCheckoutCounter();
        //carryable.enabled = false;
    }
}
