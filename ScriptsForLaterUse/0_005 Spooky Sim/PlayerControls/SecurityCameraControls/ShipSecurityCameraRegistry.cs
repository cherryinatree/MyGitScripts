using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Cameras
{
    [DisallowMultipleComponent]
    public class ShipSecurityCameraRegistry : MonoBehaviour
    {
        [Tooltip("Optional explicit ordering. If empty, rigs are auto-found.")]
        [SerializeField] private List<SecurityCameraRig> cameras = new List<SecurityCameraRig>();

        public IReadOnlyList<SecurityCameraRig> Cameras => cameras;

        private void Awake()
        {
            if (cameras == null) cameras = new List<SecurityCameraRig>();

            if (cameras.Count == 0)
            {
                cameras.AddRange(FindObjectsOfType<SecurityCameraRig>(true));
            }

            // Clean nulls
            cameras.RemoveAll(c => c == null);
        }
    }
}
