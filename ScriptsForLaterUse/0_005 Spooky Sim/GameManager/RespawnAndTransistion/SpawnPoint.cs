using UnityEngine;

namespace Cherry.Spawning
{
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Unique id used by SpawnDirector to find this point (example: HQ_DeathRespawn).")]
        public string spawnId = "HQ_DeathRespawn";

        [Tooltip("If true, SpawnDirector will use this rotation too.")]
        public bool applyRotation = true;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.75f);
        }
#endif
    }
}
