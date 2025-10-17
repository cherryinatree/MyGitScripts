using MoreMountains.CorgiEngine;
using MoreMountains.InventoryEngine;
using NUnit.Framework;
using UnityEngine;

public class CharacterInventoryInteract : CharacterAbility
{
    //these are the UIs that need to be toggled
    public GameObject inventoryDisplay;
    public GameObject buildDisplay;
    public GameObject storageDisplay;
    [HideInInspector]
    public MovableObject storageObject;
    // this is if the player is able to build
    public bool isBuildDisplay = false;
    private bool canToggleInventory = true;

    // these are the abilities that need to be disabled when the inventory is open
    private CharacterJumpBetter characterJump;
    private CharacterDash characterDash;
    private MovementAnimation movementAnimation;
    private PlayerSaveInteraction playerSaveInteraction;
    private LevelSelectController levelSelectController;

    // this is the delay between inputs so the player can't scroll through the inventory too quickly
    private Timer timerInputDelay;
    private float inputDelay = 0.2f;

    // these are the input booleans that the UI will use
    private bool leftPressed = false;
    private bool rightPressed = false;
    private bool upPressed = false;
    private bool downPressed = false;
    private bool xPressed = false;
    private bool yPressed = false;
    private bool aPressed = false;
    private bool bPressed = false;

    private bool dpadDownPressed = false;

    protected override void Initialization()
    {
        base.Initialization();
        // these are the abilities that need to be disabled when the inventory is open
        characterJump = GetComponent<CharacterJumpBetter>();
        characterDash = GetComponent<CharacterDash>();
        movementAnimation = GetComponent<MovementAnimation>();
        playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        levelSelectController = GetComponent<LevelSelectController>();
        //this is the delay between inputs so the player can't scroll through the inventory too quickly
        timerInputDelay = new Timer(inputDelay);

        // assures they are off at the start
        inventoryDisplay.SetActive(false);
        buildDisplay.SetActive(false);
        storageDisplay.SetActive(false);
        MoveObject = null;
    }

    public MovableObject MoveObject 
    { 
        get 
        { 
            return storageObject; 
        } 
        set 
        { 

            storageObject = value; 
        } 
    }


    void Update()
    {
        DiplayActivate(); // the inputs for activing the UI
        InventoryControls(); // the inputs for interacting with the UI
    }

    // this is so the UI can access the player save file
    public PlayerSaveFile GetPlayerSaveFile()
    {
        if (playerSaveInteraction == null)
        {
            playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        }

        return playerSaveInteraction.playerSaveFile;
    }

    // UI controls
    private void InventoryControls()
    {
        ResetBools();
        if (!timerInputDelay.ClockTick()) return;
        if (_horizontalInput < -0.2)
        {
            leftPressed = true;
            timerInputDelay.RestartTimer();
        }
        if (_horizontalInput > 0.2)
        {
            rightPressed = true;
            timerInputDelay.RestartTimer();
        }
        if (_verticalInput < -0.2)
        {
            downPressed = true;
            timerInputDelay.RestartTimer();
        }
        if (_verticalInput > 0.2)
        {
            upPressed = true;
            timerInputDelay.RestartTimer();
        }
        if (_inputManager.JumpButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            aPressed = true;
            //timerInputDelay.RestartTimer();
        }
        if (_inputManager.TimeControlButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            bPressed = true;
            //timerInputDelay.RestartTimer();
        }
        if (_inputManager.DashButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            yPressed = true;
            //timerInputDelay.RestartTimer();
        }
        if (_inputManager.RunButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            xPressed = true;
            //timerInputDelay.RestartTimer();
        }

    }

    // resets the input booleans
    private void ResetBools()
    {
        leftPressed = false;
        rightPressed = false;
        upPressed = false;
        downPressed = false;
        xPressed = false;
        yPressed = false;
        aPressed = false;
        bPressed = false;
    }

    // this checks if the player wants to open the UI
    private void DiplayActivate()
    {
        //Debug.Log(Input.GetJoystickNames().ToString());


        if (_inputManager.SelectButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            InventoryDisplaySwitch();
        }
        if (_inputManager.SwitchWeaponButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonUp)
        {
            BuildDisplaySwitch();
        }

      /*  if (_inputManager.DpadHorizontal > 0.2f)
        {
            Debug.Log("DpadRight");
        }
      
       This is being left here to show the code. To use the dpad,
       it needs to be treated like an axis.
       
       */
        if (dpadDownPressed && _inputManager.DpadVerticle > -0.2f)
        {
            Debug.Log("DpadDown");
            dpadDownPressed = false;
            if (MoveObject != null)
            {
                StorageDisplaySwitch();
            }
        }

        if(!dpadDownPressed && _inputManager.DpadVerticle < -0.2f)
        {

            dpadDownPressed = true;
        }

    }
    // switches the inventory display on and off
    private void InventoryDisplaySwitch()
    {
        if (!inventoryDisplay.activeSelf && IsAnyDisplayActive())
        {
            DeactivateAll();
        }
        inventoryDisplay.SetActive(!inventoryDisplay.activeSelf);
        InventoryControlsActive(!IsAnyDisplayActive());
    }
    // switches the build display on and off
    public void BuildDisplaySwitch()
    {
        if (!buildDisplay.activeSelf && IsAnyDisplayActive())
        {
            DeactivateAll();
        }
        buildDisplay.SetActive(!buildDisplay.activeSelf);
        InventoryControlsActive(!IsAnyDisplayActive());
    }

    public void StorageDisplaySwitch()
    {
        if (!inventoryDisplay.activeSelf && IsAnyDisplayActive())
        {
            DeactivateAll();
        }
        inventoryDisplay.SetActive(!inventoryDisplay.activeSelf);
        storageDisplay.SetActive(!storageDisplay.activeSelf);
        InventoryControlsActive(!IsAnyDisplayActive());
    }


    // checks to see if any UI is active
    private bool IsAnyDisplayActive()
    {
        if (inventoryDisplay.activeSelf || buildDisplay.activeSelf)
        {
            return true;
        }
        return false;
    }

    // deactivates all UI

    private void DeactivateAll()
    {

        inventoryDisplay.SetActive(false); 
        buildDisplay.SetActive(false);
        storageDisplay.SetActive(false);

    }



    // prevents the player from moving while the inventory is open
    private void InventoryControlsActive(bool activeState)
    {
        _characterHorizontalMovement.AbilityPermitted = activeState;
        if (levelSelectController != null)
            levelSelectController.AbilityPermitted = activeState;
        if (movementAnimation != null)
            movementAnimation.AbilityPermitted = activeState;
        if (characterJump != null)
            characterJump.AbilityPermitted = activeState;
        if (characterDash != null)
            characterDash.AbilityPermitted = activeState;

    }

    // allows the UI to access the input booleans
    public bool LeftPressed() { return leftPressed; }
    public bool RightPressed() { return rightPressed; }
    public bool UpPressed() { return upPressed; }
    public bool DownPressed() { return downPressed; }
    public bool XPressed() { return xPressed; }
    public bool YPressed() { return yPressed; }
    public bool APressed() { return aPressed; }
    public bool BPressed() { return bPressed; }

}
