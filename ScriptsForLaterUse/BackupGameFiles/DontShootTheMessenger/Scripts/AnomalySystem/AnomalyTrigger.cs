using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AnomalyTrigger : MonoBehaviour
{
    private Anomaly anomaly;

    private void Awake()
    {
        anomaly = GetComponent<Anomaly>();
        Collider c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (anomaly == null) return;

        // Make sure your player GameObject is tagged "Player"
        if (other.CompareTag("Player"))
        {
            anomaly.Activate(other.gameObject);
        }
    }
}
