using System.Collections.Generic;
using UnityEngine;
using Remodeling.Runtime;
using Remodeling.Data;

namespace Remodeling.Data
{
    [CreateAssetMenu(menuName = "Remodel/Upgrade Catalog")]
    public class UpgradeCatalogSO : ScriptableObject
    {
        public List<UpgradeDefinitionSO> upgrades = new();

        public IEnumerable<UpgradeDefinitionSO> GetByCategory(UpgradeCategory category)
        {
            foreach (var u in upgrades)
                if (u && u.category == category)
                    yield return u;
        }
    }
}
