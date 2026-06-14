using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Item
{
    public int id;

    public List<string> images;
    public string icon;
    public string animation;
    public string itemName;
    public string itemDiscription;
    public bool consumable = false;
    public string type;

    public int buyPrice;
    public int sellPrice;


    public string subType;
    public int effect0;
    public int effect1;
    public int effect2;
    public int effect3;
}
