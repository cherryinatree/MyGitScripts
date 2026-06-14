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
        //Debug.Log("Loading save file: " + currentFile.currentFileName);
        SaveData.Current.mainData = (MainSaveFile)SerializationManager.LoadDefaultPath(currentFile.currentFileName);
        //Debug.Log("worldObjectSaveMaster: " + currentFile.currentFileName);
        if (SaveData.Current.mainData != null && worldObjectSaveMaster != null)
        {
           // Debug.Log("Restoring world objects from save data. 1");
            LoadObjects();
        }
    }



    private void LoadObjects()
    {
        if (SaveData.Current.mainData.storeData == null) return;
        if (SaveData.Current.mainData.storeData.objects == null) return;
        if (SaveData.Current.mainData.storeData.objects.Count <= 0) return;
      
        worldObjectSaveMaster.RestoreFromSave(SaveData.Current.mainData.storeData);
    }

    public void SaveGame()
    {
        currentFile.currentFileName = SaveData.Current.mainData.saveName;
       // SaveData.Current.mainData.loadSceneData.currentSceneName = SceneManager.GetActiveScene().name;
        if (worldObjectSaveMaster != null)
        {
            SaveData.Current.mainData.storeData = worldObjectSaveMaster.BuildSnapshot();
        }
        if (GetComponent<StoreSurfaceSaveAdapter>())
        {
            GetComponent<StoreSurfaceSaveAdapter>().SaveToSaveData();
        }
        // then whatever you already do to write SaveData to disk

        SerializationManager.Save(currentFile.currentFileName, SaveData.Current.mainData);
        Debug.Log("Game saved to file: " + currentFile.currentFileName);
    }

    public void LoadGame()
    {
       SceneManager.LoadScene(SaveData.Current.mainData.loadSceneData.currentScene.sceneName, LoadSceneMode.Single);
        // Implement load logic here
    }
}
