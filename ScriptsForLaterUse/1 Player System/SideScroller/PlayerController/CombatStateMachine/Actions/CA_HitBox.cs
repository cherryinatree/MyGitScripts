using MoreMountains.CorgiEngine;
using UnityEngine;
using static MoreMountains.CorgiEngine.Character;

public class CA_HitBox : CombatAction
{
    public float spawnDelay = 0.25f;

    public float comboAttackRadius = 1.5f;
    public GameObject MeleeHitVFX;
    private bool canAttack = true;

    public override void PerformAction()
    {
        if (ShouldInitialize)
        {
            Initialization();
        }
        if(canAttack)
        {
            canAttack = false;
            Invoke("SpawnMelee", spawnDelay);
        }
    }

    public override void Initialization()
    {
        base.Initialization();

    }

    public override void OnEnterState()
    {
        base.OnEnterState();

    }

    public override void OnExitState()
    {
        base.OnExitState();
        canAttack = true;
    }


    private void SpawnMelee()
    {
        Vector3 meleeSpawnPosition = stateMachine.transform.position + new Vector3(1.5f * stateMachine.characterStatus.facingDirection, 0, 0);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(meleeSpawnPosition, comboAttackRadius);

        stateMachine.targets.Clear();

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.tag == "Enemy")
            {
                stateMachine.targets.Add(enemy.gameObject);

                enemy.GetComponent<Health>().Damage(
                    stateMachine.characterStatus.abilities[stateMachine.characterStatus.currentAbilityIndex].abilityDamage,
                    stateMachine.gameObject, 0.5f, 0.5f, new Vector3(0, 0, 0), null);


                if(MeleeHitVFX != null)
                {
                    MeleeHitVFX.SetActive(false);
                    MeleeHitVFX.SetActive(true);

                }

            }
        }
    }
}