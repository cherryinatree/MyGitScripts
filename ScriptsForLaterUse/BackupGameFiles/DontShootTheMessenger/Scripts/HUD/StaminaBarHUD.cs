using UnityEngine;
using UnityEngine.UI;

public class StaminaBarHUD : MonoBehaviour
{
    [Header("References")]
    public SprintAction sprintAction;  // Reference to your SprintAction script
    public Slider staminaSlider;       // Reference to the UI Slider

    void Start()
    {
        if (staminaSlider == null)
        {
            Debug.LogError("Stamina Slider not assigned!");
        }

        if (sprintAction == null)
        {
            Debug.LogError("SprintAction reference not assigned!");
        }

        // Initialize slider
        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = 1f;
        staminaSlider.value = 1f;
    }

    void Update()
    {
        if (sprintAction != null && staminaSlider != null)
        {
            // Get normalized stamina and update the slider
            staminaSlider.value = sprintAction.GetStaminaNormalized();
        }
    }
}
