// PingAction.cs
using UnityEngine;

public class PingAction : PlayerAction
{
    private int _pingCount;


    protected override void PerformOnServer(in ActionPayload payload)
    {
        _pingCount++; // server authority
        Debug.Log($"[PingAction][SERVER] ping #{_pingCount}");
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        Debug.Log($"[PingAction][CLIENT] ping echoed, serverCount now {_pingCount}");
    }
}
