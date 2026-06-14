using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public int totalTrenches => AnomalyManager.Instance != null ? AnomalyManager.Instance.trenchRoutes.Count : 0;

    // Called by GameManager to start a new delivery cycle
    public void StartRound()
    {
        if (AnomalyManager.Instance != null) AnomalyManager.Instance.AssignAnomalies();

        // Notify NPCs about the new round so they can update dialogue/intel
        if (NPCManager.Instance != null)
            NPCManager.Instance.OnNewRound();
    }

    // Called by Artillery when a delivery completes
    public void DeliveryComplete()
    {
        GameManager.Instance.CompleteDelivery();
    }

    // convenience
    public string GetCurrentTargetCoordinates()
    {
        // If you generate target coordinates here, return them.
        // (If you already implemented generation elsewhere, adapt this.)
        // For now we return a simple placeholder.
        return $"Target: {Random.Range(60, 200)},{Random.Range(30, 120)}";
    }
}
