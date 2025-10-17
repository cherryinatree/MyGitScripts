// LobbyEventsBridge.cs
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;

public class LobbyEventsBridge : MonoBehaviour
{
    ILobbyEvents _sub;

    async void OnEnable()
    {
        var lobby = LobbyServiceFacade.I?.CurrentLobby;
        if (lobby == null) return;

        var cb = new LobbyEventCallbacks();
        cb.LobbyChanged += OnLobbyChanged;
        cb.PlayerJoined += _ => OnLobbyChanged(default);
        cb.PlayerLeft += _ => OnLobbyChanged(default);

        _sub = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, cb);
    }

    async void OnDisable()
    {
        if (_sub != null) { await _sub.UnsubscribeAsync(); _sub = null; }
    }

    async void OnLobbyChanged(ILobbyChanges changes)
    {
        // Refresh our cached Lobby model
        await LobbyServiceFacade.I.RefreshAsync();

        // Update UI
        var ui = FindAnyObjectByType<LobbyUI>();
        ui?.Refresh();

        // If host switched to in_game, auto-join as client
        var data = LobbyServiceFacade.I.CurrentLobby.Data;
        if (data != null &&
            data.TryGetValue(LobbyState.kState, out var st) && st.Value == "in_game" &&
            data.TryGetValue(LobbyState.kRelayJoin, out var rc) && !string.IsNullOrEmpty(rc.Value))
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && !nm.IsHost && (!nm.IsClient || !nm.IsConnectedClient))
            {
                var join = await RelayConnector.JoinAsClientAsync(rc.Value, LobbyServiceFacade.I.CurrentLobby.Id);
                NetworkBootstrap.I.ConfigureTransport(join.relayServerData);
                NetworkBootstrap.I.StartClient();
            }
        }
    }
}
