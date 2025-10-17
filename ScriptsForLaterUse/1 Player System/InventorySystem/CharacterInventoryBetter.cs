using MoreMountains.CorgiEngine;
using UnityEngine;

public class CharacterInventoryBetter : CharacterAbility
{

    public InventoryDisplay inventoryDisplay;
    private bool canToggleInventory = true;

    //private CharacterHorizontalMovement _characterHorizontalMovement;
    private CharacterJumpBetter characterJump;
    private CharacterDash characterDash;
    private MovementAnimation movementAnimation;
    private PlayerSaveInteraction playerSaveInteraction;

    private Timer timerInputDelay;
    private float inputDelay = 0.2f;
    
    protected override void Initialization()
    {
        base.Initialization();
        characterJump = GetComponent<CharacterJumpBetter>();
        characterDash = GetComponent<CharacterDash>();
        movementAnimation = GetComponent<MovementAnimation>();
        playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        timerInputDelay = new Timer(inputDelay);
    }
    // Update is called once per frame
    void Update()
    {
        InventoryActivate();
        //InventoryControls();

        if (_inputManager.TimeControlButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonPressed)
        {
            Debug.Log("Time Control Button Pressed");
        }
    }

    private void AddStats(Item equipment)
    {
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].maxHealth += equipment.health;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].maxStamina += equipment.stamina;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].attack += equipment.attack;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].defense += equipment.defense;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].magicPower += equipment.magic;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].magicDefense += equipment.magicDefense;
    }

    private void RemoveStats(Item equipment)
    {

        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].maxHealth -= equipment.health;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].maxStamina -= equipment.stamina;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].attack -= equipment.attack;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].defense -= equipment.defense;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].magicPower -= equipment.magic;
        playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].magicDefense -= equipment.magicDefense;
    }

    public bool AddItemToInventory(Item item)
    {
        if(playerSaveInteraction.playerSaveFile.inventory.items.Count >= 20) return false;

        playerSaveInteraction.playerSaveFile.inventory.items.Add(item);
        return true;
    }
    public bool AddItemToEquipment(Item item)
    {
        if (item.Class == ItemScript.ItemClass.Weapon && playerSaveInteraction.playerSaveFile.inventory.Weapon == null)
        {
            playerSaveInteraction.playerSaveFile.inventory.Weapon = item;
        }
        else if (item.Class == ItemScript.ItemClass.Helm && playerSaveInteraction.playerSaveFile.inventory.Helm == null)
        {
            playerSaveInteraction.playerSaveFile.inventory.Helm = item;
        }
        else if (item.Class == ItemScript.ItemClass.Armor && playerSaveInteraction.playerSaveFile.inventory.Armor == null)
        {
            playerSaveInteraction.playerSaveFile.inventory.Armor = item;
        }
        else if(item.Class == ItemScript.ItemClass.Accessory && playerSaveInteraction.playerSaveFile.inventory.Accessory == null)
        {
            playerSaveInteraction.playerSaveFile.inventory.Accessory = item;
        } else
        {
            return false;
        }

        AddStats(item);
        return true;
    }
    public bool AddItemToQuickBar(Item item)
    {
        if(item.Class != ItemScript.ItemClass.Consumable) return false;
        if (playerSaveInteraction.playerSaveFile.inventory.QuickItems.Count >= 3) return false;

        playerSaveInteraction.playerSaveFile.inventory.QuickItems.Add(item);
        return true;
    }

    public bool RemoveItemFromInventory(Item item)
    {
        if (playerSaveInteraction.playerSaveFile.inventory.items.Contains(item))
        {
            playerSaveInteraction.playerSaveFile.inventory.items.Remove(item);
            return true;
        }
        return false;
    }

    public bool RemoveItemFromQuickBar(Item item)
    {
        if (playerSaveInteraction.playerSaveFile.inventory.QuickItems.Contains(item))
        {
            playerSaveInteraction.playerSaveFile.inventory.QuickItems.Remove(item);
            return true;
        }
        return false;
    }

    public bool RemoveItemFromEquipment(Item item)
    {
        if (item.Class == ItemScript.ItemClass.Weapon && playerSaveInteraction.playerSaveFile.inventory.Weapon == item)
        {
            playerSaveInteraction.playerSaveFile.inventory.Weapon = null;
            RemoveStats(item);
            return true;
        }
        else if (item.Class == ItemScript.ItemClass.Helm && playerSaveInteraction.playerSaveFile.inventory.Helm == item)
        {
            playerSaveInteraction.playerSaveFile.inventory.Helm = null;
            RemoveStats(item);
            return true;
        }
        else if (item.Class == ItemScript.ItemClass.Armor && playerSaveInteraction.playerSaveFile.inventory.Armor == item)
        {
            playerSaveInteraction.playerSaveFile.inventory.Armor = null;
            RemoveStats(item);
            return true;
        }
        else if (item.Class == ItemScript.ItemClass.Accessory && playerSaveInteraction.playerSaveFile.inventory.Accessory == item)
        {
            playerSaveInteraction.playerSaveFile.inventory.Accessory = null;
            RemoveStats(item);
            return true;
        }


        return false;
    }


    public PlayerSaveFile GetPlayerSaveFile()
    {
        return playerSaveInteraction.playerSaveFile;
    }
    /*
    private void InventoryControls()
    {
        if (!timerInputDelay.ClockTick()) return;
        if(inventoryDisplay.isInventoryDisplayActive)
        {
            if(_horizontalInput <-0.2)
            {
                inventoryDisplay.Left();
                timerInputDelay.RestartTimer();
            }
            if (_horizontalInput > 0.2)
            {
                inventoryDisplay.Right();
                timerInputDelay.RestartTimer();
            }
            if (_verticalInput < -0.2)
            {
                inventoryDisplay.Down();
                timerInputDelay.RestartTimer();
            }
            if (_verticalInput > 0.2)
            {
                inventoryDisplay.Up();
                timerInputDelay.RestartTimer();
            }
            if (_inputManager.JumpButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonPressed)
            {
                inventoryDisplay.SelectA();
                timerInputDelay.RestartTimer();
            }
            if(_inputManager.TimeControlButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonPressed)
            {
                inventoryDisplay.SelectB();
                timerInputDelay.RestartTimer();
            }
            if (_inputManager.DashButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonPressed)
            {
                inventoryDisplay.SelectY();
                timerInputDelay.RestartTimer();
            }
        }
    }*/

    private void InventoryControlsActive(bool activeState)
    {
        _characterHorizontalMovement.AbilityPermitted = activeState;
        characterJump.AbilityPermitted = activeState;
        characterDash.AbilityPermitted = activeState;
        movementAnimation.AbilityPermitted = activeState;
        
    }

    private void InventoryActivate()
    {
        if(_inputManager.SelectButton.State.CurrentState == MoreMountains.Tools.MMInput.ButtonStates.ButtonPressed)
        {
            DisplaySwitch();
            canToggleInventory = false;
        }
        else
        {

            canToggleInventory = true;
        }

    }

    private void DisplaySwitch()
    {
        if(!canToggleInventory)
        {
            return;
        }
        inventoryDisplay.DisplaySwitch();
        InventoryControlsActive(!inventoryDisplay.isInventoryDisplayActive);
        Debug.Log("Display Switched");
    }
}
