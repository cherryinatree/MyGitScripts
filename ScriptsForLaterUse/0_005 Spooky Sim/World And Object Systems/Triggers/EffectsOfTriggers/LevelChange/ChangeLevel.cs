using UnityEngine;

public class ChangeLevel : MonoBehaviour
{

    public string levelToChangeTo;

    public void ChangeLevelNow()
    {
        if(SaveData.Current.mainData.loadSceneData.nextScene != null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SaveData.Current.mainData.loadSceneData.nextScene.sceneName);
        }
        else if (levelToChangeTo != "")
        {

            UnityEngine.SceneManagement.SceneManager.LoadScene(levelToChangeTo);
        }
    }
}
