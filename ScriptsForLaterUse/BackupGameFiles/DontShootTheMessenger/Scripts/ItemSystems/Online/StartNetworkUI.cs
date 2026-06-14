// StartNetworkUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

using TMPro;

public class StartNetworkUI : MonoBehaviour
{
    [Header("Inputs (TMP)")]
    public TMP_InputField addressInput;
    public TMP_InputField portInput;

    [Header("Buttons")]
    public Button hostButton;
    public Button clientButton;
    public Button serverButton; // optional

    [Header("UI")]
    public GameObject panelToHideOnConnect; // assign your menu root panel

    [Header("Scene")]
    [Tooltip("Scene to load after Host/Server starts. Leave empty to stay on current scene.")]
    public string gameSceneName = "Game";

    NetworkManager net;
    UnityTransport transport;

    void Start()
    {
        net = NetworkManager.Singleton;
        if (!net)
        {
            Debug.LogError("[StartNetworkUI] No NetworkManager.Singleton in scene.");
            enabled = false;
            return;
        }

        transport = net.NetworkConfig.NetworkTransport as UnityTransport;
        if (!transport)
        {
            // Try to recover by finding one on the same GO
            transport = net.GetComponent<UnityTransport>();
            if (!transport)
            {
                Debug.LogError("[StartNetworkUI] UnityTransport component not found. Add UnityTransport to the same GameObject as NetworkManager.");
                enabled = false;
                return;
            }
            // Make sure NetworkConfig uses it
            net.NetworkConfig.NetworkTransport = transport;
        }

        if (hostButton) hostButton.onClick.AddListener(OnClickHost);
        if (clientButton) clientButton.onClick.AddListener(OnClickClient);
        if (serverButton) serverButton.onClick.AddListener(OnClickServer);

        net.OnServerStarted += OnServerStarted;
        net.OnClientConnectedCallback += OnClientConnected;
        net.OnClientDisconnectCallback += OnClientDisconnected;

        // Defaults
        if (addressInput && string.IsNullOrWhiteSpace(GetAddress())) SetAddressField("127.0.0.1");
        if (portInput && string.IsNullOrWhiteSpace(GetPortText())) SetPortField("7777");
    }

