using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Root state machine for customers and workers.
    /// Put this on the same root object as a CustomerBrain or WorkerBrain.
    /// </summary>
    public class ArcadeStateMachine : MonoBehaviour
    {
        [Header("State Machine")]
        public ArcadeAIState initialState;
        public bool startOnAwake = true;

        [SerializeField] private ArcadeAIState currentState;

        private ArcadeAgentBrain brain;
        private bool initialized;

        public ArcadeAIState CurrentState => currentState;

        private void Awake()
        {
            brain = GetComponent<ArcadeAgentBrain>();
            if (brain == null)
                brain = GetComponentInParent<ArcadeAgentBrain>();

            InitializeAllStates();
        }

        private void Start()
        {
            if (startOnAwake)
                StartMachine();
        }

        private void Update()
        {
            currentState?.Tick();
        }

        public void StartMachine()
        {
            if (!initialized)
                InitializeAllStates();

            ChangeState(initialState);
        }

        public void ChangeState(ArcadeAIState nextState)
        {
            if (currentState == nextState)
                return;

            currentState?.Exit();
            currentState = nextState;

            if (currentState == null)
                return;

            currentState.Initialize(brain, null);
            currentState.Enter();

            if (brain != null && brain.verboseLogs)
                Debug.Log($"[{brain.name}] State changed to {currentState.debugStateName}", currentState);
        }

        private void InitializeAllStates()
        {
            if (initialized)
                return;

            if (brain == null)
                brain = GetComponent<ArcadeAgentBrain>();

            ArcadeAIState[] states = GetComponentsInChildren<ArcadeAIState>(true);
            for (int i = 0; i < states.Length; i++)
                states[i].Initialize(brain, null);

            initialized = true;
        }
    }
}
