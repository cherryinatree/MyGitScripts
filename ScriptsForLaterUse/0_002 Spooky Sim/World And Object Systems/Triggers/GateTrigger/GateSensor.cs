using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GateSensor : MonoBehaviour
{
    public string gateId = "A"; // "A" or "B"
    public DirectionGate gate;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (gate == null) return;
        gate.NotifyGateEntered(gateId, other.gameObject);
    }
}
