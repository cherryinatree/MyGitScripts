using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cherry.Combat;
using Cherry.Spawning;
using Cherry.UI;
using Cherry.Animation;

namespace Cherry.Gameplay
{
    public class PlayerDeathController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PlayerHealth health;
        [SerializeField] private RagdollRig ragdoll;
        [SerializeField] private DeathScreenUI deathUI;

        [Header("Scene")]
        [SerializeField] private string homeSceneName = "Home";

        [Header("Ragdoll timing")]
        [SerializeField] private float ragdollSettleSeconds = 0.35f;
        [SerializeField] private float ragdollImpulseForce = 2.5f;

        [Header("Optional: disable while dead")]
        [SerializeField] private Behaviour[] disableWhileDead;

        private bool _running;

        private void Awake()
        {
            if (health == null) health = GetComponentInChildren<PlayerHealth>();
            if (ragdoll == null) ragdoll = GetComponent<RagdollRig>();
        }

        private void OnEnable()
        {
            if (health != null) health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            if (_running) return;
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            _running = true;

            // Disable control scripts
            if (disableWhileDead != null)
                for (int i = 0; i < disableWhileDead.Length; i++)
                    if (disableWhileDead[i] != null) disableWhileDead[i].enabled = false;

            // inside your DeathRoutine()

            // Ragdoll ON (optional)
            if (ragdoll != null && ragdoll.TrySetRagdoll(true))
            {
                var dir = (transform.forward + Vector3.up * 0.25f).normalized;
                ragdoll.AddImpulse(dir, ragdollImpulseForce);
                yield return new WaitForSecondsRealtime(ragdollSettleSeconds);
            }
            else
            {
                // No ragdoll available. Optional: play a simple death animation here if you want.
                // yield return new WaitForSecondsRealtime(0.15f);
            }


            // Let the flop read on screen before the curtain drops
            if (ragdollSettleSeconds > 0f)
                yield return new WaitForSecondsRealtime(ragdollSettleSeconds);

            // Fade + "YOU DIED"
            if (deathUI != null)
                yield return deathUI.PlayDeathSequence();

            // Request: spawn because we died
            RespawnRequest.Set(RespawnReason.Death);

            // Load Home
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone) yield return null;

            // If deathUI persists across scenes, fade in.
            if (deathUI != null)
                yield return deathUI.FadeInFromBlack();

            _running = false;
        }
    }
}
