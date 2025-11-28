using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cherry.Levels
{
    [CreateAssetMenu(fileName = "LevelCatalogue", menuName = "Cherry/Levels/Level Catalogue")]
    public class LevelCatalogue : ScriptableObject
    {
        [Tooltip("Drop LevelDefinition assets here.")]
        public List<LevelDefinition> levels = new();

        public IEnumerable<LevelDefinition> All => levels.Where(x => x != null);

        public IEnumerable<LevelDefinition> ByCategory(string cat) =>
            All.Where(x => string.Equals(x.category, cat, System.StringComparison.OrdinalIgnoreCase));

    }
}
