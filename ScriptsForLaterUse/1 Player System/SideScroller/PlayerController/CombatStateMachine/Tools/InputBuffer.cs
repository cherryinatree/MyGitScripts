using MoreMountains.CorgiEngine;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Playables;
using System;

public class InputBuffer : CharacterAbility
{
    public enum InputBufferButtons { Left, Right, Up, Down, ReturnToCenter, A, B, X, Y }
    [HideInInspector]
    public List<InputBufferButtons> buttonsPressed;
    public string buttonsCheckList;

    public AbilityList AllAbilities;
    [HideInInspector]
    public List<Abilities> characterAbilityList;
    [HideInInspector]
    public Abilities chosenAbility;
    [HideInInspector]
    public bool hasAbilityBeenChosen;

    private bool returnToCenter = true;

    private Timer comboResetTimer;
    private float comboResetDelay = 0.65f;

    private float joystickActivation = 0.7f;
    private float centerActivation = 0.2f;

    private bool aUp = true;
    private bool bUp = true;
    private bool xUp = true;
    private bool yUp = true;

    protected override void Initialization()
    {
        base.Initialization();
        hasAbilityBeenChosen = false;
        buttonsPressed = new List<InputBufferButtons>();
        buttonsCheckList = string.Empty;
        comboResetTimer = new Timer(comboResetDelay);
        CreateAbilityList();
    }

