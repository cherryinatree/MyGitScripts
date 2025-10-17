using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] TMP_Text codeText, statusText;
    [SerializeField] Transform playerListRoot;
    [SerializeField] GameObject playerRowPrefab; // children: "Name" (TMP_Text), "Ready" (TMP_Text), "KickButton" (Button)
    [SerializeField] Button readyBtn, startBtn, copyBtn, inviteBtn;

    Lobby current => LobbyServiceFacade.I.CurrentLobby;
    async void Start()
    {
        // ...
        if (readyBtn) readyBtn.onClick.AddListener(async () =>
        {
            if (LobbyServiceFacade.I == null) return;
            await LobbyServiceFacade.I.SetReadyAsync(!IsLocalReady());
            Refresh(); // <— immediate UI redraw so it "does something" right away
        });

        if (startBtn) startBtn.onClick.AddListener(OnStartIfAllReady);

        if (copyBtn) copyBtn.onClick.AddListener(() =>
        {
            var code = LobbyServiceFacade.I?.GetJoinCode();
            if (!string.IsNullOrEmpty(code)) GUIUtility.systemCopyBuffer = code;
        });

        if (inviteBtn) inviteBtn.onClick.AddListener(() =>
        {
#if STEAMWORKS
        SteamInviteBridge.OpenInviteOverlayWithConnect(LobbyServiceFacade.I?.GetJoinCode());
#endif
        });

        var poller = GetComponent<LobbyPoller>();
        if (poller != null) poller.OnLobbyChanged += Refresh;

#if STEAMWORKS
    SteamInviteBridge.SetRichPresenceConnect(LobbyServiceFacade.I?.GetJoinCode());
#endif

        Refresh();
    }

    public void Refresh()
    {
        Debug.Log($"Ready states: {string.Join(", ", current.Players.Select(p => p.Data.TryGetValue(LobbyState.kReady, out var r) ? r.Value : "NA"))}");

        foreach (Transform c in playerListRoot) Destroy(c.gameObject);
        if (current == null) return;

        var hostId = current.HostId;
        foreach (var p in current.Players)
        {
            var row = Instantiate(playerRowPrefab, playerListRoot);
            var name = p.Data.TryGetValue(LobbyState.kDisplayName, out var n) ? n.Value : p.Id[..6];
            var ready = p.Data.TryGetValue(LobbyState.kReady, out var r) && r.Value == "1";
            row.transform.Find("Name").Find("Name").GetComponent<TMP_Text>().text = name + (p.Id == hostId ? " (Host)" : "");
            row.transform.Find("Ready").Find("Ready").GetComponent<TMP_Text>().text = ready ? "Ready" : "Not Ready";

            var kickBtn = row.transform.Find("KickButton")?.GetComponent<Button>();
            if (kickBtn != null)
            {
                kickBtn.gameObject.SetActive(LobbyServiceFacade.I.IsHost() && p.Id != hostId);
                kickBtn.onClick.AddListener(async () => { await LobbyServiceFacade.I.KickAsync(p.Id); });
            }
        }

        var everyoneReady = current.Players.All(pl => pl.Data.TryGetValue(LobbyState.kReady, out var r) && r.Value == "1");
        startBtn.gameObject.SetActive(LobbyServiceFacade.I.IsHost());
        startBtn.interactable = everyoneReady;
        statusText.text = everyoneReady ? "All players ready" : "Waiting for players…";
    }

    async void OnStartIfAllReady()
    {
        var everyoneReady = current.Players.All(pl => pl.Data.TryGetValue(LobbyState.kReady, out var r) && r.Value == "1");
        if (!everyoneReady || !LobbyServiceFacade.I.IsHost()) return;

        // Allocate Relay for the game and publish join code into lobby
        var joinData = await RelayConnector.AllocateForHostAsync(current.Id, current.MaxPlayers);
        await LobbyService.Instance.UpdateLobbyAsync(current.Id,
            new UpdateLobbyOptions
            {
                Data = new System.Collections.Generic.Dictionary<string, DataObject>{
                    { LobbyState.kState,     new DataObject(DataObject.VisibilityOptions.Public, "in_game") },
                    { LobbyState.kRelayJoin, new DataObject(DataObject.VisibilityOptions.Member, joinData.relayJoinCode) }
                }
            });

        // Start NGO host and load Game scene
        NetworkBootstrap.I.ConfigureTransport(joinData.relayServerData);
        if (NetworkBootstrap.I.StartHost())
            NetworkBootstrap.I.LoadGameSceneAsHost();
        else
            Debug.LogError("Failed to start host.");
    }

    bool IsLocalReady()
    {
        var me = current?.Players.FirstOrDefault(p => p.Id == LobbyServiceFacade.I.LocalPlayerId);
        return me != null && me.Data.TryGetValue(LobbyState.kReady, out var r) && r.Value == "1";
    }
}
