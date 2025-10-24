// HallwayEntranceCommitTrigger.cs
using UnityEngine;

public enum EntranceCrossDir { FromRoomIntoHallway, FromHallwayOut }

[RequireComponent(typeof(Collider))]
public class HallwayEntranceCommitTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    private RoomTag hallway; // the hallway this trigger belongs to

    private void Start()
    {
        hallway = GetComponentInParent<RoomTag>();
        if (hallway == null)
            Debug.LogError("[HallwayEntranceCommitTrigger] Must be placed under a Hallway prefab with RoomTag.");
    }
    // HallwayEntranceCommitTrigger.cs  (replace OnTriggerEnter body)
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Direction check (room -> hallway interior?)
        var rb = other.attachedRigidbody;
        Vector3 v = rb ? rb.linearVelocity : Vector3.zero;

        EntranceCrossDir dir;
        if (v.sqrMagnitude > 0.01f)
            dir = Vector3.Dot(transform.forward, v) >= 0f ? EntranceCrossDir.FromRoomIntoHallway
                                                          : EntranceCrossDir.FromHallwayOut;
        else
        {
            var toPlayer = other.transform.position - transform.position;
            dir = Vector3.Dot(transform.forward, toPlayer) >= 0f ? EntranceCrossDir.FromRoomIntoHallway
                                                                 : EntranceCrossDir.FromHallwayOut;
        }

        if (dir != EntranceCrossDir.FromRoomIntoHallway) return;

        // NEW: if coming back from an anomaly, request a reroll instead of committing forward
        if (AnomalySystem.Instance != null && AnomalySystem.Instance.IsCurrentRoomAnomaly())
        {
            AnomalySystem.Instance.BacktrackIntoHallway(hallway);
        }
        else
        {
            // Normal forward promotion when leaving a clean room into the next hallway
            AnomalySystem.Instance?.CommitForwardFromCurrentRoom();
        }
    }

}
