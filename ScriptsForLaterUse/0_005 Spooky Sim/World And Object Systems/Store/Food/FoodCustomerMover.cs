using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Very small customer movement helper.
/// Uses transform movement so it does not require NavMesh setup.
/// Replace this with your own customer AI later if you already have one.
/// </summary>
public class FoodCustomerMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.25f;
    public float turnSpeed = 10f;
    public float stopDistance = 0.08f;
    public bool rotateTowardMovement = true;

    [Header("Runtime")]
    [SerializeField] private Transform target;
    [SerializeField] private bool isMoving;

    [Header("Events")]
    public UnityEvent onReachedTarget;

    public Transform CurrentTarget => target;
    public bool IsMoving => isMoving;

    public void MoveTo(Transform newTarget)
    {
        target = newTarget;
        isMoving = target != null;
    }

    public void StopMoving()
    {
        target = null;
        isMoving = false;
    }

    private void Update()
    {
        if (!isMoving || target == null) return;

        Vector3 current = transform.position;
        Vector3 destination = target.position;
        Vector3 flatDelta = destination - current;
        flatDelta.y = 0f;

        if (flatDelta.magnitude <= stopDistance)
        {
            transform.position = new Vector3(destination.x, current.y, destination.z);
            isMoving = false;
            onReachedTarget?.Invoke();
            return;
        }

        Vector3 step = flatDelta.normalized * moveSpeed * Time.deltaTime;
        if (step.magnitude > flatDelta.magnitude)
            step = flatDelta;

        transform.position += step;

        if (rotateTowardMovement && step.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(step.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
