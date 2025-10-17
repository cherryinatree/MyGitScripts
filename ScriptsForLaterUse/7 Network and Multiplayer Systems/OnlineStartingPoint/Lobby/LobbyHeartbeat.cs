using System.Collections;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.VisualScripting;

public class LobbyHeartbeat : MonoBehaviour
{


    
    IEnumerator Start()
    {
        // Update UI
        var ui = FindAnyObjectByType<LobbyUI>();
        while (true)
        {
            if (LobbyServiceFacade.I != null &&
                LobbyServiceFacade.I.IsHost() &&
                LobbyServiceFacade.I.CurrentLobby != null)
            {
                Debug.Log("Sending heartbeat ping for lobby " + LobbyServiceFacade.I.CurrentLobby.Id);
                yield return LobbyService.Instance.SendHeartbeatPingAsync(LobbyServiceFacade.I.CurrentLobby.Id);
            }
            ui?.Refresh();
            yield return new WaitForSeconds(1f);
        }
    }
}
