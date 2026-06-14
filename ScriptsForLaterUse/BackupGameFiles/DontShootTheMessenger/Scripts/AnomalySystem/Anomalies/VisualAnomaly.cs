using UnityEngine;

public class VisualAnomaly : Anomaly
{
    public override void Activate(GameObject player)
    {
        Debug.Log($"Visual anomaly triggered: {anomalyName}");
        // Example: flash screen, spawn visual distortion, etc.
        // (Implement your effect code here.)
    }
}
