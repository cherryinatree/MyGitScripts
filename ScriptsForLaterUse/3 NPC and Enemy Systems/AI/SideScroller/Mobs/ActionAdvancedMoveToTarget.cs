using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.InputSystem.XR;
using UnityEngine.TextCore.Text;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// This action directs the CharacterHorizontalMovement ability to move in the direction of the target.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/AI/Actions/AI Action Move Towards Target")]
    // [RequireComponent(typeof(CharacterHorizontalMovement))]
    public class ActionAdvancedMoveToTarget : AIAction
    {
        [Header("Obstacle Detection")]
        /// If set to true, the agent will change direction when hitting a wall
        [Tooltip("If set to true, the agent will change direction when hitting a wall")]
        public bool ChangeDirectionOnWall = true;
        /// If set to true, the agent will try and avoid falling
        [Tooltip("If set to true, the agent will try and avoid falling")]
        public bool AvoidFalling = false;
        /// The offset the hole detection should take into account
        [Tooltip("The offset the hole detection should take into account")]
        public Vector3 HoleDetectionOffset = new Vector3(0, 0, 0);
        /// the length of the ray cast to detect holes
        [Tooltip("the length of the ray cast to detect holes")]
        public float HoleDetectionRaycastLength = 1f;

        [Header("Layermasks")]
        /// Whether to use a custom layermask, or simply use the platform mask defined at the character level
        [Tooltip("Whether to use a custom layermask, or simply use the platform mask defined at the character level")]
        public bool UseCustomLayermask = false;
        /// if using a custom layer mask, the list of layers considered as obstacles by this AI
        [Tooltip("if using a custom layer mask, the list of layers considered as obstacles by this AI")]
        [MMCondition("UseCustomLayermask", true)]
        public LayerMask ObstaclesLayermask = LayerManager.ObstaclesLayerMask;
        /// the length of the horizontal raycast we should use to detect obstacles that may cause a direction change
        [Tooltip("the length of the horizontal raycast we should use to detect obstacles that may cause a direction change")]
        [MMCondition("UseCustomLayermask", true)]
        public float ObstaclesDetectionRaycastLength = 0.5f;
        /// the origin of the raycast (if casting against the same layer this object is on, the origin should be outside its collider, typically in front of it)
        [Tooltip("the origin of the raycast (if casting against the same layer this object is on, the origin should be outside its collider, typically in front of it)")]
        [MMCondition("UseCustomLayermask", true)]
        public Vector2 ObstaclesDetectionRaycastOrigin = new Vector2(0.5f, 0f);

        [Header("Revive")]
        /// if this is true, the character will automatically return to its initial position on revive
        [Tooltip("if this is true, the character will automatically return to its initial position on revive")]
        public bool ResetPositionOnDeath = true;
        /// The minimum distance to the target that this Character can reach
        [Tooltip("The minimum distance to the target that this Character can reach")]
        public float MinimumDistance = 1f;

        // private stuff
        protected CorgiController _controller;
        protected Character _character;
        protected Health _health;
        protected CharacterHorizontalMovement _characterHorizontalMovement;
        protected Vector2 _direction;
        protected Vector2 _startPosition;
        protected Vector2 _initialDirection;
        protected Vector3 _initialScale;
        protected float _distanceToTarget;
        protected Vector2 _raycastOrigin;
        protected RaycastHit2D _raycastHit2D;
        protected Vector2 _obstacleDirection;


        public int NumberOfJumps = 1;

        protected CharacterJump _characterJump;
        protected int _numberOfJumps = 0;
        protected Timer Timer;
        protected float Delay = 0.5f;

        /// <summary>
        /// On init we grab our CharacterHorizontalMovement ability
        /// </summary>
        public override void Initialization()
        {
            _controller = GetComponentInParent<CorgiController>();
            _character = GetComponentInParent<Character>();
            _characterHorizontalMovement = _character?.FindAbility<CharacterHorizontalMovement>();
            _health = _character.CharacterHealth;
            // initialize the start position
            _startPosition = transform.position;
            // initialize the direction
            _direction = _character.IsFacingRight ? Vector2.right : Vector2.left;

            _initialDirection = _direction;
            _initialScale = transform.localScale;
            _characterJump = this.gameObject.GetComponentInParent<Character>()?.FindAbility<CharacterJump>();
            Timer = new Timer(Delay);
        }

        /// <summary>
        /// On PerformAction we move
        /// </summary>
        public override void PerformAction()
        {
            CheckForWalls();
            CheckForHoles();

            Move();
        }

        /// <summary>
        /// Moves the character in the decided direction
        /// </summary>
        protected virtual void Move()
        {
            if (_brain.Target == null)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
                return;
            }
            if (Mathf.Abs(this.transform.position.x - _brain.Target.position.x) < MinimumDistance)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
                return;
            }

            if (this.transform.position.x < _brain.Target.position.x)
            {
                _characterHorizontalMovement.SetHorizontalMove(1f);
            }
            else
            {
                _characterHorizontalMovement.SetHorizontalMove(-1f);
            }
        }

        /// <summary>
        /// When entering the state we reset our movement.
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();
            if (_characterHorizontalMovement == null)
            {
                Initialization();
            }
            _characterHorizontalMovement.SetHorizontalMove(0f);
        }

        /// <summary>
        /// When exiting the state we reset our movement.
        /// </summary>
        public override void OnExitState()
        {
            base.OnExitState();
            _characterHorizontalMovement?.SetHorizontalMove(0f);
        }



        protected virtual void CheckForWalls()
        {

            if (UseCustomLayermask)
            {
                Debug.Log("Using custom layermask");
                if (DetectObstaclesCustomLayermask())
                {
                    if (!ChangeDirectionOnWall)
                    {
                        JumpForWall();
                    }
                    else
                    {

                        ChangeDirection();
                    }
                }
            }
            else
            {
                Debug.Log("Using regular layermask");
                // if the agent is colliding with something, make it turn around
                if (DetectObstaclesRegularLayermask())
                {
                    Debug.Log("Detected obstacle");
                    if (!ChangeDirectionOnWall)
                    {
                        JumpForWall();
                    }
                    else
                    {

                        ChangeDirection();
                    }
                }
            }
        }

        private void JumpForWall()
        {
            Debug.Log("Jumping for wall");
            if (_numberOfJumps < NumberOfJumps)
            {
                _characterJump.JumpStart();
                _numberOfJumps++;
            }
            if (Timer.ClockTick())
            {
                _numberOfJumps = 0;
                Timer.RestartTimer();
            }
        }

        /// <summary>
        /// Returns true if an obstacle is colliding with this AI, using its controller layer masks
        /// </summary>
        /// <returns></returns>
        protected bool DetectObstaclesRegularLayermask()
        {
            return (_direction.x < 0 && _controller.State.IsCollidingLeft) || (_direction.x > 0 && _controller.State.IsCollidingRight);
        }

        /// <summary>
        /// Returns true if an obstacle is in front of the character, using a custom layer mask
        /// </summary>
        /// <returns></returns>
        protected bool DetectObstaclesCustomLayermask()
        {
            if (_character.IsFacingRight)
            {
                _raycastOrigin = transform.position + (_controller.Bounds.x / 2 + ObstaclesDetectionRaycastOrigin.x) * transform.right + ObstaclesDetectionRaycastOrigin.y * transform.up;
                _obstacleDirection = Vector2.right;
            }
            else
            {
                _raycastOrigin = transform.position - (_controller.Bounds.x / 2 + ObstaclesDetectionRaycastOrigin.x) * transform.right + ObstaclesDetectionRaycastOrigin.y * transform.up;
                _obstacleDirection = Vector2.left;
            }

            _raycastHit2D = MMDebug.RayCast(_raycastOrigin, _obstacleDirection, ObstaclesDetectionRaycastLength, ObstaclesLayermask, Color.gray, true);

            return _raycastHit2D;
        }

        /// <summary>
        /// Checks for holes 
        /// </summary>
        protected virtual void CheckForHoles()
        {
            // if we're not grounded or if we're not supposed to check for holes, we do nothing and exit
            if (!AvoidFalling || !_controller.State.IsGrounded)
            {
                return;
            }

            // we send a raycast at the extremity of the character in the direction it's facing, and modified by the offset you can set in the inspector.

            if (_character.IsFacingRight)
            {
                _raycastOrigin = transform.position + (_controller.Bounds.x / 2 + HoleDetectionOffset.x) * transform.right + HoleDetectionOffset.y * transform.up;
            }
            else
            {
                _raycastOrigin = transform.position - (_controller.Bounds.x / 2 + HoleDetectionOffset.x) * transform.right + HoleDetectionOffset.y * transform.up;
            }

            if (UseCustomLayermask)
            {
                _raycastHit2D = MMDebug.RayCast(_raycastOrigin, -transform.up, HoleDetectionRaycastLength, ObstaclesLayermask, Color.gray, true);
            }
            else
            {
                _raycastHit2D = MMDebug.RayCast(_raycastOrigin, -transform.up, HoleDetectionRaycastLength, _controller.PlatformMask | _controller.MovingPlatformMask | _controller.OneWayPlatformMask | _controller.MovingOneWayPlatformMask, Color.gray, true);
            }

            // if the raycast doesn't hit anything
            if (!_raycastHit2D)
            {
                // we change direction
                ChangeDirection();
            }
        }

        /// <summary>
        /// Changes the current movement direction
        /// </summary>
        protected virtual void ChangeDirection()
        {
            _direction = -_direction;
        }
    }
}