using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float StaminaRegenRate = 1f;
    public float StaminaDepletionRate = 8f;
    public float StaminaCutThreshold = 0.5f;
    public float StaminaActivateThreshold = 0.5f;
    private float stamina = 10f;
    private float maxStamina = 10f;

    private PlayerSaveFile playerSaveFile;

    public float Stamina
    {
        get
        {
            return stamina;
        }
        set
        {
            stamina = value;
        }
    }

    public float MaxStamina
    {
        get
        {
            return maxStamina;
        }
        set
        {
            maxStamina = value;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerSaveFile = GetComponent<PlayerSaveInteraction>().GetPlayerSaveFile();
        maxStamina = playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].maxStamina;
        stamina = playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].stamina;
    }

    // Update is called once per frame
    void Update()
    {
        if(stamina < maxStamina)
        {
            stamina += StaminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public void DepleteStamina(float amount)
    {
        stamina -= amount;
    }
}
