using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandMoney : CombatAction
{
    [Header("Flow")]
    public UnityEvent onArrivedAtItem;

    private CustomerShopping customerShopping;
    private CheckOutCounter checkOut;

    [Header("Props")]
    public GameObject moneyPrefab;
    public GameObject cardPrefab;
    public Transform ArmR;

    [Header("Animator Params")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger that plays the offer animation.")]
    [SerializeField] private string handMoneyTriggerParam = "HandMoney";

    [Tooltip("Bool that keeps the character in the waiting pose.")]
    [SerializeField] private string handMoneyIdleBoolParam = "HandMoneyIdle";

    [Header("Offer Detection")]
    [SerializeField] private int animLayer = 0;

    [Tooltip("Tag on the HandMoney offer state (recommended).")]
    [SerializeField] private string handMoneyStateTag = "HandMoney";

    [Tooltip("Exact offer state name (optional). Example: \"Base Layer.HandMoney\"")]
    [SerializeField] private string handMoneyStateName = "";

    [Header("Timing")]
    [Tooltip("How often to replay the HandMoney offer while waiting.")]
    [SerializeField] private float repeatOfferEverySeconds = 1.25f;

    [Tooltip("Safety: if the offer animation never 'finishes', we continue anyway.")]
    [SerializeField] private float maxOfferSeconds = 2.0f;

    [Header("Cash behavior")]
    public Dictionary<Checkout.Outcome, float> myWeights = new Dictionary<Checkout.Outcome, float> {
        { Checkout.Outcome.NextTwenty, 0.7f },
        { Checkout.Outcome.NextTen,    0.2f },
        { Checkout.Outcome.NextFive,   0.05f },
        { Checkout.Outcome.NextHundred,0.04f },
        { Checkout.Outcome.NextDollar, 0.01f }
    };

    private bool isPayingWithCard;
    private bool exactChange;
    private float paymentAmount;

    private bool _sequenceStarted;
    private bool _paymentCompleted;
    private Coroutine _loopRoutine;

    private int _handMoneyTriggerHash;
    private int _handMoneyIdleBoolHash;

    private Quaternion _armBaseLocalRot;
    private bool _armBaseCached;

    protected override void Awake()
    {
        base.Awake();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!string.IsNullOrWhiteSpace(handMoneyTriggerParam))
            _handMoneyTriggerHash = Animator.StringToHash(handMoneyTriggerParam);

        if (!string.IsNullOrWhiteSpace(handMoneyIdleBoolParam))
            _handMoneyIdleBoolHash = Animator.StringToHash(handMoneyIdleBoolParam);

        if (moneyPrefab != null) moneyPrefab.SetActive(false);
        if (cardPrefab != null) cardPrefab.SetActive(false);

        if (ArmR != null)
        {
            _armBaseLocalRot = ArmR.localRotation;
            _armBaseCached = true;
        }
    }

    public override void OnEnterState()
    {
        base.OnEnterState();

        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        checkOut = null;

        if (customerShopping != null && customerShopping.AssignedCheckOutLine != null)
            checkOut = customerShopping.AssignedCheckOutLine.GetComponent<CheckOutCounter>();

        isPayingWithCard = Random.value > 0.5f;
        exactChange = Random.value > 0.5f;

        paymentAmount = 0f;
        _sequenceStarted = false;
        _paymentCompleted = false;

        if (moneyPrefab != null) moneyPrefab.SetActive(false);
        if (cardPrefab != null) cardPrefab.SetActive(false);

        // reset arm so we don't accumulate rotations each time
        if (ArmR != null && _armBaseCached)
            ArmR.localRotation = _armBaseLocalRot;

        // Enter "waiting with money out" mode right away
        SetHandMoneyIdle(true);

        // stop any previous loop if we re-entered
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();

        // Leave waiting mode
        SetHandMoneyIdle(false);

        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    public override void PerformAction()
    {
        if (checkOut == null) return;

        if (checkOut.allItemsScanned && !_sequenceStarted)
        {
            _sequenceStarted = true;

            ShowPropAndComputePayment();

            // Put arm into a “present” pose once (optional)
            if (ArmR != null && _armBaseCached)
                ArmR.localRotation = _armBaseLocalRot * Quaternion.Euler(90f, 0f, 0f);

            _loopRoutine = StartCoroutine(OfferLoop());
        }
    }
    private bool madeOffer = false;
    private IEnumerator OfferLoop()
    {
        // While the player hasn’t taken it, keep offering
        while (!_paymentCompleted)
        {
            // Fire the offer animation
            if(!madeOffer)
            PlayOffer();
            madeOffer = true;
            // Wait for it to finish (or timeout). Animator should return to HandMoneyIdle automatically
            // because HandMoneyIdle bool is still true.
            yield return WaitForOfferFinishOrTimeout(maxOfferSeconds);

            // Hang out in idle a bit before repeating
            float t = 0f;
            while (t < repeatOfferEverySeconds && !_paymentCompleted)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        onArrivedAtItem?.Invoke();
    }

    private void ShowPropAndComputePayment()
    {
        if (checkOut == null) return;

        if (isPayingWithCard)
        {
            if (cardPrefab != null) cardPrefab.SetActive(true);
        }
        else
        {
            if (moneyPrefab != null) moneyPrefab.SetActive(true);

            if (exactChange)
            {
                paymentAmount = checkOut.GetTotalPrice();
            }
            else
            {
                var (autoAmount, autoBills, chosenOutcome) =
                    Checkout.Cash.GenerateTendered(checkOut.GetTotalPrice(), myWeights, singleDenominationWhenPossible: true);

                // Kept consistent with your current behavior:
                paymentAmount = Checkout.Cash.ChangeDue(checkOut.GetTotalPrice(), autoAmount);
            }
        }
    }

    private void SetHandMoneyIdle(bool value)
    {
        if (animator == null) return;
        if (_handMoneyIdleBoolHash != 0)
            animator.SetBool(_handMoneyIdleBoolHash, value);
    }

    private void PlayOffer()
    {
        if (animator == null) return;
        if (_handMoneyTriggerHash != 0)
            animator.SetTrigger(_handMoneyTriggerHash);
    }

    private IEnumerator WaitForOfferFinishOrTimeout(float maxSeconds)
    {
        if (animator == null) yield break;

        bool canDetect = !string.IsNullOrWhiteSpace(handMoneyStateTag) || !string.IsNullOrWhiteSpace(handMoneyStateName);
        if (!canDetect)
        {
            float startFallback = Time.time;
            while (Time.time - startFallback < maxSeconds && !_paymentCompleted)
                yield return null;
            yield break;
        }

        float start = Time.time;
        bool entered = false;

        bool Matches(AnimatorStateInfo info)
        {
            bool tagOk = !string.IsNullOrWhiteSpace(handMoneyStateTag) && info.IsTag(handMoneyStateTag);
            bool nameOk = !string.IsNullOrWhiteSpace(handMoneyStateName) && info.IsName(handMoneyStateName);
            return tagOk || nameOk;
        }

        while (Time.time - start < maxSeconds && !_paymentCompleted)
        {
            var info = animator.GetCurrentAnimatorStateInfo(animLayer);

            if (Matches(info))
                entered = true;

            // "Finished" once we've been in the offer state and it played through
            if (entered && Matches(info) && info.normalizedTime >= 1f && !animator.IsInTransition(animLayer))
                yield break;

            yield return null;
        }
        // Timeout falls through
    }

    // Called externally when the player takes cash
    public void PaymentCash()
    {
        if (moneyPrefab != null) moneyPrefab.SetActive(false);

        if (checkOut != null)
            checkOut.saleSystem.UpdateCashAndChange(paymentAmount);

        _paymentCompleted = true;
    }

    // Called externally when the player takes card
    public void PaymentCard()
    {
        if (cardPrefab != null) cardPrefab.SetActive(false);

        if (checkOut != null)
            checkOut.saleSystem.PayWithCard();

        _paymentCompleted = true;
    }
}
