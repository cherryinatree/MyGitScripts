using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[System.Serializable]
public class MainMenuEvent : UnityEvent<GameObject> { }

public class SaveSlotManager : MonoBehaviour
{
    public string slotName;
    public string loadSceneName;
    public TextMeshProUGUI slotText;
    public Image slotImage;
    public Button slotButton;
    public Button deleteButton;

    MainSaveFile saveSlotSaveFile;
    public UnityEvent NewGameSaveSetup;
    public UnityEvent LoadSaveFileSetup;

    public bool isNewGame = true;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        saveSlotSaveFile = (MainSaveFile)SerializationManager.Load(Application.persistentDataPath + "/saves/" + slotName + ".save");
        UpdateSlotDisplay();
        slotButton.onClick.AddListener(OnSlotButtonPressed);
    }

    private void UpdateSlotDisplay()
    {
        if (saveSlotSaveFile != null)
        {
            slotText.text = saveSlotSaveFile.saveName;
            // Update other UI elements as needed
        }
        else
        {
            slotText.text = "Empty Slot";
            // Update other UI elements as needed
        }
    }


    public void OnSlotButtonPressed()
    {
        if (!isNewGame)
        {
            LoadGame();
        }
        else
        {
            NewGame();
        }
    }

    private void LoadGame()
    {
        LoadSaveFileSetup.Invoke();
        SceneManager.LoadScene(loadSceneName);
        // Logic to load the existing save file
    }

    private void NewGame()
    {
        NewGameSaveSetup.Invoke();
        SceneManager.LoadScene(loadSceneName);
       // Logic to start a new game and create a new save file
    }


}
