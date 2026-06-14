using UnityEngine;
using UnityEngine.AI;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Shared brain utilities for customers and workers.
    /// This intentionally stays generic so it can sit beside your existing systems.
    /// </summary>
    [RequireComponent(typeof(ArcadeStateMachine))]
    public class ArcadeAgentBrain : MonoBehaviour
    {
        [Header("Identity")]
        public string displayName = "Arcade Agent";

        [Header("Movement")]
        public NavMeshAgent navMeshAgent;
        public Transform visualRoot;
        public float destinationReachedDistance = 0.35f;

        [Header("Optional Animation")]
        public Animator animator;
        public string movingBoolParameter = "Moving";

        [Header("Debug")]
        public bool verboseLogs;

        public ArcadeStateMachine StateMachine { get; private set; }

        protected virtual void Awake()
        {
            StateMachine = GetComponent<ArcadeStateMachine>();

            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        protected virtual void Update()
        {
            UpdateAnimator();
        }

        public virtual void MoveTo(Vector3 worldPosition)
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(worldPosition);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, worldPosition, Time.deltaTime * 2.5f);
            }
        }

        public virtual void StopMoving()
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        public virtual bool HasReachedDestination()
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                if (navMeshAgent.pathPending)
                    return false;

                return navMeshAgent.remainingDistance <= Mathf.Max(destinationReachedDistance, navMeshAgent.stoppingDistance);
            }

            return true;
        }

        private void UpdateAnimator()
        {
            if (animator == null || string.IsNullOrWhiteSpace(movingBoolParameter))
                return;

            bool moving = false;

            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                moving = navMeshAgent.velocity.sqrMagnitude > 0.05f;

            animator.SetBool(movingBoolParameter, moving);
        }
    }
}
