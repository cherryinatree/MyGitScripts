// WalkAction.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class WalkAction : PlayerAction
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool serverAuthority = true;

    private CharacterController _cc;
    private Vector3 _desiredLocalDir; // set on server when payload arrives

    protected override void Awake()
    {
        base.Awake();
        _cc = GetComponent<CharacterController>();
    }

    // Continuous: send desired move vector from CorePlayer
    protected override bool BuildPayloadFromCoreInput(out ActionPayload payload)
    {
        Vector2 m = corePlayer.MoveInput;                 // <- from CorePlayer
        Vector3 dir = new Vector3(m.x, 0, m.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        payload = new ActionPayload { code = 100, v1 = dir };
        return true; // always send; you can add a deadzone if you want
    }

    protected override void PerformOnServer(in ActionPayload payload)
    {
        _desiredLocalDir = payload.v1;
    }

    // Server drives the actual motion every frame
    private void Update()
    {
        if (serverAuthority && IsServer && _cc)
        {
            Vector3 worldDir = transform.TransformDirection(_desiredLocalDir);
            if (worldDir.sqrMagnitude > 0.0001f)
            {
                _cc.Move(worldDir * moveSpeed * Time.deltaTime);
                transform.forward = Vector3.Lerp(transform.forward, worldDir.normalized, 0.2f);
            }
        }
    }

    protected override void PerformOnClients(in ActionPayload payload)
    {
        // Optional: animator speed / footstep VFX
        // Debug.Log("WalkAction PerformOnClients");  // <-- your debug will now fire
    }
}
