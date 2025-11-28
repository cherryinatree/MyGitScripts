using UnityEditor.SceneManagement;
using UnityEngine;

public class ItemReadyForCheckout : Interactable
{

    private Carryable carryable;
    private SellItemHolder sellItemHolder;
    public CheckOutCounter checkOutCounter;

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
        //ScanMe();
    }

    public void ScanMe()
    {
        Debug.Log("ScanMe called from ItemReadyForCheckout.");
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
