using UnityEngine;
using System;
using System.Collections.Generic;

public class RemodelManager : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private List<string> ownedUpgradeIds = new();

    public event Action OnRemodelChanged;

    public bool HasUpgrade(string upgradeId) => ownedUpgradeIds.Contains(upgradeId);

    public bool TryApply()
    {
       
        OnRemodelChanged?.Invoke();

        // If you use NavMeshSurface and room layout changes, rebuild here:
        // var surf = FindObjectOfType<NavMeshSurface>(); if (surf) surf.BuildNavMesh();

        return true;
    }

    public IEnumerable<string> OwnedUpgradeIds() => ownedUpgradeIds;
}
