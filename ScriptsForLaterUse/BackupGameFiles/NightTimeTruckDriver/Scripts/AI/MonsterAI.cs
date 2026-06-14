using UnityEngine;


public interface IMonsterBehavior
{
    void Execute(MonsterAI monster);
    void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState);
}

public interface IMonsterTrigger
{
    bool ShouldActivate(MonsterAI monster);
}


public class MonsterAI : MonoBehaviour
{
    public enum MonsterState { Idle, Active, Attacking, Fleeing, Despawned }
    public MonsterState currentState = MonsterState.Idle;

    private IMonsterBehavior[] behaviors;
    private IMonsterTrigger[] triggers;

    void Awake()
    {
        // Collect behaviors and triggers attached to this monster
        behaviors = GetComponents<IMonsterBehavior>();
        triggers = GetComponents<IMonsterTrigger>();
    }

    void Update()
    {
        // Continuously check triggers
        foreach (var trigger in triggers)
        {
            if (trigger.ShouldActivate(this))
            {
                SetState(MonsterState.Active);
            }
        }

        // Run behaviors based on state
        foreach (var behavior in behaviors)
        {
            behavior.Execute(this);
        }
    }

    public void SetState(MonsterState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        foreach (var behavior in behaviors)
        {
            behavior.OnStateChange(this, newState);
        }
    }

    public void Despawn()
    {
        SetState(MonsterState.Despawned);
        Destroy(gameObject, 1f);
    }
}
