using UnityEngine;

public class SetUpNewSave : MonoBehaviour
{
    public void NewSave(string saveName)
    {
        MainSaveFile newSave = new MainSaveFile();
        newSave.saveName = saveName;
        newSave.playerData = new PlayerData();
        newSave.progressionData = new ProgressionSave();
        newSave.storeData = new StoreSave();
        newSave.loadSceneData = new LoadSceneData();

        CurrentFile currentFile = new CurrentFile();
        currentFile.currentFileName = saveName;

        SerializationManager.Save("saves", saveName, "save", newSave);
        SerializationManager.Save("saves", "currentFile", "save", currentFile);
        Debug.Log("New save created: " + saveName);
    }

    public void LoadSave(string saveName) { 
        CurrentFile currentFile = new CurrentFile();
        currentFile.currentFileName = saveName;

        SerializationManager.Save("saves", "currentFile", "save", currentFile);
        Debug.Log("Save loaded: " + saveName);
    }
}
