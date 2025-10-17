using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplay : MonoBehaviour
{
    //public PlayerSaveInteraction playerSaveHolder;
    public CharacterInventoryInteract characterInventory;
    public GameObject inventoryDisplay;
    public GameObject equipmentDisplay;
    public GameObject statsDisplay;
    public GameObject infoDisplay;
    public GameObject quickBarDisplay;
    public GameObject buttonDisplay;

    public GameObject storageDisplay;
    //public GameObject goldDisplay;
    //public TextMeshProUGUI goldText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI infoText;
    public GameObject focusIndicator;
    public bool isInventoryDisplayActive = true;
    public Sprite emptyIcon;

    private Vector2 focus;
    private GameObject[] InteractableWindows;

    private void Start()
    {
        focus = Vector2.zero;
        InteractableWindows = new GameObject[] { inventoryDisplay, equipmentDisplay, quickBarDisplay, storageDisplay };

        if (SaveData.Current.mainData == null)
        {
            SaveData.Current = SerializationManager.Load("Save1") as SaveData;
        }

        UpdateInventoryFeedback();
        UpdateFocusInticator();
        //DisplaySwitch();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
    private void Update()
    {
        RecieveInput();
    }

    private void GetInformation()
    {
        switch (focus.x)
        {
            case 0:
                if(characterInventory.GetPlayerSaveFile().inventory.items.Count <= (int)focus.y) TurnOffInfoDisplay();
                else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y]);
                break;
            case 1:
                if (characterInventory.GetPlayerSaveFile().inventory.QuickItems.Count <= (int)focus.y) TurnOffInfoDisplay();
                else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y]);
                break;
            case 2:
                switch (focus.y)
                {
                    case 0:
                        if (characterInventory.GetPlayerSaveFile().inventory.Weapon == null) TurnOffInfoDisplay();
                        else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.Weapon);
                        break;
                    case 1:
                        if (characterInventory.GetPlayerSaveFile().inventory.Helm == null) TurnOffInfoDisplay();
                        else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.Helm);
                        break;
                    case 2:
                        if (characterInventory.GetPlayerSaveFile().inventory.Armor == null) TurnOffInfoDisplay();
                        else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.Armor);
                        break;
                    case 3:
                        if (characterInventory.GetPlayerSaveFile().inventory.Accessory == null) TurnOffInfoDisplay();
                        else TurnOnInfoDisplay(characterInventory.GetPlayerSaveFile().inventory.Accessory);
                        break;
                }
                break;
            case 3:
                if (characterInventory.MoveObject.items.Count <= (int)focus.y) TurnOffInfoDisplay();
                else TurnOnInfoDisplay(characterInventory.MoveObject.items[(int)focus.y]);
                break;
        }
    }

    private void UpdateStatsInfo()
    {
        PlayerSaveFile playerSaveFile = characterInventory.GetPlayerSaveFile();
        if(playerSaveFile.myCharacterStats.Length == 0) return;
        statsText.text = playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].maxHealth +
            "\n" + playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].maxStamina +
            "\n" + playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].attack +
            "\n" + playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].defense +
            "\n" + playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].magicPower +
            "\n" + playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].magicDefense;
    }

    private void OnEnable()
    {
        UpdateStatsInfo();
    }

    private void TurnOffInfoDisplay()
    {
        infoText.text = "";
        infoDisplay.SetActive(false);
    }

    private void TurnOnInfoDisplay(Item item)
    {
        infoDisplay.SetActive(true);
        infoText.text = item.ItemName +"\n\n" + item.Description;
    }

    private void UpdateInventoryFeedback()
    {

        PlayerSaveFile saveFile = characterInventory.GetPlayerSaveFile();
        for (int i = 0; i < inventoryDisplay.transform.GetChild(0).childCount; i++)
        {
            if (i < saveFile.inventory.items.Count)
            {

                if (saveFile.inventory.items[i] != null)
                {

                    inventoryDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.items[i].IconLocation);
                }
                else
                {
                    inventoryDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = emptyIcon;
                }
            }
            else
            {

                inventoryDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = emptyIcon;
            }
        }
        for(int i = 0; i <3 ; i++)
        {
            if (i < saveFile.inventory.QuickItems.Count)
            {
                if (saveFile.inventory.QuickItems[i] != null)
                {
                    quickBarDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.QuickItems[i].IconLocation);
                }
                else
                {

                    quickBarDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = emptyIcon;
                }
            }
            else
            {

                quickBarDisplay.transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = emptyIcon;
            }
        }

        if(saveFile.inventory.Weapon != null)
        {
            equipmentDisplay.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.Weapon.IconLocation);
        }
        else
        {
            equipmentDisplay.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = emptyIcon;
        }
        if (saveFile.inventory.Helm != null)
        {

            equipmentDisplay.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.Helm.IconLocation);
        }
        else
        {
            equipmentDisplay.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = emptyIcon;
        }
        if (saveFile.inventory.Armor != null)
        {

            equipmentDisplay.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.Armor.IconLocation);
        }
        else
        {
            equipmentDisplay.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = emptyIcon;
        }
        if (saveFile.inventory.Accessory != null)
        {
            equipmentDisplay.transform.GetChild(0).GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>(saveFile.inventory.Accessory.IconLocation);
        }
        else
        {
            equipmentDisplay.transform.GetChild(0).GetChild(3).GetComponent<Image>().sprite = emptyIcon;
        }

        UpdateStorageDisplay();
        UpdateStatsInfo();
    }

    private void UpdateStorageDisplay()
    {
        if (characterInventory.MoveObject == null) { return; }
        if (characterInventory.MoveObject.items == null) { characterInventory.MoveObject.items = new System.Collections.Generic.List<Item>(); }
        for (int i = 0; i < storageDisplay.transform.GetChild(0).childCount; i++)
        {
            if (i < characterInventory.MoveObject.items.Count)
            {

                if (characterInventory.MoveObject.items[i] != null)
                {

                    storageDisplay.transform.GetChild(0).GetChild(i).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>(characterInventory.MoveObject.items[i].IconLocation);
                }
                else
                {
                    storageDisplay.transform.GetChild(0).GetChild(i).GetChild(0).GetComponent<Image>().sprite = emptyIcon;
                }
            }
            else
            {

                storageDisplay.transform.GetChild(0).GetChild(i).GetChild(0).GetComponent<Image>().sprite = emptyIcon;
            }
        }
    }

    public void DisplaySwitch()
    {
        focus = Vector2.zero;
        inventoryDisplay.SetActive(!inventoryDisplay.activeSelf);
        equipmentDisplay.SetActive(!equipmentDisplay.activeSelf);
        statsDisplay.SetActive(!statsDisplay.activeSelf);
        quickBarDisplay.SetActive(!quickBarDisplay.activeSelf);
        buttonDisplay.SetActive(!buttonDisplay.activeSelf);
        //goldDisplay.SetActive(!goldDisplay.activeSelf);
        focusIndicator.SetActive(!focusIndicator.activeSelf);
        infoDisplay.SetActive(focusIndicator.activeSelf);
        isInventoryDisplayActive = !isInventoryDisplayActive;
        if(isInventoryDisplayActive)
        {
            GetInformation();
            UpdateInventoryFeedback();
            UpdateFocusInticator();
        }
    }

    public void UpdateGoldText()
    {
        //goldText.text = characterInventory.GetPlayerSaveFile().inventory.gold.ToString();
    }

    private void RecieveInput()
    {
        if (characterInventory.LeftPressed())
        {
            Left();
            Debug.Log("Left");
        }
        if (characterInventory.RightPressed())
        {
            Right();
        }
        if (characterInventory.UpPressed())
        {
            Up();
        }
        if (characterInventory.DownPressed())
        {
            Down();
        }
        if (characterInventory.APressed())
        {
            SelectA();
        }
        if (characterInventory.BPressed())
        {
            SelectB();
        }
        if (characterInventory.XPressed())
        {
            SelectX();
        }
        if (characterInventory.YPressed())
        {
            SelectY();
        }
    }

    private void Left()
    {
        ChangeFocus(-1);
        GetInformation();
    }
    private void Right()
    {
        ChangeFocus(1);
        GetInformation();
    }
    private void Up()
    {
        ChangeFocus(-4);
        GetInformation();
    }
    private void Down()
    {
        ChangeFocus(4);
        GetInformation();
    }
    private void SelectA()
    {
        Use();
        UpdateInventoryFeedback();
    }
    private void SelectB()
    {
        Drop();
        UpdateInventoryFeedback();
    }
    private void SelectY()
    {
        Equip();
        UpdateInventoryFeedback();
    }
    private void SelectX()
    {
        Store();
        UpdateInventoryFeedback();
    }

    private void Equip()
    {

        switch (focus.x)
        {
            case 0:
                if(characterInventory.GetPlayerSaveFile().inventory.items.Count <= (int)focus.y) return;
                if (characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y].Class == ItemScript.ItemClass.Consumable)
                {
                    if (SaveInteraction.AddItemToQuickBar(
                        characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.GetPlayerSaveFile()))
                    {
                        SaveInteraction.RemoveItemFromInventory(
                            characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.GetPlayerSaveFile());
                    }
                }
                else if (characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y].Class == ItemScript.ItemClass.Weapon ||
                    characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y].Class == ItemScript.ItemClass.Helm ||
                    characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y].Class == ItemScript.ItemClass.Armor ||
                    characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y].Class == ItemScript.ItemClass.Accessory)
                {

                    if (SaveInteraction.AddItemToEquipment(
                        characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.GetPlayerSaveFile()))
                    {
                        SaveInteraction.RemoveItemFromInventory(
                            characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.GetPlayerSaveFile());
                    }
                }
                else
                {
                    return;
                }
                break;
            case 1:
                if (characterInventory.GetPlayerSaveFile().inventory.QuickItems.Count <= (int)focus.y) return;

                if (SaveInteraction.AddItemToInventory(
                    characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.GetPlayerSaveFile()))
                {
                    SaveInteraction.RemoveItemFromQuickBar(
                        characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.GetPlayerSaveFile());
                }
                break;
            case 2:
                switch (focus.y)
                {
                    case 0:
                        if (characterInventory.GetPlayerSaveFile().inventory.Weapon == null) return;
                        if(SaveInteraction.AddItemToInventory(
                            characterInventory.GetPlayerSaveFile().inventory.Weapon, characterInventory.GetPlayerSaveFile()))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Weapon, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 1:
                        if (characterInventory.GetPlayerSaveFile().inventory.Helm == null) return;
                        if(SaveInteraction.AddItemToInventory(
                            characterInventory.GetPlayerSaveFile().inventory.Helm, characterInventory.GetPlayerSaveFile()))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Helm, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 2:
                        if (characterInventory.GetPlayerSaveFile().inventory.Armor == null) return;
                        if (SaveInteraction.AddItemToInventory(
                            characterInventory.GetPlayerSaveFile().inventory.Armor, characterInventory.GetPlayerSaveFile()))
                        {

                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Armor, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 3:
                        if (characterInventory.GetPlayerSaveFile().inventory.Accessory == null) return;
                        if (SaveInteraction.AddItemToInventory(
                            characterInventory.GetPlayerSaveFile().inventory.Accessory, characterInventory.GetPlayerSaveFile()))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Accessory, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                }break;
            case 3:

                if (characterInventory.MoveObject.items.Count <= (int)focus.y) return;
                if (SaveInteraction.AddItemToInventory(
                    characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.GetPlayerSaveFile()))
                {
                    SaveInteraction.RemoveItemFromStorage(
                        characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.MoveObject);
                }
                break;
        }
    }

    private void Drop()
    {

    }

    private void Use()
    {

    }

    private void Store()
    {

        switch (focus.x)
        {
            case 0:
                if (characterInventory.GetPlayerSaveFile().inventory.items.Count <= (int)focus.y) return;
                if (SaveInteraction.AddItemToStorage(
                    characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.MoveObject))
                {
                    SaveInteraction.RemoveItemFromInventory(
                        characterInventory.GetPlayerSaveFile().inventory.items[(int)focus.y], characterInventory.GetPlayerSaveFile());
                }
                
                break;
            case 1:
                if (characterInventory.GetPlayerSaveFile().inventory.QuickItems.Count <= (int)focus.y) return;

                if (SaveInteraction.AddItemToStorage(
                    characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.MoveObject))
                {
                    SaveInteraction.RemoveItemFromQuickBar(
                        characterInventory.GetPlayerSaveFile().inventory.QuickItems[(int)focus.y], characterInventory.GetPlayerSaveFile());
                }
                break;
            case 2:
                switch (focus.y)
                {
                    case 0:
                        if (characterInventory.GetPlayerSaveFile().inventory.Weapon == null) return;
                        if (SaveInteraction.AddItemToStorage(
                            characterInventory.GetPlayerSaveFile().inventory.Weapon, characterInventory.MoveObject))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Weapon, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 1:
                        if (characterInventory.GetPlayerSaveFile().inventory.Helm == null) return;
                        if (SaveInteraction.AddItemToStorage(
                            characterInventory.GetPlayerSaveFile().inventory.Helm, characterInventory.MoveObject))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Helm, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 2:
                        if (characterInventory.GetPlayerSaveFile().inventory.Armor == null) return;
                        if (SaveInteraction.AddItemToStorage(
                            characterInventory.GetPlayerSaveFile().inventory.Armor, characterInventory.MoveObject))
                        {

                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Armor, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                    case 3:
                        if (characterInventory.GetPlayerSaveFile().inventory.Accessory == null) return;
                        if (SaveInteraction.AddItemToStorage(
                            characterInventory.GetPlayerSaveFile().inventory.Accessory, characterInventory.MoveObject))
                        {
                            SaveInteraction.RemoveItemFromEquipment(
                                characterInventory.GetPlayerSaveFile().inventory.Accessory, characterInventory.GetPlayerSaveFile());
                        }
                        break;
                }
                break;
            case 3:
                break;
        }
    }

    private void ChangeFocus(int direction)
    {
        focus.y += direction;
        if (focus.y < 0)
        {
            focus.x--;
            if (focus.x < 0)
            {
                if (storageDisplay.activeSelf)
                {
                    focus.x = InteractableWindows.Length - 1;
                }
                else
                {
                    focus.x = InteractableWindows.Length - 2;
                }
            }
            focus.y = InteractableWindows[(int)focus.x].transform.GetChild(0).childCount - 1;
        }
        else if (focus.y >= InteractableWindows[(int)focus.x].transform.GetChild(0).childCount)
        {
            focus.x++;
            int interactLength = InteractableWindows.Length;
            if (!storageDisplay.activeSelf)
            {
                interactLength--;
            }
            
            if (focus.x >= interactLength)
            {
                focus.x = 0;
            }
            focus.y = 0;
        }
        UpdateFocusInticator();
    }

    private void UpdateFocusInticator()
    {
        focusIndicator.transform.position = InteractableWindows[(int)focus.x].transform.GetChild(0).GetChild((int)focus.y).position;
    }
}
