using MoreMountains.CorgiEngine;
using UnityEngine;
using static MoreMountains.CorgiEngine.Character;

public class CharacterStatus : CharacterAbility
{
    public int facingDirection = 1;

    private AbilityList abilityList;
    public Abilities[] abilities;
    private PlayerSaveInteraction playerSaveInteraction;
    public int currentAbilityIndex = 0;
    private bool loadAbilities = false;
    public float horizontalInput;
    public float verticalInput;

    protected override void Initialization()
    {
        base.Initialization();
        horizontalInput = _horizontalInput;
        verticalInput = _verticalInput;
    }

    private void Update()
    {
        horizontalInput = _horizontalInput;
        verticalInput = _verticalInput;
        if (!loadAbilities)
        {
            LoadVariables();
            loadAbilities = true;
        }
        FacingDirection();
    }


    private void LoadVariables()
    {
        playerSaveInteraction = GetComponent<PlayerSaveInteraction>();
        abilities = new Abilities[3];
        abilityList = Resources.Load<AbilityList>("Scripts/PlayerController/Abilities/AbilityList/AllAbilities");



        for (int i = 0; i < 3; i++)
        {
            abilities[i] = abilityList.abilities[
                playerSaveInteraction.playerSaveFile.myCharacterStats[playerSaveInteraction.playerSaveFile.SelectedCharacterIndex].abilitiesID[i]];
        }
    }

    private void FacingDirection()
    {
        if (_horizontalInput < 0)
        {
            facingDirection = -1;
        }
        else if (_horizontalInput > 0)
        {
            facingDirection = 1;
        }
    }


}
