using UnityEngine;

namespace Cherry.Spawning
{
    public class RespawnPoint : MonoBehaviour
    {
        [SerializeField] private string id = "Respawn";
        public string Id => id;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.75f);
        }
    }
}
