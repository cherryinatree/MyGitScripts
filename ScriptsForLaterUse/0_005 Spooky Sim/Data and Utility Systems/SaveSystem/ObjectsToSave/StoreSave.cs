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

[Serializable]
public struct StoreSurfaceCustomizationData
{
    public string surfaceId;   // StoreSurface.uniqueId
    public string sceneName;   // scene the surface belongs to
    public string finishId;    // FinishMaterialEntry.id (stable)

    // tint (Color) as floats
    public float tintR;
    public float tintG;
    public float tintB;
    public float tintA;

    // tiling (Vector2) as floats
    public float tilingX;
    public float tilingY;
}

[System.Serializable]
public class StoreSave
{
    public int version = 2; // bump, optional but recommended

    public List<PlacedObjectData> objects = new List<PlacedObjectData>();
    public List<InventorySave> storageAreas;

    // NEW:
    public List<StoreSurfaceCustomizationData> surfaceCustomizations = new List<StoreSurfaceCustomizationData>();
}

