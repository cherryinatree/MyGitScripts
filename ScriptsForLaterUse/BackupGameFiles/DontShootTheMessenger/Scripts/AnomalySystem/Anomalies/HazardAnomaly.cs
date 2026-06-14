using UnityEngine;

public class HazardAnomaly : Anomaly
{
    public int damage = 9999; // huge by default

    public override void Activate(GameObject player)
    {
        Debug.Log($"Hazard anomaly triggered: {anomalyName}");
        // If lethal, kill immediately via GameManager (if present)
        if (lethal && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        // else, you can call player's health component here if you have one
    }
}