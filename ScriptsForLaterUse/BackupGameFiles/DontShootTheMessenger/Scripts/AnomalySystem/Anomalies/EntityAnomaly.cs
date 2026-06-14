using UnityEngine;


public class EntityAnomaly : Anomaly
{
    public GameObject hostileEntityPrefab;

    public override void Activate(GameObject player)
    {
        Debug.Log("Entity anomaly triggered: " + anomalyName);
        Instantiate(hostileEntityPrefab, transform.position, Quaternion.identity);
    }
}