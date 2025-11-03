using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Carryable : MonoBehaviour
{
    [Header("When Held")]
    [Tooltip("Optional local offset while held.")]
    public Vector3 heldLocalPosition = Vector3.zero;
    public Vector3 heldLocalEuler = Vector3.zero;

    [Header("When Docked (e.g., on shelf)")]
    public bool freezeWhenDocked = true;

    private Rigidbody _rb;
    private Collider[] _cols;
    private Transform _originalParent;

    private MerchandisingFixtures myFixture;

    private bool isOnCheckoutCounter = false;

    ItemReadyForCheckout itemReadyForCheckout;

    public Rigidbody Rb => _rb;
    public bool IsHeldOrDocked { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cols = GetComponentsInChildren<Collider>(includeInactive: true);
        _originalParent = transform.parent;
        itemReadyForCheckout = GetComponent<ItemReadyForCheckout>();
    }

    public void SetOnCheckoutCounter()
    {
        isOnCheckoutCounter = true;
    }

    public void AttachToHolder(Transform holder)
    {
        if (!_rb) return;
        if (isOnCheckoutCounter)
        {
            itemReadyForCheckout.ScanMe();
            return;
        }
        IsHeldOrDocked = true;

        // Kinematic while in hand for stability
        _rb.isKinematic = true;

        // Leave colliders on; optional: set to triggers to avoid bumping into world while held
        // foreach (var c in _cols) c.isTrigger = true;

        transform.SetParent(holder, worldPositionStays: false);
        transform.localPosition = heldLocalPosition;
        transform.localRotation = Quaternion.Euler(heldLocalEuler);
    }

    public void DetachFromHolder()
    {
        if (!_rb) return;

        transform.SetParent(null, true);
        _rb.isKinematic = false;

        // foreach (var c in _cols) c.isTrigger = false;

        IsHeldOrDocked = false;
    }

    /// <summary>
    /// Use when starting a tween to a target (remain kinematic but unparented).
    /// </summary>
    public void DetachKeepKinematic()
    {
        transform.SetParent(null, true);
        if (_rb) _rb.isKinematic = true;
        // foreach (var c in _cols) c.isTrigger = true;
        IsHeldOrDocked = true; // still considered �controlled�
    }

    /// <summary>
    /// Put into a fixed slot (e.g., shelf). Optionally freeze rigidbody.
    /// </summary>
    public void DockTo(ShelfSlot slot, bool asChild = true)
    {
        if (asChild) { 
            transform.SetParent(slot.transform, worldPositionStays: false);

            transform.localPosition = Vector3.zero;
            transform.rotation = slot.transform.rotation;

        }
        else
        {
            //transform.position = slot.position;
            transform.localPosition = slot.transform.position;
            transform.rotation = slot.transform.rotation;
        }
        if (_rb)
        {
            _rb.isKinematic = freezeWhenDocked;
            if (!freezeWhenDocked)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        // foreach (var c in _cols) c.isTrigger = false;
        IsHeldOrDocked = true;
        slot.SetItem(this);

    }

    public void UndockToWorld()
    {
        transform.SetParent(null, true);
        if (_rb) _rb.isKinematic = false;
        // foreach (var c in _cols) c.isTrigger = false;
        IsHeldOrDocked = false;
    }

    public void SetFixtureParent(MerchandisingFixtures fixture)
    {
        myFixture = fixture;
    }

    public void ClearFixtureParent()
    {
        myFixture = null;
    }

    public MerchandisingFixtures GetFixtureParent()
    {
        if (myFixture == null) return null;
        return myFixture;
    }


    /// <summary>
    /// Used by box unloading to spawn tween start nicely at a given pose.
    /// </summary>
    public void PrepareTweenFrom(Vector3 pos, Quaternion rot)
    {
        if (_rb) _rb.isKinematic = true;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(pos, rot);
        IsHeldOrDocked = true;
    }
}
