using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using MoreMountains.CorgiEngine;
using MoreMountains;

[System.Serializable]
public class CombatActionsList : CSM_RecordableArray<CombatAction>
{
}
[System.Serializable]
public class CombatTransitionsList : CSM_RecordableArray<CombatTranistion>
{
}

/// <summary>
/// A State is a combination of one or more actions, and one or more transitions. An example of a state could be "_patrolling until an enemy gets in range_".
/// </summary>
[System.Serializable]
public class CombatState
{

    /// the name of the state (will be used as a reference in Transitions
    public string StateName;

    [CSM_RecordableAttribute(null, "Action", null)]
    public CombatActionsList Actions;
    [CSM_RecordableAttribute(null, "Transition", null)]
    public CombatTransitionsList Transitions;/*

        /// a list of actions to perform in this state
        public List<AIAction> Actions;
        /// a list of transitions to evaluate to exit this state
        public List<AITransition> Transitions;*/



    protected CombatStateMachine stateMachine;

    /// <summary>
    /// Sets this state's brain to the one specified in parameters
    /// </summary>
    /// <param name="brain"></param>
    public virtual void SetBrain(CombatStateMachine brain)
    {
        stateMachine = brain;
    }

    /// <summary>
    /// On enter state we pass that info to our actions and decisions
    /// </summary>
    public virtual void EnterState()
    {
        foreach (CombatAction action in Actions)
        {
            action.OnEnterState();
        }
        foreach (CombatTranistion transition in Transitions)
        {
            if (transition.Decision != null)
            {
                transition.Decision.OnEnterState();
            }
        }
    }

    /// <summary>
    /// On exit state we pass that info to our actions and decisions
    /// </summary>
    public virtual void ExitState()
    {
        foreach (CombatAction action in Actions)
        {
            action.OnExitState();
        }
        foreach (CombatTranistion transition in Transitions)
        {
            if (transition.Decision != null)
            {
                transition.Decision.OnExitState();
            }
        }
    }

    /// <summary>
    /// Performs this state's actions
    /// </summary>
    public virtual void PerformActions()
    {
        if (Actions.Count == 0) { return; }
        for (int i = 0; i < Actions.Count; i++)
        {
            if (Actions[i] != null)
            {
                Actions[i].PerformAction();
            }
            else
            {
                Debug.LogError("An action in " + stateMachine.gameObject.name + " on state " + StateName + " is null.");
            }
        }
    }

    /// <summary>
    /// Tests this state's transitions
    /// </summary>
    public virtual void EvaluateTransitions()
    {
        if (Transitions.Count == 0) { return; }
        for (int i = 0; i < Transitions.Count; i++)
        {
            if (Transitions[i].Decision != null)
            {
                if (Transitions[i].Decision.Decide())
                {
                    if (!string.IsNullOrEmpty(Transitions[i].TrueState))
                    {
                        stateMachine.TransitionToState(Transitions[i].TrueState);
                        break;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Transitions[i].FalseState))
                    {
                        stateMachine.TransitionToState(Transitions[i].FalseState);
                        break;
                    }
                }
            }
        }
    }
}