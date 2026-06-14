using UnityEngine;

public class StoreInteractable : MonoBehaviour
{
    // Call when player presses Interact near the store
    public void OpenForLocalPlayer(InputContextController ctx)
    {
        ctx.PushContext(ControlContext.Store);
        // show store UI etc.
        Time.timeScale = 1f; // keep running; your choice
    }

    public void CloseForLocalPlayer(InputContextController ctx)
    {
        ctx.PopContext();
        // hide store UI
    }
}
