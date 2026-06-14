using UnityEngine;

namespace Remodeling.Runtime
{
    public class ShipSizeTierApplier : MonoBehaviour
    {
        [SerializeField] private SizeTierObject[] tierObjects;

        public void Apply(int tier)
        {
            if (tierObjects == null || tierObjects.Length == 0)
                tierObjects = FindObjectsOfType<SizeTierObject>(includeInactive: true);

            foreach (var obj in tierObjects)
                if (obj) obj.ApplyTier(tier);
        }
    }
}
