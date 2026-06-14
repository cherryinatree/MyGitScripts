using System.Collections.Generic;
using Remodeling.Data;
using Remodeling.Runtime;
using UnityEngine;

namespace Remodeling.Runtime
{
    public class ShipRemodelController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform shipRoot;
        [SerializeField] private Transform DeliveryPoint;
        [SerializeField] private ShipSizeTierApplier sizeApplier;
        [SerializeField] private PlayerStats stats;

        [Header("Catalog")]
        [SerializeField] private UpgradeCatalogSO catalog;

        private readonly Dictionary<string, UpgradeDefinitionSO> _lookup = new();

        private void Awake()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup.Clear();
            if (!catalog || catalog.upgrades == null) return;

            foreach (var u in catalog.upgrades)
                if (u && !string.IsNullOrWhiteSpace(u.id))
                    _lookup[u.id] = u;
        }

        public RemodelContext CreateContext(PlayerUpgradeState state) => new RemodelContext
        {
            shipRoot = shipRoot,
            sizeApplier = sizeApplier,
            playerState = state,
            stats = stats
        };

       /* public void ApplyBaseline(PlayerUpgradeState state)
        {
            // Apply tier first (so SizeTierObjects are correct)
            sizeApplier.Apply(state.SizeTier);

            // Apply owned upgrades on top
            foreach (var id in state.OwnedUpgradeIds)
            {
                if (!_lookup.TryGetValue(id, out var def) || def.actions == null) continue;

                var ctx = CreateContext(state);
                foreach (var a in def.actions)
                    if (a)
                        if (def.category == UpgradeCategory.Automation) 
                        {
                            a.Apply(ctx, DeliveryPoint);
                        }
                        else a.Apply(ctx); // baseline doesn't need undo
            }
        }*/
        public void ApplyBaseline(PlayerUpgradeState state)
        {
            sizeApplier.Apply(state.SizeTier);

            foreach (var p in state.Purchases)
            {
                if (!_lookup.TryGetValue(p.id, out var def) || def.actions == null) continue;
                Debug.Log("Applying baseline for upgrade ID: " + p.id + " with count: " + p.count);
                var ctx = CreateContext(state);
                ctx.currentUpgradeId = p.id;
                ctx.currentPurchaseCount = Mathf.Max(0, p.count);

                foreach (var a in def.actions)
                {
                    if (!a) continue;

                    if (a.ApplyPerPurchase)
                    {
                        for (int i = 0; i < ctx.currentPurchaseCount; i++)
                        {
                            if (ValidateIfSpawnObject(def.category))
                            {
                                a.Apply(ctx, DeliveryPoint);
                            }
                            else
                            {
                                a.Apply(ctx); // baseline doesn't need undo
                            }
                        }
                    }
                    else
                    {
                        if (ValidateIfSpawnObject(def.category))
                        {
                            a.Apply(ctx, DeliveryPoint);
                        }
                        else
                        {
                            a.Apply(ctx); // baseline doesn't need undo
                        }
                    }
                }
            }
        }

        private bool ValidateIfSpawnObject(UpgradeCategory category)
        {
            if(category == UpgradeCategory.Automation)
                return true;
            if (category == UpgradeCategory.Collector)
                return true;
            if (category == UpgradeCategory.ArHeadset)
                return true;
            if (category == UpgradeCategory.Rig)
                return true;
            if (category == UpgradeCategory.Misc)
                return true;
            return false;
        }


        public List<IRemodelUndo> ApplyUpgradeWithUndo(PlayerUpgradeState state, UpgradeDefinitionSO def)
        {
            var undos = new List<IRemodelUndo>();
            if (!def || def.actions == null) return undos;

            var ctx = CreateContext(state);
            ctx.currentUpgradeId = def.id;
            ctx.currentPurchaseCount = state.GetCount(def.id) + 1; // preview next level

            foreach (var a in def.actions)
            {
                if (!a) continue;

                if (a.ApplyPerPurchase)
                {
                    // preview only ONE additional purchase
                    var undo = a.Apply(ctx);
                    if (undo != null) undos.Add(undo);
                }
                else
                {
                    var undo = a.Apply(ctx);
                    if (undo != null) undos.Add(undo);
                }
            }

            return undos;
        }

    }
}
