using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class CA_SpawnAbility : CombatAction
{
    public bool joyDirection = false;

    public Vector3 facingDirection = Vector3.zero;
    public Vector3 turnObject = Vector3.zero;
    public float speed = 15;
    public GameObject abilityPrefab;
    public Vector3 spawnPoint;

    private bool actionComplete = false;

    public override void PerformAction()
    {
        if (actionComplete)
        {
            return;
        }

        actionComplete = true;
        if (joyDirection)
        {
            GameObject ability = Instantiate(abilityPrefab, stateMachine.playerSkills.abilitySpawnTransform.position, Quaternion.identity);
            facingDirection = stateMachine.playerSkills.abilitySpawnTransform.forward;
            Debug.Log("Facing Direction: " + facingDirection);
            ability.transform.rotation = Quaternion.LookRotation(facingDirection);
            if (turnObject != Vector3.zero)
            {
                Debug.Log("Turning Object");
                ability.transform.Rotate(turnObject);
            }
            ability.GetComponent<AbilityMove>().SetParameters(facingDirection, speed);
        }
        else
        {
            GameObject ability = Instantiate(abilityPrefab, stateMachine.transform.position + spawnPoint, Quaternion.identity);

            if (facingDirection != Vector3.zero)
            {
                ability.transform.rotation = Quaternion.LookRotation(facingDirection);
                if (turnObject != Vector3.zero)
                {
                    ability.transform.Rotate(turnObject);
                }
                ability.GetComponent<AbilityMove>().SetParameters(facingDirection, speed);
            }
        }


        //ability.GetComponent<Ability>().caster = caster;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        actionComplete = false;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        actionComplete = false;
    }
}