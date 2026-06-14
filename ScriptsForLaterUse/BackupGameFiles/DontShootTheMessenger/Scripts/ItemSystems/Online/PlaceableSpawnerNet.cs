// PlaceableSpawnerNet.cs
using Unity.Netcode;
using UnityEngine;

public class PlaceableSpawnerNet : NetworkBehaviour
{
    public ItemDatabase db;
    [Tooltip("Max distance from owner to allow placement.")]
    public float maxPlaceDistance = 15f;
    public LayerMask floorMask = ~0;

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlaceServerRpc(NetworkObjectReference whoRef, string itemId, Vector3 pos, Quaternion rot)
    {
        if (!whoRef.TryGet(out var no)) return;
        var inv = no.GetComponent<CharacterInventoryNet>();
        if (!inv) return;
        if (!db) return;

        // Validate distance
        if (Vector3.Distance(no.transform.position, pos) > maxPlaceDistance) return;

        // Resolve def & check placeable
        var def = db.Get(itemId);
        if (!def || !def.isPlaceable || !def.placeablePrefab) return;

        // Ensure they have the item
        if (inv.CountLocal(itemId) <= 0) return;

        // Optional: overlap/ground checks
        // Physics.CheckBox / SphereCast etc.

        // Consume and spawn
        inv.RequestRemoveItemServerRpc(itemId, 1);

        var go = Instantiate(def.placeablePrefab, pos, rot);
        var netObj = go.GetComponent<NetworkObject>();
        if (!netObj) netObj = go.AddComponent<NetworkObject>();
        netObj.Spawn(true);
    }
}
