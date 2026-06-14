using UnityEngine;

public abstract class MessItem : MonoBehaviour
{
    public enum MessKind { Stain, Trash }

    [Header("Mess")]
    public MessKind Kind;

    [Tooltip("Where a bot/player should stand while interacting.")]
    public Transform InteractionPoint;

    [Tooltip("Optional: who created this mess.")]
    public IntruderMaster SourceIntruder;

    public bool IsResolved { get; protected set; }

    public bool IsClaimed => _claimedBy != null;
    private GameObject _claimedBy;

    public Vector3 JobPoint => InteractionPoint != null ? InteractionPoint.position : transform.position;

    protected virtual void OnEnable() => MessRegistry.Register(this);
    protected virtual void OnDisable() => MessRegistry.Unregister(this);

    public bool TryClaim(GameObject claimer)
    {
        if (IsResolved) return false;
        if (_claimedBy != null && _claimedBy != claimer) return false;
        _claimedBy = claimer;
        return true;
    }

    public void ReleaseClaim(GameObject claimer)
    {
        if (_claimedBy == claimer) _claimedBy = null;
    }

    // What happens when a robot/player uses it.
    public abstract void Interact(GameObject interactor);
}
