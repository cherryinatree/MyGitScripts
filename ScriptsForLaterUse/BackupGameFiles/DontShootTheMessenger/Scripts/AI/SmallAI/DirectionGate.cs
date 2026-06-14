using System.Collections.Generic;
using UnityEngine;

public class DirectionGate : MonoBehaviour
{
    public enum Order { A_then_B, B_then_A }

    [Header("Sensors")]
    public GateSensor sensorA;
    public GateSensor sensorB;

    [Header("Direction Config")]
    [Tooltip("Which order counts as approaching the corner.")]
    public Order approachOrder = Order.A_then_B;

    [Header("Controlled Triggers")]
    public GameObject appearTrigger;   // your existing appear trigger (EnemyTrigger with makeAppear = true)
    public GameObject retreatTrigger;  // your existing retreat trigger (EnemyTrigger with makeAppear = false)

    [Header("Timing")]
    [Tooltip("Max seconds allowed between hitting first and second gate to count as a pass.")]
    public float maxGateWindow = 2.5f;

    private struct PassInfo { public string first; public float time; }
    private readonly Dictionary<GameObject, PassInfo> _lastPass = new();

    private bool _directionEstablished;
    private bool _appearHasFired;

    private void Awake()
    {
        if (sensorA) sensorA.gate = this;
        if (sensorB) sensorB.gate = this;

        // Start with triggers off until direction is known
        if (appearTrigger) appearTrigger.SetActive(false);
        if (retreatTrigger) retreatTrigger.SetActive(false);
    }

    public void NotifyGateEntered(string gateId, GameObject playerObj)
    {
        var now = Time.time;

        if (_lastPass.TryGetValue(playerObj, out var info))
        {
            // second hit within window?
            if (now - info.time <= maxGateWindow)
            {
                var pair = info.first + gateId; // "AB" or "BA"

                bool isApproach =
                    (approachOrder == Order.A_then_B && pair == "AB") ||
                    (approachOrder == Order.B_then_A && pair == "BA");

                if (isApproach && !_directionEstablished)
                {
                    _directionEstablished = true;
                    // allow the appear trigger now
                    if (appearTrigger) appearTrigger.SetActive(true);
                }
            }
        }

        // record/update first hit
        _lastPass[playerObj] = new PassInfo { first = gateId, time = now };
    }

    /// <summary>
    /// Call this from the Appear trigger when it actually fires,
    /// so we only allow Retreat after the enemy has shown up.
    /// </summary>
    public void NotifyAppearFired()
    {
        _appearHasFired = true;
        if (_directionEstablished && retreatTrigger)
            retreatTrigger.SetActive(true);
    }

    /// <summary>
    /// Optional: reset state if you need the setup to be reusable later.
    /// </summary>
    public void ResetGate()
    {
        _lastPass.Clear();
        _directionEstablished = false;
        _appearHasFired = false;
        if (appearTrigger) appearTrigger.SetActive(false);
        if (retreatTrigger) retreatTrigger.SetActive(false);
    }
}
