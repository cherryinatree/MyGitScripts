using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class GrabItem : CombatAction
{
    [Header("Flow Events")]
    public UnityEvent GoToLine;
    public UnityEvent GrabAnotherItem;

    [Header("Animation")]
    [Tooltip("Animator on the customer (or a child). If null, we'll try GetComponentInChildren<Animator>().")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger parameter that plays the grab animation.")]
    [SerializeField] private string grabTriggerParam = "Grab";

    [Tooltip("Animator layer index where the grab animation plays.")]
    [SerializeField] private int animLayer = 0;

    [Tooltip("Optional: state name to detect completion (ex: \"Base Layer.Grab\"). Leave empty to skip name checking.")]
    [SerializeField] private string grabStateName = "";

    [Tooltip("Optional: tag on the grab state (recommended). Leave empty to skip tag checking.")]
    [SerializeField] private string grabStateTag = "Grab";

    [Tooltip("Safety: if the animation hasn't completed by this time, we proceed anyway.")]
    [SerializeField] private float maxWaitForGrabSeconds = 2.5f;

    [Tooltip("If true, the customer will stop moving while grabbing.")]
    [SerializeField] private bool stopAgentWhileGrabbing = true;

    private CustomerShopping customerShopping;
    private NavMeshAgent navMeshAgent;

    private GameObject shelfFixture;

    // Internal grab sequencing
    private bool _grabStarted;
    private bool _enteredGrabState;
    private bool _itemBagged;
    private float _grabStartTime;

    private int _grabTriggerHash;

    public override void OnEnterState()
    {
        base.OnEnterState();

        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!string.IsNullOrWhiteSpace(grabTriggerParam))
            _grabTriggerHash = Animator.StringToHash(grabTriggerParam);

        if (navMeshAgent != null)
            navMeshAgent.updateRotation = false;

        // Cache the current shelf fixture we want to face
        shelfFixture = null;
        if (customerShopping != null && customerShopping.shoppingList != null && customerShopping.shoppingList.Count > 0)
        {
            var first = customerShopping.shoppingList[0];
            if (first != null && first.GetFixtureParent() != null)
                shelfFixture = first.GetFixtureParent().gameObject;
        }

        // Reset per-grab state
        _grabStarted = false;
        _enteredGrabState = false;
        _itemBagged = false;
        _grabStartTime = 0f;
    }

    public override void PerformAction()
    {
        FaceDirection();

        if (customerShopping == null || customerShopping.shoppingList == null)
        {
            GoToLine?.Invoke();
            return;
        }

        // Nothing left to grab, go to checkout
        if (customerShopping.shoppingList.Count == 0)
        {
            GoToLine?.Invoke();
            return;
        }

        // Start the grab for this item (once)
        if (!_grabStarted)
        {
            _grabStarted = true;
            _grabStartTime = Time.time;

            if (stopAgentWhileGrabbing && navMeshAgent != null)
                navMeshAgent.isStopped = true;

            PlayGrabAnimation();
            return; // wait for animation to progress
        }

        // Wait for animation to finish (or time out), then bag exactly one item
        if (!_itemBagged)
        {
            bool timedOut = (Time.time - _grabStartTime) >= maxWaitForGrabSeconds;
            bool finished = IsGrabAnimationFinished();

            if (!finished && !timedOut)
                return;

            // Commit the grab
            customerShopping.PutItemInBag();
            _itemBagged = true;

            if (stopAgentWhileGrabbing && navMeshAgent != null)
                navMeshAgent.isStopped = false;

            // Decide next step
            if (customerShopping.shoppingList.Count > 0)
                GrabAnotherItem?.Invoke();
            else
                GoToLine?.Invoke();

            return;
        }

        // If we ever get here, do nothing; state should transition via events.
    }

    private void PlayGrabAnimation()
    {
        // No animator? Just proceed via timeout/finish checks.
        if (animator == null) return;

        if (!string.IsNullOrWhiteSpace(grabTriggerParam))
            animator.SetTrigger(_grabTriggerHash);
    }

    private bool IsGrabAnimationFinished()
    {
        // If we can't read animation state, don't block forever.
        if (animator == null) return true;

        // If we're transitioning, consider it "not finished yet" once we've entered the grab
        if (_enteredGrabState && animator.IsInTransition(animLayer))
            return false;

        var info = animator.GetCurrentAnimatorStateInfo(animLayer);

        bool matches =
            (!string.IsNullOrWhiteSpace(grabStateTag) && info.IsTag(grabStateTag)) ||
            (!string.IsNullOrWhiteSpace(grabStateName) && info.IsName(grabStateName));

        // If you didn't provide a name or tag, we can't reliably detect completion, so rely on timeout.
        if (string.IsNullOrWhiteSpace(grabStateTag) && string.IsNullOrWhiteSpace(grabStateName))
            return false;

        if (matches)
            _enteredGrabState = true;

        // Don’t declare "finished" until we've actually been in the grab state at least once.
        if (!_enteredGrabState)
            return false;

        // normalizedTime >= 1 means the state has played through once (works best if the clip is NOT looping)
        return matches && info.normalizedTime >= 1f;
    }

    private void FaceDirection()
    {
        if (shelfFixture == null) return;

        Vector3 directionToTarget = shelfFixture.transform.position - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
    }
}
