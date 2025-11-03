using UnityEngine;

public abstract class CombatDecision : MonoBehaviour
{
    /// Decide will be performed every frame while the Brain is in a state this Decision is in. Should return true or false, which will then determine the transition's outcome.
    public abstract bool Decide();

    /// a label you can set to organize your AI Decisions, not used by anything else 
    [Tooltip("a label you can set to organize your AI Decisions, not used by anything else")]
    public string Label;
    public virtual bool DecisionInProgress { get; set; }
    protected CombatStateMachine stateMachine;

    /// <summary>
    /// On Awake we grab our Brain
    /// </summary>
    protected virtual void Awake()
    {
        stateMachine = this.gameObject.GetComponentInParent<CombatStateMachine>();
    }

    /// <summary>
    /// Meant to be overridden, called when the game starts
    /// </summary>
    public virtual void Initialization()
    {
    }



    /// <summary>
    /// Meant to be overridden, called when the Brain enters a State this Decision is in
    /// </summary>
    public virtual void OnEnterState()
    {
        DecisionInProgress = true;
    }

    /// <summary>
    /// Meant to be overridden, called when the Brain exits a State this Decision is in
    /// </summary>
    public virtual void OnExitState()
    {
        DecisionInProgress = false;
    }
}