    void OnDestroy()
    {
        if (!net) return;
        net.OnServerStarted -= OnServerStarted;
        net.OnClientConnectedCallback -= OnClientConnected;
        net.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public void OnClickHost()
    {
        if (!net) { Debug.LogError("[StartNetworkUI] No NetworkManager."); return; }

        // If we’re already running, handle it gracefully
        if (net.IsListening)
        {
            if (net.IsHost)
            {
                // We are already Host: just hide menu and (optionally) ensure we’re in the game scene
                if (panelToHideOnConnect) panelToHideOnConnect.SetActive(false);

                if (!string.IsNullOrEmpty(gameSceneName) &&
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != gameSceneName)
                {
                    net.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                }

                Debug.Log("[StartNetworkUI] Already hosting; skipping StartHost.");
                return;
            }
            else
            {
                // Running as Client or dedicated Server — restart as Host
                Debug.LogWarning("[StartNetworkUI] Already listening (client/server). Restarting as Host...");
                StartCoroutine(RestartAsHostCoroutine());
                return;
            }
        }

        // Normal preflight & start
        if (!Preflight(out var errors))
        {
            LogErrors("[StartNetworkUI] Host preflight failed:", errors);
            return;
        }

        ushort port = ParsePort();
        (net.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport)
            .SetConnectionData("0.0.0.0", port);

        //net.LogLevel = NetworkLogLevel.Developer;

        try
        {
            if (net.StartHost())
            {
                if (!string.IsNullOrEmpty(gameSceneName))
                    net.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                DumpWhyListeningFailed("Host", port);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[StartNetworkUI] Exception during StartHost: " + ex);
        }
    }

    System.Collections.IEnumerator RestartAsHostCoroutine()
    {
        // Remember desired port
        ushort port = ParsePort();

        // Stop current mode
        net.Shutdown();
        // Wait one frame to let transport release the socket
        yield return null;

        // Re-bind and start host
        var utp = net.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        utp.SetConnectionData("0.0.0.0", port);

        if (net.StartHost())
        {
            if (panelToHideOnConnect) panelToHideOnConnect.SetActive(false);
            if (!string.IsNullOrEmpty(gameSceneName))
                net.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            Debug.Log("[StartNetworkUI] Restarted as Host.");
        }
        else
        {
            DumpWhyListeningFailed("Host", port);
        }
    }

    public void OnClickClient()
    {
        if (!net) { Debug.LogError("[StartNetworkUI] No NetworkManager."); return; }

        // If networking is already running on this instance, restart cleanly as Client
        if (net.IsListening && !net.IsClient)
        {
            Debug.LogWarning("[StartNetworkUI] Already listening (host/server). Restarting as Client...");
            StartCoroutine(RestartAsClientCoroutine());
            return;
        }
        if (net.IsListening && net.IsClient)
        {
            // Already a client; just hide the menu
            if (panelToHideOnConnect) panelToHideOnConnect.SetActive(false);
            Debug.Log("[StartNetworkUI] Already a client; skipping StartClient.");
            return;
        }

        if (!PreflightClientOnly(out var errors))
        {
            LogErrors("[StartNetworkUI] Client preflight failed:", errors);
            return;
        }

        string addr = GetAddress();
        ushort port = ParsePort();

        var utp = net.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        if (!utp)
        {
            Debug.LogError("[StartNetworkUI] UnityTransport not set.");
            return;
        }

        utp.SetConnectionData(addr, port);

       // net.LogLevel = NetworkLogLevel.Developer;

        try
        {
            if (!net.StartClient())
            {
                Debug.LogError($"[StartNetworkUI] StartClient() returned false. Address={addr} Port={port}. " +
                               "Is the host running? Is the address/port correct? Firewall open on host?");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[StartNetworkUI] Exception during StartClient: " + ex);
        }
    }

    System.Collections.IEnumerator RestartAsClientCoroutine()
    {
        string addr = GetAddress();
        ushort port = ParsePort();

        net.Shutdown();
        yield return null; // let transport release the socket

        var utp = net.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        utp.SetConnectionData(addr, port);

        if (!net.StartClient())
        {
            Debug.LogError($"[StartNetworkUI] Restart StartClient() failed. Address={addr} Port={port}.");
        }
        else if (panelToHideOnConnect)
        {
            panelToHideOnConnect.SetActive(false);
        }
    }


    void OnClickServer()
    {
        if (!Preflight(out var errors))
        {
            LogErrors("[StartNetworkUI] Server preflight failed:", errors);
            return;
        }

        ushort port = ParsePort();
        transport.SetConnectionData("0.0.0.0", port);

       // net.LogLevel = NetworkLogLevel.Developer;

        try
        {
            bool started = net.StartServer();
            if (!started)
            {
                DumpWhyListeningFailed("Server", port);
                return;
            }

            if (!string.IsNullOrEmpty(gameSceneName))
                net.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[StartNetworkUI] Exception during StartServer: " + ex);
        }
    }

    // ---------- EVENTS ----------
    void OnServerStarted()
    {
        if (panelToHideOnConnect) panelToHideOnConnect.SetActive(false);
        Debug.Log("[StartNetworkUI] Server started.");
    }

    void OnClientConnected(ulong clientId)
    {
        if (clientId == net.LocalClientId && panelToHideOnConnect)
            panelToHideOnConnect.SetActive(false);

        Debug.Log($"[StartNetworkUI] Client connected: {clientId} (local={clientId == net.LocalClientId})");
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == net.LocalClientId && panelToHideOnConnect)
            panelToHideOnConnect.SetActive(true);

        Debug.LogWarning($"[StartNetworkUI] Client disconnected: {clientId} (local={clientId == net.LocalClientId})");
    }

    // ---------- PREFLIGHT VALIDATION ----------
    bool Preflight(out List<string> errors)
    {
        errors = new List<string>();
        if (!net) errors.Add("NetworkManager not found.");

        // Transport
        if (!transport) errors.Add("UnityTransport not set on NetworkManager.");

        // Already listening?
        if (net && net.IsListening) errors.Add("NetworkManager is already listening.");

        // Player Prefab
        if (net && !net.NetworkConfig.PlayerPrefab)
            errors.Add("Player Prefab is NOT assigned in NetworkManager → Player Prefab.");
        else if (net && net.NetworkConfig.PlayerPrefab && !net.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>())
            errors.Add("Player Prefab is missing a NetworkObject component.");

        // Port
        ushort port = ParsePort();
        if (port == 0) errors.Add("Port parsed as 0 (invalid). Enter 1–65535.");

        // Duplicate managers (warning only)
        var managers = FindObjectsOfType<NetworkManager>();
        if (managers.Length > 1)
            Debug.LogWarning($"[StartNetworkUI] Found {managers.Length} NetworkManagers. Duplicates can cause failures.");

        return errors.Count == 0;
    }

    bool PreflightClientOnly(out List<string> errors)
    {
        errors = new List<string>();
        if (!net) errors.Add("NetworkManager not found.");
        if (!transport) errors.Add("UnityTransport not set on NetworkManager.");

        string addr = GetAddress();
        if (string.IsNullOrWhiteSpace(addr)) errors.Add("Address is empty.");

        ushort port = ParsePort();
        if (port == 0) errors.Add("Port parsed as 0 (invalid). Enter 1–65535.");

        return errors.Count == 0;
    }

    void DumpWhyListeningFailed(string mode, ushort port)
    {
        // Common causes
        Debug.LogError(
            $"[StartNetworkUI] Start{mode}() returned false. " +
            $"Checks: " +
            $"PlayerPrefab={(net.NetworkConfig.PlayerPrefab ? net.NetworkConfig.PlayerPrefab.name : "null")}, " +
            $"HasNetworkObject={(net.NetworkConfig.PlayerPrefab && net.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>() ? "yes" : "no")}, " +
            $"Transport={(transport ? "UnityTransport" : "null")}, " +
            $"Port={port}. " +
            $"If you see socket bind errors above, try a different port (e.g., 7778) or allow the app in your firewall."
        );
    }

    // ---------- HELPERS ----------
    ushort ParsePort()
    {
        if (!portInput) return 7777;
        var txt = GetPortText().Trim();
        if (ushort.TryParse(txt, out var p)) return p;
        return 0;
    }

    string GetAddress() => addressInput ? addressInput.text : "127.0.0.1";
    string GetPortText() => portInput ? portInput.text : "7777";

    void SetAddressField(string v)
    {
#if TMP_PRESENT
        if (addressInput) addressInput.text = v;
#else
        if (addressInput) addressInput.text = v;
#endif
    }

    void SetPortField(string v)
    {
#if TMP_PRESENT
        if (portInput) portInput.text = v;
#else
        if (portInput) portInput.text = v;
#endif
    }

    static void LogErrors(string header, List<string> errors)
    {
        Debug.LogError(header);
        foreach (var e in errors) Debug.LogError(" - " + e);
    }
}
