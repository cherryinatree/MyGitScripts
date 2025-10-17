using MoreMountains.Tools;
using UnityEngine;

public class AID_WaitTime : AIDecision
{
    private Timer timer;
    public float waitTime = 1f;
    public override bool Decide()
    {
        if (timer.ClockTick())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Initialization()
    {
        base.Initialization();
        timer = new Timer(waitTime);
    }

    public override void OnEnterState()
    {
        timer.RestartTimer();
    }

    public override void OnExitState()
    {
        timer.RestartTimer();
    }


}