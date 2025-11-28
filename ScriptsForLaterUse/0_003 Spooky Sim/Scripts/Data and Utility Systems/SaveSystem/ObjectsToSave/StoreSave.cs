using System;
using System.Collections.Generic;
using UnityEngine;




[Serializable]
public struct PlacedObjectData
{
    public string id;           // stable unique object id
    public string prefabPath;   // Resources path, e.g., "Placeables/Shelf_A"
    public Vector3 position;    // world pos
    public Quaternion rotation; // world rot
    public string sceneBelongingTo; // scene name where the object was placed
}

[System.Serializable]
public class StoreSave 
{

    public int version = 1;
    public List<PlacedObjectData> objects = new List<PlacedObjectData>();

    public List<InventorySave> storageAreas;

}
