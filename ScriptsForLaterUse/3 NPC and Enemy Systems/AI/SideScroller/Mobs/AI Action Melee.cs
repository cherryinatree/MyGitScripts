using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// An Action that shoots using the currently equipped weapon. If your weapon is in auto mode, will shoot until you exit the state, and will only shoot once in SemiAuto mode. You can optionnally have the character face (left/right) the target, and aim at it (if the weapon has a WeaponAim component).
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/AI/Actions/AI Action Melee")]
    public class AIActionMelee : AIAction
{
    /// if true, the Character will face the target (left/right) when shooting
    [Tooltip("if true, the Character will face the target (left/right) when shooting")]
    public bool FaceTarget = true;
    /// if true the Character will aim at the target when shooting
    [Tooltip("if true the Character will aim at the target when shooting")]
    public bool AimAtTarget = false;
    /// a constant offset to apply to the target's position when aiming : 0,1,0 would aim more towards the head, for example
    [Tooltip("a constant offset to apply to the target's position when aiming : 0,1,0 would aim more towards the head, for example")]
    public Vector3 TargetOffset = Vector3.zero;
    /// the ability to pilot

    protected Character _character;
    protected Vector3 _weaponAimDirection;
    protected int _numberOfShoots = 0;
    protected bool _shooting = false;
    protected Animator _animator;
        protected Timer _timerAttackRate;
        public float AttackRate = 1f;

    /// <summary>
    /// On init we grab our CharacterHandleWeapon ability
    /// </summary>
    public override void Initialization()
    {
        _character = GetComponent<Character>();
        _animator = GetComponent<Animator>();
            _timerAttackRate = new Timer(AttackRate);
    }

    /// <summary>
    /// On PerformAction we face and aim if needed, and we shoot
    /// </summary>
    public override void PerformAction()
    {
        TestFaceTarget();
        Shoot();
    }

    /// <summary>
    /// Sets the current aim if needed
    /// </summary>
    protected virtual void Update()
    {
       
    }

    /// <summary>
    /// Faces the target if required
    /// </summary>
    protected virtual void TestFaceTarget()
    {
        if (!FaceTarget || (_brain.Target == null))
        {
            return;
        }

        if (this.transform.position.x > _brain.Target.position.x)
        {
            _character.Face(Character.FacingDirections.Left);
        }
        else
        {
            _character.Face(Character.FacingDirections.Right);
        }
    }


    /// <summary>
    /// Activates the weapon
    /// </summary>
    protected virtual void Shoot()
    {
        if (_timerAttackRate.ClockTick())
        {
            _animator.SetTrigger("Hit");
                _timerAttackRate.RestartTimer();
        }
    }

    /// <summary>
    /// When entering the state we reset our shoot counter and grab our weapon
    /// </summary>
    public override void OnEnterState()
    {
        base.OnEnterState();
        _numberOfShoots = 0;
        _shooting = true;
    }

    /// <summary>
    /// When exiting the state we make sure we're not shooting anymore
    /// </summary>
    public override void OnExitState()
    {
        base.OnExitState();
        _shooting = false;
    }
}
}