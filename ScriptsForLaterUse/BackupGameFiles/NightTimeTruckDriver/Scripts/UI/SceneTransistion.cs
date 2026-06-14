using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransistion : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        //UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name); // Unload current scene
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.Log("Next scene not set.");
        }
    }
}
