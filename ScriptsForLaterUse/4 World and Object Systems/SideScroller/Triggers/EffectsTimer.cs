using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsTimer : MonoBehaviour
{
    public bool triggerActivated = false;
    public float time = 4;
    private Timer timer;


    public bool hasStartDelay = false;
    public float startDelay = 10;
    public bool hasEndDelay = false;
    public float endDelay = 10;

    private void Start()
    {


        if (hasStartDelay)
        {

            timer = new Timer(startDelay);
        }
        else
        {
            timer = new Timer(time);
        }
    }


    // Update is called once per frame
    void Update()
    {
            MainTick();
        
    }

    private void MainTick()
    {



        if (timer.ClockTick())
        {

            triggerChange();
            timer.RestartTimer();

        }


    }

    private void triggerChange()
    {
        if (triggerActivated)
        {
            triggerActivated = false;

            if (hasEndDelay)
            {
                Debug.Log("End Delay");
                timer.NewStopTime(endDelay);
            }


        }
        else
        {
            triggerActivated = true;

            timer.NewStopTime(time);
        }
    }
}