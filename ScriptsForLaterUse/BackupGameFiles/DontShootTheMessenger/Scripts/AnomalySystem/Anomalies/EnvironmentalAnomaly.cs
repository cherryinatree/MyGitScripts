using UnityEngine;


public class EnvironmentalAnomaly : Anomaly
{
    public override void Activate(GameObject player)
    {
        Debug.Log("Environmental anomaly triggered: " + anomalyName);
        // Example: change lighting, spawn fog, etc.
    }
}