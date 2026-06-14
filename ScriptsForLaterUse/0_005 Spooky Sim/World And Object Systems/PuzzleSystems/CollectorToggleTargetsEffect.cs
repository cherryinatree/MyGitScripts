using UnityEngine;

namespace Cherry.Puzzles
{
    [AddComponentMenu("Cherry/Puzzles/Effects/Collector Toggle Targets")]
    public class CollectorToggleTargetsEffect : MonoBehaviour, ICollectorEffect
    {
        [SerializeField] private GameObject[] enableWhileActive;
        [SerializeField] private GameObject[] disableWhileActive;

        public void OnFed(ItemTriggeredCollector collector, Cherry.Inventory.ItemDefinition item, int amountFed, int progress, int required) { }

        public void OnActivated(ItemTriggeredCollector collector, Cherry.Inventory.ItemDefinition item)
        {
            SetAll(enableWhileActive, true);
            SetAll(disableWhileActive, false);
        }

        public void OnDeactivated(ItemTriggeredCollector collector, Cherry.Inventory.ItemDefinition item)
        {
            SetAll(enableWhileActive, false);
            SetAll(disableWhileActive, true);
        }

        private static void SetAll(GameObject[] arr, bool state)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i]) arr[i].SetActive(state);
        }
    }
}