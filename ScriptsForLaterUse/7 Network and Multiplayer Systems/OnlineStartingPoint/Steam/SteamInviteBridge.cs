using UnityEngine;
#if STEAMWORKS
using Steamworks;
#endif

public static class SteamInviteBridge
{
#if STEAMWORKS
    static Callback<GameRichPresenceJoinRequested_t> _joinReq;
#endif

    public static void SetRichPresenceConnect(string lobbyCode)
    {
#if STEAMWORKS
        if (!SteamBootstrap.IsReady || string.IsNullOrEmpty(lobbyCode)) return;
        SteamFriends.SetRichPresence("status", "In Lobby");
        SteamFriends.SetRichPresence("connect", $"ugc://join/{lobbyCode}"); // your custom scheme
#endif
    }

    public static void OpenInviteOverlayWithConnect(string lobbyCode)
    {
#if STEAMWORKS
        if (!SteamBootstrap.IsReady) return;
        SetRichPresenceConnect(lobbyCode);
        SteamFriends.ActivateGameOverlay("Friends");
#endif
    }

    public static void RegisterJoinHandler(System.Action<string> onLobbyCode)
    {
#if STEAMWORKS
        if (!SteamBootstrap.IsReady) return;
        _joinReq = Callback<GameRichPresenceJoinRequested_t>.Create(data =>
        {
            var connect = SteamFriends.GetFriendRichPresence(data.m_steamIDFriend, "connect");
            if (!string.IsNullOrEmpty(connect) && connect.StartsWith("ugc://join/"))
            {
                var code = connect.Substring("ugc://join/".Length);
                onLobbyCode?.Invoke(code);
            }
        });
#endif
    }
}
