using UnityEngine;
using UnityEngine.SceneManagement;
using Cherry.Spawning;

namespace Cherry.Gameplay
{
    public class LoadToHome : MonoBehaviour
    {
        [SerializeField] private string homeSceneName = "Home";

        public void LoadGameToHome()
        {
            // Your save system loads data, then:
            RespawnRequest.Set(RespawnReason.Load);
            SceneManager.LoadScene(homeSceneName);
        }

        public void RestartDayToHome()
        {
            RespawnRequest.Set(RespawnReason.RestartDay);
            SceneManager.LoadScene(homeSceneName);
        }
    }
}
