using UnityEngine;

public class PlayerMana : MonoBehaviour
{

    [Header("Stamina")]
    public float manaRegenRate = 1f;
    private float mana = 10f;
    private float maxMana = 10f;

    private PlayerSaveFile playerSaveFile;

    public float Mana
    {
        get { return mana; } set { mana = value; }
    }

    public float MaxMana
    {
        get { return maxMana; } set { maxMana = value; }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerSaveFile = GetComponent<PlayerSaveInteraction>().GetPlayerSaveFile();
        MaxMana = playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].maxMana;
        mana = playerSaveFile.myCharacterStats[playerSaveFile.SelectedCharacterIndex].mana;
    }

    // Update is called once per frame
    void Update()
    {

        if (mana < maxMana)
        {
            mana += manaRegenRate * Time.deltaTime;
        }
        else
        {
            mana = maxMana;
        }
    }
}