    private void CreateAbilityList()
    {
        PlayerSaveFile playerSaveFile = GetComponent<PlayerSaveInteraction>().playerSaveFile;

        foreach (int ability in playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].abilitiesID)
        {
            for (int i = 0; i < AllAbilities.abilities.Count; i++)
            {
                if(ability == AllAbilities.abilities[i].abilityID)
                {
                    characterAbilityList.Add(AllAbilities.abilities[i]);
                }
            }
        }
    }


    public override void ProcessAbility()
    {
        base.ProcessAbility();
    }

    protected override void HandleInput()
    {
        base.HandleInput();

        hasAbilityBeenChosen = false;
        if (comboResetTimer.ClockTick())
        {
            buttonsPressed.Clear();
            //buttonsPressed.Add(InputBufferButtons.ReturnToCenter);
            comboResetTimer.RestartTimer();
        }

        int buttonCount = buttonsPressed.Count - 1;
        if(buttonCount <= 0)
        {
            buttonCount = 0;
        }

        
        if (_horizontalInput > joystickActivation && CheckLastButtonPressed(buttonsPressed, InputBufferButtons.Right))
        {
            returnToCenter = false;
            comboResetTimer.RestartTimer();
            LastButtonWasActivation();
            buttonsPressed.Add(InputBufferButtons.Right);
            
        }
        if (_horizontalInput < -joystickActivation && CheckLastButtonPressed(buttonsPressed, InputBufferButtons.Left))
        {
            returnToCenter = false;
            comboResetTimer.RestartTimer();
            LastButtonWasActivation();
            buttonsPressed.Add(InputBufferButtons.Left);
        }
        if (_verticalInput > joystickActivation && CheckLastButtonPressed(buttonsPressed, InputBufferButtons.Up))
        {
            returnToCenter = false;
            comboResetTimer.RestartTimer();
            LastButtonWasActivation();
            buttonsPressed.Add(InputBufferButtons.Up);
        }
        if (_verticalInput < -joystickActivation && CheckLastButtonPressed(buttonsPressed, InputBufferButtons.Down))
        {
            returnToCenter = false;
            comboResetTimer.RestartTimer();
            LastButtonWasActivation();
            buttonsPressed.Add(InputBufferButtons.Down);
        }
        if (_verticalInput < centerActivation && _verticalInput > -centerActivation && 
            _horizontalInput > -centerActivation && _horizontalInput < centerActivation)
        {
            returnToCenter = true;
            //buttonsPressed.Add(InputBufferButtons.ReturnToCenter);
        }
        if (_inputManager.RunButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonDown)
        {
            xUp = false;
            comboResetTimer.RestartTimer();
            buttonsPressed.Add(InputBufferButtons.X);
        }
        if (_inputManager.RunButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            xUp = true;
        }
        if (_inputManager.DashButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonDown)
        {
            yUp = false;
            comboResetTimer.RestartTimer();
            buttonsPressed.Add(InputBufferButtons.Y);
        }
        if (_inputManager.DashButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            yUp = true;
        }
        if (_inputManager.TimeControlButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonDown)
        {
            bUp = false;
            comboResetTimer.RestartTimer();
            buttonsPressed.Add(InputBufferButtons.B);
        }
        if (_inputManager.TimeControlButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            bUp = true;
        }
        if (_inputManager.JumpButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonDown)
        {
            aUp = false;
            comboResetTimer.RestartTimer();
            buttonsPressed.Add(InputBufferButtons.A);
        }
        if (_inputManager.JumpButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            aUp = true;
        }

        
        

        int buttonsCount = buttonsPressed.Count;
        if(buttonsCount > 3)
        {
            for (int i = 3; i < buttonsCount; i++)
            {
                buttonsPressed.RemoveAt(0);
            }
        }

        //Debug.Log(buttonsPressed.Count);    

        buttonsCheckList = ConvertBufferToString(buttonsPressed);



        foreach (Abilities ability in characterAbilityList)
        {
            if (CheckIfComboMatches(ability.comboButtons, buttonsCheckList))
            {
                hasAbilityBeenChosen = true;
                chosenAbility = ability;
                comboResetTimer.RestartTimer();
                buttonsPressed.Clear();
                //buttonsPressed.Add(InputBufferButtons.ReturnToCenter);
            }
        }


    }

    private bool CheckLastButtonPressed(List<InputBufferButtons> inputs, InputBufferButtons checkForMe)
    {

        if (returnToCenter)
        {
            return true;
        }

        if(inputs.Count > 0)
        {
            if (inputs[inputs.Count-1] != checkForMe)
            {
                return true;
            }
        }
        return false;
    }

    private void LastButtonWasActivation()
    {
        if(buttonsPressed.Count <= 0) return;
        InputBufferButtons press = buttonsPressed[buttonsPressed.Count - 1];
        if (press == InputBufferButtons.A || press == InputBufferButtons.B ||
            press == InputBufferButtons.X || press == InputBufferButtons.Y) 
        {
            buttonsPressed.Clear();
        }
    }


    private bool CheckIfComboMatches(List<InputBufferButtons> abilityButtons, string checkList)
    {
        int checkListCount = checkList.Length;
        string cutString = checkList;
        string cutStringAppend = cutString;
        for (int i = 0; i < checkListCount; i++)
        {
            if(ConvertBufferToString(abilityButtons) == cutString)
            {
                return true;
            }
            else
            {
                if (cutStringAppend.Length > 1)
                {
                    cutString = "";

                    for (int x = 1; x < cutStringAppend.Length; x++)
                    {
                        cutString += cutStringAppend[x];
                    }
                    cutStringAppend = cutString;
                }
                else
                {
                    cutString = cutStringAppend;
                }
            }
        }

        return false;
        
    }




    private string ConvertBufferToString(List<InputBufferButtons> abilityComboList)
    {
        string converter = string.Empty;
        for (int i = 0; i < abilityComboList.Count; i++)
        {
            switch (abilityComboList[i])
            {
                case InputBuffer.InputBufferButtons.Left:
                    converter += "L";
                    break;
                case InputBuffer.InputBufferButtons.Right:
                    converter += "R";
                    break;
                case InputBuffer.InputBufferButtons.Up:
                    converter += "U";
                    break;
                case InputBuffer.InputBufferButtons.Down:
                    converter += "D";
                    break;
                case InputBuffer.InputBufferButtons.A:
                    converter += "A ";
                    break;
                case InputBuffer.InputBufferButtons.B:
                    converter += "B ";
                    break;
                case InputBuffer.InputBufferButtons.X:
                    converter += "X ";
                    break;
                case InputBuffer.InputBufferButtons.Y:
                    converter += "Y ";
                    break;
                default:
                    break;
            }
        }

        return converter;
    }
}
