// FinalSpawnPlacer.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class FinalSpawnPlacer : MonoBehaviour
{
    [Tooltip("Leave empty to apply to ANY loaded scene, or set exact scene name you load for gameplay.")]
    public string targetSceneName = "Game";

    void OnEnable()
    {
        //var nm = NetworkManager.Singleton;
        NetworkManager nm = GetComponent<NetworkManager>();
        if (!nm) return;
        nm.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;
        nm.SceneManager.OnSceneEvent -= OnSceneEvent;
    }

    void OnSceneEvent(SceneEvent e)
    {
        Debug.Log("Scene event");
        // Only the server assigns spawn positions
        if (!NetworkManager.Singleton.IsServer) return;

        // Run when the new scene has fully loaded for everyone
        if (e.SceneEventType != SceneEventType.LoadEventCompleted) return;

        // If you set a specific scene name, filter for it
        if (!string.IsNullOrEmpty(targetSceneName) && e.SceneName != targetSceneName) return;

        // Find the SpawnPointManager that lives in the loaded scene
        var spawner = FindObjectOfType<SpawnPointManager>();
        if (!spawner)
        {
            Debug.LogWarning("[FinalSpawnPlacer] No SpawnPointManager found in scene.");
            return;
        }

        // Place every connected client's player at the next spawn point
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerNO = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (!playerNO) continue;

            var (pos, rot) = spawner.GetNext();

            // If a CharacterController exists, toggle it while moving to avoid physics issues
            var cc = playerNO.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            playerNO.transform.SetPositionAndRotation(pos, rot);

            if (cc) cc.enabled = true;
        }
    }
}
