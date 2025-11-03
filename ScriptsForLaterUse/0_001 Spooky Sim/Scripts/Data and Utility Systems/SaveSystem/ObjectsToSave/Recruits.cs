using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recruits
{
    public List<Character> recruits;
    public int LastDayRefreshed;

    public int MaxNewRecruits = 4;
}
