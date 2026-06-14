using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GateSensor : MonoBehaviour
{
    public string gateId = "A"; // "A" or "B"
    public DirectionGate gate;

    public LayerMask targetLayerMask;
    public string targetTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(((1 << other.gameObject.layer) & targetLayerMask) == 0) return;
        if (!string.IsNullOrEmpty(targetTag))
        {
            if (!other.CompareTag(targetTag)) return; 
        }
        if (gate == null) return;
        gate.NotifyGateEntered(gateId, other.gameObject);
    }
}
