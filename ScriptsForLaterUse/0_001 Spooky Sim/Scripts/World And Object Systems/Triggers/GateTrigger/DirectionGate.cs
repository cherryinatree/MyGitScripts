using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DirectionGate : MonoBehaviour
{
    public enum Order { A_then_B, B_then_A }

    // --- UnityEvents you can wire up in the Inspector ---
    [System.Serializable] public class PlayerEvent : UnityEvent<GameObject> { }

    [Header("Events")]
    [Tooltip("Fires once when the correct approach order is detected within the time window.")]
    public PlayerEvent OnApproachEstablished;

    [Tooltip("Fires when appear is allowed (same moment as OnApproachEstablished).")]
    public PlayerEvent OnAllowAppear;

    [Tooltip("Fires when you notify the gate that the appear action has actually happened.")]
    public PlayerEvent OnAppearFired;

    [Tooltip("Fires after appear has happened, enabling retreat logic.")]
    public PlayerEvent OnAllowRetreat;

    [Tooltip("Fires when ResetGate() is called.")]
    public UnityEvent OnGateReset;

    [Header("Sensors")]
    public GateSensor sensorA;
    public GateSensor sensorB;

    [Header("Direction Config")]
    [Tooltip("Which order counts as approaching the corner.")]
    public Order approachOrder = Order.A_then_B;

    [Header("Timing")]
    [Tooltip("Max seconds allowed between hitting first and second gate to count as a pass.")]
    public float maxGateWindow = 2.5f;

    [Header("Filtering")]
    [Tooltip("Only track objects with this tag (empty = no filter).")]
    public string playerTag = "Player";

    private struct PassInfo { public string first; public float time; }
    private readonly Dictionary<GameObject, PassInfo> _lastPass = new();

    private bool _directionEstablished;
    private bool _appearHasFired;
    private GameObject _lastPlayer; // last object that satisfied the approach (optional to pass to events)

    [Header("Re-arm")]
    [SerializeField] private bool autoResetAfterAppear = true;
    [SerializeField] private float autoResetDelay = 0f; // seconds

    private void Awake()
    {
        if (sensorA) sensorA.gate = this;
        if (sensorB) sensorB.gate = this;
    }

    /// <summary>
    /// Called by GateSensor when something enters either gate. gateId must be "A" or "B".
    /// </summary>
    public void NotifyGateEntered(string gateId, GameObject obj)
    {
        if (!string.IsNullOrEmpty(playerTag) && !obj.CompareTag(playerTag)) return;

        var now = Time.time;

        if (_lastPass.TryGetValue(obj, out var info))
        {
            if (now - info.time <= maxGateWindow)
            {
                var pair = info.first + gateId; // "AB" or "BA"
                bool isApproach =
                    (approachOrder == Order.A_then_B && pair == "AB") ||
                    (approachOrder == Order.B_then_A && pair == "BA");

                if (isApproach && !_directionEstablished)
                {
                    //_directionEstablished = true;
                    _lastPlayer = obj;


                    // Fire events so you can plug in any function from other scripts
                    OnApproachEstablished?.Invoke(obj);
                    OnAllowAppear?.Invoke(obj);
                }
            }
        }

        // record/update first hit
        _lastPass[obj] = new PassInfo { first = gateId, time = now };
    }

    /// <summary>
    /// Call this when your "appear" action actually triggers (e.g., enemy spawned).
    /// You can wire this from a button, an animation event, or a script.
    /// </summary>
    // Change NotifyAppearFired like this:
    public void NotifyAppearFired()
    {
        _appearHasFired = true;

        if (_directionEstablished)
        {
            OnAppearFired?.Invoke(_lastPlayer);
            OnAllowRetreat?.Invoke(_lastPlayer);

            if (autoResetAfterAppear)
                StartCoroutine(ResetAfterDelay(autoResetDelay));
        }
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        ResetGate(); // re-arms the gate for the next pass
    }

    /// <summary>
    /// Optional: reset state if you need the setup to be reusable later.
    /// </summary>
    public void ResetGate()
    {
        _lastPass.Clear();
        _directionEstablished = false;
        _appearHasFired = false;
        _lastPlayer = null;


        OnGateReset?.Invoke();
    }

    // --- Optional helpers if you prefer code registration instead of Inspector ---
    public void AddOnApproachEstablishedListener(UnityAction<GameObject> cb) => OnApproachEstablished.AddListener(cb);
    public void AddOnAllowAppearListener(UnityAction<GameObject> cb) => OnAllowAppear.AddListener(cb);
    public void AddOnAppearFiredListener(UnityAction<GameObject> cb) => OnAppearFired.AddListener(cb);
    public void AddOnAllowRetreatListener(UnityAction<GameObject> cb) => OnAllowRetreat.AddListener(cb);
    public void AddOnResetListener(UnityAction cb) => OnGateReset.AddListener(cb);
}
