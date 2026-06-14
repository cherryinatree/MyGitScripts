using UnityEngine;

namespace Cherry.Airlocks
{
    [AddComponentMenu("Cherry/Airlocks/Pressure Door Link")]
    public class PressureDoorLink : MonoBehaviour
    {
        [Header("Door State Source")]
        [SerializeField] private DoorSignal doorSignal;

        [Header("Connected Volumes (null means Vacuum/Space)")]
        [SerializeField] private PressureVolume sideA;
        [SerializeField] private PressureVolume sideB;

        [Header("Vacuum Pull Point (usually just outside the outer door)")]
        [SerializeField] private Transform suctionPoint;

        public DoorSignal DoorSignal => doorSignal;
        public bool IsOpen => doorSignal && doorSignal.IsOpen;

        public PressureVolume SideA => sideA;
        public PressureVolume SideB => sideB;

        public bool ConnectsToVacuum => sideA == null || sideB == null;

        public Transform SuctionPoint => suctionPoint ? suctionPoint : transform;

        public PressureVolume GetOther(PressureVolume v)
        {
            if (v == sideA) return sideB;
            if (v == sideB) return sideA;
            return null;
        }

        public PressureVolume NonVacuumSide()
        {
            return sideA ? sideA : sideB;
        }

        private void Awake()
        {
            if (sideA) sideA.RegisterDoor(this);
            if (sideB) sideB.RegisterDoor(this);

            if (!doorSignal)
                Debug.LogWarning($"{name}: PressureDoorLink has no DoorSignal assigned.", this);
        }

        private void OnDestroy()
        {
            if (sideA) sideA.UnregisterDoor(this);
            if (sideB) sideB.UnregisterDoor(this);
        }
    }
}
