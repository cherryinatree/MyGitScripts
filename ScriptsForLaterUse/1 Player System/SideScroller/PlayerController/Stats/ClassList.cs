using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassList", menuName = "ScriptableObjects/ClassList", order = 1)]
public class ClassList : ScriptableObject
{
    public List<AbilityList> classes;
}
