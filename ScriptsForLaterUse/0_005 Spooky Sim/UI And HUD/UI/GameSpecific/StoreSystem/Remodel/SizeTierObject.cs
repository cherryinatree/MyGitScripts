using UnityEngine;


namespace Remodeling.Runtime
{
  
    public class SizeTierObject : MonoBehaviour
    {
        [Tooltip("Lowest ship size tier where this object should be active.")]
        public int minTier = 0;

        [Tooltip("Highest ship size tier where this object should be active. -1 = no max.")]
        public int maxTier = -1;

        public void ApplyTier(int currentTier)
        {
            bool active = currentTier >= minTier && (maxTier < 0 || currentTier <= maxTier);
            if (gameObject.activeSelf != active) gameObject.SetActive(active);
        }
    }
}
