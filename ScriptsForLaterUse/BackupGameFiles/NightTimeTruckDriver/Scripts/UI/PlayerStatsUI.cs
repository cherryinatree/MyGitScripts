using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider gasSlider;
    public Slider hungerSlider;
    public Slider sanitySlider;
    public Slider speedSlider;
    public Slider rpmSlider;

    [Header("Text Stats")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI payRateText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI bonusText;

    [Header("References")]
    public TruckControllerMain truck;
    [HideInInspector]
    public PlayerData playerStats;
    public Transform player;
    public Transform deliveryTarget;

    private void Start()
    {
        playerStats = SaveSingleton.Instance.truckStats; // Get the player stats from the singleton
        Transform display = GameObject.Find("Truck").transform.Find("Monitor").Find("Canvas").Find("Display");
        player = GameObject.Find("Truck").transform;
        truck = GetComponent<TruckControllerMain>();
        if (display != null)
        {
            gasSlider = display.Find("GasSlider").GetComponent<Slider>();
            hungerSlider = display.Find("HungerSlider").GetComponent<Slider>();
            sanitySlider = display.Find("SanitySlider").GetComponent<Slider>();
            speedSlider = display.Find("SpeedSlider").GetComponent<Slider>();
            rpmSlider = display.Find("RPMSlider").GetComponent<Slider>();

            distanceText = display.Find("DistanceText").Find("Text").GetComponent<TextMeshProUGUI>();
            moneyText = display.Find("MoneyText").Find("Text").GetComponent<TextMeshProUGUI>();
            payRateText = display.Find("PayRateText").Find("Text").GetComponent<TextMeshProUGUI>();
            gearText = display.Find("GearText").Find("Text").GetComponent<TextMeshProUGUI>();
            bonusText = display.Find("BonusText").Find("Text").GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("Display not found in Truck Monitor!");
        }
        speedSlider.maxValue = 120;
        rpmSlider.maxValue = 6000; // Assuming max RPM is 6000
        sanitySlider.maxValue = 100; // Assuming max sanity is 100
        gasSlider.maxValue = 100;
        hungerSlider.maxValue = 100; // Assuming max hunger is 100


    }

    void Update()
    {
        
        // Slider values
        //Debug.Log(playerStats);
        gasSlider.value = playerStats.gas;
        hungerSlider.value = playerStats.hunger;
        sanitySlider.value = playerStats.sanity;
        speedSlider.value = truck.GetCurrentSpeedMph();
        rpmSlider.value = truck.GetCurrentRPM();


        // Text fields
        distanceText.text = $"Distance: {Vector3.Distance(player.position, deliveryTarget.position):F0}m";
        moneyText.text = $"Money: ${playerStats.money:F2}";
        payRateText.text = $"Pay/mile: ${playerStats.payPerMile:F2}";
        gearText.text = $"Gear: {truck.GetCurrentGear()}";
        bonusText.text = $"Bonus/mile: ${playerStats.bonusPerMile:F2}";
    }
}