using UnityEngine;
using Cherry.Cameras;

public class MonitorModuleProvider : MonoBehaviour, IMonitorModuleProvider
{
    // Replace this with your real equipment check.
    public bool HasMonitorModuleEquipped => hasIt;

    [SerializeField] private bool hasIt = true; // debug toggle
}
