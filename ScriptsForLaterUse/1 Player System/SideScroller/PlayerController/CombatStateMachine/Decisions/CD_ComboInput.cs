using MoreMountains.Tools;
using UnityEngine;

public class CD_ComboInput : CombatDecision
{
    public enum InputType { X, DownX, ForwardX, UpX, Y, DownY, ForwardY, UpY }
    public enum JoystickInputType { Down, Forward, Up, StandStill }
    public InputType inputType;
    private JoystickInputType joystickInputType;

    private bool buttonPressed = false;

    private Timer minTime;
    public float minTimeValue = 0.4f;

    public override void OnEnterState()
    {
        base.OnEnterState();
        minTime = new Timer(minTimeValue);
    }


    public override bool Decide()
    {
        //if(stateMachine.targets == null) return false;
        //if(stateMachine.targets.Count == 0) return false;

        if (!buttonPressed)
        {
            buttonPressed = IsAttacking();
        }

        if(buttonPressed && minTime.ClockTick())
        {
            buttonPressed = false;
            return true;
        }

        if (buttonPressed && stateMachine.isAttackFinished)
        {

            buttonPressed = false;
            return true;
        }
        else
        {

           return false;
        }

    }


    private bool IsAttacking()
    {
        bool xPressed = stateMachine.inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonUp;
        bool yPressed = stateMachine.inputManager.DashButton.State.CurrentState == MMInput.ButtonStates.ButtonUp;

        if (xPressed && IsXType())
        {
            Direction();

            return MatchXtype();
        }
        else if (yPressed && IsYType())
        {
            Direction();
            return MatchYtype();
        }
        else
        {
            return false;
        }
    }

    private bool MatchXtype()
    {
        if (inputType == InputType.X && joystickInputType == JoystickInputType.StandStill)
        {
            return true;
        }
        else if (inputType == InputType.DownX && joystickInputType == JoystickInputType.Down)
        {
            return true;
        }
        else if (inputType == InputType.ForwardX && joystickInputType == JoystickInputType.Forward)
        {
            return true;
        }
        else if (inputType == InputType.UpX && joystickInputType == JoystickInputType.Up)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool MatchYtype()
    {
        if (inputType == InputType.Y && joystickInputType == JoystickInputType.StandStill)
        {
            return true;
        }
        else if (inputType == InputType.DownY && joystickInputType == JoystickInputType.Down)
        {
            return true;
        }
        else if (inputType == InputType.ForwardY && joystickInputType == JoystickInputType.Forward)
        {
            return true;
        }
        else if (inputType == InputType.UpY && joystickInputType == JoystickInputType.Up)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool IsXType()
    {
        if (inputType == InputType.X || inputType == InputType.DownX || inputType == InputType.ForwardX || inputType == InputType.UpX)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool IsYType()
    {
        if (inputType == InputType.Y || inputType == InputType.DownY || inputType == InputType.ForwardY || inputType == InputType.UpY)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Direction()
    {
        if (stateMachine.characterStatus.horizontalInput > 0.5f)
        {
            joystickInputType = JoystickInputType.Forward;
        }
        else if (stateMachine.characterStatus.horizontalInput < -0.5f)
        {
            joystickInputType = JoystickInputType.Forward;
        }
        else if (stateMachine.characterStatus.verticalInput > 0.5f)
        {
            joystickInputType = JoystickInputType.Up;
        }
        else if (stateMachine.characterStatus.verticalInput < -0.5f)
        {
            joystickInputType = JoystickInputType.Down;
        }
        else
        {
            joystickInputType = JoystickInputType.StandStill;
        }
    }

}