using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindingListener : MonoBehaviour
{
    private KeybindingManager keybindingManager;

    private CombatControlsEnable enactCombat;
    public CombatUIenable enactUI;
    public ActionsOutOfCombat enactOutOfCombat;
    public ActionsOutOfCombatUI enactOutOfCombatUI;

    private int whichScript;

    private float holdTime = 0.3f;
    private float repeatDelay = 0.1f;
    private Timer timer;
    private Timer timer2;

    private CharactersMovement charactersMovement;

    private bool isFirstFrame = true;

    private void Start()
    {
        keybindingManager = GameObject.Find("User").GetComponent<KeybindingManager>();
        
        whichScript = 1;

        enactCombat = new CombatControlsEnable();
        enactUI = new CombatUIenable();
        enactOutOfCombat = new ActionsOutOfCombat();
        enactOutOfCombatUI = new ActionsOutOfCombatUI();

        timer = new Timer(holdTime);
        timer2 = new Timer(repeatDelay);

        charactersMovement = new CharactersMovement();

    }

    private void Update()
    {
        DoesPlayerHaveControl();
        Listen();

        if(isFirstFrame)
        {
            isFirstFrame = false;
            charactersMovement.CursorCheck();
        }

        if (whichScript == 2)
        {
            charactersMovement.MoveCharacter();
        }
    }

    private void Listen()
    {

        bool isKeyDown = false;
        foreach (string key in keybindingManager.keybindings.Keys)
        {
            if (Input.GetKeyDown(keybindingManager.keybindings[key]))
            {
                isKeyDown = true;
                EnactKeys(key);
            }
        }

        if (Input.GetKey(keybindingManager.keybindings["MoveUp"]))
        {
            isKeyDown = true;
            if (timer.ClockTick())
            {
                if (timer2.ClockTick())
                {
                    EnactKeys("MoveUp");
                    timer2.RestartTimer();
                }
            }
        }
        if (Input.GetKey(keybindingManager.keybindings["MoveDown"]))
        {
            isKeyDown = true;
            if (timer.ClockTick())
            {
                if (timer2.ClockTick())
                {
                    EnactKeys("MoveDown");
                    timer2.RestartTimer();
                }
            }
        }
        if (Input.GetKey(keybindingManager.keybindings["MoveLeft"]))
        {
            isKeyDown = true;
            if (timer.ClockTick())
            {
                if (timer2.ClockTick())
                {
                    EnactKeys("MoveLeft");
                    timer2.RestartTimer();
                }
            }
        }
        if (Input.GetKey(keybindingManager.keybindings["MoveRight"]))
        {
            isKeyDown = true;
            if (timer.ClockTick())
            {

                if (timer2.ClockTick())
                {
                    EnactKeys("MoveRight");
                    timer2.RestartTimer();
                }
            }
        }


        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) ||
            Input.GetKeyUp(KeyCode.D))
        {
            if (!isKeyDown)
            {
                timer.RestartTimer();
            }
        }
    }

    private void EnactKeys(string key)
    {
        if (whichScript == 0)
        {
            Debug.Log("0 Enacting key: " + key);

            enactCombat.Key(key);
        }
        else if (whichScript == 1)
        {
            Debug.Log("1 Enacting key: " + key);

            enactUI.Key(key);
        }
        else if (whichScript == 2)
        {
            Debug.Log("2 Enacting key: " + key);

            enactOutOfCombat.Key(key);
        }else if (whichScript == 3)
        {
            Debug.Log("3 Enacting key: " + key);

            enactOutOfCombatUI.Key(key);
        }
    }

    public void UpdateKeyboard()
    {
        DoesPlayerHaveControl();
    }


    private void DoesPlayerHaveControl()
    {
        if (CombatSingleton.Instance.battleSystem.CurrentTeam == 0)
        {
            IsTheUiOn();
            InfoCheckIfOccupied();
        }
    }


    private void IsTheUiOn()
    {
        if(CombatSingleton.Instance.battleSystem.State == BATTLESTATE.OUTOFCOMBAT)
        {
            if (CombatSingleton.Instance.isUiOn)
            {
                whichScript = 3;
            }
            else
            {
                whichScript = 2;
            }
        }
        else
        {
            if (CombatSingleton.Instance.isUiOn)
            {
                whichScript = 1;
            }
            else
            {
                whichScript = 0;
            }
        }

    }

    private void InfoCheckIfOccupied()
    {
        if (CombatSingleton.Instance.CursorCube.GetComponent<Cube>().MyType == GROUNDTYPE.Occupied)
        {
            CombatSingleton.Instance.InfoCharacter = CubeRetriever.GetCharacterOnCursor();
            CombatPanelManipulator.ActivatePanel("CharacterInfoPanel");
        }
        else
        {
            CombatPanelManipulator.DeactivatePanel("CharacterInfoPanel");
            CombatSingleton.Instance.InfoCharacter = null;
        }
    }
}
