using UnityEngine;
public class LevelSystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int level = 0;
    public int Level => level;

    // Call this from your XP system when level changes.
    public void SetLevel(int newLevel) => level = Mathf.Max(0, newLevel);
}
