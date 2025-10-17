using MoreMountains.CorgiEngine;
using Unity.VisualScripting;
using UnityEngine;

public class CA_AirBurst : CombatAction
{
    private Vector3[] origionalPosition;
    public enum TriggerType { DelayStart, DelayEnd }

    [Header("VFX")]
    public GameObject VFX_Initial;
    public GameObject VFX_Continuous;
    public TriggerType playerTriggerType;
    public GameObject[] VFX_Trigger;
    public GameObject VFX_End;

    public GameObject VFX_EnemyInitial;
    public GameObject VFX_EnemyContinuous;
    public TriggerType enemyTriggerType;
    public GameObject[] VFX_EnemyTrigger;
    public GameObject VFX_EnemyEnd;


    [Header("Animation")]
    public string playerAnimationTrigger;
    public string enemyAnimationTrigger;


    [Header("Variables")]
    public bool TurnOffGravity = true;
    public bool TurnOffCollisions = true;
    public bool physicsBased = false;
    public float knockUpDuration = 4f;
    public float speed = 5f;
    public float hitStopDelay = 0.1f;
    public Vector2 knockUpPosition = new Vector2(2, 5);
    public AnimationCurve knockUpGraphX;
    public AnimationCurve knockUpGraphY;
    public AnimationCurve TravelGraphX;
    public AnimationCurve TravelGraphY;



    [Header("Where the player will go")]
    public Vector3[] playerCoordinates;
    public Vector3[] enemyCoordinates;


    private int playerCurrentCoordinate = 0;
    private int enemyCurrentCoordinate = 0;

    private float currentKnockUpTime = 0f;
    private bool canAttack = true;

    private Timer hitStopDelayTimer;

    private bool attackComplete = false;
    private bool alreadyTriggered = false;

    public override void Initialization()
    {
        base.Initialization();
        VFX_Continuous.SetActive(false);
    }

    public override void PerformAction()
    {
        if (canAttack)
        {
            canAttack = false;
            currentKnockUpTime = 0;
            //VFX_Continuous.SetActive(true);

            VFXon();
            stateMachine.controller.GravityActive(false);
            stateMachine.controller.CollisionsOff();

            hitStopDelayTimer = new Timer(hitStopDelay);
        }
        if (stateMachine.targets.Count > 0) AttackKnockUpTarget();

    }

    private void AttackKnockUpTarget()
    {
        for (int i = 0; i < stateMachine.targets.Count; i++)
        {
            stateMachine.targets[i].transform.position = origionalPosition[i];
        }
        stateMachine.transform.position = CalculateNewPosition(origionalPosition[0]);
    }

    private Vector3 CalculateNewPosition(Vector3 startingPosition)
    {
        if(attackComplete)
        {
            if (hitStopDelayTimer.ClockTick())
            {
                stateMachine.isAttackFinished = true;
            }
            return startingPosition + playerCoordinates[playerCurrentCoordinate];

        }

        currentKnockUpTime += Time.deltaTime;
        Vector3 newPosition = stateMachine.transform.position;


        if (hitStopDelayTimer.ClockTick())
        {
            if (!alreadyTriggered)
            {
                InstantiateTriggers(newPosition, TriggerType.DelayEnd);
                alreadyTriggered = true;
            }

            newPosition = Preformance(startingPosition, newPosition);
        }
        else
        {
            alreadyTriggered = false;
        }

        return newPosition;
    }

    private Vector3 Preformance(Vector3 startingPosition, Vector3 newPosition)
    {
        Debug.Log("Performance");
        newPosition = Vector3.MoveTowards(stateMachine.transform.position, startingPosition + playerCoordinates[playerCurrentCoordinate], speed * Time.deltaTime);

        if (Vector3.Distance(newPosition, startingPosition + playerCoordinates[playerCurrentCoordinate]) < 0.01f)
        {
            playerCurrentCoordinate++;
            hitStopDelayTimer.RestartTimer();

            InstantiateTriggers(newPosition, TriggerType.DelayStart);

            if (playerCurrentCoordinate >= playerCoordinates.Length)
            {
                playerCurrentCoordinate = 0;
                if (VFX_End != null)
                Instantiate(VFX_End, startingPosition, Quaternion.identity);
                attackComplete = true;
            }
        }

        return newPosition;
    }

    private float GenerateSineWave(float amplitude, float frequency, float phase, float time)
    {
        return amplitude * Mathf.Sin(2 * Mathf.PI * frequency * time + phase);
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        stateMachine.isAttackFinished = false;
        if(stateMachine.targets.Count > 0)
        {
            origionalPosition = new Vector3[stateMachine.targets.Count];
            for (int i = 0; i < stateMachine.targets.Count; i++)
            {
                //origionalPosition[i] = stateMachine.targets[i].transform.position;
                origionalPosition[i] = new Vector3(stateMachine.transform.position.x + knockUpPosition.x * stateMachine.characterStatus.facingDirection, 
                    stateMachine.transform.position.y + knockUpPosition.y, 0);
            }
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();

        VFXoff();
        stateMachine.controller.GravityActive(true);
        stateMachine.controller.CollisionsOn();
        canAttack = true;
        attackComplete = false;
        stateMachine.isAttackFinished = false;
    }

    private void InstantiateTriggers(Vector3 position, TriggerType triggerType)
    {

        if (VFX_Trigger != null && playerTriggerType == triggerType)
        {
            if (VFX_Trigger.Length > 0)
            {
                foreach (GameObject trigger in VFX_Trigger)
                {
                    Instantiate(trigger, position, Quaternion.identity);
                }
            }
        }
        if (VFX_EnemyTrigger != null && enemyTriggerType == triggerType)
        {
            if (VFX_EnemyTrigger.Length > 0)
            {
                foreach (GameObject trigger in VFX_EnemyTrigger)
                {
                    Instantiate(trigger, stateMachine.targets[0].transform.position, Quaternion.identity);
                }
            }
        }

    }

    private void VFXon()
    {
        if (VFX_Continuous != null)
            VFX_Continuous.SetActive(true);
        if (VFX_EnemyContinuous != null)
        {
            VFX_EnemyContinuous.SetActive(true);
            VFX_EnemyContinuous.transform.position = stateMachine.targets[0].transform.position;
        }
    }
    private void VFXoff()
    {
        if (VFX_Continuous != null)
            VFX_Continuous.SetActive(false);
        if (VFX_EnemyContinuous != null)
        {
            VFX_EnemyContinuous.SetActive(false);
        }
    }
}
