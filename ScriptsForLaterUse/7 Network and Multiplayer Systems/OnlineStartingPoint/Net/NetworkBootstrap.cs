using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap I { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "Game";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ConfigureTransport(RelayServerData relayData)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(relayData);
    }

    public bool StartHost()
    {
        return NetworkManager.Singleton.StartHost();
    }

    public bool StartClient()
    {
        return NetworkManager.Singleton.StartClient();
    }

    public void LoadLobbyScene() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);

    public void LoadGameSceneAsHost()
    {
        if (NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}
