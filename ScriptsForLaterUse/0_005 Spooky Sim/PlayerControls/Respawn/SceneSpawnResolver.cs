using System.Collections;
using UnityEngine;

namespace Cherry.Spawning
{
    public class SceneSpawnResolver : MonoBehaviour
    {
        [Header("Player Finding")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float waitForPlayerSeconds = 2f;

        [Header("Spawn IDs")]
        [SerializeField] private string defaultSpawnId = "Respawn";
        [SerializeField] private string deathSpawnId = "Respawn_Death";
        [SerializeField] private string loadSpawnId = "Respawn_Load";
        [SerializeField] private string restartDaySpawnId = "Respawn_Day";

        private IEnumerator Start()
        {
            // Decide which spawn ID to use
            string targetId = defaultSpawnId;

            if (RespawnRequest.TryConsume(out var reason, out var overrideId))
            {
                if (!string.IsNullOrWhiteSpace(overrideId)) targetId = overrideId;
                else
                {
                    targetId = reason switch
                    {
                        RespawnReason.Death => deathSpawnId,
                        RespawnReason.Load => loadSpawnId,
                        RespawnReason.RestartDay => restartDaySpawnId,
                        _ => defaultSpawnId
                    };
                }
            }

            // Find spawn point
            var spawnPoints = FindObjectsOfType<RespawnPoint>();
            RespawnPoint target = null;
            foreach (var sp in spawnPoints)
            {
                if (sp != null && sp.Id == targetId) { target = sp; break; }
            }

            if (target == null && spawnPoints.Length > 0)
            {
                Debug.LogWarning($"SceneSpawnResolver: No SpawnPoint '{targetId}'. Using first spawn point '{spawnPoints[0].Id}'.");
                target = spawnPoints[0];
            }

            if (target == null)
            {
                Debug.LogWarning("SceneSpawnResolver: No SpawnPoint found in scene.");
                yield break;
            }

            // Wait for player to exist (important if you instantiate player after scene load)
            GameObject playerObj = null;
            float t = 0f;
            while (playerObj == null && t < waitForPlayerSeconds)
            {
                playerObj = GameObject.FindGameObjectWithTag(playerTag);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (playerObj == null)
            {
                Debug.LogWarning($"SceneSpawnResolver: Could not find player with tag '{playerTag}' within {waitForPlayerSeconds}s.");
                yield break;
            }

            // Teleport safely for common movement setups:
            var cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = target.transform.position;
                rb.rotation = target.transform.rotation;
            }
            else
            {
                playerObj.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            }

            if (cc != null) cc.enabled = true;

            // Ensure ragdoll is off after spawn if present
            var ragdoll = playerObj.GetComponent<Cherry.Animation.RagdollRig>();
            if (ragdoll != null && ragdoll.IsRagdoll)
                ragdoll.TrySetRagdoll(false);
        }
    }
}
