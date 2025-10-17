using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityList", menuName = "Abilities/AbilityList")]
public class AbilityList : ScriptableObject
{
    public Stats Class;
    public List<Abilities> abilities;

}
