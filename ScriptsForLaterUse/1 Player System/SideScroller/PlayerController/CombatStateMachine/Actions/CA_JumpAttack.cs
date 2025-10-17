using UnityEditor.Build;
using UnityEngine;

public class CA_JumpAttack : CombatAction
{

    public AnimationCurve TravelGraphX;
    public AnimationCurve TravelGraphY;
    public string playerAnimationTrigger = "JumpDown";
    public float currentKnockUpTime = 0f;
    public float phase1Speed = 2f;
    public float phase2Speed = 25f;
    private bool canAttack = true;
    Vector3 startingPosition;

    public override void Initialization()
    {
        base.Initialization();

    }

    public override void PerformAction()
    {

        if (canAttack)
        {
            canAttack = false;
            currentKnockUpTime = 0;
            startingPosition = stateMachine.transform.position;
            //VFX_Continuous.SetActive(true);
            //stateMachine.controller.GravityActive(false);
            stateMachine.animator.SetBool(playerAnimationTrigger, true);


        }
        if (currentKnockUpTime * phase1Speed <= 1)
        {
            stateMachine.transform.position = CalculateNewPosition(startingPosition);
        }
        else
        {
            stateMachine.gameObject.GetComponent<Rigidbody2D>().gravityScale = 2;
            //stateMachine.transform.position = new Vector3(stateMachine.transform.position.x, 
              //  stateMachine.transform.position.y - (5*Time.deltaTime), stateMachine.transform.position.z);
                
        }

    }

    private Vector3 CalculateNewPosition(Vector3 startingPosition)
    {
        
            Vector3 newPosition = new Vector3(startingPosition.x + TravelGraphX.Evaluate(currentKnockUpTime * phase1Speed),
                startingPosition.y + TravelGraphY.Evaluate(currentKnockUpTime * phase1Speed), startingPosition.z);

        Debug.Log("Travel: " + (startingPosition.y + TravelGraphY.Evaluate(currentKnockUpTime * phase1Speed)));
        Debug.Log("TravelY: " + (TravelGraphY.Evaluate(currentKnockUpTime * phase1Speed)));
        Debug.Log("StartY: " + (startingPosition.y));
        currentKnockUpTime += Time.deltaTime;
            return newPosition;
        
    }

    public override void OnEnterState()
    {
        base.OnEnterState(); 
        canAttack = true;
        currentKnockUpTime = 0f;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        canAttack = true;
        currentKnockUpTime = 0f;
        stateMachine.gameObject.GetComponent<Rigidbody2D>().gravityScale = 1;
        stateMachine.animator.SetBool(playerAnimationTrigger, false);
        stateMachine.controller.GravityActive(true);
    }
}
