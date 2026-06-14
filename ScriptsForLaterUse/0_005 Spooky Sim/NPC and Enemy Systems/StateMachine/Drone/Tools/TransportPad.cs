using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TransportPad : MonoBehaviour, IRobotInteractable
{
    public static readonly List<TransportPad> AllPads = new();

    [Header("Network")]
    public string networkId = "ShipTransportA";

    [Header("Points")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform exitPoint;

    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
    public Transform ExitPoint => exitPoint != null ? exitPoint : transform;

    [Header("Pad Anim")]
    [SerializeField] private Animator padAnimator;
    [SerializeField] private string padEnterTrigger = "Enter";
    [SerializeField] private string padExitTrigger = "Exit";

    [Header("Robot Anim")]
    [SerializeField] private string robotEnterTrigger = "EnterTransport";
    [SerializeField] private string robotExitTrigger = "ExitTransport";

    [Header("Timing")]
    [SerializeField] private float enterSeconds = 1.0f;
    [SerializeField] private float exitSeconds = 0.6f;

    private void Awake()
    {
        if (padAnimator == null) padAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (!AllPads.Contains(this)) AllPads.Add(this);
    }

    private void OnDisable()
    {
        AllPads.Remove(this);
    }

    public bool CanInteract(GameObject robot)
    {
        var nav = robot.GetComponentInParent<RobotNavigator>();
        if (nav == null) return false;

        var dest = nav.DesiredTransportDestination;
        if (dest == null) return false;
        if (dest == this) return false;

        // Simple rule: only allow travel within same networkId
        return dest.networkId == networkId;
    }

    public IEnumerator Interact(RobotNavigator robot)
    {
        var dest = robot.DesiredTransportDestination;
        if (dest == null || dest.networkId != networkId || dest == this)
            yield break;

        robot.PlayTrigger(robotEnterTrigger);
        if (padAnimator != null) padAnimator.SetTrigger(padEnterTrigger);
        yield return new WaitForSeconds(enterSeconds);

        // Teleport to destination pad exit
        robot.WarpTo(dest.ExitPoint.position, dest.ExitPoint.rotation);

        if (dest.padAnimator != null) dest.padAnimator.SetTrigger(padExitTrigger);
        robot.PlayTrigger(robotExitTrigger);
        yield return new WaitForSeconds(exitSeconds);

        // Signal step complete
        robot.NotifyTransportArrived(dest);
    }
}
