using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AirLockDisposal : MonoBehaviour
{
    [Header("Where robots stand")]
    [SerializeField] private Transform interactionPoint;
    public Transform InteractionPoint => interactionPoint;

    [Header("Where trash is placed (inside chamber)")]
    [SerializeField] private Transform chamberRoot;
    [SerializeField] private Transform chamberDropPoint;

    [Header("Optional: where trash gets ejected from")]
    [SerializeField] private Transform ejectPoint;

    [Header("Airlock Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";
    [SerializeField] private string ventTrigger = "Vent";

    [Header("Timings")]
    [SerializeField] private float openSeconds = 0.6f;
    [SerializeField] private float closeSeconds = 0.6f;
    [SerializeField] private float ventSeconds = 1.5f;

    [Header("Venting Behavior")]
    public bool EjectWithPhysics = true;
    public float EjectForce = 10f;
    public float DestroyAfterVentSeconds = 3f; // set 0 to keep trash floating forever 😈

    public bool IsBusy { get; private set; }
    public bool LastCycleComplete { get; private set; }

    private readonly List<GameObject> _chamberTrash = new();

    public bool CanDispose(GameObject interactor)
    {
        if (IsBusy) return false;
        if (interactor == null) return false;

        var carrier = interactor.GetComponentInChildren<TrashCarrier>() ?? interactor.GetComponent<TrashCarrier>();
        return carrier != null && carrier.Count > 0;
    }

    public IEnumerator RunDisposalCycle(GameObject interactor)
    {
        Debug.Log(1111);
        if (!CanDispose(interactor)) yield break;
        Debug.Log(2222);
        IsBusy = true;
        LastCycleComplete = false;

        var carrier = interactor.GetComponentInChildren<TrashCarrier>() ?? interactor.GetComponent<TrashCarrier>();
        if (carrier == null) { IsBusy = false; yield break; }

        if (chamberRoot == null) chamberRoot = transform;
        if (chamberDropPoint == null) chamberDropPoint = chamberRoot;

        // 1) Open inner door
        if (animator != null && !string.IsNullOrWhiteSpace(openTrigger))
            animator.SetTrigger(openTrigger);

        if (openSeconds > 0f) yield return new WaitForSeconds(openSeconds);

        // 2) Drop physical trash into chamber
        _chamberTrash.Clear();
        Vector3 dropPos = chamberDropPoint.position;
        _chamberTrash.AddRange(carrier.DropAllInto(chamberDropPoint, dropPos, scatterRadius: 0.25f));
/*
        // 3) Close door
        if (animator != null && !string.IsNullOrWhiteSpace(closeTrigger))
            animator.SetTrigger(closeTrigger);

        if (closeSeconds > 0f) yield return new WaitForSeconds(closeSeconds);

        // 4) Vent
        if (animator != null && !string.IsNullOrWhiteSpace(ventTrigger))
            animator.SetTrigger(ventTrigger);

        // Give the animation a moment, then eject/destroy
        if (ventSeconds > 0f) yield return new WaitForSeconds(ventSeconds);

        VentTrash();*/

        LastCycleComplete = true;
        IsBusy = false;
    }

    private void VentTrash()
    {
        if (_chamberTrash.Count == 0) return;

        // If we want an actual ejection, move objects to eject point & add force
        if (EjectWithPhysics && ejectPoint != null)
        {
            for (int i = 0; i < _chamberTrash.Count; i++)
            {
                var go = _chamberTrash[i];
                if (go == null) continue;

                go.transform.position = ejectPoint.position + Random.insideUnitSphere * 0.15f;

                if (!go.TryGetComponent<Rigidbody>(out var rb))
                    rb = go.AddComponent<Rigidbody>();

                rb.isKinematic = false;
                rb.AddForce(ejectPoint.forward * EjectForce, ForceMode.VelocityChange);
            }
        }

        if (DestroyAfterVentSeconds > 0f)
            StartCoroutine(DestroyLater(_chamberTrash, DestroyAfterVentSeconds));
    }

    private IEnumerator DestroyLater(List<GameObject> list, float delay)
    {
        yield return new WaitForSeconds(delay);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) Destroy(list[i]);
        }
    }
}
