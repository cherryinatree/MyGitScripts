using UnityEngine;

public class JanitorSensor : MonoBehaviour
{
    public MessItem CurrentTarget { get; private set; }

    public bool AcquireTarget()
    {
        CurrentTarget = MessRegistry.FindNearestUnclaimed(transform.position);
        if (CurrentTarget == null) return false;
        return CurrentTarget.TryClaim(gameObject);
    }

    public void ClearTarget()
    {
        if (CurrentTarget != null) CurrentTarget.ReleaseClaim(gameObject);
        CurrentTarget = null;
    }
}
