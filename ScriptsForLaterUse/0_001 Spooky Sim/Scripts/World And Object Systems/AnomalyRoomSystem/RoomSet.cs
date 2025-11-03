// RoomSet.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AnomalyGame/RoomSet")]
public class RoomSet : ScriptableObject
{
    public GameObject startRoom;
    public List<GameObject> hallwayPrefabs;
    public List<GameObject> cleanRoomPrefabs;
    public List<GameObject> anomalyRoomPrefabs;
    public GameObject endRoom;
}
