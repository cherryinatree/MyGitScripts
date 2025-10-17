using MoreMountains.CorgiEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveInteraction : CharacterAbility
{
    public int playerIndex;
    public PlayerSaveFile playerSaveFile;
    public GameObject[] characters;


    // Start is called before the first frame update
    protected override void Initialization()
    {
        base.Initialization();
        if (SaveData.Current.mainData == null)
        {
            SaveData.Current = SerializationManager.Load("Save1") as SaveData;
        }

        if (!SaveData.Current.mainData.connected[playerIndex])
        {
            gameObject.SetActive(false);
        }
        else
        {
            playerSaveFile = SaveData.Current.mainData.players[playerIndex];
            ChooseCharacter();
        }
    }

    public PlayerSaveFile GetPlayerSaveFile()
    {
        if (SaveData.Current.mainData == null)
        {
            SaveData.Current = SerializationManager.Load("Save1") as SaveData;
            playerSaveFile = SaveData.Current.mainData.players[playerIndex];
            ChooseCharacter();
        }
        return playerSaveFile;
    }

    public GameObject GetActiveCharacter()
    {

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].activeSelf)
            {
                return characters[i];
            }
        }
        return null;
    }



    private void ChooseCharacter()
    {
        characters = new GameObject[_character.CharacterModel.transform.childCount];

        for (int i = 0; i < _character.CharacterModel.transform.childCount; i++)
        {
            characters[i] = _character.CharacterModel.transform.GetChild(i).gameObject;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(false);
        }

        characters[SaveData.Current.mainData.players[playerIndex].SelectedCharacterIndex].SetActive(true);
    }

}
