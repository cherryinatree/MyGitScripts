//using GamingTools;
using System.Collections.Generic;
using UnityEngine;

public class CombatStateMachine : MonoBehaviour
{

    /// the collection of states
    public List<CombatState> States;
    /// this brain's current state
    public virtual CombatState CurrentState { get; protected set; }

    protected CombatDecision[] _decisions;
    protected CombatAction[] _actions;
    protected CombatState _initialState;

    public bool ResetBrainOnStart = true;
    public bool ResetBrainOnEnable = false;


    protected void Start()
    {
        if (ResetBrainOnStart)
        {
            ResetBrain();
        }
    }

    private void GetComponets()
    {
    }

    public virtual CombatAction[] GetAttachedActions()
    {
        CombatAction[] actions = this.gameObject.GetComponentsInChildren<CombatAction>();
        return actions;
    }

    public virtual CombatDecision[] GetAttachedDecisions()
    {
        CombatDecision[] decisions = this.gameObject.GetComponentsInChildren<CombatDecision>();
        return decisions;
    }

    protected virtual void OnEnable()
    {
        if (ResetBrainOnEnable)
        {
            ResetBrain();
        }
    }

    /// <summary>
    /// On awake we set our brain for all states
    /// </summary>
    protected virtual void Awake()
    {
        foreach (CombatState state in States)
        {
            state.SetBrain(this);
        }
        _decisions = GetAttachedDecisions();
        _actions = GetAttachedActions();
    }


    /// <summary>
    /// Every frame we update our current state
    /// </summary>
    protected virtual void Update()
    {
        if ((CurrentState == null) || (Time.timeScale == 0f))
        {
            return;
        }
        GetComponets();

       // Debug.Log(CurrentState.StateName);

        CurrentState.PerformActions();


        CurrentState.EvaluateTransitions();


        StoreLastKnownPosition();
    }

    /// <summary>
    /// Transitions to the specified state, trigger exit and enter states events
    /// </summary>
    /// <param name="newStateName"></param>
    public virtual void TransitionToState(string newStateName)
    {
        if (CurrentState == null)
        {
            CurrentState = FindState(newStateName);
            if (CurrentState != null)
            {
                CurrentState.EnterState();
            }
            return;
        }
        if (newStateName != CurrentState.StateName)
        {
            CurrentState.ExitState();
            OnExitState();

            CurrentState = FindState(newStateName);
            if (CurrentState != null)
            {
                CurrentState.EnterState();
            }
        }
    }

    /// <summary>
    /// When exiting a state we reset our time counter
    /// </summary>
    protected virtual void OnExitState()
    {

    }

    /// <summary>
    /// Initializes all decisions
    /// </summary>
    protected virtual void InitializeDecisions()
    {
        if (_decisions == null)
        {
            _decisions = GetAttachedDecisions();
        }
        foreach (CombatDecision decision in _decisions)
        {
            decision.Initialization();
        }
    }

    /// <summary>
    /// Initializes all actions
    /// </summary>
    protected virtual void InitializeActions()
    {
        if (_actions == null)
        {
            _actions = GetAttachedActions();
        }
        foreach (CombatAction action in _actions)
        {
            action.Initialization();
        }
    }

    /// <summary>
    /// Returns a state based on the specified state name
    /// </summary>
    /// <param name="stateName"></param>
    /// <returns></returns>
    protected CombatState FindState(string stateName)
    {
        foreach (CombatState state in States)
        {
            if (state.StateName == stateName)
            {
                return state;
            }
        }
        if (stateName != "")
        {
            Debug.LogError("You're trying to transition to state '" + stateName + "' in " + this.gameObject.name + "'s AI Brain, but no state of this name exists. Make sure your states are named properly, and that your transitions states match existing states.");
        }
        return null;
    }

    /// <summary>
    /// Stores the last known position of the target
    /// </summary>
    protected virtual void StoreLastKnownPosition()
    {

    }

    /// <summary>
    /// Resets the brain, forcing it to enter its first state
    /// </summary>
    public virtual void ResetBrain()
    {
        InitializeDecisions();
        InitializeActions();

        this.enabled = true;

        if (CurrentState != null)
        {
            CurrentState.ExitState();
            OnExitState();
        }

        if (States.Count > 0)
        {
            CurrentState = States[0];
            CurrentState?.EnterState();
        }
    }
}