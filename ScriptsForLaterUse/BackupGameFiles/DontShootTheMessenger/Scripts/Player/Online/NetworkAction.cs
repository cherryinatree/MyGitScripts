// NetworkAction.cs
using UnityEngine;
using Unity.Netcode;

#region Payload
/// <summary>
/// A flexible payload you can reuse across many actions.
/// Use only the fields you need for a given action.
/// </summary>
public struct ActionPayload : INetworkSerializable
{
    public int code;                // e.g., action subtype, button state, etc.
    public Vector3 v1;              // e.g., move direction, world pos A
    public Vector3 v2;              // e.g., look delta, world pos B
    public float f1;                // e.g., intensity, duration
    public float f2;                // e.g., extra float
    public bool b1;                 // e.g., pressed / toggled
    public ulong targetNetworkId;   // e.g., interacted object

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref code);
        serializer.SerializeValue(ref v1);
        serializer.SerializeValue(ref v2);
        serializer.SerializeValue(ref f1);
        serializer.SerializeValue(ref f2);
        serializer.SerializeValue(ref b1);
        serializer.SerializeValue(ref targetNetworkId);
    }

    public static ActionPayload Empty => default;
}
#endregion

/// <summary>
/// Base class that handles the RPC pipeline + cooldown.
/// Now supports an ActionPayload so derived classes can send data.
/// </summary>
public abstract class NetworkAction : NetworkBehaviour
{
    [Header("Networking")]
    [Tooltip("If false, any client may request the action (useful for world actions).")]
    [SerializeField] private bool requireOwnershipToRequest = false;

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 0.1f;

    private float _lastLocalUseTime;
    private NetworkVariable<float> lastServerUseTime =
        new(writePerm: NetworkVariableWritePermission.Server);

    /// <summary>
    /// Call on the local client (usually the owner) to request the action with data.
    /// </summary>
    protected void RequestAction(in ActionPayload payload)
    {
        if (!IsClient) return;

        // local feel cooldown (purely client-side)
        if (Time.time - _lastLocalUseTime < cooldownSeconds) return;
        _lastLocalUseTime = Time.time;

        // optional ownership gate
        if (requireOwnershipToRequest && !IsOwner) return;

        RequestActionServerRpc(payload);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestActionServerRpc(ActionPayload payload, ServerRpcParams rpcParams = default)
    {
        // server anti-spam
        if (Time.time - lastServerUseTime.Value < cooldownSeconds) return;

        if (requireOwnershipToRequest)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (OwnerClientId != sender) return;
        }

        // authoritative application
        if (!ValidateOnServer(payload)) return;

        lastServerUseTime.Value = Time.time;
        PerformOnServer(payload);

        // fan out to all clients for visual feedback
        PerformOnClientsClientRpc(payload);
    }

    [ClientRpc]
    private void PerformOnClientsClientRpc(ActionPayload payload)
    {
        PerformOnClients(payload);
    }

    /// <summary>Optional extra guard on the server.</summary>
    protected virtual bool ValidateOnServer(in ActionPayload payload) => true;

    /// <summary>Server-authoritative logic (state changes happen here).</summary>
    protected abstract void PerformOnServer(in ActionPayload payload);

    /// <summary>Client feedback (VFX/SFX/animation), called on all clients.</summary>
    protected abstract void PerformOnClients(in ActionPayload payload);
}
