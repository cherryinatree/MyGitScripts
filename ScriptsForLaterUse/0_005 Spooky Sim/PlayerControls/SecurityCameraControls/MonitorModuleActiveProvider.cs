using UnityEngine;
using Cherry.Cameras;

namespace Cherry.Cameras
{
    [DisallowMultipleComponent]
    public class MonitorModuleActiveProvider : MonoBehaviour, IMonitorModuleProvider
    {
        [Tooltip("This GameObject is toggled active when the player wants to use security cameras.")]
        [SerializeField] private GameObject cameraMonitorModuleObject;

        public bool HasMonitorModuleEquipped =>
            cameraMonitorModuleObject != null && cameraMonitorModuleObject.activeInHierarchy;
    }
}
