// ConnectionApprovalSpawner.cs
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class ConnectionApprovalSpawner : MonoBehaviour
{
    NetworkManager nm;

    void Awake()
    {
        nm = GetComponent<NetworkManager>();        // guaranteed to exist on this GameObject
        nm.NetworkConfig.ConnectionApproval = true; // or tick it in the Inspector
        nm.ConnectionApprovalCallback += Approval;
    }

    void OnDestroy()
    {
        if (nm != null) nm.ConnectionApprovalCallback -= Approval;
    }

    void Approval(NetworkManager.ConnectionApprovalRequest req,
                  NetworkManager.ConnectionApprovalResponse resp)
    {
        var (pos, rot) = SpawnPointManager.Instance
            ? SpawnPointManager.Instance.GetNext()
            : (Vector3.zero, Quaternion.identity);

        resp.Approved = true;
        resp.CreatePlayerObject = true;
        resp.Position = pos;
        resp.Rotation = rot;
        // resp.PlayerPrefabHash = null; // use a different prefab if you want
    }
}
