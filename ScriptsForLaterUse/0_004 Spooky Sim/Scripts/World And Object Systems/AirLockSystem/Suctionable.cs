using UnityEngine;
using UnityEngine.AI;

namespace Cherry.Airlocks
{
    [AddComponentMenu("Cherry/Airlocks/Suctionable")]
    public class Suctionable : MonoBehaviour
    {
        [Header("Auto-Detection (optional)")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private CharacterController characterController;
        //[SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Tuning")]
        [Tooltip("Cap for CharacterController/NavMeshAgent displacement feel.")]
        [SerializeField] private float maxMoveSpeed = 25f;
        [SerializeField] private float MultiplyForce = 1f;


        [Tooltip("If true, NavMeshAgent is disabled while being sucked (prevents it fighting the force).")]
        [SerializeField] private bool disableNavAgentWhileSucked = true;

        private int _suctionRefs;

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();
            //navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Awake()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
            if (!characterController) characterController = GetComponent<CharacterController>();
            //if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public void NotifySuctionStart()
        {
            _suctionRefs++;
           /* if (_suctionRefs == 1 && disableNavAgentWhileSucked && navMeshAgent && navMeshAgent.enabled)
            {
                navMeshAgent.enabled = false;
            }*/
        }

        public void NotifySuctionEnd()
        {
            _suctionRefs = Mathf.Max(0, _suctionRefs - 1);
           /* if (_suctionRefs == 0 && disableNavAgentWhileSucked && navMeshAgent && !navMeshAgent.enabled)
            {
                navMeshAgent.enabled = true;
            }*/
        }

        /// <summary>
        /// strength is treated like "acceleration-ish" for RB, and "speed-ish" for CC/Agent.
        /// Tune suction strength in the controller.
        /// </summary>
        public void ApplySuction(Vector3 direction, float strength, float dt)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            direction.Normalize();

            // Rigidbody: use physics
            if (rb)
            {
                rb.AddForce(direction * strength* MultiplyForce, ForceMode.Acceleration);
                return;
            }

            // CharacterController: move directly
            if (characterController && characterController.enabled)
            {
                float speed = Mathf.Min(strength, maxMoveSpeed);
                characterController.Move(direction * (speed * dt));
                return;
            }

            // NavMeshAgent fallback (if enabled)
           /* if (navMeshAgent && navMeshAgent.enabled)
            {
                float speed = Mathf.Min(strength, maxMoveSpeed);
                navMeshAgent.Move(direction * (speed * dt));
            }*/
        }
    }
}
