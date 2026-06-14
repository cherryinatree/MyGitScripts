using Remodeling.Runtime;
using UnityEngine;

namespace Remodeling.Data
{
    public class RemodelContext
    {
        public Transform shipRoot;
        public ShipSizeTierApplier sizeApplier;
        public PlayerUpgradeState playerState;
        public PlayerStats stats;

        public string currentUpgradeId;
        public int currentPurchaseCount;
    }
}
