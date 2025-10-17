using UnityEngine;

namespace ProcGen.Anomalies
{
    /// <summary>Put this on any room prefab that should count as an "anomaly" for scoring.</summary>
    public class AnomalyTag : MonoBehaviour
    {
        public bool isAnomaly = true;
    }
}
