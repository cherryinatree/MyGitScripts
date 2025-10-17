using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPad : MonoBehaviour
{

    public GeneralTrigger trigger;

    public bool isPressed = false;
    private bool waitTillOffPad = true;

    public bool hasTimer = false;
    public float time = 4;
    private Timer timer;

    public ButtonPad[] LinkedPads;

    private void Start()
    {
        timer = new Timer(time);
    }


    // Update is called once per frame
    void Update()
    {
        if(trigger != null)
        {
            if (trigger.TriggerActivated)
            {
                if (waitTillOffPad)
                {
                    if (isPressed)
                    {

                        isPressed = false;
                    }
                    else
                    {

                        isPressed = true;
                    }
                    PadLink();
                    waitTillOffPad = false;
                }
            }
            else
            {

                waitTillOffPad = true;
            }
        }


        if (hasTimer)
        {
            if (isPressed)
            {
                if (timer.ClockTick())
                {
                    isPressed = false;
                    timer.RestartTimer();
                }
            }
        }
    }

    private void PadLink()
    {
        if(LinkedPads != null)
        {
            foreach (var pad in LinkedPads)
            {
                if (pad != null)
                {
                    pad.isPressed = isPressed;
                }
            }
        }
    }
}
