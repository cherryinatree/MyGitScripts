using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMove : IState
{
    GameObject target;

    public override void EnterState(StateMachine stateMachine)
    {

        MyCondition(stateMachine);



    }

    public override void UpdateState(StateMachine stateMachine)
    {
        
    }


    private void MyCondition(StateMachine stateMachine)
    {

    }

   

    private void ClosestEnemy(StateMachine stateMachine)
    {
        
    }


    private void NoEnemies(StateMachine stateMachine)
    {
        return;
    }

}
