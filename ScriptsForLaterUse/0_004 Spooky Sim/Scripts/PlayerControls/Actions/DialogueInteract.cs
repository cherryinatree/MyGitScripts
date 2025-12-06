using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player interaction: raycast + proximity triggers. Requires CorePlayer.
/// Uses raycast first; falls back to closest valid proximity target.
/// </summary>
[RequireComponent(typeof(CorePlayer))]
public class DialougeInteract : PlayerAction
{
    

    protected void Start()
    {
    }

    protected override void Subscribe(CorePlayer c)
    {
        c.OnCpressedStarted += TryInteract;
        // optional: keep focus updated
        BindContinuousInputs(true);
    }

    protected override void Unsubscribe(CorePlayer c)
    {
        c.OnCpressedStarted -= TryInteract;
        BindContinuousInputs(false);
    }

   
    private void TryInteract()
    {
        MainStoryCharacterDialogueRunner.Instance.ContinueConversation();
    }

  
}
