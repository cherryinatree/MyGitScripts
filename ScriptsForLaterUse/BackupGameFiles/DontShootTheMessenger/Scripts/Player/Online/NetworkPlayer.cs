// NetworkPlayer.cs
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Optional: assign things only the local owner should run")]
    [SerializeField] private Behaviour[] localOnlyBehaviours;
    [SerializeField] private GameObject[] localOnlyObjects;

    [Header("Optional: camera you only enable for the local owner")]
    [SerializeField] private Camera localPlayerCamera;

    /// <summary>
    /// Fired on the owning client when the object spawns.
    /// Great place to bind input, enable local UI, etc.
    /// </summary>
    public event System.Action OnLocalOwnerReady;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Owner/local initialization
        bool isLocal = IsOwner && IsClient;
        SetLocalOnlyActive(isLocal);

        if (isLocal)
        {
            OnLocalOwnerReady?.Invoke();
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsOwner && IsClient) SetLocalOnlyActive(true);
    }

    public override void OnLostOwnership()
    {
        base.OnLostOwnership();
        if (IsClient) SetLocalOnlyActive(false);
    }

    private void SetLocalOnlyActive(bool enable)
    {
        if (localOnlyBehaviours != null)
        {
            foreach (var b in localOnlyBehaviours)
            {
                if (b != null) b.enabled = enable;
            }
        }
        if (localOnlyObjects != null)
        {
            foreach (var go in localOnlyObjects)
            {
                if (go != null) go.SetActive(enable);
            }
        }
        if (localPlayerCamera != null)
        {
            localPlayerCamera.enabled = enable;
            var audioListener = localPlayerCamera.GetComponent<AudioListener>();
            if (audioListener) audioListener.enabled = enable;
        }
    }

    #region Helpers
    protected void RunIfOwner(System.Action action)
    {
        if (IsOwner && IsClient) action?.Invoke();
    }

    protected void RunIfServer(System.Action action)
    {
        if (IsServer) action?.Invoke();
    }

    protected void LogNet(string msg)
    {
        Debug.Log($"[{name}] (S:{IsServer} C:{IsClient} O:{IsOwner}) {msg}");
    }
    #endregion
}
