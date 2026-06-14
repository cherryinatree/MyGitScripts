using UnityEngine;

public class TrenchRoute : MonoBehaviour
{
    [Tooltip("Optional display name for the inspector/logs")]
    public string trenchName;
    [HideInInspector] public bool isAnomalous = false;

    private Anomaly assignedAnomaly;

    public void SetAnomalous(bool anomalous)
    {
        isAnomalous = anomalous;

        // if making safe, clear any assigned anomaly prefab
        if (!isAnomalous)
        {
            ClearAnomaly();
        }
    }

    public void AssignAnomaly(Anomaly anomaly)
    {
        ClearAnomaly();
        assignedAnomaly = anomaly;
        isAnomalous = (anomaly != null);
    }

    public void ClearAnomaly()
    {
        if (assignedAnomaly != null)
        {
            Destroy(assignedAnomaly.gameObject);
            assignedAnomaly = null;
        }
        isAnomalous = false;
    }

    public bool IsAnomalous()
    {
        return isAnomalous;
    }

    public Anomaly GetAnomaly()
    {
        return assignedAnomaly;
    }
}
