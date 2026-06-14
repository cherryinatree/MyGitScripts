using UnityEngine;

public class DespawnOnLightBehavior : MonoBehaviour, IMonsterBehavior
{
    /* private FlashlightController flashlight; // your existing flashlight script

     void Start()
     {
         flashlight = FindObjectOfType<FlashlightController>();
     }
    */
    public void Execute(MonsterAI monster)
    {
        /*if (monster.currentState == MonsterAI.MonsterState.Active && flashlight.IsShiningOn(monster.gameObject))
        {
            monster.Despawn();
        }*/
    }

    public void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState) { }
}
