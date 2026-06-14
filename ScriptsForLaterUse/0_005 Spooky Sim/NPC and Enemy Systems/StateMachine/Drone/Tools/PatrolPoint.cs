using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PatrolPoint : MonoBehaviour
{
    public static readonly List<PatrolPoint> All = new();

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
}
