using Cherry.Combat;
using SUPERCharacter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsHUD : MonoBehaviour
{
    public Cherry.Combat.PlayerHealth health;
    public Slider healthSlider;


    public PlayerBattery battery;
    public Slider batterySlider;

    public TextMeshProUGUI spaceBucks;

    public SUPERCharacterAIO super;
    public Slider staminaSlider;

    void OnEnable() 
    {
        health.OnHealthChanged += HandleHealth;
        battery.OnBatteryChanged += HandleBattery;

        healthSlider.maxValue = health.MaxHealth;
        healthSlider.value = health.CurrentHealth;

        batterySlider.maxValue = battery.MaxBattery;
        batterySlider.value = battery.CurrentBattery;

        staminaSlider.maxValue = super.Stamina;
        staminaSlider.value = super.currentStaminaLevel;
    }

    void OnDisable()
    {
        health.OnHealthChanged -= HandleHealth;
        battery.OnBatteryChanged -= HandleBattery;
    }


    private void Update()
    {
        HeandleSpaceBucks();
        HandleStamina();
    }

    void HandleStamina()
    {

        staminaSlider.value = super.currentStaminaLevel;
    }

    void HeandleSpaceBucks()
    {
        spaceBucks.text = SaveData.Current.mainData.playerData.money.ToString("N2");
    }

    void HandleHealth(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    void HandleBattery(float current, float max, float normalized)
    {
        batterySlider.maxValue = max;
        batterySlider.value = current;
    }
}
