using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MovableObject
{
    public string id;
    public Vector3 position;
    public Quaternion rotation;
    public int buildObjectID;

    public float lastCollectionTime;
    public List<Item> items;
}
