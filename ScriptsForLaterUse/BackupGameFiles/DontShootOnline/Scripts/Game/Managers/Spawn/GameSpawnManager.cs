using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameSpawnManager : MonoBehaviour
{
    [Header("Assign your Player prefab (with NetworkObject)")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Drop your spawn point transforms here (order matters)")]
    [SerializeField] private List<Transform> spawnPoints = new();

    private void Awake()
    {
        // Ensure NetworkManager exists
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager in scene.");
            return;
        }

        // We’re doing manual player spawn; disable the auto one
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

        // Subscribe to client connect/disconnect
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return; // server/host spawns

        // Choose a spawn point (round-robin)
        int index = (int)(clientId % (ulong)Mathf.Max(1, spawnPoints.Count));
        Vector3 pos = spawnPoints.Count > 0 ? spawnPoints[index].position : Vector3.zero;
        Quaternion rot = spawnPoints.Count > 0 ? spawnPoints[index].rotation : Quaternion.identity;

        var playerObj = Instantiate(playerPrefab, pos, rot);
        var netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Player prefab needs a NetworkObject.");
            Destroy(playerObj);
            return;
        }

        // Give ownership to the connecting client
        netObj.SpawnAsPlayerObject(clientId, true);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // If you want: find their player object and Despawn. NGO auto-despawns player objects by default.
    }
}
