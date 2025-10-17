using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelect : CharacterAbility
{
    [HideInInspector]
    public GameObject[] characters;
    public Character ThisGameObject;
    public int playerIndex;
    [HideInInspector]
    public int selectedCharacter = 0;
    private bool connected = false;
    [HideInInspector]
    public bool isReady = false;
    

    protected override void Initialization()
    {
        base.Initialization();
        characters = new GameObject[_character.CharacterModel.transform.childCount];

        for (int i = 0; i < _character.CharacterModel.transform.childCount; i++)
        {
            characters[i] = _character.CharacterModel.transform.GetChild(i).gameObject;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(false);
        }

        //characters[selectedCharacter].SetActive(true);
        SaveData.Current.mainData.players[playerIndex].SelectedCharacterIndex = selectedCharacter;

        SaveData.Current.mainData.connected[playerIndex] = false;
    }

    private void Update()
    {
        if (_inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
        {
            if (connected)
                NextCharacter();
            else
            {
                connected = true;
                SaveData.Current.mainData.connected[playerIndex] = true;
                characters[selectedCharacter].SetActive(true);
            }
        }
        if (_inputManager.DashButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
        {
            if (connected)
                ConfirmCharacter();
            else
            {
                connected = true;
                SaveData.Current.mainData.connected[playerIndex] = true;
                characters[selectedCharacter].SetActive(true);
            }
        }
        if (_inputManager.JumpButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
        {
            if (connected)
            {
                //NextScene();
                isReady = !isReady;
            }
            else
            {
                connected = true;
                SaveData.Current.mainData.connected[playerIndex] = true;
                characters[selectedCharacter].SetActive(true);
            }
        }

    }

    public void NextCharacter()
    {
        Debug.Log("SelectCharacter");
        characters[selectedCharacter].SetActive(false);
        selectedCharacter++;
        if (selectedCharacter >= characters.Length)
        {
            selectedCharacter = 0;
        }
        characters[selectedCharacter].SetActive(true);


        SaveData.Current.mainData.players[playerIndex].SelectedCharacterIndex = selectedCharacter;
    }



    public void SelectCharacter(int index)
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = index;
        characters[selectedCharacter].SetActive(true);
    }

    public void ConfirmCharacter()
    {
       // Debug.Log("ConfirmCharacter");
       // GameManager.Instance.StoreSelectedCharacter(ThisGameObject);


    }
    public void NextScene()
    {

        SerializationManager.Save(SaveData.Current.saveName, SaveData.Current);
        Debug.Log("Next Scene");
        MMSceneLoadingManager.LoadScene("TestScene2");

    }
}