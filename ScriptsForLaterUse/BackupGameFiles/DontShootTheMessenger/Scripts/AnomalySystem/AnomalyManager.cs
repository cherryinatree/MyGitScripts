using System.Collections.Generic;
using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    public static AnomalyManager Instance;

    [Header("Trench Routes (assign in inspector in correct order)")]
    public List<TrenchRoute> trenchRoutes = new List<TrenchRoute>();

    [Header("Anomaly Prefabs (assign any number; lists may be empty)")]
    public List<GameObject> visualAnomalies;
    public List<GameObject> audioAnomalies;
    public List<GameObject> entityAnomalies;
    public List<GameObject> environmentalAnomalies;
    public List<GameObject> hazardAnomalies;

    [Range(0f, 1f)]
    public float anomalyChancePerTrench = 0.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AssignAnomalies()
    {
        // Clear previous anomalies first
        foreach (var trench in trenchRoutes)
            trench.ClearAnomaly();

        for (int i = 0; i < trenchRoutes.Count; i++)
        {
            var trench = trenchRoutes[i];

            bool willBeAnomalous = Random.value < anomalyChancePerTrench;
            trench.SetAnomalous(willBeAnomalous);

            if (willBeAnomalous)
            {
                SpawnRandomAnomaly(trench);
            }
        }

    }

    private void SpawnRandomAnomaly(TrenchRoute trench)
    {
        // choose a category randomly, but gracefully handle empty lists
        var pools = new List<List<GameObject>> { visualAnomalies, audioAnomalies, entityAnomalies, environmentalAnomalies, hazardAnomalies };
        // filter pools that have items
        var nonEmptyPools = pools.FindAll(p => p != null && p.Count > 0);
        if (nonEmptyPools.Count == 0)
        {
            return;
        }

        var chosenPool = nonEmptyPools[Random.Range(0, nonEmptyPools.Count)];
        GameObject prefab = chosenPool[Random.Range(0, chosenPool.Count)];
        if (prefab == null) return;

        // instantiate at trench position (you can use a child spawn point instead)
        GameObject obj = Instantiate(prefab, trench.transform.position, Quaternion.identity);
        Anomaly anomaly = obj.GetComponent<Anomaly>();
        if (anomaly == null)
        {
            Debug.LogWarning("AnomalyManager: Spawned prefab has no Anomaly component.");
            Destroy(obj);
            return;
        }

        trench.AssignAnomaly(anomaly);
    }

    /// <summary>
    /// Returns indices of trenches that are safe this round.
    /// </summary>
    public List<int> GetSafeTrenchIndexes()
    {
        var list = new List<int>();
        for (int i = 0; i < trenchRoutes.Count; i++)
            if (!trenchRoutes[i].IsAnomalous())
                list.Add(i);
        return list;
    }

    /// <summary>
    /// Returns first safe trench index, or -1 if none.
    /// </summary>
    public int GetFirstSafeTrenchIndex()
    {
        var safe = GetSafeTrenchIndexes();
        return safe.Count > 0 ? safe[0] : -1;
    }
}
