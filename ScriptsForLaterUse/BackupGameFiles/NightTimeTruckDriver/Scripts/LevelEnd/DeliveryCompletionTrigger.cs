using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeliveryCompletionTrigger : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject completionUI;
    public TMP_Text summaryText;

    [Header("Game Stats")]
    public PlayerData truckStats; // Reference to your truck stats script
    public Transform startPosition;

    [Header("Next Level Settings")]
    public string nextSceneName = ""; // Leave empty if not loading next scene

    private bool completed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        if (other.CompareTag("Player"))
        {
            SaveSingleton.Instance.truckStats.distanceTraveled = Vector3.Distance(startPosition.position, transform.position);

            completed = true;

            ShowCompletionUI();
            Time.timeScale = 0f; // Pause game (optional)
        }
    }

    void ShowCompletionUI()
    {  
        // Unlock and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        truckStats = SaveSingleton.Instance.truckStats; // Get the truck stats from the singleton
        if (completionUI != null)
        {
            completionUI.SetActive(true);
        }

        if (summaryText != null && truckStats != null)
        {
            summaryText.text =
                $"Delivery Complete!\n" +
                $"Distance: {truckStats.distanceTraveled:F1} miles\n" +
                $"Pay Rate: ${truckStats.payPerMile:F2}/mile\n" +
                $"Bonus: ${truckStats.bonusPerMile:F2}/mile\n" +
                $"Total Earned: ${(truckStats.distanceTraveled * (truckStats.payPerMile + truckStats.bonusPerMile)):F2}";
        }

        SaveSingleton.Instance.truckStats.money += (truckStats.distanceTraveled * (truckStats.payPerMile + truckStats.bonusPerMile));
        SerializationManager.Save("main2", SaveSingleton.Instance.truckStats); // Save the game state
    }

    public void ContinueToNextScene()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name); // Unload current scene
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("Next scene not set.");
        }
    }
}
