using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Equipment
{

    public int id;

    public List<string> images;
    public string icon;
    public string equipmentName;
    public string equipmentDiscription;
    public string type;


    public int buyPrice;
    public int sellPrice;


    public string subType;
    public int health;
    public int mana;
    public int attack;
    public int defence;
    public int speed;
    public int intelligence;
}
