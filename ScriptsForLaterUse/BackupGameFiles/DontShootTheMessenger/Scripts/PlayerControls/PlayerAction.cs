// PlayerAction.cs
using UnityEngine;
using Unity.Netcode;

public abstract class PlayerAction : NetworkAction
{
    [Header("Sending")]
    [SerializeField] private bool continuousSend = true;        // Walk/Look = true, Jump/Interact = false
    [SerializeField, Range(5, 60)] private int sendRateHz = 20; // For continuous actions

    protected CorePlayer corePlayer;   // <-- hook into your CorePlayer inputs
    private float _nextSendTime;

    protected virtual void Awake()
    {
        // CorePlayer should be on the same root as NetworkObject
        corePlayer = GetComponent<CorePlayer>();
        if (!corePlayer) corePlayer = GetComponentInParent<CorePlayer>();
    }

    protected virtual void Update()
    {
        // Only the local owner should sample inputs & send requests
        if (!IsOwner || !IsClient || corePlayer == null) return;

        if (continuousSend)
        {
            if (Time.time >= _nextSendTime)
            {
                if (BuildPayloadFromCoreInput(out var payload))
                    RequestAction(payload);

                _nextSendTime = Time.time + (1f / Mathf.Max(1, sendRateHz));
            }
        }
        else
        {
            if (WantsToFireOnceFromCore(out var payload))
                RequestAction(payload);
        }
    }

    /// <summary>For continuous actions like Walk/Look. Read from 'core' here.</summary>
    protected virtual bool BuildPayloadFromCoreInput(out ActionPayload payload)
    {
        payload = ActionPayload.Empty; return false;
    }

    /// <summary>For discrete actions like Jump/Interact. Read from 'core' here.</summary>
    protected virtual bool WantsToFireOnceFromCore(out ActionPayload payload)
    {
        payload = ActionPayload.Empty; return false;
    }
}
