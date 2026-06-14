// EquipmentNet.cs
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class EquipmentNet : NetworkBehaviour
{
    public ItemDatabase db;
    [Header("Sockets")]
    public Transform primarySocket;
    public Transform secondarySocket;
    public Transform headSocket;
    public Transform bodySocket;

    public NetworkVariable<FixedString64Bytes> primaryItemId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> secondaryItemId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> headItemId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> bodyItemId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    GameObject primaryLocal, secondaryLocal, headLocal, bodyLocal;

    public override void OnNetworkSpawn()
    {
        if (db) db.Init();
        primaryItemId.OnValueChanged += (_, n) => RefreshSlot(ref primaryLocal, primarySocket, n.ToString());
        secondaryItemId.OnValueChanged += (_, n) => RefreshSlot(ref secondaryLocal, secondarySocket, n.ToString());
        headItemId.OnValueChanged += (_, n) => RefreshSlot(ref headLocal, headSocket, n.ToString());
        bodyItemId.OnValueChanged += (_, n) => RefreshSlot(ref bodyLocal, bodySocket, n.ToString());

        // initial refresh
        RefreshSlot(ref primaryLocal, primarySocket, primaryItemId.Value.ToString());
        RefreshSlot(ref secondaryLocal, secondarySocket, secondaryItemId.Value.ToString());
        RefreshSlot(ref headLocal, headSocket, headItemId.Value.ToString());
        RefreshSlot(ref bodyLocal, bodySocket, bodyItemId.Value.ToString());
    }

    void RefreshSlot(ref GameObject holder, Transform socket, string itemId)
    {
        if (holder) Destroy(holder);
        if (string.IsNullOrEmpty(itemId) || !socket || !db) return;

        var def = db.Get(itemId);
        if (def && def.equippedPrefab)
        {
            holder = Instantiate(def.equippedPrefab, socket);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
        }
    }

    // ===== Equip requests (server authoritative) =====
    [ServerRpc(RequireOwnership = false)]
    public void RequestEquipServerRpc(NetworkObjectReference whoRef, string itemId, EquipSlot slot)
    {
        if (!whoRef.TryGet(out var no)) return;
        var inv = no.GetComponent<CharacterInventoryNet>(); // ensures caller is a valid player
        if (!inv || inv.CountLocal(itemId) <= 0) return;

        var def = db.Get(itemId);
        if (!def) return;

        switch (slot)
        {
            case EquipSlot.PrimaryWeapon:
                primaryItemId.Value = itemId;
                break;
            case EquipSlot.SecondaryWeapon:
                secondaryItemId.Value = itemId;
                break;
            case EquipSlot.Head:
                headItemId.Value = itemId;
                break;
            case EquipSlot.Body:
                bodyItemId.Value = itemId;
                break;
        }
        // If equipping consumes inventory: uncomment next line
        // inv.RequestRemoveItemServerRpc(itemId, 1);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUnequipServerRpc(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.PrimaryWeapon: primaryItemId.Value = ""; break;
            case EquipSlot.SecondaryWeapon: secondaryItemId.Value = ""; break;
            case EquipSlot.Head: headItemId.Value = ""; break;
            case EquipSlot.Body: bodyItemId.Value = ""; break;
        }
    }
}
