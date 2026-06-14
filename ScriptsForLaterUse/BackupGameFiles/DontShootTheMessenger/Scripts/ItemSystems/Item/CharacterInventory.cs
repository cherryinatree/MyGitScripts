// CharacterInventory.cs
using UnityEngine;

public class CharacterInventory : CharacterInventoryNet
{
    public Inventory inventory;
    public CurrencyWallet wallet;

    private void Start()
    {
        if (!inventory) inventory = GetComponent<Inventory>();
        if (!wallet) wallet = GetComponent<CurrencyWallet>();
    }

    private void Update()
    {
        Debug.Log("Credits: " + wallet.credits);
        Debug.Log("inventory: " + inventory.contents.Count);
        Debug.Log("Contents: " + contents.Count);
    }
}
