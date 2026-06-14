using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CleanableTarget : MonoBehaviour
{
    public static readonly List<CleanableTarget> All = new();

    [Header("State")]
    public bool NeedsCleaning = true;

    [Header("Behavior")]
    public float CleanSeconds = 3f;
    public Transform interactionPoint;

    [Header("Events")]
    public UnityEvent OnCleaningStarted;
    public UnityEvent OnCleaningCompleted;

    private RobotMaster _claimedBy;

    public Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        _claimedBy = null;
    }

    public bool TryClaim(RobotMaster robot)
    {
        if (!NeedsCleaning) return false;
        if (_claimedBy != null && _claimedBy != robot) return false;
        _claimedBy = robot;
        return true;
    }

    public void Release(RobotMaster robot)
    {
        if (_claimedBy == robot) _claimedBy = null;
    }

    public bool IsClaimedByOther(RobotMaster robot) => _claimedBy != null && _claimedBy != robot;

    public void MarkCleaned()
    {
        NeedsCleaning = false;
        OnCleaningCompleted?.Invoke();
    }
}
