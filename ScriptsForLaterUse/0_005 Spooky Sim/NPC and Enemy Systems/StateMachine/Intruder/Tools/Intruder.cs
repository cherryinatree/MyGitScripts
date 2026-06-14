using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class Intruder : MonoBehaviour
{
    public static readonly List<Intruder> All = new();

    [Header("Events")]
    public UnityEvent OnAlertRaised;
    public UnityEvent OnNeutralized;

    public Health Health { get; private set; }
    public bool IsActive => Health != null && !Health.IsDead;

    private void Awake()
    {
        Health = GetComponent<Health>();
        if (Health != null)
            Health.OnDied.AddListener(Neutralize);
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        UpdateAlert();
    }

    private void OnDisable()
    {
        All.Remove(this);
        UpdateAlert();
    }

    public void RaiseAlert()
    {
        OnAlertRaised?.Invoke();
        UpdateAlert();
    }

    public void Neutralize()
    {
        if (Health == null || Health.IsDead == false)
        {
            // If Neutralize called manually, force it dead
            if (Health != null) Health.TakeDamage(999999f, null);
        }

        OnNeutralized?.Invoke();
        UpdateAlert();
    }

    private void UpdateAlert()
    {
        bool any = false;
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i] != null && All[i].IsActive)
            {
                any = true;
                break;
            }
        }
        IntruderAlertSystem.SetAlert(any);
    }
}
