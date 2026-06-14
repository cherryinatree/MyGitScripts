using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{

    private float stopTime;
    private float timer;


    public Timer(float howLong)
    {
        stopTime = howLong;
        timer = 0;
    }

    public bool ClockTick()
    {
        timer += Time.deltaTime;
        if (timer > stopTime)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RestartTimer()
    {
        timer = 0;
    }

    public void NewStopTime(float howLong)
    {
        stopTime = howLong;
    }
}


