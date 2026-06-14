using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public static class LobbyState
{
    // Lobby Data keys
    public const string kVisibility = "visibility";   // "public" | "private" | "friends"
    public const string kState = "state";        // "lobby" | "in_game"
    public const string kRelayJoin = "relayCode";    // relay join code once host starts
    public const string kOwnerId = "owner";        // host playerId

    // Player Data keys
    public const string kReady = "ready";        // "0" | "1"
    public const string kDisplayName = "name";

    public static Dictionary<string, DataObject> NewLobbyData(string visibility, string ownerId)
    {
        return new Dictionary<string, DataObject> {
            { kVisibility, new DataObject(DataObject.VisibilityOptions.Public, visibility) },
            { kState,      new DataObject(DataObject.VisibilityOptions.Public, "lobby") },
            { kOwnerId,    new DataObject(DataObject.VisibilityOptions.Member, ownerId) }
        };
    }
}
