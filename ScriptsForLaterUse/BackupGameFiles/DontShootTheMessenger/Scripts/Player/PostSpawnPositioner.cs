// PostSpawnPositioner.cs
using UnityEngine;
using Unity.Netcode;

public class PostSpawnPositioner : MonoBehaviour
{
    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;
        nm.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;
        nm.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var playerNO = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (!playerNO) return;

        var (pos, rot) = SpawnPointManager.Instance
            ? SpawnPointManager.Instance.GetNext()
            : (Vector3.zero, Quaternion.identity);

        playerNO.transform.SetPositionAndRotation(pos, rot);
    }
}
