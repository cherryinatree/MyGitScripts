using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;


public class PlayerHUD : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI nameText;
    public GameObject[] ability;
    public GameObject player;
    public Slider healthSlider;
    public Slider staminaSlider;
    public GameObject dashIcon;

    private MoreMountains.CorgiEngine.Health health;
    private PlayerSkills playerSkills;
    private CharacterRunBetter run;
    private CharacterDash dash;
    private GameObject[] abilityHighlight;
    private Slider[] abilityCooldown;

    private bool loaded = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }



    // Update is called once per frame
    void Update()
    {
        if (!loaded)
        {
            Loaded();
            loaded = true;
        }
        UpdateHealth();
        UpdateAbilityCooldown();
        HilgihtAbility();
        UpdateStamina();
        UpdateDash();
    }

    private void Loaded()
    {
        health = player.GetComponent<MoreMountains.CorgiEngine.Health>();
        playerSkills = player.GetComponent<PlayerSkills>();
        run = player.GetComponent<CharacterRunBetter>();
        dash = player.GetComponent<CharacterDash>();
        abilityHighlight = new GameObject[3];
        abilityCooldown = new Slider[3];
        //healthSlider.maxValue = health.MaximumHealth;
        //healthSlider.value = health.CurrentHealth;
        //staminaSlider.maxValue = run.MaxStamina;
        //staminaSlider.value = run.Stamina;
        for (int i = 0; i < 3; i++)
        {
            abilityHighlight[i] = ability[i].transform.Find("Highlight").gameObject;
            abilityCooldown[i] = ability[i].GetComponent<Slider>();
            abilityCooldown[i].maxValue = playerSkills.CooldownTime[i];
            abilityCooldown[i].value = 0;
        }
    }

    private void UpdateDash()
    {
        if (dash.DashConditions())
        {
            dashIcon.SetActive(true);
        }
        else
        {

           dashIcon.SetActive(false);
        }
    }

    private void UpdateStamina()
    {
        //staminaSlider.value = run.Stamina;
    }

    private void UpdateHealth()
    {


        healthSlider.value = health.CurrentHealth;
        healthText.text = health.CurrentHealth.ToString() +"/" + health.MaximumHealth.ToString();
    }

    private void UpdateAbilityCooldown()
    {
        for(int i = 0; i < playerSkills.abilities.Length; i++)
        {
            float cooldownTimeLeft = playerSkills.CooldownTime[i] - playerSkills.CooldownTimers[i].currentTime;
            if(cooldownTimeLeft < 0)
            {
                cooldownTimeLeft = 0;
            }
            abilityCooldown[i].value = cooldownTimeLeft;
        }
    }

    private void HilgihtAbility()
    {
        for (int i = 0; i < playerSkills.abilities.Length; i++)
        {
            if (playerSkills.currentAbilityIndex == i)
            {
                abilityHighlight[i].SetActive(true);
            }
            else
            {
                abilityHighlight[i].SetActive(false);
            }
        }
    }
}
