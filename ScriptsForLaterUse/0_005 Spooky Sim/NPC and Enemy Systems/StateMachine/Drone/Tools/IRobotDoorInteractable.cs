using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class IRobotDoorInteractable : MonoBehaviour, IRobotInteractable
{
    [Header("Points")]
    [SerializeField] private Transform interactionPoint;
    public Transform InteractionPoint => interactionPoint;

    [Header("Locking")]
    [SerializeField] private bool isLocked;
    public bool IsLocked { get => isLocked; set => isLocked = value; }
    public bool AllowSecurityOverride = true;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private ObjectMoveToDifferentPositions doorMover;

    [SerializeField] private NavMeshObstacle obstacle; // carving=true recommended
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";

    [Header("Robot Anim")]
    [SerializeField] private string robotUseTrigger = "UseDoor";

    [Header("Timing")]
    [SerializeField] private float openSeconds = 0.75f;
    [SerializeField] private float stayOpenSeconds = 1.0f;

    private int _openRequests = 0;
    private bool _isOpen = false;

    [Header("Nav Blocking")]
    [SerializeField] private bool blockNavOnlyWhenLocked = true;


    private void Awake()
    {
        if (doorAnimator == null) doorAnimator = GetComponentInChildren<Animator>();
        if (doorMover == null) doorMover = GetComponentInChildren<ObjectMoveToDifferentPositions>();
        if (obstacle == null) obstacle = GetComponentInChildren<NavMeshObstacle>();
        UpdateObstacle();

    }

    public void ChangeLockedStatus(bool locked)
    {
        IsLocked = locked;
        UpdateObstacle();
    }


    private void UpdateObstacle()
    {
        if (obstacle == null) return;

        // If true: door only blocks nav when locked (recommended)
        if (blockNavOnlyWhenLocked)
            obstacle.enabled = IsLocked;
    }


    public bool CanInteract(GameObject robot)
    {
        if (!IsLocked) return true;

        if (AllowSecurityOverride)
        {
            var access = robot.GetComponentInParent<IRobotAccess>();
            if (access != null && access.HasSecurityClearance) return true;
        }

        return false;
    }

    public IEnumerator Interact(RobotNavigator robot)
    {
        Debug.Log("1 Door opened for robot " + robot.name);
        if (IsLocked && !CanInteract(robot.gameObject))
            yield break;

        _openRequests++;

        if (!_isOpen)
        {
            robot.PlayTrigger(robotUseTrigger);

            if (doorAnimator != null) doorAnimator.SetTrigger(openTrigger);
            if (doorAnimator == null && doorMover != null) doorMover.MoveToNextPlace();
            if (obstacle != null) obstacle.enabled = false;
            Debug.Log("2 Door opened for robot " + robot.name);
            _isOpen = true;
            yield return new WaitForSeconds(openSeconds);
        }

        yield return new WaitForSeconds(stayOpenSeconds);

        _openRequests--;
        if (_openRequests <= 0)
        {
            if (doorAnimator != null) doorAnimator.SetTrigger(closeTrigger);
            if (doorAnimator == null && doorMover != null) doorMover.MoveToNextPlace();
            if (obstacle != null) obstacle.enabled = true;
            _isOpen = false;
        }
    }
}
