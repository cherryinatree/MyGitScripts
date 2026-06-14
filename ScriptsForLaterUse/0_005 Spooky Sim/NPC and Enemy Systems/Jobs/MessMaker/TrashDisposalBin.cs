using UnityEngine;

[DisallowMultipleComponent]
public class TrashDisposalBin : MonoBehaviour
{
    public string DisposeAnimTrigger = "Dispose"; // optional
    public int TotalDisposed { get; private set; }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        var carrier = interactor.GetComponentInParent<TrashCarrier>() ?? interactor.GetComponent<TrashCarrier>();
        if (carrier == null) return;

        var anim = interactor.GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrWhiteSpace(DisposeAnimTrigger))
            anim.SetTrigger(DisposeAnimTrigger);

        //TotalDisposed += carrier.DumpAll();
    }
}
