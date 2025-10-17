using MoreMountains.CorgiEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static MoreMountains.CorgiEngine.Character;

public class CA_FrameAttack : CombatAction
{
    //public string attackTrigger = "attack1";
    private bool attackStarted = false;

    private InputBuffer myInputBuffer;
    private Abilities chosenAttack;

    private List<GameObject> alreadyHitEnemies;
    private HealthIntermediate healthIntermediate;
    private CurrentCondition currentCondition;

    private bool drawCube = false;
    private bool drawCirc = false;


    private bool prefabSpawned = false;

    private bool comboActivated = false;

    private Timer inThisStateCheck;
    private float inThisStateCheckTime = 0.75f;

    public override void Initialization()
    {
        base.Initialization();
        //chosenAttack = GetComponent<InputBuffer>().chosenAbility;
        myInputBuffer = GetComponent<InputBuffer>();
        alreadyHitEnemies = new List<GameObject>();
        currentCondition = GetComponent<CurrentCondition>();
        inThisStateCheck = new Timer(inThisStateCheckTime);
    }

    public override void PerformAction()
    {

        Debug.Log("Frame Attack");
        if (!attackStarted)
        {
            if (stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName(chosenAttack.abilityAnimationTrigger))
            {
                attackStarted = true;
                inThisStateCheck.RestartTimer();
            }
            if (inThisStateCheck.ClockTick())
            {
                if (stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                {
                    Debug.Log("Idle Triggered");
                    stateMachine.isAttackFinished = true;
                }
                inThisStateCheck.RestartTimer();
            }
        }
        else
        {

            inThisStateCheck.RestartTimer();
            if (!stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName(chosenAttack.abilityAnimationTrigger))
            { 
                stateMachine.isAttackFinished = true;
            }
            SpawnAttack();
        }

    }


    private void SpawnAttack()
    {
        if (IsDamageTime())
        {
            if (chosenAttack.hitBoxType != Abilities.AbilityHitBox.SpawnPrefab)
            {
                PerformActionsOnHitEnemy(FindOverlap());
            }
            else
            {
                SpawnPrefab();
            }
        }

        if(stateMachine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > chosenAttack.damageTime)
        {
            CheckForCombo();
        }
    }

    private void CheckForCombo()
    {
        if (myInputBuffer.hasAbilityBeenChosen)
        {
            if (myInputBuffer.chosenAbility == chosenAttack)
            {
                AbilityStartUp(chosenAttack.comboAbility);
            }
        }
    }



    private void SpawnPrefab()
    {
        if (prefabSpawned)
        {
            return;
        }

        prefabSpawned = true;
        if (chosenAttack.joyDirection)
        {
            GameObject ability = Instantiate(chosenAttack.abilityPrefab, stateMachine.playerSkills.abilitySpawnTransform.position, Quaternion.identity);
            Vector3 facingDirection = stateMachine.playerSkills.abilitySpawnTransform.forward;

            ability.transform.rotation = Quaternion.LookRotation(facingDirection);
            if (chosenAttack.turnObject != Vector3.zero)
            {
                ability.transform.Rotate(chosenAttack.turnObject);
            }
            ability.GetComponent<AbilityMove>().SetParameters(facingDirection, chosenAttack.speed);
        }
        else
        {
            GameObject ability = Instantiate(chosenAttack.abilityPrefab, stateMachine.transform.position + chosenAttack.spawnPoint, Quaternion.identity);

            if (chosenAttack.facingDirection != Vector3.zero)
            {
                ability.transform.rotation = Quaternion.LookRotation(chosenAttack.facingDirection);
                if (chosenAttack.turnObject != Vector3.zero)
                {
                    ability.transform.Rotate(chosenAttack.turnObject);
                }
                ability.GetComponent<AbilityMove>().SetParameters(chosenAttack.facingDirection, chosenAttack.speed);
            }
        }

    }


    private bool IsDamageTime()
    {
        if (stateMachine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= chosenAttack.damageTime &&
                stateMachine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < chosenAttack.recoveryTime)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private Collider2D[] FindOverlap()
    {
        if (chosenAttack.hitBoxType == Abilities.AbilityHitBox.Circle)
        {
            drawCirc = true;
            return Physics2D.OverlapCircleAll(FindMeleeSpawnPosition(), chosenAttack.hitBoxRadius);
        }
        else if (chosenAttack.hitBoxType == Abilities.AbilityHitBox.Box)
        {
            drawCube = true;
            return Physics2D.OverlapBoxAll(FindMeleeSpawnPosition(), chosenAttack.hitBoxSize, chosenAttack.hitBoxAngle);
        }

        return null;
    }

    private void PerformActionsOnHitEnemy(Collider2D[] hitEnemies)
    {

        foreach (Collider2D enemy in hitEnemies)
        {

            if (enemy.tag == "Enemy")
            {
                bool enemyAlreadyHit = false;

                for (int i = 0; i < alreadyHitEnemies.Count; i++)
                {
                    if (enemy.gameObject == alreadyHitEnemies[i])
                    {
                        enemyAlreadyHit = true;
                    }
                }

                if (enemyAlreadyHit)
                {
                    continue;
                }

                alreadyHitEnemies.Add(enemy.gameObject);

                if (chosenAttack.abilityImpactVFX != null)
                {
                    GameObject selfVFX = Instantiate(chosenAttack.abilitySelfVFX);
                    selfVFX.transform.position = enemy.transform.position;
                    selfVFX.transform.parent = enemy.transform;
                }


                enemy.GetComponent<HealthIntermediate>().Damage(
                    stateMachine.characterStatus.abilities[stateMachine.characterStatus.currentAbilityIndex].abilityDamage,
                    stateMachine.gameObject, new Vector2(chosenAttack.knockBackForce.x * stateMachine.characterStatus.facingDirection,
                    chosenAttack.knockBackForce.y), 0.5f, 0.5f, new Vector3(0, 0, 0), null);

            }
        }
    }

    private Vector3 FindMeleeSpawnPosition()
    {
        return transform.position +
            new Vector3(chosenAttack.hitBoxPosition.x * stateMachine.characterStatus.facingDirection, chosenAttack.hitBoxPosition.y, 0);
    }


    public override void OnEnterState()
    {
        base.OnEnterState();
        stateMachine.LossControl();
        AbilityStartUp(myInputBuffer.chosenAbility);
        inThisStateCheck.RestartTimer();
    }


    private void AbilityStartUp(Abilities currentAbility)
    {
        stateMachine.isAttackFinished = false;
        attackStarted = false;
        drawCube = false;
        drawCirc = false;
        prefabSpawned = false;


        currentCondition.currentAnimation = currentAbility.abilityAnimationTrigger;
        alreadyHitEnemies = new List<GameObject>();
        chosenAttack = currentAbility;
        stateMachine.animator.SetTrigger(chosenAttack.abilityAnimationTrigger);
        inThisStateCheck.RestartTimer();

        SpawnSelfVFX();
    }

    private void SpawnSelfVFX()
    {

        if (chosenAttack.abilitySelfVFX != null)
        {
            GameObject selfVFX = Instantiate(chosenAttack.abilitySelfVFX);
            selfVFX.transform.position = transform.position;
            selfVFX.transform.parent = transform;
            if(stateMachine.characterStatus.facingDirection == -1)
            {
                selfVFX.transform.Rotate(new Vector3(0, 180, 0));
            }
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        stateMachine.RegainControl();
        inThisStateCheck.RestartTimer();
        stateMachine.isAttackFinished = false;
        attackStarted = false;
    }

    private void OnDrawGizmos()
    {
        if (chosenAttack != null)
        {
            if (drawCirc)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(FindMeleeSpawnPosition(), chosenAttack.hitBoxRadius);
                drawCirc = false;
            }
            if (drawCube)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(FindMeleeSpawnPosition(), chosenAttack.hitBoxSize);
                drawCube = false;
            }
        }
    }

    
}