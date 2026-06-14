using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MessStain : MessItem
{
    [Header("Cleaning")]
    public float CleanSeconds = 2.5f;
    public string CleanerAnimTrigger = "Clean"; // optional
    public bool RequireTool = false;            // later hook

    private Coroutine _cleanRoutine;

    private void Reset()
    {
        Kind = MessKind.Stain;
    }

    public override void Interact(GameObject interactor)
    {
        if (IsResolved) return;
        if (_cleanRoutine != null) return;

        _cleanRoutine = StartCoroutine(CleanRoutine(interactor));
    }

    private IEnumerator CleanRoutine(GameObject interactor)
    {
        // Optional: play animation on interactor if it has an Animator
        var anim = interactor != null ? interactor.GetComponentInChildren<Animator>() : null;
        if (anim != null && !string.IsNullOrWhiteSpace(CleanerAnimTrigger))
            anim.SetTrigger(CleanerAnimTrigger);

        float t = 0f;
        while (t < CleanSeconds)
        {
            // If the interactor disappeared, stop cleaning
            if (interactor == null) break;
            t += Time.deltaTime;
            yield return null;
        }

        Resolve();
    }

    private void Resolve()
    {
        IsResolved = true;
        // You can fade decals here if you want. For now destroy.
        Destroy(gameObject);
    }
}
