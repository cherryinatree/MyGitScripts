using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    private List<NPC_Action> allActions = new List<NPC_Action>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RefreshActionList();
    }

    public void RefreshActionList()
    {
        allActions.Clear();
        allActions.AddRange(FindObjectsOfType<NPC_Action>());
    }

    public void OnNewRound()
    {
        // Ensure the action list is current (in case NPCs were instantiated)
        RefreshActionList();

        TrenchRoute[] trenches = AnomalyManager.Instance != null ? AnomalyManager.Instance.trenchRoutes.ToArray() : new TrenchRoute[0];

        foreach (var action in allActions)
        {
            // call the optional round hook
            action.OnRoundStart(trenches);
        }

    }
}
