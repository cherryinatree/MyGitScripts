using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// A parent state that runs its own child-state machine.
    /// Example:
    /// Checkout Parent State
    ///     - Join Line
    ///     - Wait In Line
    ///     - Use Station
    /// </summary>
    public class ArcadeParentState : ArcadeAIState
    {
        [Header("Child State Machine")]
        public ArcadeAIState firstChildState;
        public bool restartChildWhenEntered = true;

        [SerializeField] private ArcadeAIState currentChildState;

        public ArcadeAIState CurrentChildState => currentChildState;

        public override void Enter()
        {
            base.Enter();

            if (restartChildWhenEntered || currentChildState == null)
                ChangeChildState(firstChildState);
            else
                currentChildState.Enter();
        }

        public override void Tick()
        {
            currentChildState?.Tick();
        }

        public override void Exit()
        {
            currentChildState?.Exit();
            base.Exit();
        }

        public void ChangeChildState(ArcadeAIState nextChildState)
        {
            if (currentChildState == nextChildState)
                return;

            currentChildState?.Exit();
            currentChildState = nextChildState;

            if (currentChildState == null)
            {
                ChildFinishedWithoutNextState();
                return;
            }

            currentChildState.Initialize(brain, this);
            currentChildState.Enter();
        }

        public void ChildFinishedWithoutNextState()
        {
            currentChildState?.Exit();
            currentChildState = null;
            CompleteState();
        }
    }
}
