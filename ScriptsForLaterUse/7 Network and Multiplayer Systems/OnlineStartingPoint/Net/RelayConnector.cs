using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public static class RelayConnector
{
    public struct RelayJoinData
    {
        public string lobbyId;
        public string relayJoinCode;
        public RelayServerData relayServerData;
    }

    public static async Task<RelayJoinData> AllocateForHostAsync(string lobbyId, int maxPlayers)
    {
        var alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        // Use Multiplayer Services helper to build RelayServerData
        var relayData = AllocationUtils.ToRelayServerData(alloc, "dtls"); // or "udp" / "wss" per platform
        return new RelayJoinData { lobbyId = lobbyId, relayJoinCode = code, relayServerData = relayData };
    }

    public static async Task<RelayJoinData> JoinAsClientAsync(string relayJoinCode, string lobbyId)
    {
        var join = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        var relayData = AllocationUtils.ToRelayServerData(join, "dtls");
        return new RelayJoinData { lobbyId = lobbyId, relayJoinCode = relayJoinCode, relayServerData = relayData };
    }
}
