using System.Collections;
using UnityEngine;

/// <summary>
/// Simple swinging door: rotates the target transform by +openAngle around openAxis,
/// then back to its original rotation. Supports local/world space and optional highlight.
/// </summary>
public class DoorInteractable : Interactable
{
    [Header("Door Setup")]
    [Tooltip("Transform that actually rotates (use the hinge/pivot). Defaults to this.transform.")]
    public Transform door;

    [Tooltip("Axis to rotate around (e.g., Y for typical door).")]
    public Vector3 openAxis = Vector3.up;

    [Tooltip("How many degrees to rotate when opening.")]
    public float openAngle = 90f;

    [Tooltip("Rotate in local space (true) or world space (false).")]
    public bool useLocalSpace = true;

    [Header("Motion")]
    [Tooltip("Seconds to fully open/close.")]
    public float duration = 0.25f;

    [Tooltip("Ease curve over time (0..1).")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Start the scene already open?")]
    public bool startOpen = false;

    [Header("Highlight (optional)")]
    [Tooltip("Optional highlight helper. If absent, no highlight is shown.")]
    public SimpleHighlighter highlighter;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    private bool _isOpen;
    private bool _isAnimating;
    private Quaternion _closedRotLocal, _openRotLocal;
    private Quaternion _closedRotWorld, _openRotWorld;
    private Coroutine _spin;

    public bool IsOpen => _isOpen;
    public bool IsAnimating => _isAnimating;

    private void Reset()
    {
        door = transform;
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (door == null)
            door = transform;

        // Cache closed rotations.
        _closedRotLocal = door.localRotation;
        _closedRotWorld = door.rotation;

        // Compute open rotations once.
        Vector3 axis = openAxis.normalized;
        Quaternion delta = Quaternion.AngleAxis(openAngle, axis);

        _openRotLocal = _closedRotLocal * delta;
        _openRotWorld = _closedRotWorld * delta;

        if (startOpen)
        {
            _isOpen = true;

            if (useLocalSpace)
                door.localRotation = _openRotLocal;
            else
                door.rotation = _openRotWorld;
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        return base.CanInteract(interactor) && !_isAnimating;
    }

    public override void Interact(GameObject interactor)
    {
        ToggleDoor();
    }

    public void InteractWithDoor()
    {
        Debug.Log("InteractWithDoor called.");
        ToggleDoor();
    }

    public override void OnFocusGained(GameObject interactor)
    {
        if (highlighter)
            highlighter.SetHighlighted(true);
    }

    public override void OnFocusLost(GameObject interactor)
    {
        if (highlighter)
            highlighter.SetHighlighted(false);
    }

    /// <summary>
    /// Call this from another script, trigger, UnityEvent, animation event, etc.
    /// If the door is already open, this does nothing.
    /// </summary>
    public void OpenDoor()
    {
        if (_isOpen)
            return;

        SetOpen(true);
    }

    /// <summary>
    /// Call this from another script, trigger, UnityEvent, animation event, etc.
    /// If the door is already closed, this does nothing.
    /// </summary>
    public void CloseDoor()
    {
        if (!_isOpen)
            return;

        SetOpen(false);
    }

    /// <summary>
    /// Toggles the door between open and closed.
    /// </summary>
    public void ToggleDoor()
    {
        SetOpen(!_isOpen);
    }

    public void SetOpen(bool open)
    {
        // Already in this state, or already moving toward this state.
        // Do nothing.
        if (_isOpen == open)
            return;

        _isOpen = open;

        if (_isOpen)
        {
            if (doorOpenSound != null && audioSource != null)
                audioSource.PlayOneShot(doorOpenSound);
        }
        else
        {
            if (doorCloseSound != null && audioSource != null)
                audioSource.PlayOneShot(doorCloseSound);
        }

        if (_spin != null)
            StopCoroutine(_spin);

        _spin = StartCoroutine(RotateDoor(open));
    }

    private IEnumerator RotateDoor(bool toOpen)
    {
        _isAnimating = true;

        Quaternion from = useLocalSpace ? door.localRotation : door.rotation;

        Quaternion to = useLocalSpace
            ? (toOpen ? _openRotLocal : _closedRotLocal)
            : (toOpen ? _openRotWorld : _closedRotWorld);

        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            float k = ease != null
                ? Mathf.Clamp01(ease.Evaluate(Mathf.Clamp01(t)))
                : Mathf.Clamp01(t);

            if (useLocalSpace)
                door.localRotation = Quaternion.Slerp(from, to, k);
            else
                door.rotation = Quaternion.Slerp(from, to, k);

            yield return null;
        }

        // Snap at end.
        if (useLocalSpace)
            door.localRotation = to;
        else
            door.rotation = to;

        _isAnimating = false;
        _spin = null;
    }
}