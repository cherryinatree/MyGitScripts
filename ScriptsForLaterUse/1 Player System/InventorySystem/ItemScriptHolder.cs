using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/ItemList")]
public class ItemScriptHolder : ScriptableObject
{
   public List<ItemScript> items;
}
