using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MainSaveFile
{
    public string saveName;
    public List<Item> itemInventory;
    public List<Item> itemStorage;
    public List<Equipment> equipmentStorage;

    public PlayerData playerData;
    
    public List<Character> characters;
    public List<Character> charactersStorage;
    public Recruits recruits;

    public BattleLoadData battleLoadData;

    public LoadSceneData loadSceneData;

    public Research Research;
    public Buildings buildings;
}
