using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class ArcadeTicketStripCollectible : MonoBehaviour
{
    [Header("Visual Ticket Segments")]
    [SerializeField] private GameObject ticketSegmentPrefab;
    [SerializeField] private Transform segmentParent;
    [SerializeField] private Vector3 segmentRotation = Vector3.forward;

    [Header("Ticket Strip Shape")]
    [Tooltip("Direction tickets come out in local space. Use 0,0,1 or 0,0,-1 most of the time.")]
    [SerializeField] private Vector3 segmentLocalDirection = Vector3.forward;

    [SerializeField] private float segmentSpacing = 0.075f;
    [SerializeField] private int maxVisualSegments = 60;

    [Header("Dispense Timing")]
    [SerializeField] private float timeBetweenSegments = 0.035f;
    [SerializeField] private float segmentGrowSpeed = 12f;

    [Header("Wave Motion")]
    [SerializeField] private bool animateWave = true;
    [SerializeField] private Vector3 waveAxis = Vector3.up;
    [SerializeField] private float waveAmplitude = 0.025f;
    [SerializeField] private float waveSpeed = 7f;
    [SerializeField] private float wavePhaseOffset = 0.55f;

    [Header("Collecting")]
    [SerializeField] private bool collectOnMouseDown = true;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Collider")]
    [SerializeField] private Vector3 colliderPadding = new Vector3(0.12f, 0.12f, 0.12f);
    [SerializeField] private float colliderWidth = 0.25f;
    [SerializeField] private float colliderHeight = 0.18f;

    [Header("Events")]
    public UnityEvent<int> onCollected;

    private readonly List<Transform> spawnedSegments = new();
    private readonly List<Vector3> basePositions = new();

    private ArcadeTicketInventory ticketInventory;
    private ArcadeTicketDispenser dispenser;

    private BoxCollider boxCollider;
    private Coroutine dispenseRoutine;

    private int totalTickets;
    private int targetVisualSegments;

    private bool collected;

    public int TotalTickets => totalTickets;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (segmentParent == null)
            segmentParent = transform;

        segmentLocalDirection =
            segmentLocalDirection.sqrMagnitude <= 0.001f
                ? Vector3.forward
                : segmentLocalDirection.normalized;

        waveAxis =
            waveAxis.sqrMagnitude <= 0.001f
                ? Vector3.up
                : waveAxis.normalized;
    }

    public void Initialize(
        int ticketAmount,
        ArcadeTicketInventory inventory,
        ArcadeTicketDispenser owningDispenser)
    {
        ticketInventory = inventory;
        dispenser = owningDispenser;

        AddMoreTickets(ticketAmount);
    }

    public void AddMoreTickets(int amount)
    {
        if (amount <= 0)
            return;

        totalTickets += amount;

        targetVisualSegments =
            Mathf.Clamp(
                totalTickets,
                1,
                maxVisualSegments);

        if (dispenseRoutine == null)
            dispenseRoutine = StartCoroutine(DispenseRoutine());

        UpdateCollider();
    }

    private IEnumerator DispenseRoutine()
    {
        while (spawnedSegments.Count < targetVisualSegments)
        {
            SpawnSegment(spawnedSegments.Count);

            UpdateCollider();

            yield return new WaitForSeconds(timeBetweenSegments);
        }

        dispenseRoutine = null;
    }

    private void SpawnSegment(int index)
    {
        if (ticketSegmentPrefab == null)
        {
            Debug.LogWarning("Ticket strip has no ticket segment prefab assigned.");
            return;
        }

        GameObject segment =
            Instantiate(
                ticketSegmentPrefab,
                segmentParent);

        Transform segmentTransform = segment.transform;

        Vector3 basePosition =
            segmentLocalDirection *
            segmentSpacing *
            index;

        segmentTransform.localPosition = basePosition;
        segmentTransform.localRotation = Quaternion.Euler(segmentRotation);
        segmentTransform.localScale = Vector3.zero;

        Collider childCollider = segment.GetComponent<Collider>();

        if (childCollider != null)
            childCollider.enabled = false;

        spawnedSegments.Add(segmentTransform);
        basePositions.Add(basePosition);

        StartCoroutine(GrowSegment(segmentTransform));
    }

    private IEnumerator GrowSegment(Transform segment)
    {
        if (segment == null)
            yield break;

        while (segment.localScale.x < 0.98f)
        {
            segment.localScale =
                Vector3.Lerp(
                    segment.localScale,
                    Vector3.one,
                    Time.deltaTime * segmentGrowSpeed);

            yield return null;
        }

        segment.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!animateWave)
            return;

        AnimateWave();
    }

    private void AnimateWave()
    {
        float time = Time.time * waveSpeed;

        for (int i = 0; i < spawnedSegments.Count; i++)
        {
            Transform segment = spawnedSegments[i];

            if (segment == null)
                continue;

            float wave =
                Mathf.Sin(time - i * wavePhaseOffset) *
                waveAmplitude;

            segment.localPosition =
                basePositions[i] +
                waveAxis * wave;
        }
    }

    private void UpdateCollider()
    {
        if (boxCollider == null)
            return;

        int count =
            Mathf.Max(
                spawnedSegments.Count,
                targetVisualSegments,
                1);

        float stripLength =
            Mathf.Max(
                segmentSpacing * count,
                segmentSpacing);

        Vector3 center =
            segmentLocalDirection *
            stripLength *
            0.5f;

        Vector3 size =
            new Vector3(
                colliderWidth,
                colliderHeight,
                stripLength) +
            colliderPadding;

        if (Mathf.Abs(segmentLocalDirection.x) >
            Mathf.Abs(segmentLocalDirection.z))
        {
            size =
                new Vector3(
                    stripLength,
                    colliderHeight,
                    colliderWidth) +
                colliderPadding;
        }

        boxCollider.center = center;
        boxCollider.size = size;
    }

    private void OnMouseDown()
    {
        if (!collectOnMouseDown)
            return;

        CollectTickets();
    }

    public void CollectTickets()
    {
        if (collected)
            return;

        collected = true;

        if (ticketInventory == null)
            ticketInventory = ArcadeTicketInventory.Instance;

        if (ticketInventory != null)
            ticketInventory.AddTickets(totalTickets);
        else
            Debug.LogWarning("No ArcadeTicketInventory found. Tickets were not added.");

        onCollected?.Invoke(totalTickets);

        if (dispenser != null)
            dispenser.NotifyTicketsCollected(this, totalTickets);

        if (destroyOnCollect)
            Destroy(gameObject);
    }
}