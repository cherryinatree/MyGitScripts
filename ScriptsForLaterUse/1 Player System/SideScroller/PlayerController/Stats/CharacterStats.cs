using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStats 
{
    public string prefabName;
    public string prefabLocation;
    public int level;
    public int experience;
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    public int stamina;
    public int maxStamina;
    public int attack;
    public int defense;
    public int magicPower;
    public int magicDefense;
    public float speed;

    public int[] abilitiesID;
}
