using UnityEngine;

public class PlayerActionManager : MonoBehaviour
{
    private NetworkAction[] actions;

    void Awake()
    {
        actions = GetComponents<NetworkAction>();
    }

    void Update()
    {
        foreach (var action in actions)
        {
            //action.PerformAction();
        }
    }
}
