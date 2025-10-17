using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.Windows;

public class LevelSelectController : CharacterAbility
{
    public float speed = 5f;
    private Bounds _bounds;
    private NavMeshAgent _navMeshAgent;
    private Animator animator;
    private int _running;

    protected override void Initialization()
    {
        base.Initialization();

        _navMeshAgent = GetComponent<NavMeshAgent>();


        PlayerSaveInteraction playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        if (SaveData.Current.mainData == null)
        {
            SaveData.Current = SerializationManager.Load("Save1") as SaveData;
        }

        _bounds.center = GameObject.Find("MultiplayerLevelManager").GetComponent<MultiplayerLevelManager>().LevelBounds.center;
        _bounds.size = GameObject.Find("MultiplayerLevelManager").GetComponent<MultiplayerLevelManager>().LevelBounds.size;

        if (playerSaveInteraction.characters.Length == 0) return;
        animator = playerSaveInteraction.characters[SaveData.Current.mainData.players[playerSaveInteraction.playerIndex].SelectedCharacterIndex].GetComponent<Animator>();
        
    }

    protected override void InitializeAnimatorParameters()
    {

      //  RegisterAnimatorParameter("run", AnimatorControllerParameterType.Bool, out _running);

    }

        private void Update()
    {
        if (!AbilityPermitted) return;
        Movement();
        FacingDirection();
    }

    private void Movement()
    {
        //Vector3 newPosition = transform.position + new Vector3(_horizontalInput, 0, _verticalInput) * speed * Time.deltaTime;
        Vector3 newPosition = transform.position + new Vector3(_horizontalInput, 0, _verticalInput);
        // Clamp the position to stay within the bounds
        //newPosition.x = Mathf.Clamp(newPosition.x, _controller.BoundsLeft.x, _controller.BoundsRight.x);
        //newPosition.z = Mathf.Clamp(newPosition.y, _controller.BoundsBottom.y, _controller.BoundsTop.y);

        //transform.position = new Vector3(newPosition.x, 0, newPosition.z);

        if(animator == null)
        {
            try
            {

                PlayerSaveInteraction playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
                animator = playerSaveInteraction.characters[SaveData.Current.mainData.players[playerSaveInteraction.playerIndex].SelectedCharacterIndex].GetComponent<Animator>();
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
        if (_bounds.Contains(newPosition))
        {
            if(_horizontalInput == 0 && _verticalInput == 0)
            {
                animator.SetBool("run", false);
            }
            else 
            {                 
                animator.SetBool("run", true);
            }
            //transform.position = newPosition;
            _navMeshAgent.SetDestination(newPosition);
           // animator.SetBool("run", true);
           // MMAnimatorExtensions.UpdateAnimatorBool(_animator, "run", true);

        }
        else
        {
            //animator.SetBool("run", false);
            //MMAnimatorExtensions.UpdateAnimatorBool(_animator, "run", false);
        }
        //transform.position += new Vector3(_horizontalInput * Time.deltaTime * speed, 0, _verticalInput * Time.deltaTime * speed);
    }

    private void FacingDirection()
    {
        if(_horizontalInput == 0 && _verticalInput == 0)
        {
            return;
        }
        float angle = CalculateAngle(_horizontalInput, _verticalInput);
        transform.rotation = Quaternion.Euler(0, -angle, 0);
    }
    public float CalculateAngle(float xInput, float yInput)
    {
        // Calculate the angle in radians
        float angleInRadians = Mathf.Atan2(yInput, xInput);

        // Convert the angle to degrees
        float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

        return angleInDegrees;
    }
}
