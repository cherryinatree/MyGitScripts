using Cherry.Inventory;
using NUnit.Framework;
using UnityEngine;

[System.Serializable]
public class InventorySave 
{
    public string inventoryName;
    public int[] itemDefinitionsIDs;
    public int[] itemAmounts;
}
