using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;

public class AIA_FrameAttack : AIAction
{
    //public string attackTrigger = "attack1";
    private bool attackStarted = false;
    [Range(0, 100)] [Tooltip("The amount of damage the attack will do.")]
    public int Damage = 5;

    //private InputBuffer myInputBuffer;
    public Abilities chosenAttack;
    private Animator animator;
    private CorgiController controller;
    private Character myCharacter;
    private CurrentCondition currentCondition;

    private List<GameObject> alreadyHitEnemies;


    private bool drawCube = false;
    private bool drawCirc = false;


    private bool prefabSpawned = false;
    private bool comboActivated = false;
    private bool selfVFXSpawned = false;
    private bool attackTriggered = false;

    private Timer timerAttackDelay;
    [Range(0, 1)] [Tooltip("The amount of time the attack will be delayed.")]
    public float attackDelay = 0.15f;



    public override void Initialization()
    {
        base.Initialization();
        //chosenAttack = GetComponent<InputBuffer>().chosenAbility;
        //myInputBuffer = GetComponent<InputBuffer>();
        alreadyHitEnemies = new List<GameObject>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CorgiController>();
        myCharacter = GetComponent<Character>();
        currentCondition = GetComponent<CurrentCondition>();
        timerAttackDelay = new Timer(attackDelay);
    }

    public override void PerformAction()
    {

        if (timerAttackDelay.ClockTick() && !attackTriggered)
        {
            Attack();
            attackTriggered = true;
        }

        if (!attackStarted)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(chosenAttack.abilityAnimationTrigger))
            {
                attackStarted = true;
            }
        }
        else
        {

            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(chosenAttack.abilityAnimationTrigger))
            {
                //stateMachine.isAttackFinished = true;
            }
            SpawnAttack();
        }

    }

    private void Attack()
    {
        animator.SetTrigger(chosenAttack.abilityAnimationTrigger);
        currentCondition.currentAnimation = chosenAttack.abilityAnimationTrigger;
    }


    private void SpawnAttack()
    {
        if (IsDamageTime())
        {

            if (chosenAttack.hitBoxType != Abilities.AbilityHitBox.SpawnPrefab)
            {
                PerformActionsOnHitEnemy(FindOverlap());

                if (!selfVFXSpawned)
                {
                    SpawnSelfVFX();
                    selfVFXSpawned = true;
                }
            }
            else
            {
                SpawnPrefab();
            }
        }


        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > chosenAttack.damageTime)
        {
            CheckForCombo();
        }
    }

    private void CheckForCombo()
    {
        /*if (myInputBuffer.hasAbilityBeenChosen)
        {
            if (myInputBuffer.chosenAbility == chosenAttack)
            {
                AbilityStartUp(chosenAttack.comboAbility);
            }
        }*/


        //AbilityStartUp(chosenAttack.comboAbility);
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
            GameObject ability = Instantiate(chosenAttack.abilityPrefab, transform.position, Quaternion.identity);

            Vector3 facingDirection = new Vector3(myCharacter.IsFacingRight ? 1 : -1, 0, 0);

            ability.transform.rotation = Quaternion.LookRotation(facingDirection);
            if (chosenAttack.turnObject != Vector3.zero)
            {
                ability.transform.Rotate(chosenAttack.turnObject);
            }
            ability.GetComponent<AbilityMove>().SetParameters(facingDirection, chosenAttack.speed);
        }
        else
        {
            GameObject ability = Instantiate(chosenAttack.abilityPrefab, transform.position + chosenAttack.spawnPoint, Quaternion.identity);

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
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= chosenAttack.damageTime &&
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime < chosenAttack.recoveryTime)
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

            if (enemy.gameObject.layer == 9)
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
                float facingDirection = myCharacter.IsFacingRight ? 1 : -1;

                Vector2 attackForce = new Vector2(chosenAttack.knockBackForce.x * facingDirection,
                    chosenAttack.knockBackForce.y);
                enemy.GetComponent<HealthIntermediate>().Damage( Damage, gameObject, attackForce, 0.5f, 0.15f, new Vector3(0, 0, 0), null);

            }
        }
    }

    private Vector3 FindMeleeSpawnPosition()
    {
        float facingDirection = myCharacter.IsFacingRight ? 1 : -1;
        return transform.position +
            new Vector3(chosenAttack.hitBoxPosition.x * facingDirection, chosenAttack.hitBoxPosition.y, 0);
    }


    public override void OnEnterState()
    {
        base.OnEnterState();
        AbilityStartUp(chosenAttack);
    }


    private void AbilityStartUp(Abilities currentAbility)
    {
       // stateMachine.isAttackFinished = false;
        attackStarted = false;
        drawCube = false;
        drawCirc = false;
        prefabSpawned = false;
        selfVFXSpawned = false;
        attackTriggered = false;

        alreadyHitEnemies = new List<GameObject>();
        chosenAttack = currentAbility;
        //animator.SetTrigger(chosenAttack.abilityAnimationTrigger);
        //currentCondition.currentAnimation = chosenAttack.abilityAnimationTrigger;



    }

    private void SpawnSelfVFX()
    {

        if (chosenAttack.abilitySelfVFX != null)
        {
            GameObject selfVFX = Instantiate(chosenAttack.abilitySelfVFX);
            selfVFX.transform.position = transform.position;
            selfVFX.transform.parent = transform;
            if (!myCharacter.IsFacingRight)
            {
                selfVFX.transform.Rotate(new Vector3(0, 180, 0));
            }
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        //stateMachine.isAttackFinished = false;
        attackStarted = false;
        selfVFXSpawned = false;
        attackTriggered = false;
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