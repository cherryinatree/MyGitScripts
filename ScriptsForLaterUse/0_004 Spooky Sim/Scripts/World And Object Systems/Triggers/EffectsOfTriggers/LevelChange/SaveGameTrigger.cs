using UnityEngine;

public class SaveGameTrigger : MonoBehaviour
{

    public LevelDefinition levelToSaveAsCurrent;

   public void SaveGameNow()
    {
        InGameSaveManager saveManager = FindFirstObjectByType<InGameSaveManager>();
        if (saveManager != null)
        {
            if(levelToSaveAsCurrent != null)
            {
                SaveData.Current.mainData.loadSceneData.nextScene = levelToSaveAsCurrent.ConvertToLevelRunSaveData();
                SaveData.Current.mainData.loadSceneData.currentScene = SaveData.Current.mainData.loadSceneData.nextScene;
            }
            FindFirstObjectByType<InGameSaveManager>().SaveGame();
        }
    }
}
