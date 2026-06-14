// GameState.cs
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    public HashSet<string> flags = new HashSet<string>(); // e.g., "FiredOnce"
    public int targetIndex = 0; // which target you're on

    private void Awake() { Instance = this; }

    public bool HasFlag(string f) => flags.Contains(f);
    public void SetFlag(string f) => flags.Add(f);
    public void ClearFlag(string f) => flags.Remove(f);
}
