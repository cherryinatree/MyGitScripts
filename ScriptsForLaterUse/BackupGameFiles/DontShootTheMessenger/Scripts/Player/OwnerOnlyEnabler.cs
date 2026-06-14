// OwnerOnlyEnabler.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class OwnerOnlyEnabler : NetworkBehaviour
{
    [Header("Enable only for the local owner")]
    public MonoBehaviour[] componentsToEnableForOwner; // e.g., PlayerController, LookController
    public Behaviour[] behavioursToEnableForOwner; // if you have non-MonoBehaviour Behaviours
    public GameObject[] objectsToEnableForOwner;    // HUD, crosshair, etc.

    [Header("Auto-detect common components")]
    public bool togglePlayerInput = true;
    public bool toggleCameraAndAudioListener = true;

    public override void OnNetworkSpawn()
    {
        Apply(IsOwner);
    }

    void Apply(bool isOwner)
    {
        foreach (var c in componentsToEnableForOwner) if (c) c.enabled = isOwner;
        foreach (var b in behavioursToEnableForOwner) if (b) b.enabled = isOwner;
        foreach (var o in objectsToEnableForOwner) if (o) o.SetActive(isOwner);

        if (togglePlayerInput)
        {
            var pi = GetComponent<PlayerInput>();
            if (pi) pi.enabled = isOwner;
        }

        if (toggleCameraAndAudioListener)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam) cam.enabled = isOwner;
            var al = GetComponentInChildren<AudioListener>(true);
            if (al) al.enabled = isOwner;
        }
    }
}
