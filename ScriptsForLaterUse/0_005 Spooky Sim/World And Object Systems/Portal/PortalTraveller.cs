using UnityEngine;

namespace Cherry.Portals
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Portals/Portal Traveller")]
    public class PortalTraveller : MonoBehaviour
    {
        public Transform teleportPivot;   // set to MainCamera transform
        public Rigidbody rb;
        public CharacterController characterController;

        public Transform Pivot => teleportPivot != null ? teleportPivot : transform;

        public void BeforeTeleport()
        {
            if (characterController != null) characterController.enabled = false;
        }

        public void AfterTeleport()
        {
            if (characterController != null) characterController.enabled = true;
        }
    }
}
