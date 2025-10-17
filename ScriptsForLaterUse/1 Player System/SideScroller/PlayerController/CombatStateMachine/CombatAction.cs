using MoreMountains.Tools;
using UnityEngine;

public abstract class CombatAction : MonoBehaviour
{
    public enum InitializationModes { EveryTime, OnlyOnce, }

    public InitializationModes InitializationMode;
    protected bool _initialized;

    /// a label you can set to organize your AI Actions, not used by anything else 
    [Tooltip("a label you can set to organize your AI Actions, not used by anything else")]
    public string Label;
    public abstract void PerformAction();
    public virtual bool ActionInProgress { get; set; }
    protected CombatStateMachine stateMachine;

    protected virtual bool ShouldInitialize
    {
        get
        {
            switch (InitializationMode)
            {
                case InitializationModes.EveryTime:
                    return true;
                case InitializationModes.OnlyOnce:
                    return _initialized == false;
            }
            return true;
        }
    }

    /// <summary>
    /// On Awake we grab our AIBrain
    /// </summary>
    protected virtual void Awake()
    {
        stateMachine = this.gameObject.GetComponentInParent<CombatStateMachine>();
    }

    /// <summary>
    /// Initializes the action. Meant to be overridden
    /// </summary>
    public virtual void Initialization()
    {
        _initialized = true;
    }

    /// <summary>
    /// Describes what happens when the brain enters the state this action is in. Meant to be overridden.
    /// </summary>
    public virtual void OnEnterState()
    {
        ActionInProgress = true;
    }

    /// <summary>
    /// Describes what happens when the brain exits the state this action is in. Meant to be overridden.
    /// </summary>
    public virtual void OnExitState()
    {
        ActionInProgress = false;
    }
}