using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
/*
public static class CharacterLoader
{
    public static string characterDataFile;

    public static void LoadCharacters(TextAsset jsonCharacterList)
    {
         CharacterData characterData = JsonUtility.FromJson<CharacterData>(jsonCharacterList.text);
        List<Character> characters = new List<Character>();
            for (int i = 0; i < characterData.characters.Length; i++)
            {
                characters[i] = new Character();

                characters[i].id = characterData.characters[i].id;
                characters[i].characterName = characterData.characters[i].characterName;
                characters[i].scriptName = characterData.characters[i].scriptName;
                characters[i].scriptPosition = characterData.characters[i].scriptPosition;
                characters[i].level = characterData.characters[i].level;
                characters[i].maxHealth = characterData.characters[i].maxHealth;
                characters[i].attack = characterData.characters[i].attack;
                characters[i].defense = characterData.characters[i].defense;
                characters[i].speed = characterData.characters[i].speed;
            }
            // Do something with the characters array
            SaveData.Current.mainData.characters = characters;

    }
}

[System.Serializable]
public class CharacterData
{
    public Character[] characters;
}
*/