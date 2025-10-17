using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public class PlayerSkills : CharacterAbility
{
    private ClassList classList;
    public int currentAbilityIndex = 0;
    private int currentClassIndex = 0;
    public Transform abilitySpawnTransform;
    public Transform MeleeTransform;
    private Transform characterTransform;
    private float firePointDistance = 1.5f;
    Vector3 aim;
    private GameObject aimObject;
    //private GameObject aimObjectPrefab;
    Animator animator;
    private PlayerSaveInteraction playerSaveInteraction;

    private Timer timer;
    private float delay = 0.2f;
    
    private bool switchAbility = false;
    private bool shoot = false;

    public Timer[] CooldownTimers;
    public float[] CooldownTime;

    public Abilities[] abilities;


    public GameObject[] MeleeCombos;

    public GameObject Dash;
    public GameObject Run;
    public GameObject Jump;
    public GameObject WallJump;
    public GameObject MeleeHitPointVFX;
    public GameObject MeleeHitVFX;

    private int whichCombo = 0;
    private int ComboIndex = 0;
    private float meleeComboMax = 4;
    private Timer attackTimer;
    private float attackDelay = 0.5f;
    private Timer comboTimer;
    private float comboDelay = 0.75f;

    private float comboAttackRadius = 1;
    public Vector2 meleeForce = new Vector2(15, 5);
    public float immuneAfterAttack = 2.5f;

    private int buttonPressed = 0;
    private bool[] combo = new bool[4];

    private GameObject target;

    bool[] comboAbilities = new bool[10];
    private bool comboChosen = false;


    private bool attackReady = true;
    private bool isAttacking = false;
    private int facingDirection = 1;


    protected override void Initialization()
    {
        base.Initialization();

        playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        GameObject player = playerSaveInteraction.GetActiveCharacter();
        whichCombo = playerSaveInteraction.playerSaveFile.SelectedCharacterIndex;
        this.classList = Resources.Load<ClassList>("Scripts/PlayerController/Stats/ClassList/ClassList");
        this.abilitySpawnTransform = transform.Find("FirePoint");
        aimObject = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/InGameHUD/Pointer"));
        aim = Vector3.zero;
        MeleeTransform = transform.Find("MeleePoint");
        timer = new Timer(delay);
        attackTimer = new Timer(attackDelay);
        comboTimer = new Timer(comboDelay); 

        CooldownTimers = new Timer[3];
        CooldownTime = new float[3];
        abilities = new Abilities[3];

        target = null;



        MeleeCombos[2].SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            abilities[i] = classList.classes[currentClassIndex].abilities[i];
            CooldownTime[i] = abilities[i].abilityCooldown;
            CooldownTimers[i] = new Timer(CooldownTime[i]);
        }

        for (int i = 0; i < 4; i++) 
        {
            combo[i] = false;
        
        }
        for (int i = 0; i < 10; i++)
        {
            comboAbilities[i] = false;
        }
    }


    public void Update()
    {
        CheckVariables();
        //CheckActivateAbilities();
        //CheckAttack();
        CheckAim();
        FlipMelee();

        timer.ClockTick();
    }


    

    /*
        Contantly checking if an enemy is in range of a combo attack.
        Attack or Special button pressed
        Check if the character was pressing a direction as well
        if the buttons+direction match a combo, the combo is executed
        a boolean called "comboChosen" is set to true so the execution stage can begin
        the VFX for that combo is played
        during the combo execution, the player is immune to damage
        the player quickly moves to the nearest enemy
        if no enemy is in range, the player will stay still
        when the combo is finished, the player is no longer immune to damage
        
     
     
     
     */
   
    private void CheckAttack()
    {
        CheckForEnemyInComboRange();
        if (!CheckCombo()) return;
        if(!isAttacking)
        {
            ExecuteCombo();
            Invoke("SpawnMelee", 0.25f);
            ExecuteVFX();
            Immune(); 
            StillEnemy();
            comboTimer.RestartTimer();
            isAttacking = true;
        }

        //MoveToTarget();
        if (comboTimer.ClockTick())
        {
            IsAttackFinished();
            isAttacking = false;
        }



    }

    

    private void CheckForEnemyInComboRange()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(MeleeTransform.position, 5);

        float distance = 100000;

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.tag == "Enemy")
            {
                if (Vector2.Distance(transform.position, enemy.transform.position) < distance)
                {
                    distance = Vector2.Distance(MeleeTransform.position, enemy.transform.position);
                    target = enemy.gameObject;
                }
            }
        }
    }

    private bool CheckCombo()
    {
        if(comboChosen)
        {
            return true;
        }

        if (_inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
        {
            if (_horizontalInput > 0.75f)
            {
                comboAbilities[0] = true;
                comboChosen = true;
                return true;
            }
            else if (_horizontalInput < -0.75f)
            {
                comboAbilities[1] = true;
                comboChosen = true;
                return true;
            }
            else if (_verticalInput > 0.75f)
            {
                comboAbilities[2] = true;
                comboChosen = true;
                return true;
            }
            else if (_verticalInput < -0.75f)
            {
                comboAbilities[3] = true;
                comboChosen = true;
                return true;
            }
            else
            {
                comboAbilities[4] = true;
                comboChosen = true;
                return true;
            }
        }

       /* if(_inputManager.DashButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
        {

            if (_horizontalInput > 0.75f)
            {
                comboAbilities[5] = true;
                comboChosen = true;
                return true;
            }
            else if (_horizontalInput < -0.75f)
            {
                comboAbilities[6] = true;
                comboChosen = true;
                return true;
            }
            else if (_verticalInput > 0.75f)
            {
                comboAbilities[7] = true;
                comboChosen = true;
                return true;
            }
            else if (_verticalInput < -0.75f)
            {
                comboAbilities[8] = true;
                comboChosen = true;
                return true;
            }
            else
            {
                comboAbilities[9] = true;
                comboChosen = true;
                return true;
            }
        }*/
            return false;
    }





    private void ExecuteCombo()
    {

        animator.SetTrigger("attack1");
    }

    private void ExecuteVFX()
    {
        MeleeHitPointVFX.SetActive(false);
        MeleeHitPointVFX.SetActive(true);
        MeleeCombos[2].SetActive(false);
        MeleeCombos[2].SetActive(true);
        MeleeVFX();
    }

    private void StillEnemy()
    {
        if(target == null)
        {
            return;
        }

    }

    private void KnockBackEnemy()
    {
        if(target == null)
        {
            return;
        }
    }

    private void MoveToTarget()
    {
        Vector3 offset = new Vector3(-1.5f, 0,0);
        if(target != null)
        {
            Vector2 direction = target.transform.position - transform.position;
            transform.position = Vector2.MoveTowards(transform.position, target.transform.position + offset, 25 * Time.deltaTime);
        }
    }

    private void IsAttackFinished()
    {
        KnockBackEnemy();
        NoLongerImmune();
        MeleeCombos[2].SetActive(false);
        for (int i = 0; i < 10; i++)
        {
            comboAbilities[i] = false;
        }
        comboChosen = false;
        target = null;
    }

    private void CheckAim()
    {
        aim = _inputManager.SecondaryMovement;
        //aim = playerInput.actions["Aim"].ReadValue<Vector2>();
        if (aim != Vector3.zero)
        {
            UpdateAim();
        }
        else
        {
            if (aimObject.activeSelf)
            {
                aimObject.SetActive(false);
            }
        }
    }

    private void CheckActivateAbilities()
    {
        if (comboTimer.ClockTick())
        {
                Debug.Log("ComboTimer: " + ComboIndex);
                ComboIndex = 0;
                buttonPressed = 0;
                comboTimer.RestartTimer();
            
        }

        if (!combo[3])
        {

            //Invoke(nameof(NoLongerImmune), immuneAfterAttack);
            NoLongerImmune();
            attackReady = true;
        }

        if (_inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
        {
            if (!switchAbility && attackReady)
            {
                if(buttonPressed < combo.Length)
                {
                    combo[buttonPressed] = true;
                    buttonPressed++;
                }
                else
                {
                    buttonPressed = 0;
                    attackReady = false;
                }
                //Fire();
                switchAbility = true;
                comboTimer.RestartTimer();

            }
        }

        if (_inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
        {
            switchAbility = false;
        }

        if (_inputManager.SwitchWeaponButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
        {
            if (!shoot)
            {
                Debug.Log("SwitchAbility");
                SwitchAbility();
                shoot = true;
            }
        }
        if (_inputManager.SwitchWeaponButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
        {
            shoot = false;
        }

        for (int i = 0; i < 3; i++)
        {

            CooldownTimers[i].ClockTick();

        }

        Fire();
        
    }

    private void FlipMelee()
    {
        if(_horizontalInput < 0)
        {
            //meleeForce *= new Vector2(-1, 1);
            facingDirection = -1;
            MeleeTransform.localPosition = new Vector3(-1f, 0, 0);
        }
        else if (_horizontalInput > 0)
        {
            //meleeForce *= new Vector2(-1, 1);
            facingDirection = 1;
            MeleeTransform.localPosition = new Vector3(1f, 0, 0);
        }
    }


    private void MoveTowardTarget()
    {

    }

    private void UpdateAim()
    {

        if (!aimObject.activeSelf)
        {
            aimObject.SetActive(true);
        }

        Vector2 aimDirection = new Vector2(aim.x, aim.y).normalized;

        //aimDirection.y += 0.5f;
        abilitySpawnTransform.position =
            new Vector3(characterTransform.position.x, characterTransform.position.y + 0.5f, characterTransform.position.z) +
            new Vector3(aimDirection.x, aimDirection.y, 0f) * firePointDistance;

        aimObject.transform.position = abilitySpawnTransform.position;

        // Rotate the fire point to face the aim direction
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        //abilitySpawnTransform.rotation = Quaternion.Euler(-(angle+360), 90, 0);
        abilitySpawnTransform.forward = aimDirection;

        aimObject.transform.up = -aimDirection;

    }

    /*
        In fire 2, the amount of times the fire button is press is counted. The firing will 
        trigger one by one until the last ability is used. If the combo timer triggers, the
    combo index will be reset to 0.

     
     */
    private void Fire()
    {


        if (currentAbilityIndex == 0)
        {


            animator.SetBool("atk1", combo[0]);
            animator.SetBool("atk2", combo[1]);
            animator.SetBool("atk3", combo[2]);
            animator.SetBool("atk4", combo[3]);


            if (animator.GetCurrentAnimatorStateInfo(0).IsName("attack1") && combo[0])
            {
                Immune();
                combo[0] = false;
                animator.SetBool("atk1", combo[0]);
                MeleeVFX();
                Invoke("SpawnMelee", 0.15f);
            }
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("attack2") && combo[1])
            {
                combo[1] = false;
                animator.SetBool("atk2", combo[1]);
                MeleeVFX();
                Invoke("SpawnMelee", 0.15f);
            }
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("attack3") && combo[2])
            {
                combo[2] = false;
                animator.SetBool("atk3", combo[2]);
                MeleeVFX();
                Invoke("SpawnMelee", 0.15f);
            }
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("attack4") && combo[3])
            {
                combo[3] = false;
                animator.SetBool("atk4", combo[3]);
                MeleeVFX();
                Invoke("SpawnMelee", 0.15f);
                buttonPressed = 0;
            }

            
            //animator.SetTrigger("atk1");
            //MeleeVFX();
            //Invoke("SpawnMelee", 0.25f);
        }
        else
        {
            animator.SetTrigger("cast");
            Invoke("SpawnAbility", 0.5f);
        }
    }


    private void SpawnMelee()
    {
        //GameObject abilityPrefab = Resources.Load<GameObject>("Prefabs/Abilities/" + classList.classes[currentClassIndex].abilities[currentAbilityIndex].abilityLocation);
        //GameObject ability = GameObject.Instantiate(abilityPrefab, MeleeTransform.position, MeleeTransform.rotation);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(MeleeTransform.position, comboAttackRadius);
        attackReady = true;

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.tag == "Enemy")
            {
                Debug.Log("Hit: " + enemy.name);
                //ComboIndex++;
                comboTimer.RestartTimer();
                enemy.GetComponent<Health>().Damage(abilities[currentAbilityIndex].abilityDamage, MeleeTransform.gameObject, 0.5f, 0.5f, new Vector3(50,50,0), null);
                
                Vector2 force = new Vector2(15, 5);
                enemy.GetComponent<Health>().AssociatedController.SetForce(force * facingDirection);

                MeleeHitVFX.SetActive(false);
                MeleeHitVFX.SetActive(true);
                Immune();
                Invoke(nameof(NoLongerImmune), immuneAfterAttack);
                //enemy.GetComponent<Rigidbody2D>().AddForce(new Vector2(5, 5), ForceMode2D.Impulse);
            }
        }
    }

    private void Immune()
    {
        _character.CharacterHealth.ImmuneToDamage = true;
    }

    private void NoLongerImmune()
    {

        _character.CharacterHealth.ImmuneToDamage = false;
    }

    private void SpawnAbility()
    {

        GameObject abilityPrefab = Resources.Load<GameObject>("Prefabs/Abilities/" + classList.classes[currentClassIndex].abilities[currentAbilityIndex].abilityLocation);
        GameObject ability = GameObject.Instantiate(abilityPrefab, abilitySpawnTransform.position, abilitySpawnTransform.rotation);
        ability.transform.Rotate(0, 90, 0);
    }

    private void MeleeVFX()
    {
        if(ComboIndex < MeleeCombos[whichCombo].transform.childCount)
        {
            MeleeCombos[whichCombo].transform.GetChild(ComboIndex).gameObject.SetActive(false);
            MeleeCombos[whichCombo].transform.GetChild(ComboIndex).gameObject.SetActive(true);
            //ComboIndex++;
        }
        else 
        { 
            ComboIndex = 0;
            MeleeCombos[whichCombo].transform.GetChild(ComboIndex).gameObject.SetActive(false);
            MeleeCombos[whichCombo].transform.GetChild(ComboIndex).gameObject.SetActive(true);
        }
    }


    private void SwitchAbility()
    {
        currentAbilityIndex++;
        if (currentAbilityIndex >= 3)
        {
            currentAbilityIndex = 0;
        }
    }


    private void SwitchClass()
    {
        currentAbilityIndex++;
        if (currentAbilityIndex >= 2)
        {
            currentAbilityIndex = 0;
        }
    }


    private void CheckVariables()
    {

        if (characterTransform == null || animator == null)
        {
            characterTransform = playerSaveInteraction.GetActiveCharacter().transform;
            animator = characterTransform.GetComponent<Animator>();
        }
   
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(MeleeTransform.position, comboAttackRadius);
        
    }

    
    
}
