// NetItemPickup.cs
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject)), RequireComponent(typeof(Collider))]
public class NetItemPickup : NetworkBehaviour
{
    public string itemId;
    public int quantity = 1;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned) return;

        var inv = other.GetComponentInParent<CharacterInventoryNet>();
        if (!inv || !inv.IsOwner) return; // only local owner requests

        RequestPickupServerRpc(inv.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestPickupServerRpc(NetworkObjectReference whoRef)
    {
        if (!whoRef.TryGet(out var no)) return;
        var inv = no.GetComponent<CharacterInventoryNet>();
        if (!inv) return;

        inv.RequestAddItemServerRpc(itemId, quantity); // server adds to networked inventory
        NetworkObject.Despawn();
    }
}
