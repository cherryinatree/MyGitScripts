// VendorSpawner.cs
using UnityEngine;
using Unity.Netcode;

public class VendorSpawner : NetworkBehaviour
{
    public GameObject vendorPrefab;
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        if (!vendorPrefab) { Debug.LogError("[VendorSpawner] Assign vendorPrefab"); return; }

        var go = Instantiate(vendorPrefab, spawnPoint ? spawnPoint.position : Vector3.zero, spawnPoint ? spawnPoint.rotation : Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        if (!no) no = go.AddComponent<NetworkObject>();
        no.Spawn(true);
    }
}
