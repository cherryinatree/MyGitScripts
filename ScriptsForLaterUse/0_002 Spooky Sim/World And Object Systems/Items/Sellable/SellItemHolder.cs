using static UnityEditor.Progress;
using UnityEngine;

[DisallowMultipleComponent]
public class SellItemHolder : MonoBehaviour
{
    [SerializeField] private SellableItem item;

    public SellableItem Item => item;

    // Convenience passthroughs (safe if item is null)
    public int ItemId => item ? item.ItemId : -1;
    public float Price => item ? item.Price : 0f;
    public string DisplayName => item ? item.DisplayName : string.Empty;
    public string Description => item ? item.Description : string.Empty;
    public Sprite Icon => item ? item.Icon : null;

}
