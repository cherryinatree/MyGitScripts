// DoorwayTrigger.cs
using UnityEngine;

public enum DoorwayKind
{
    EnterFromHallway, // entering room from the hallway side
    ExitForward,      // leaving room via the forward door to whatever comes next
    BackToHallway     // leaving room back into the same hallway
}

[RequireComponent(typeof(Collider))]
public class DoorwayTrigger : MonoBehaviour
{
    public DoorwayKind kind;
    public string playerTag = "Player"; // tag your player

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Find the nearest RoomTag up the hierarchy (the room this doorway belongs to)
        var room = GetComponentInParent<RoomTag>();
        if (room == null)
        {
            // Might be a hallway doorway (rare). We still notify manager without a room.
            //AnomalySystem.Instance?.OnDoorwayCrossed(kind, null);
            return;
        }

       // AnomalySystem.Instance?.OnDoorwayCrossed(kind, room);
    }
}
