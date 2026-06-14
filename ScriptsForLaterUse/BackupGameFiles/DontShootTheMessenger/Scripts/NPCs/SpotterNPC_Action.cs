using System.Collections.Generic;
using UnityEngine;

public class SpotterNPC_Action : NPC_Action
{
    private List<int> safeTrenches = new List<int>();

    public override void OnPlayerInteract()
    {
        // Example: show a quick log or set UI
    }

    public override void OnRoundStart(TrenchRoute[] trenches)
    {
        safeTrenches.Clear();
        for (int i = 0; i < trenches.Length; i++)
        {
            if (!trenches[i].IsAnomalous())
                safeTrenches.Add(i);
        }
    }

    // helper for other code to query
    public int GetFirstSafeTrenchIndex()
    {
        return safeTrenches.Count > 0 ? safeTrenches[0] : -1;
    }
}
