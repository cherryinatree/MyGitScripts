using MoreMountains.CorgiEngine;
using System.Collections.Generic;
using UnityEngine;

public class HealthIntermediate : CharacterAbility
{
    [HideInInspector]
    public bool isBlocking = false;
    [HideInInspector]
    public bool isHit = false;

    private Health health;
    private CorgiController controller;
    private PlayerStamina stamina;

    protected override void Initialization()
    {
        base.Initialization();

        isHit = false;

        health = GetComponent<Health>();
        controller = GetComponent<CorgiController>();
        stamina = GetComponent<PlayerStamina>();
    }

    public override void ProcessAbility()
    {
        base.ProcessAbility();

        if(_inputManager == null)         
        {
            return;
        }
        if (isBlocking)
        {
            if (_inputManager.ThrowButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
            {
                isBlocking = false;
            }
        }
        else
        {
            if (_inputManager.ThrowButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonDown)
            {
                isBlocking = true;
            }
        }
    }





    public void Damage(float damage, GameObject instigator, Vector2 force, float flickerDuration,
            float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null)
    {
        if (!isBlocking)
        {
            health.Damage(damage, instigator, flickerDuration, invincibilityDuration, damageDirection, typedDamages);
            isHit = true;
            LossControl();
        }
        else
        {
            float stam = stamina.Stamina;
            if(stam - damage <= 0)
            {
                stamina.Stamina = 0;
                health.Damage((damage-stam), instigator, flickerDuration, invincibilityDuration, damageDirection, typedDamages);
                isBlocking = false;
                isHit = true; 
                LossControl();
            }
            else
            {
                stamina.Stamina -= damage;
            }
        }

        AddForce(force);
    }

    private void AddForce(Vector2 forceToAdd)
    {
        if (isBlocking)
        {
            GetComponent<CorgiController>().AddForce(new Vector2((forceToAdd.x / 2), 0));
        }
        else
        {

            GetComponent<CorgiController>().AddForce(forceToAdd);
        }
    }


    public void LossControl()
    {
        _character.ConditionState.ChangeState(CharacterStates.CharacterConditions.Stunned);
       // _characterHorizontalMovement.AbilityPermitted = false;
        //controller.enabled = false;
        
        if(_inputManager != null) _inputManager.enabled = false;
    }

    public void RegainControl()
    {
        _character.ConditionState.ChangeState(CharacterStates.CharacterConditions.Normal);
        //_characterHorizontalMovement.AbilityPermitted = true;
        //controller.enabled = true;
        
        if (_inputManager != null) _inputManager.enabled = true;
    }

}
