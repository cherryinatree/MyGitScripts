using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BuilderDisplay : MonoBehaviour
{
    public CharacterInventoryInteract characterInventory;
    public Construction characterBuilder;

    public GameObject Tabs;
    public GameObject Items;
    public GameObject CostPanel;
    public GameObject BenifitPanel;
    public TextMeshProUGUI pageText;
    public GameObject Arrows;
    private int page = 0;
    private int maxPage = 0;
    private int currentTab = 0;

    private Vector2 focus;
    private GameObject[] InteractableWindows;
    public GameObject focusIndicator;

    public int rowLength = 4;
    public int totalPerPage = 10;

    private List<BuildObject> TownObjects;
    private List<BuildObject> UtilityObjects;
    private List<BuildObject> DecorativeObjects;

    private List<GameObject> BuildIcons;

    public BuildObject DefaultBuildObjectTown;
    public BuildObject DefaultBuildObjectUtility;
    public BuildObject DefaultBuildObjectDecoration;

    public BuildObjectList allBuildObjects;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        InteractableWindows = new GameObject[] { Tabs, Items, Arrows };
        if (SaveData.Current.mainData == null)
        {
            SaveData.Current = SerializationManager.Load("Save1") as SaveData;
        }

        LoadIcons();
        InitalHighlight();
        LoadBuildObjects();
        LoadPage();
        UpdateMaxPage();
        UpdatePageText();
    }

    // Update is called once per frame
    void Update()
    {
        RecieveInput();
    }

    private void UpdateMaxPage()
    {
        switch (currentTab)
        {
            case 0:
                maxPage = Mathf.CeilToInt(TownObjects.Count / totalPerPage);
                break;
            case 1:
                maxPage = Mathf.CeilToInt(UtilityObjects.Count / totalPerPage);
                break;
            case 2:
                maxPage = Mathf.CeilToInt(DecorativeObjects.Count / totalPerPage);
                break;
            default:
                break;
        }
    }

    private void UpdatePageText()
    {
        pageText.text = (page + 1) + "/" + (maxPage + 1);
    }

    private void LoadIcons()
    {
        BuildIcons = new List<GameObject>();
        for (int i = 0; i < Items.transform.childCount; i++)
        {
            BuildIcons.Add(Items.transform.GetChild(i).gameObject);
        }
    }

    private void LoadPage()
    {
        for (int i = 0; i < BuildIcons.Count; i++)
        {
            Items.transform.GetChild(i).Find("Icon").gameObject.GetComponent<Image>().sprite = null;
        }
        switch (currentTab)
        {
            case 0:
                LoadTabPage(TownObjects);
                break;
            case 1:
                LoadTabPage(UtilityObjects);
                break;
            case 2:
                LoadTabPage(DecorativeObjects);
                break;
            default:
                break;
        }
        UpdatePageText();
    }

    private void LoadTabPage(List<BuildObject> builds)
    {
        for(int i = 0; i < totalPerPage; i++)
        {
            if (i + (page * totalPerPage) < builds.Count)
            {
                BuildIcons[i].transform.Find("Icon").GetComponent<Image>().sprite = builds[i + (page * totalPerPage)].buildSprite;
            }
        }
    }

    private void LoadBuildObjects()
    {
        if (SaveData.Current.mainData.buildObjectsID == null)
        {
            SaveData.Current.mainData.buildObjectsID = new List<int>();

            SaveData.Current.mainData.buildObjectsID.Add(DefaultBuildObjectUtility.buildID);
            SaveData.Current.mainData.buildObjectsID.Add(1);
            SaveData.Current.mainData.buildObjectsID.Add(DefaultBuildObjectDecoration.buildID);
            for (int i = 0; i < 12; i++)
            {
                SaveData.Current.mainData.buildObjectsID.Add(DefaultBuildObjectTown.buildID);
            }
            for (int i = 0; i < 12; i++)
            {
                SaveData.Current.mainData.buildObjectsID.Add(DefaultBuildObjectDecoration.buildID);
            }
            for (int i = 0; i < 12; i++)
            {
                SaveData.Current.mainData.buildObjectsID.Add(DefaultBuildObjectTown.buildID);
            }
        }
        TownObjects = new List<BuildObject>();
        UtilityObjects = new List<BuildObject>();
        DecorativeObjects = new List<BuildObject>();

        foreach (int buildObject in SaveData.Current.mainData.buildObjectsID)
        {
            BuildObject buildMe = allBuildObjects.buildObjects[0];
            foreach(BuildObject build in allBuildObjects.buildObjects)
            {
                if(build.buildID == buildObject)
                {
                    buildMe = build;
                    break;
                }
            }

            switch (buildMe.buildType)
            {
                case BuildObject.BuildType.Town:
                    TownObjects.Add(buildMe);
                    break;
                case BuildObject.BuildType.Utility:
                    UtilityObjects.Add(buildMe);
                    break;
                case BuildObject.BuildType.Decoration:
                    DecorativeObjects.Add(buildMe);
                    break;
            }
        }
    }

    private void ChangeFocus(int direction)
    {
        DeHighlight();
        focus.y += direction;
        if (focus.y < 0)
        {
            focus.x--;
            if (focus.x < 0)
            {
                focus.x = InteractableWindows.Length - 1;
            }
            focus.y = InteractableWindows[(int)focus.x].transform.childCount - 1;
        }
        else if (focus.y >= InteractableWindows[(int)focus.x].transform.childCount)
        {
            focus.x++;
            if (focus.x >= InteractableWindows.Length)
            {
                focus.x = 0;
            }
            focus.y = 0;
        }
        Highlight();
    }

    private void Left()
    {
        ChangeFocus(-1);
    }
    private void Right()
    {
        ChangeFocus(1);
    }
    private void Up()
    {
        ChangeFocus(-rowLength);
    }
    private void Down()
    {
        ChangeFocus(rowLength);
    }
    private void SelectA()
    {
        switch (focus.x)
        {
            case 0:
                currentTab = (int)focus.y;
                page = 0;
                UpdateMaxPage();
                LoadPage();
                break;
            case 1:
                List<BuildObject> SwapList;
                switch (currentTab)
                {
                    case 0:
                        SwapList = TownObjects;
                        break;
                    case 1:
                        SwapList = UtilityObjects;
                        break;
                    case 2:
                        SwapList = DecorativeObjects;
                        break;
                    default:
                        SwapList = TownObjects;
                        break;
                }

                if((int)focus.y + (page * totalPerPage) < SwapList.Count)
                {
                    characterBuilder.StartPlacing(SwapList[(int)focus.y + (page * totalPerPage)]);
                    characterInventory.BuildDisplaySwitch();
                }
                else
                {
                    Debug.Log("No Build Object Selected");
                }
                break;
            case 2:
                if (focus.y == 0)
                {
                    if (page > 0)
                    {
                        page--;
                        LoadPage();
                    }
                }
                else
                {
                    if (page < maxPage)
                    {
                        page++;
                        LoadPage();
                    }
                }
                break;
        }
    }
    private void SelectB()
    {
    }
    private void SelectX()
    {
    }
    private void SelectY()
    {
    }

    

    private void RecieveInput()
    {
        if (characterInventory.LeftPressed())
        {
            Left();
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
    private void Highlight()
    {
        //focusIndicator.transform.position = InteractableWindows[(int)focus.x].transform.GetChild((int)focus.y).position;
        InteractableWindows[(int)focus.x].transform.GetChild((int)focus.y).Find("Highlight").gameObject.SetActive(true);
    }
    private void DeHighlight()
    {
        //focusIndicator.transform.position = InteractableWindows[(int)focus.x].transform.GetChild((int)focus.y).position;
        InteractableWindows[(int)focus.x].transform.GetChild((int)focus.y).Find("Highlight").gameObject.SetActive(false);
    }

    private void InitalHighlight()
    {

        for (int i = 0; i < InteractableWindows.Length; i++)
        {
            for (int j = 0; j < InteractableWindows[i].transform.childCount; j++)
            {
                InteractableWindows[i].transform.GetChild(j).Find("Highlight").gameObject.SetActive(false);
            }
        }

        Highlight();
    }
}
