using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAbilities
{
    public PlayerInput playerInput;
    private ClassList classList;
    private int currentAbilityIndex = 0;
    private int currentClassIndex = 0;
    private Transform abilitySpawnTransform;
    private Transform MeleeTransform;
    private Transform characterTransform;
    private float firePointDistance = 1.5f;
    Vector3 aim;
    private GameObject aimObject;
    //private GameObject aimObjectPrefab;
    Animator animator;

    public PlayerAbilities(PlayerInput playerInput, ClassList classList, 
        Transform abilitySpawnTransform, Transform characterTransform, Transform meleeTransform)
    {
        this.playerInput = playerInput;
        this.classList = classList;
        this.abilitySpawnTransform = abilitySpawnTransform;
        this.characterTransform = characterTransform;
        aimObject = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/InGameHUD/Pointer"));
        //aimObjectPrefab = Resources.Load<GameObject>("Prefabs/InGameHUD/Pointer");
        aim = Vector3.zero;
        animator = characterTransform.gameObject.GetComponent<Animator>();
        MeleeTransform = meleeTransform;
    }

    public void UpdateAbilities()
    {
        if (playerInput.actions["Fire"].triggered)
        {
            Fire();
        }

        if (playerInput.actions["SwitchAbility"].triggered)
        {
            SwitchAbility();
        }
        
        aim = playerInput.actions["Aim"].ReadValue<Vector2>();
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

    private void UpdateAim()
    {

        if (!aimObject.activeSelf)
        {
            aimObject.SetActive(true);
        }

        Vector2 aimDirection = new Vector2(aim.x, aim.y).normalized;

        //aimDirection.y += 0.5f;
        abilitySpawnTransform.position = 
            new Vector3(characterTransform.position.x, characterTransform.position.y +0.5f, characterTransform.position.y) + 
            new Vector3(aimDirection.x, aimDirection.y, 0f) * firePointDistance;

        aimObject.transform.position = abilitySpawnTransform.position;

        // Rotate the fire point to face the aim direction
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        //abilitySpawnTransform.rotation = Quaternion.Euler(-(angle+360), 90, 0);
        abilitySpawnTransform.forward = aimDirection;
        
        aimObject.transform.up = -aimDirection;

    }
    

    private void Fire()
    {
        if(currentAbilityIndex == 0)
        {
            animator.SetTrigger("atk1");
            GameObject abilityPrefab = Resources.Load<GameObject>("Prefabs/Abilities/" + classList.classes[currentClassIndex].abilities[currentAbilityIndex].abilityLocation);
            GameObject ability = GameObject.Instantiate(abilityPrefab, MeleeTransform.position, Quaternion.identity);

        }
        else
        {
            animator.SetTrigger("atk2");
            GameObject abilityPrefab = Resources.Load<GameObject>("Prefabs/Abilities/" + classList.classes[currentClassIndex].abilities[currentAbilityIndex].abilityLocation);
            GameObject ability = GameObject.Instantiate(abilityPrefab, abilitySpawnTransform.position, abilitySpawnTransform.rotation);
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
}
