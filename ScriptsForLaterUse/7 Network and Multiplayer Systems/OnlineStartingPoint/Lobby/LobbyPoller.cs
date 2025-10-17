using System.Collections;
using UnityEngine;
using Unity.Netcode; // for NetworkManager checks

public class LobbyPoller : MonoBehaviour
{
    public System.Action OnLobbyChanged;

    [SerializeField] float intervalSeconds = 2f;
    Coroutine _loop;

    void OnEnable() { _loop = StartCoroutine(PollLoop()); }
    void OnDisable() { if (_loop != null) StopCoroutine(_loop); _loop = null; }

    IEnumerator PollLoop()
    {
        while (true)
        {
            var facade = LobbyServiceFacade.I;
            if (facade != null && facade.CurrentLobby != null)
            {
                var before = facade.CurrentLobby.LastUpdated;

                // Wait for the async refresh to complete
                var refreshTask = facade.RefreshAsync();
                while (!refreshTask.IsCompleted) yield return null;

                var after = facade.CurrentLobby?.LastUpdated ?? before;
                if (after != before)
                    OnLobbyChanged?.Invoke();

                // === Auto-join game when host starts ===
                var data = facade.CurrentLobby?.Data;
                if (data != null &&
                    data.TryGetValue(LobbyState.kState, out var st) && st.Value == "in_game" &&
                    data.TryGetValue(LobbyState.kRelayJoin, out var rc) && !string.IsNullOrEmpty(rc.Value))
                {
                    var nm = NetworkManager.Singleton;
                    // Only non-hosts that aren't already connected should join
                    if (nm != null && !nm.IsHost && (!nm.IsClient || !nm.IsConnectedClient))
                    {
                        // Join Relay, configure transport, then StartClient
                        var joinTask = RelayConnector.JoinAsClientAsync(rc.Value, facade.CurrentLobby.Id);
                        while (!joinTask.IsCompleted) yield return null;

                        NetworkBootstrap.I.ConfigureTransport(joinTask.Result.relayServerData);
                        NetworkBootstrap.I.StartClient();
                    }
                }
            }

            yield return new WaitForSeconds(intervalSeconds);
        }
    }
}
