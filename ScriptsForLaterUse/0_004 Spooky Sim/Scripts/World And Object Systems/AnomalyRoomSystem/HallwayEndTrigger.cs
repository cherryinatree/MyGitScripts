// HallwayEndTrigger.cs
using UnityEngine;

public enum CrossDir { TowardRoom, TowardHallwayInterior }

[RequireComponent(typeof(Collider))]
public class HallwayEndTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    private RoomTag hallway;

    private void Awake()
    {
        hallway = GetComponentInParent<RoomTag>();
        if (hallway == null) Debug.LogError("[HallwayEndTrigger] Must be a child of a Hallway prefab with RoomTag.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Estimate crossing direction using velocity if available, else fall back to relative movement.
        Vector3 v = Vector3.zero;
        var rb = other.attachedRigidbody;
        if (rb != null) v = rb.linearVelocity;

        // If nearly stopped, bias toward "TowardRoom" only if the player is in front of the gate
        CrossDir dir;
        if (v.sqrMagnitude > 0.01f)
        {
            dir = Vector3.Dot(transform.forward, v) >= 0f ? CrossDir.TowardRoom : CrossDir.TowardHallwayInterior;
        }
        else
        {
            // Fallback: compare player position side of the gate
            var toPlayer = other.transform.position - transform.position;
            dir = Vector3.Dot(transform.forward, toPlayer) >= 0f ? CrossDir.TowardRoom : CrossDir.TowardHallwayInterior;
        }

        AnomalySystem.Instance?.OnHallwayGateCrossed(hallway, dir);
    }
}
