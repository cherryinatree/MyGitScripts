using UnityEngine;
using Cherry.Inventory;

[AddComponentMenu("Combat/Beam Loot On Death")]
public class BeamLootOnDeath : MonoBehaviour
{
    [Header("Drop")]
    public ItemDefinition dropItem;             // ectoplasm item definition
    [Min(1)] public int quantity = 1;

    [Tooltip("Prefab that has HarvestableItemSource configured OR can be empty and we’ll add it.")]
    public GameObject dropPrefab;

    private bool _dropped;

    public void SpawnAndTryCollect(Vector3 atWorldPos, Transform beamOrigin)
    {
        if (_dropped) return;
        _dropped = true;

        GameObject dropGo;
        if (dropPrefab != null)
        {
            dropGo = Instantiate(dropPrefab, atWorldPos, Quaternion.identity);
        }
        else
        {
            dropGo = new GameObject("EnemyDrop");
            dropGo.transform.position = atWorldPos;
        }

        var his = dropGo.GetComponent<HarvestableItemSource>();
        if (his == null) his = dropGo.AddComponent<HarvestableItemSource>();

        his.item = dropItem;
        his.quantity = quantity;

        // Try immediate beam-harvest. If inventory is full, your harvest code should fail gracefully.
        his.TryHarvestFromBeam(atWorldPos, beamOrigin);
    }
}