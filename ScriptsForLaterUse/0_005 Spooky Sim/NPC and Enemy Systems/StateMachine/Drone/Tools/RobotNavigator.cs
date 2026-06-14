using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class RobotNavigator : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent Agent;
    public Animator Anim;

    [Header("Tuning")]
    public float ArriveDistance = 0.4f;
    public float FaceTurnSpeed = 720f;

    public bool IsBusy { get; private set; }
    public bool HasGoal { get; private set; }
    public Vector3 Goal { get; private set; }

    // Used by TransportPads to know where to send this robot
    public TransportPad DesiredTransportDestination { get; set; }

    // Transport step signaling
    public int TransportSequence { get; private set; }
    public TransportPad LastArrivedPad { get; private set; }

    private Coroutine _interactionRoutine;

    private void Awake()
    {
        if (Agent == null) Agent = GetComponent<NavMeshAgent>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
    }
    public IEnumerator ForceInteract(IRobotInteractable interactable)
    {
        if (interactable == null) yield break;
        if (IsBusy) yield break;

        // Respect CanInteract rules
        if (!interactable.CanInteract(gameObject))
            yield break;

        // Run the same interaction routine used by triggers
        yield return DoInteraction(interactable);
    }

    public void SetGoal(Vector3 worldPos)
    {
        HasGoal = true;
        Goal = worldPos;

        Agent.isStopped = false;
        Agent.SetDestination(Goal);
        SetMoveAnim(true);
    }

    public void Stop()
    {
        HasGoal = false;
        Agent.isStopped = true;
        Agent.ResetPath();
        SetMoveAnim(false);
    }

    public bool ReachedGoal()
    {
        if (!HasGoal) return true;
        if (IsBusy) return false;
        if (Agent.pathPending) return false;
        return Agent.remainingDistance <= ArriveDistance;
    }

    private void Update()
    {
        if (!IsBusy && HasGoal)
        {
            bool moving = Agent.pathPending || Agent.remainingDistance > ArriveDistance;
            SetMoveAnim(moving);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("RobotNavigator OnTriggerStay called");
        if (IsBusy) return;

        var interactable = other.GetComponentInParent<IRobotInteractable>();
        if (interactable == null) return;
        if (!interactable.CanInteract(gameObject)) return;

        if (_interactionRoutine == null)
            _interactionRoutine = StartCoroutine(DoInteraction(interactable));
    }
    private void OnTriggerStay(Collider other)
    {
       // Debug.Log("RobotNavigator OnTriggerStay called");
        if (IsBusy) return;
       // Debug.Log("RobotNavigator is not busy");
        if (_interactionRoutine != null) return;
       // Debug.Log("No interaction routine running");

        var interactable = other.GetComponentInParent<IRobotInteractable>();
        if (interactable == null) return;
      //  Debug.Log("Found interactable: " + interactable);
        if (!interactable.CanInteract(gameObject)) return;
       // Debug.Log("Can interact with it");

        _interactionRoutine = StartCoroutine(DoInteraction(interactable));
    }

    protected IEnumerator DoInteraction(IRobotInteractable interactable)
    {
        IsBusy = true;

        Agent.isStopped = true;
        SetMoveAnim(false);

        if (interactable.InteractionPoint != null)
        {
            yield return MovePreciselyTo(interactable.InteractionPoint.position, 0.18f);
            yield return FaceTowards(interactable.InteractionPoint.position);
        }

        yield return interactable.Interact(this);

        IsBusy = false;
        _interactionRoutine = null;

        if (HasGoal)
        {
            Agent.isStopped = false;
            Agent.SetDestination(Goal);
        }
    }

    private IEnumerator MovePreciselyTo(Vector3 pos, float stopDist)
    {
        Agent.isStopped = false;
        Agent.SetDestination(pos);

        while (!Agent.pathPending && Agent.remainingDistance > stopDist)
            yield return null;

        Agent.isStopped = true;
    }

    public IEnumerator FaceTowards(Vector3 worldPos)
    {
        Vector3 flatDir = worldPos - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                FaceTurnSpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    public void WarpTo(Vector3 pos, Quaternion rot)
    {
        Agent.Warp(pos);
        transform.rotation = rot;

        Agent.ResetPath();
        if (HasGoal) Agent.SetDestination(Goal);
    }

    public void NotifyTransportArrived(TransportPad arrivedAt)
    {
        TransportSequence++;
        LastArrivedPad = arrivedAt;
    }

    public void PlayTrigger(string triggerName)
    {
        if (Anim == null || string.IsNullOrWhiteSpace(triggerName)) return;
        Anim.ResetTrigger(triggerName);
        Anim.SetTrigger(triggerName);
    }

    private void SetMoveAnim(bool moving)
    {
        if (Anim == null) return;
        Anim.SetBool("Moving", moving);
    }
}
