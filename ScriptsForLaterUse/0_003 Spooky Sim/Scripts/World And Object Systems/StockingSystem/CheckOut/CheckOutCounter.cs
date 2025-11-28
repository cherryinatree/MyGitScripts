using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CheckOutCounter : MonoBehaviour
{
    public GameObject scanner;
    public GameObject baggingArea;
    public GameObject placementArea;

    private List<Carryable> itemsOnCounter = new List<Carryable>();
    private List<Carryable> scannedItems = new List<Carryable>();
    public float handToTargetDuration = 0.5f;

    public SaleSystem saleSystem;

    private Carryable scanItem;
    private bool scannedItemFlag = false;

    public bool allItemsScanned
    {
        get
        {
            return itemsOnCounter.Count == 0;
        }
    }

    public void ResetCounter()
    {
        scannedItemFlag = false;
        foreach (var item in scannedItems)
        {
            Destroy(item.gameObject);
        }
        scannedItems.Clear();
    }

    public float GetTotalPrice()
    {
        return saleSystem.GetTotalPrice();
    }

    private void Start()
    {
        saleSystem = GetComponent<SaleSystem>();
    }

    private void Update()
    {
        if (scanItem != null && !scannedItemFlag)
        {
            StartCoroutine(PutInBag());
        }
    }


    public void PlaceItemInBaggingArea(Carryable item)
    {
        item.transform.position = placementArea.transform.position;
        item.transform.rotation = placementArea.transform.rotation;
        itemsOnCounter.Add(item);
        item.GetComponent<ItemReadyForCheckout>().SetCheckOutCounter(this);
    }

    public void ScanItem(Carryable item)
    {
        Debug.Log("ScanItem called.");
        if (itemsOnCounter.Contains(item))
        {
            Debug.Log("ScanItem set ");
            scanItem = item;
            scannedItemFlag = false;
        }
    }


    private IEnumerator PutInBag()
    {
        if(scanItem == null || scannedItemFlag)
        {
            yield break;
        }
        yield return SimpleTween.MoveRotate(scanItem.transform, baggingArea.transform.position, baggingArea.transform.rotation, handToTargetDuration);

        if (scanItem == null || scannedItemFlag)
        {
            yield break;
        }
        saleSystem.AddItemToSaleUI(scanItem.GetComponent<SellItemHolder>().DisplayName, scanItem.GetComponent<SellItemHolder>().Price);
        //scanItem.gameObject.SetActive(false);
        scannedItems.Add(scanItem);
        itemsOnCounter.Remove(scanItem);
        scanItem.gameObject.SetActive(false);
        scanItem = null;
        scannedItemFlag = true;

    }
}
