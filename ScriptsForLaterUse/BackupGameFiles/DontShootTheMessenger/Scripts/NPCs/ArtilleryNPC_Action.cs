using UnityEngine;

public class ArtilleryNPC_Action : NPC_Action
{
    public AudioSource artilleryAudio;
    public ParticleSystem artilleryEffect;
    private string currentTargetCoordinates;

    public override void OnPlayerInteract()
    {
        // optional: open dialogue
    }

    // Called by option script or directly by dialog
    public void ReceiveFromPlayerAndFire()
    {
        string held = PlayerInventory.Instance != null ? PlayerInventory.Instance.heldCoordinates : null;
        if (string.IsNullOrEmpty(held))
        {
            Debug.Log($"{npc.npcName}: You have no coordinates!");
            return;
        }

        currentTargetCoordinates = held;
        PlayerInventory.Instance.ClearCoordinates();

        FireArtillery();
    }

    private void FireArtillery()
    {
        if (artilleryAudio != null) artilleryAudio.Play();
        if (artilleryEffect != null) artilleryEffect.Play();

        Debug.Log($"{npc.npcName} fired artillery at {currentTargetCoordinates}!");

        // mark game events
        if (GameState.Instance != null)
        {
            GameState.Instance.SetFlag("FiredOnce");
            GameState.Instance.targetIndex++;
        }

        // notify round manager -> game manager
        var rm = FindObjectOfType<RoundManager>();
        if (rm != null) rm.DeliveryComplete();

        npc.TriggerActions("fired_artillery");
    }
}
