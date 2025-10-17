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

    private bool _isOpen;
    private bool _isAnimating;
    private Quaternion _closedRotLocal, _openRotLocal;
    private Quaternion _closedRotWorld, _openRotWorld;
    private Coroutine _spin;

    AudioSource _audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;


    private void Reset()
    {
        door = transform;
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (door == null) door = transform;

        // cache closed rotations
        _closedRotLocal = door.localRotation;
        _closedRotWorld = door.rotation;

        // compute open rotations once
        var axis = openAxis.normalized;
        var delta = Quaternion.AngleAxis(openAngle, axis);

        _openRotLocal = _closedRotLocal * delta;
        _openRotWorld = _closedRotWorld * delta;

        if (startOpen)
        {
            _isOpen = true;
            if (useLocalSpace) door.localRotation = _openRotLocal;
            else door.rotation = _openRotWorld;
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        // Always interactable while enabled; you could add a 'locked' flag here.
        return base.CanInteract(interactor) && !_isAnimating;
    }

    public override void Interact(GameObject interactor)
    {
        SetOpen(!_isOpen);
    }

    public override void OnFocusGained(GameObject interactor)
    {
        if (highlighter) highlighter.SetHighlighted(true);
    }

    public override void OnFocusLost(GameObject interactor)
    {
        if (highlighter) highlighter.SetHighlighted(false);
    }

    public void SetOpen(bool open)
    {
        if (_isOpen == open && !_isAnimating) return;
        _isOpen = open;

        if (_isOpen)
        {
            if(doorOpenSound != null && _audioSource != null)
                _audioSource.PlayOneShot(doorOpenSound);
        }
        else
        {
            if(doorCloseSound != null && _audioSource != null)
                _audioSource.PlayOneShot(doorCloseSound);
        }

        if (_spin != null) StopCoroutine(_spin);
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
            float k = ease != null ? Mathf.Clamp01(ease.Evaluate(Mathf.Clamp01(t))) : Mathf.Clamp01(t);

            if (useLocalSpace) door.localRotation = Quaternion.Slerp(from, to, k);
            else door.rotation = Quaternion.Slerp(from, to, k);

            yield return null;
        }

        // snap at end
        if (useLocalSpace) door.localRotation = to;
        else door.rotation = to;

        _isAnimating = false;
        _spin = null;
    }
}
