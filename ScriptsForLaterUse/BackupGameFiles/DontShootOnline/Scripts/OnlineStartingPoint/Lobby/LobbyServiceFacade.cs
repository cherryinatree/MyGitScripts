using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyServiceFacade : MonoBehaviour
{
    public static LobbyServiceFacade I { get; private set; }
    public Lobby CurrentLobby { get; private set; }
    public string LocalPlayerId => AuthenticationService.Instance.PlayerId;

    void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

    public async Task InitAsync(bool useSteamAuth = false)
    {
        await UnityServices.InitializeAsync();
        if (useSteamAuth)
        {
            // TODO: swap to Steam auth in production
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    Player NewLocalPlayer(string displayName = null) => new Player(
        id: LocalPlayerId,
        data: new Dictionary<string, PlayerDataObject>{
            { LobbyState.kDisplayName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, displayName ?? ("Player " + LocalPlayerId[..5])) },
            { LobbyState.kReady,       new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") }
        });

    public async Task<Lobby> CreateLobbyAsync(string name, int maxPlayers, string visibility)
    {
        var options = new CreateLobbyOptions
        {
            IsPrivate = visibility != "public",
            Player = NewLocalPlayer(),
            Data = LobbyState.NewLobbyData(visibility, LocalPlayerId)
        };
        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, options);
        return CurrentLobby;
    }

    public async Task<Lobby> JoinByCodeAsync(string code)
    {
        CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code.Trim(), new JoinLobbyByCodeOptions { Player = NewLocalPlayer() });
        return CurrentLobby;
    }

    public async Task<Lobby> QuickJoinPublicAsync()
    {
        var pubs = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>{
                new QueryFilter(field: QueryFilter.FieldOptions.S1, op: QueryFilter.OpOptions.EQ, value: "public")
            },
            Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) }
        });

        var candidate = pubs.Results.FirstOrDefault();
        if (candidate == null) throw new Exception("No public lobbies found");
        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(candidate.Id, new JoinLobbyByIdOptions { Player = NewLocalPlayer() });
        return CurrentLobby;
    }

    public string GetJoinCode() => CurrentLobby?.LobbyCode;

    public async Task SetReadyAsync(bool ready)
    {
        if (CurrentLobby == null) return;
        await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, LocalPlayerId, new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>{
                { LobbyState.kReady, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ready ? "1" : "0") }
            }
        });
        await RefreshAsync();
    }

    public bool IsHost() => CurrentLobby?.HostId == LocalPlayerId;

    public async Task KickAsync(string playerId)
    {
        if (!IsHost() || CurrentLobby == null) return;
        await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, playerId);
        await RefreshAsync();
    }

    public async Task<Lobby> RefreshAsync()
    {
        if (CurrentLobby == null) return null;
        CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
        return CurrentLobby;
    }
}
