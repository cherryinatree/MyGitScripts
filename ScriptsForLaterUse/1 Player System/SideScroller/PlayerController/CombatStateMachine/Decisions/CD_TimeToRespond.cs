using UnityEngine;

public class CD_TimeToRespond : CombatDecision
    {
    public float timeToRespond = 4f;
    private float currentTime = 0f;



    public override bool Decide()
    {

        currentTime += Time.deltaTime;
        if (currentTime >= timeToRespond)
        {
            return true;
        }

        return false;
    }

    public override void Initialization()
    {
        base.Initialization();
        currentTime = 0;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        currentTime = 0;
    }
}