using UnityEngine;
using UnityEngine.UI;
using MoreMountains.CorgiEngine;
using TMPro;

public class PlayerGauges : MonoBehaviour
{
    public GameObject player;

    public Image healthGauge;
    public Image staminaGauge;
    public Image manaGauge;

    private Health playerHealth;
    private PlayerStamina playerStamina;
    private PlayerMana playerMana;

    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI manaText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerHealth = player.GetComponent<Health>();
        playerStamina = player.GetComponent<PlayerStamina>();
        playerMana = player.GetComponent<PlayerMana>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGauges();
        UpdateText();
    }

    private void UpdateGauges()
    {
        healthGauge.fillAmount = playerHealth.CurrentHealth / playerHealth.MaximumHealth;
        staminaGauge.fillAmount = playerStamina.Stamina / playerStamina.MaxStamina;
        manaGauge.fillAmount = playerMana.Mana / playerMana.MaxMana;
    }

    private void UpdateText()
    {
        healthText.text = Mathf.FloorToInt(playerHealth.CurrentHealth).ToString();
        staminaText.text = Mathf.FloorToInt(playerStamina.Stamina).ToString();
        manaText.text = Mathf.FloorToInt(playerMana.Mana).ToString();
    }
}
