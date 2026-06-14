// StarterPack.cs (on the Player prefab)
using Unity.Netcode;
using UnityEngine;

public class StarterPack : NetworkBehaviour
{
    public CharacterInventoryNet inv;
    public string[] startItemIds = { "ammo_9mm" };
    public int[] startItemQtys = { 30 };
    public int startCredits = 200;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        if (!inv) inv = GetComponent<CharacterInventoryNet>();
        inv.RequestAddCreditsServerRpc(startCredits);
        for (int i = 0; i < startItemIds.Length; i++)
            inv.RequestAddItemServerRpc(startItemIds[i], startItemQtys[i]);
    }
}
