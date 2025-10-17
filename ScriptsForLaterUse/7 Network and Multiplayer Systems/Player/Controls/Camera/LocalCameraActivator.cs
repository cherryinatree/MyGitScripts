using Unity.Netcode;
using UnityEngine;

// Attach to the Player root (same object as NetworkObject)
public class LocalCameraActivator : NetworkBehaviour
{
    [Header("Assign your player camera + (optional) Cinemachine VCam")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener; // optional but recommended

#if CINEMACHINE
    [SerializeField] private Cinemachine.CinemachineVirtualCamera vcam;
#endif

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // If you forgot to wire references, try to find the first Camera under this player.
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>(true);
        if (!audioListener) audioListener = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;

        bool isLocal = IsOwner;

        // Enable ONLY for the local owner
        SetCameraActive(isLocal);

        // For non-owners, make sure their camera is fully disabled so you don't see through them
        // (you can still keep a tiny helper camera for special FX if needed, but default is off)
#if CINEMACHINE
        if (vcam) vcam.Priority = isLocal ? 100 : 0;
#endif

        // Optional: if this is the local player, disable any stray scene camera(s)
        if (isLocal)
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam != playerCamera && cam.enabled)
                {
                    // Don’t disable other players’ cameras; they should already be disabled
                    var isUnderAnotherPlayer = cam.GetComponentInParent<NetworkObject>() != null;
                    if (!isUnderAnotherPlayer) cam.enabled = false;
                }
            }
        }
    }

    void SetCameraActive(bool on)
    {
        if (playerCamera) playerCamera.enabled = on;
        if (audioListener) audioListener.enabled = on;

#if CINEMACHINE
        if (vcam)
        {
            vcam.gameObject.SetActive(on);
            if (on && vcam.Follow == null)
            {
                // Optional: auto-wire follow/look if you forgot
                var pivot = GetComponentInChildren<Transform>();
                vcam.Follow = pivot; vcam.LookAt = pivot;
            }
        }
#endif
    }
}
