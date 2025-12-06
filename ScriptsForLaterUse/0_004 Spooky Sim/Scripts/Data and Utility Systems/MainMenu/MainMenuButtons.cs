using UnityEngine;
using UnityEngine.Events;



public class MainMenuButtons : MonoBehaviour
{
    public GameObject SavesPanel;

    SaveSlotManager[] saveSlots;
    private void Start()
    {
        saveSlots = SavesPanel.GetComponentsInChildren<SaveSlotManager>();
        SavesPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Continue()
    {
    }

    public void Settings()
    {

    }

    public void NewGame()
    {
        SavesPanel.SetActive(!SavesPanel.activeSelf);
        foreach (var slot in saveSlots)
        {
            slot.isNewGame = true;
        }
    }
    public void LoadGame()
    {
        SavesPanel.SetActive(!SavesPanel.activeSelf);
        foreach (var slot in saveSlots)
        {
            slot.isNewGame = false;
        }
    }
}
