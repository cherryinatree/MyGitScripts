using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{

    public static HUDController Instance { get; private set; }

    [Header("References")]
    public TMP_Text objectivesText;
    public TMP_Text progressText;
    public Slider healthBar;

    private int deliveriesCompleted = 0;
    private int totalDeliveries = 10;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        UpdateObjective("Deliver coordinates to artillery.");
        UpdateProgress();
        SetHealth(1f); // full health
    }

    #region Objective
    public void UpdateObjective(string newObjective)
    {
        if (objectivesText != null)
            objectivesText.text = newObjective;
    }
    #endregion

    #region Progress
    public void SetTotalDeliveries(int total)
    {
        totalDeliveries = total;
        UpdateProgress();
    }

    public void IncrementDelivery()
    {
        deliveriesCompleted = Mathf.Clamp(deliveriesCompleted + 1, 0, totalDeliveries);
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        if (progressText != null)
            progressText.text = $"Deliveries: {deliveriesCompleted}/{totalDeliveries}";
    }
    #endregion

    #region Health
    // healthValue = 0-1
    public void SetHealth(float healthValue)
    {
        if (healthBar != null)
            healthBar.value = Mathf.Clamp01(healthValue);
    }
    #endregion
}
