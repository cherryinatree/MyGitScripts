using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static UnityEditor.Progress;

public class CustomerShopping : MonoBehaviour
{
    [HideInInspector]
    public List<Carryable> shoppingList;
    [HideInInspector]
    public List<Carryable> itemsCarried;
    private CustomerStoreManager storeManager;

    private CheckOutLine _assignedCheckOutLine;

    public int shoppingListMaxSize = 3;

    public GameObject shoppingBag;
    public float handToTargetDuration = 0.5f;


    public CheckOutLine AssignedCheckOutLine
    {
        get { return _assignedCheckOutLine; }
        set { _assignedCheckOutLine = value; }
    }


    public void InitializeShoppingList()
    {
        storeManager = FindFirstObjectByType<CustomerStoreManager>();
        shoppingList = new List<Carryable>();
        itemsCarried = new List<Carryable>();

        int shoppingListSize = Random.Range(1, shoppingListMaxSize + 1);

        for (int i = 0; i < shoppingListSize; i++)
        {
            Carryable item = storeManager.GetRandomItem();
            if (item != null)
            {
                shoppingList.Add(item);
            }
        }
    }

    public Vector3 GetNextItemLocation()
    {
        if(shoppingList.Count == 0) return Vector3.zero;
        return storeManager.GetItemPosition(shoppingList[0]).position;
    }

    private void AddItemToCarriedList(Carryable item)
    {
        if (!itemsCarried.Contains(item))
        {
            itemsCarried.Add(item);
        }

        if (shoppingList.Contains(item))
        {
            shoppingList.Remove(item);
        }
    }

    private void GrabItemFromShelf(Carryable item)
    {
        AddItemToCarriedList(item);
    }

    public void PutItemInBag()
    {
    
        if(shoppingList.Count == 0) return;
        if (Vector3.Distance((shoppingList[0]).transform.position, transform.position) < 2f)
        {
            StartCoroutine(PutInBag(shoppingList[0]));
        }
    }

    private IEnumerator PutInBag(Carryable item)
    {
        yield return SimpleTween.MoveRotate(item.transform, shoppingBag.transform.position, shoppingBag.transform.rotation, handToTargetDuration);
        
        GrabItemFromShelf(item);
        item.UndockToWorld();
        item.gameObject.SetActive(false);
    }

}
