using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class AIA_Persue : AIAction
{
    private Ai_Eyes eyes;
    private CorgiController controller;
    private CharacterHorizontalMovement moveHoriziontal;
    private MoreMountains.CorgiEngine.Character character;
    private CharacterJump jump;

    [Range(0, 10)]
    public float speed = 3;


    public override void PerformAction()
    {
        if(eyes.lastPlayerSeen.transform.position.x < transform.position.x)
        {
            moveHoriziontal.SetHorizontalMove(-1* speed);
            character.Face(MoreMountains.CorgiEngine.Character.FacingDirections.Left);

            if(eyes.shouldJumpLeft)
            {
                jump.JumpStart();
                Debug.Log("jump");
            }
            
        }
        else
        {
            moveHoriziontal.SetHorizontalMove(speed);
            character.Face(MoreMountains.CorgiEngine.Character.FacingDirections.Right);

            if (eyes.shouldJumpRight)
            {
                jump.JumpStart();
                Debug.Log("jump");
            }
        }
    }

    public override void Initialization()
    {
        base.Initialization();
        eyes = GetComponent<Ai_Eyes>();
        controller = GetComponent<CorgiController>();
        moveHoriziontal = GetComponent<CharacterHorizontalMovement>();
        jump = GetComponent<CharacterJump>();

        character = GetComponentInParent<MoreMountains.CorgiEngine.Character>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        Debug.Log("persue");
        character._animator.SetBool("run", true);
    }

    public override void OnExitState()
    {
        base.OnExitState();
        character._animator.SetBool("run", false);
        moveHoriziontal.SetHorizontalMove(0);

    }
}

