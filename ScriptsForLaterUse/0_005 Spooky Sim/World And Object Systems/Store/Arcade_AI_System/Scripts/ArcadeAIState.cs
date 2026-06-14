using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Base class for every customer/worker AI state.
    /// Can be placed on the root object or on child GameObjects.
    /// </summary>
    public abstract class ArcadeAIState : MonoBehaviour
    {
        [Header("State Flow")]
        [Tooltip("Default state to enter after this state completes.")]
        public ArcadeAIState nextState;

        [Tooltip("Useful for debugging in the inspector.")]
        public string debugStateName;

        protected ArcadeAgentBrain brain;
        protected ArcadeParentState parentState;
        protected bool hasEntered;

        public virtual void Initialize(ArcadeAgentBrain ownerBrain, ArcadeParentState ownerParent)
        {
            brain = ownerBrain;
            parentState = ownerParent;

            if (string.IsNullOrWhiteSpace(debugStateName))
                debugStateName = GetType().Name;
        }

        public virtual void Enter()
        {
            hasEntered = true;
        }

        public virtual void Tick()
        {
        }

        public virtual void Exit()
        {
            hasEntered = false;
        }

        protected void CompleteState()
        {
            ChangeTo(nextState);
        }

        /// <summary>
        /// Changes state safely.
        /// If this state is inside a parent state and the target is another child of that same parent,
        /// it changes the child state.
        /// If the target lives outside that parent, it jumps back to the root state machine.
        /// This lets child states use top-level failure states like "Choose Activity" or "Leave Arcade".
        /// </summary>
        protected void ChangeTo(ArcadeAIState targetState)
        {
            if (parentState != null)
            {
                if (targetState == null)
                {
                    parentState.ChildFinishedWithoutNextState();
                    return;
                }

                bool targetBelongsToSameParent = targetState.transform.IsChildOf(parentState.transform);

                if (targetBelongsToSameParent)
                {
                    parentState.ChangeChildState(targetState);
                    return;
                }

                ChangeRootState(targetState);
                return;
            }

            ChangeRootState(targetState);
        }

        protected void ChangeRootState(ArcadeAIState targetState)
        {
            if (brain != null && brain.StateMachine != null)
                brain.StateMachine.ChangeState(targetState);
        }

        protected void LogState(string message)
        {
            if (brain != null && brain.verboseLogs)
                Debug.Log($"[{brain.name}] {debugStateName}: {message}", this);
        }
    }
}
