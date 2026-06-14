using System.Collections;
using UnityEngine;

namespace Cherry.Spawning
{
    [DisallowMultipleComponent]
    public class TransitionSceneController : MonoBehaviour
    {
        [Tooltip("How long the transition lasts before continuing (seconds).")]
        [SerializeField] private float transitionDuration = 2.0f;

        [Tooltip("Optional: start automatically on scene load.")]
        [SerializeField] private bool autoStart = true;

        private bool _running;

        private void Start()
        {
            if (autoStart) BeginTransition();
        }

        public void BeginTransition()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(Run());
        }

        // If you prefer animation events, you can skip this coroutine
        // and call SpawnDirector.Instance.TransitionComplete() directly from an event.
        private IEnumerator Run()
        {
            yield return new WaitForSeconds(transitionDuration);

            if (SpawnDirector.Instance != null)
                SpawnDirector.Instance.TransitionComplete();
            else
                Debug.LogWarning("TransitionSceneController: No SpawnDirector in scene/persistent objects.");
        }
    }
}
