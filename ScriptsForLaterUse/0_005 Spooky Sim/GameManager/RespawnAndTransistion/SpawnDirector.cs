using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cherry.Spawning
{
    public enum SpawnReason
    {
        None,
        Death,
        LoadGame,
        RestartDay
    }

    [DisallowMultipleComponent]
    public class SpawnDirector : MonoBehaviour
    {
        public static SpawnDirector Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string transitionSceneName = "DeathTransition";
        [SerializeField] private string headquartersSceneName = "Headquarters";

        [Header("Spawn Ids (must match SpawnPoint.spawnId)")]
        [SerializeField] private string deathRespawnId = "HQ_DeathRespawn";
        [SerializeField] private string loadGameSpawnId = "HQ_LoadSpawn";
        [SerializeField] private string restartDaySpawnId = "HQ_DayStart";

        [Header("Player Finding")]
        [Tooltip("Tag used to find the player after a scene loads.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("If true, will try a few frames to find the player (useful if player spawns a moment after scene load).")]
        [SerializeField] private bool waitForPlayer = true;

        [Tooltip("How many frames to wait while searching for the player.")]
        [SerializeField] private int maxPlayerFindFrames = 60;

        // Current queued request
        private bool _hasRequest;
        private string _targetScene;
        private string _targetSpawnId;
        private SpawnReason _reason;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // -------------------------
        // Public API (call these)
        // -------------------------

        /// <summary>Call when the player dies.</summary>
        public void BeginDeathFlow()
        {
            QueueSpawn(headquartersSceneName, deathRespawnId, SpawnReason.Death);
            SceneManager.LoadScene(transitionSceneName);
        }

        /// <summary>Call when loading a save (if you always want a fixed spawn instead of saved position).</summary>
        public void LoadFromSaveToHQ()
        {
            QueueSpawn(headquartersSceneName, loadGameSpawnId, SpawnReason.LoadGame);
            SceneManager.LoadScene(headquartersSceneName);
        }

        /// <summary>Call when restarting the day.</summary>
        public void RestartDayToHQ()
        {
            QueueSpawn(headquartersSceneName, restartDaySpawnId, SpawnReason.RestartDay);
            SceneManager.LoadScene(headquartersSceneName);
        }

        /// <summary>Called by the Transition scene when it finishes.</summary>
        public void TransitionComplete()
        {
            if (!_hasRequest)
            {
                Debug.LogWarning("SpawnDirector: TransitionComplete called, but no spawn request is queued.");
                return;
            }

            SceneManager.LoadScene(_targetScene);
        }

        // -------------------------
        // Internals
        // -------------------------

        private void QueueSpawn(string targetScene, string spawnId, SpawnReason reason)
        {
            _hasRequest = true;
            _targetScene = targetScene;
            _targetSpawnId = spawnId;
            _reason = reason;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_hasRequest) return;

            // Only apply spawn when we reach the destination scene
            if (scene.name != _targetScene) return;

            StartCoroutine(ApplySpawnWhenReady());
        }

        private IEnumerator ApplySpawnWhenReady()
        {
            // Find spawn point first (it should exist in the destination scene)
            SpawnPoint spawnPoint = FindSpawnPointById(_targetSpawnId);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"SpawnDirector: No SpawnPoint found with id '{_targetSpawnId}' in scene '{_targetScene}'.");
            }

            // Find player
            GameObject player = null;

            if (waitForPlayer)
            {
                for (int i = 0; i < maxPlayerFindFrames; i++)
                {
                    player = GameObject.FindGameObjectWithTag(playerTag);
                    if (player != null) break;
                    yield return null;
                }
            }
            else
            {
                player = GameObject.FindGameObjectWithTag(playerTag);
            }

            if (player == null)
            {
                Debug.LogWarning($"SpawnDirector: Could not find player with tag '{playerTag}' to apply spawn.");
                yield break;
            }

            // Apply spawn
            if (spawnPoint != null)
            {
                TeleportPlayer(player.transform, spawnPoint);
            }
            else
            {
                // Fallback: do nothing, or you could drop them at (0,0,0)
                // player.transform.position = Vector3.zero;
            }

            // Clear request after successful destination load attempt
            _hasRequest = false;
            _targetScene = null;
            _targetSpawnId = null;
            _reason = SpawnReason.None;
        }

        private static void TeleportPlayer(Transform player, SpawnPoint spawnPoint)
        {
            // If there's a CharacterController, disable briefly to avoid “stuck in collider” jitters
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // If there's a Rigidbody, zero velocity first
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            player.position = spawnPoint.Position;
            if (spawnPoint.applyRotation)
                player.rotation = spawnPoint.Rotation;

            if (cc != null) cc.enabled = true;
        }

        private static SpawnPoint FindSpawnPointById(string spawnId)
        {
            var points = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null && points[i].spawnId == spawnId)
                    return points[i];
            }
            return null;
        }
    }
}
