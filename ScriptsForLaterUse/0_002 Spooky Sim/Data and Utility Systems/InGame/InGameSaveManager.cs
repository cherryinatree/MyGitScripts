using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameSaveManager : MonoBehaviour
{
    CurrentFile currentFile;
    WorldObjectSaveMaster worldObjectSaveMaster;
    private bool uloadingScene = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        worldObjectSaveMaster = GetComponent<WorldObjectSaveMaster>();
        currentFile = (CurrentFile)SerializationManager.LoadDefaultPath("currentFile");
        SaveData.Current = new SaveData();
        Debug.Log("Loading save file: " + currentFile.currentFileName);
        SaveData.Current.mainData = (MainSaveFile)SerializationManager.LoadDefaultPath(currentFile.currentFileName);
        Debug.Log("worldObjectSaveMaster: " + currentFile.currentFileName);
        if (SaveData.Current.mainData != null && worldObjectSaveMaster != null)
        {
            Debug.Log("Restoring world objects from save data. 1");
            LoadObjects();
        }
    }



    private void LoadObjects()
    {
        if (SaveData.Current.mainData.storeData == null) return;
        Debug.Log("Restoring world objects from save data. 2");
        if (SaveData.Current.mainData.storeData.objects == null) return;
        Debug.Log("Restoring world objects from save data. 3");
        if (SaveData.Current.mainData.storeData.objects.Count <= 0) return;
      
        Debug.Log("Restoring world objects from save data. 4");
        worldObjectSaveMaster.RestoreFromSave(SaveData.Current.mainData.storeData);
    }

    public void SaveGame()
    {
        currentFile.currentFileName = SaveData.Current.mainData.saveName;
        SaveData.Current.mainData.loadSceneData.currentSceneName = SceneManager.GetActiveScene().name;
        if (worldObjectSaveMaster != null)
        {
            Debug.Log("Saving world objects to save data.");
            SaveData.Current.mainData.storeData = worldObjectSaveMaster.BuildSnapshot();
        }

        SerializationManager.Save(currentFile.currentFileName, SaveData.Current.mainData);
    }

    public void LoadGame()
    {
       SceneManager.LoadScene(SaveData.Current.mainData.loadSceneData.currentSceneName, LoadSceneMode.Single);
        // Implement load logic here
    }
}
