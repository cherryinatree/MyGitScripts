using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AIA_Jumping : AIAction
{

    private Ai_Eyes eyes;
    private CorgiController controller;
    private CharacterHorizontalMovement moveHoriziontal;
    private MoreMountains.CorgiEngine.Character character;
    private CharacterJump jump;
    [Range(0, 10)]
    public float speed = 1.5f;

    public override void Initialization()
    {
        base.Initialization();
        eyes = GetComponent<Ai_Eyes>();
        controller = GetComponent<CorgiController>();
        moveHoriziontal = GetComponent<CharacterHorizontalMovement>();
        jump = GetComponent<CharacterJump>();

        character = GetComponentInParent<MoreMountains.CorgiEngine.Character>();
    }

    public override void PerformAction()
    {
        if (eyes.lastPlayerSeen.transform.position.x < transform.position.x)
        {
            moveHoriziontal.SetHorizontalMove(-1 * speed);
            character.Face(MoreMountains.CorgiEngine.Character.FacingDirections.Left);

        }
        else
        {
            moveHoriziontal.SetHorizontalMove(speed);
            character.Face(MoreMountains.CorgiEngine.Character.FacingDirections.Right);
        }

    }

    public override void OnEnterState()
    {
        base.OnEnterState();
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }
}