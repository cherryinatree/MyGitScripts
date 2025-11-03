using System.Collections;
using System.Xml.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class CarryAction : PlayerAction
{
    [Header("View / Reach")]
    [SerializeField] private Camera cameraRef;
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Hold Settings")]
    [SerializeField] private Transform holdAnchor;          // child of camera recommended
    [SerializeField] private Vector3 defaultLocalHoldPos = new(0f, -0.1f, 0.6f);
    [SerializeField] private Vector3 defaultLocalHoldEuler = Vector3.zero;

    [Header("Placement Animation")]
    [SerializeField] private float handToTargetDuration = 0.25f; // seconds per item when placing to shelf
    [SerializeField] private float perItemStagger = 0.08f;       // delay between items when unloading a box

    private Carryable _held;
    private bool _busyAnimating;

    protected override void Subscribe(CorePlayer c)
    {
        // Hook your CorePlayer input events here:
        c.OnInteractStarted += OnInteractPressed;
        c.DropPressedStarted += OnDropPressed;
    }

    protected override void Unsubscribe(CorePlayer c)
    {
        c.OnInteractCanceled -= OnInteractPressed;
        c.DropPressedCanceled -= OnDropPressed;
    }

    protected override void Awake()
    {
        base.Awake();

        if (!cameraRef)
        {
            cameraRef = GetComponentInChildren<Camera>();
            if (!cameraRef) cameraRef = Camera.main;
        }

        if (!holdAnchor && cameraRef)
        {
            var go = new GameObject("HoldAnchor");
            holdAnchor = go.transform;
            holdAnchor.SetParent(cameraRef.transform, false);
            holdAnchor.localPosition = defaultLocalHoldPos;
            holdAnchor.localRotation = Quaternion.Euler(defaultLocalHoldEuler);
        }
    }

    private void OnInteractPressed()
    {
        if (_busyAnimating) return;

        // If we hold a box AND we’re aiming at a shelf => unload box onto shelf
        if (_held && _held.TryGetComponent<BoxContainer>(out var box))
        {
            if (TryRaycast(out var hit))
            {
                if (hit.collider.TryGetComponent<Shelf>(out var shelf))
                {
                    StartCoroutine(UnloadBoxToShelf(box, shelf));
                    return;
                }
            }
        }

        // If we hold a single item and are aiming at shelf => place item to shelf
        if (_held && !_held.TryGetComponent<BoxContainer>(out _))
        {

            if (TryRaycast(out var hit) && hit.collider.TryGetComponent<Shelf>(out var shelf))
            {
                Debug.Log("Scale 1: " + transform.lossyScale.x);
                TryPlaceHeldToShelf(shelf);
                Debug.Log("Scale 2: " + transform.lossyScale.x);
                return;
            }

            // If aiming at a box => store item into box
            if (TryRaycast(out hit) && hit.collider.TryGetComponent<BoxContainer>(out var targetBox))
            {
                if (targetBox.TryStore(_held))
                {
                    _held = null;
                }
                return;
            }
        }

        // If we hold nothing => try pick up something
        if (_held == null)
        {
            if (TryRaycast(out var hit))
            {
                if (hit.collider.TryGetComponent<Carryable>(out var carry))
                {
                    TryPickup(carry);
                }
                if (hit.collider.TryGetComponent<Shelf>(out Shelf shelf))
                {
                    TryGrabFromShelf(shelf);
                }
            }
        }
    }
    private void TryGrabFromShelf(Shelf shelf)
    {

        if (_held != null) return;

        StartCoroutine(GrabFromShelf(shelf));
    }
    private IEnumerator GrabFromShelf(Shelf shelf) {         
        

        Carryable item = shelf.UndockFromSlot();
        if (item == null) yield break;
        _busyAnimating = true;
        // Detach from hand but keep kinematic while tweening
        item.DetachKeepKinematic(); // stays kinematic; no parent
        yield return SimpleTween.MoveRotate(item.transform, holdAnchor.transform.position, holdAnchor.transform.rotation, handToTargetDuration);


        _held = item;
        _held.AttachToHolder(holdAnchor);

        _busyAnimating = false;
    }

    private void OnDropPressed()
    {
        if (_busyAnimating) return;
        if (_held == null) return;

        var rb = _held.Rb;
        _held.DetachFromHolder();
        _held = null;

        // Give a small forward toss
        if (rb)
        {
            rb.AddForce(cameraRef.transform.forward * 1.5f, ForceMode.VelocityChange);
        }
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;
        if (!cameraRef) return false;
        var ray = new Ray(cameraRef.transform.position, cameraRef.transform.forward);
        return Physics.Raycast(ray, out hit, interactRange, interactMask, QueryTriggerInteraction.Ignore);
    }

    private void TryPickup(Carryable c)
    {
        if (c == null || c.IsHeldOrDocked) return;
        if (_held != null) return;

        _held = c;
        _held.AttachToHolder(holdAnchor);
    }

    private void TryPlaceHeldToShelf(Shelf shelf)
    {
        if (_held == null || shelf == null) return;

        if (!shelf.TryGetFreeSlot(out ShelfSlot slot))
            return; // shelf is full

        StartCoroutine(PlaceOneFromHandToSlot(_held, shelf, slot));
    }

    private IEnumerator PlaceOneFromHandToSlot(Carryable item, Shelf shelf, ShelfSlot slot)
    {
        _busyAnimating = true;
        // Detach from hand but keep kinematic while tweening
        item.DetachKeepKinematic(); // stays kinematic; no parent

        yield return SimpleTween.MoveRotate(item.transform, slot.transform.position, slot.transform.rotation, handToTargetDuration);

        // Dock into shelf slot
        shelf.DockIntoSlot(item, slot);

        // If that was our held item, clear it
        if (_held == item) _held = null;

        _busyAnimating = false;

    }

    private IEnumerator UnloadBoxToShelf(BoxContainer box, Shelf shelf)
    {
        if (box.StoredCount == 0) yield break;

        _busyAnimating = true;

        // Stream items one-by-one
        while (box.StoredCount > 0)
        {
            if (!shelf.TryGetFreeSlot(out var slot))
                break; // shelf filled mid-stream

            var item = box.PopItem(); // reactivates & returns next item at box.EjectPoint
            if (item == null) break;

            // keep item kinematic while tweening to shelf
            item.PrepareTweenFrom(box.EjectPoint ? box.EjectPoint.position : box.transform.position,
                                  box.EjectPoint ? box.EjectPoint.rotation : box.transform.rotation);

            yield return SimpleTween.MoveRotate(item.transform, slot.transform.position, slot.transform.rotation, handToTargetDuration);

            shelf.DockIntoSlot(item, slot);

            // a tiny stagger to make it feel “one by one”
            if (perItemStagger > 0f) yield return new WaitForSeconds(perItemStagger);
        }

        _busyAnimating = false;
    }
}
