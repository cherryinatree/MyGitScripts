using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Inventory
{
    public interface IBeamHarvestable
    {
        bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin);
    }

    [AddComponentMenu("Cherry/Inventory/Storage Container")]
    public class StorageContainer : MonoBehaviour, IBeamHarvestable
    {
        public interface IStorageAbsorbRule
        {
            bool CanAbsorbItem(ItemDefinition item, int amount);
        }
        // Optional: expose read-only view for UI later
        [Header("Storage Capacity")]
        [SerializeField, Min(1)] private int slotCapacity = 24;
        [SerializeField, Min(1)] private int stackLimit = 99;

        [Header("State")]
        [SerializeField] private List<ItemStack> slots = new();

        [Header("Absorb Thrown Items")]
        [Tooltip("If true, absorbs items via trigger events. Works when this collider is Trigger OR the drop enters a Trigger.")]
        [SerializeField] private bool absorbOnTrigger = true;

        [Tooltip("If true, absorbs items via collision events (non-trigger collider contact).")]
        [SerializeField] private bool absorbOnCollision = true;

        [Header("Beam Harvest (Dispense)")]
        [Tooltip("Prefab that flies to the player. Must have PickupMover component.")]
        [SerializeField] private GameObject pickupPrefab;

        [Tooltip("Amount dispensed per harvest call.")]
        [SerializeField, Min(1)] private int perPickup = 1;

        [Tooltip("Cooldown between dispense attempts (seconds). Helps because your ClickRaycaster calls HandleHit every frame).")]
        [SerializeField, Min(0f)] private float dispenseCooldown = 0.12f;

        [Tooltip("Spawn offset from hit point when dispensing.")]
        [SerializeField] private Vector3 dispenseSpawnOffset = Vector3.up * 0.05f;

        public event Action OnStorageChanged;

        private float _lastDispenseTime = -999f;

        public AudioSource audioSource;
        public AudioClip FillSound;
        public AudioClip dispenseSound;

        private void Awake()
        {
            EnsureSlotListSize();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnValidate()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            stackLimit = Mathf.Max(1, stackLimit);
            EnsureSlotListSize();
        }
        public int TryRemoveAtIndex(int slotIndex, int amount)
        {
            if (amount <= 0) return 0;

            EnsureSlotListSize();

            if (slotIndex < 0 || slotIndex >= slots.Count)
                return 0;

            var s = slots[slotIndex];

            if (s.IsEmpty)
                return 0;

            int removed = Mathf.Min(amount, s.amount);

            s.amount -= removed;

            if (s.amount <= 0)
                s.Clear();

            slots[slotIndex] = s;

            if (removed > 0)
                OnStorageChanged?.Invoke();

            return removed;
        }

        public int TryRemove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            EnsureSlotListSize();

            int remaining = amount;
            int removedTotal = 0;

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];

                if (s.IsEmpty || s.item != item)
                    continue;

                int removed = Mathf.Min(remaining, s.amount);

                s.amount -= removed;

                if (s.amount <= 0)
                    s.Clear();

                slots[i] = s;

                remaining -= removed;
                removedTotal += removed;
            }

            if (removedTotal > 0)
                OnStorageChanged?.Invoke();

            return removedTotal;
        }

        public void NotifyStorageChanged()
        {
            OnStorageChanged?.Invoke();
        }
        private void EnsureSlotListSize()
        {
            if (slots == null) slots = new List<ItemStack>();

            // Keep exactly slotCapacity entries
            if (slots.Count > slotCapacity)
                slots.RemoveRange(slotCapacity, slots.Count - slotCapacity);

            while (slots.Count < slotCapacity)
                slots.Add(default);
        }

        // -------------------- Absorb --------------------
        private void OnTriggerEnter(Collider other)
        {
            if (!absorbOnTrigger) return;
            TryAbsorbFrom(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!absorbOnCollision) return;
            TryAbsorbFrom(collision.gameObject);
        }

        private void TryAbsorbFrom(GameObject go)
        {
            if (go == null) return;

            // Must be a world drop
            var drop = go.GetComponentInParent<WorldItemDrop>();
            if (drop == null) return;

            if (!drop.TryPeek(out var itemDef, out var amt)) return;

            // Ask any rule components if this item is allowed into storage.
            var rules = GetComponents<IStorageAbsorbRule>();
            for (int i = 0; i < rules.Length; i++)
            {
                if (!rules[i].CanAbsorbItem(itemDef, amt))
                    return;
            }

            // Attempt to store (can be partial)
            int added = TryAdd(itemDef, amt);
            if (added <= 0) return;

            // Play sound
            if (audioSource != null && FillSound != null)
            {
                audioSource.PlayOneShot(FillSound);
            }

            bool empty = drop.Consume(added);
            OnStorageChanged?.Invoke();

            // If fully consumed, remove the physical object
            if (empty)
                Destroy(go);
        }

        // -------------------- Beam Harvest (Dispense) --------------------
        public bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin)
        {
            if (pickupPrefab == null) return false;
            if (beamOrigin == null) return false;
            if (Time.time - _lastDispenseTime < dispenseCooldown) return false;

            // find first non-empty slot
            int slotIndex = FindFirstNonEmptySlot();
            if (slotIndex < 0) return false;

            var s = slots[slotIndex];
            if (s.IsEmpty) return false;

            int take = Mathf.Min(perPickup, s.amount);
            var itemDef = s.item;

            // Reserve immediately (important: prevents duplication if ray hits every frame)
            s.amount -= take;
            if (s.amount <= 0) s.Clear();
            slots[slotIndex] = s;
            OnStorageChanged?.Invoke();

            _lastDispenseTime = Time.time;

            // spawn pickup mover
            Vector3 spawnPos = hitPoint + dispenseSpawnOffset;
            var go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

            var mover = go.GetComponent<PickupMover>();
            if (mover == null)
            {
                Debug.LogError($"{name}: pickupPrefab requires PickupMover.");
                // refund
                TryAdd(itemDef, take);
                OnStorageChanged?.Invoke();
                Destroy(go);
                return false;
            }

            mover.Initialize(
                item: itemDef,
                amount: take,
                target: beamOrigin,
                onArrive: (success) =>
                {
                    // If container got destroyed mid-flight, can't refund; just stop.
                    if (!this) return;

                    if (!success)
                    {
                        // Refund back into container if player inventory was full / sink missing
                        TryAdd(itemDef, take);
                        OnStorageChanged?.Invoke();
                    }
                });
            
            // Play sound
            if (audioSource != null && dispenseSound != null)
            {
                audioSource.PlayOneShot(dispenseSound);
            }

            return true;
        }

        private int FindFirstNonEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty)
                    return i;
            return -1;
        }

        // -------------------- Storage Logic --------------------
        /// <summary>Adds up to amount. Returns how many were added.</summary>
        public int TryAdd(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;
            EnsureSlotListSize();

            int cap = Mathf.Min(stackLimit, item.ItemMaxStack);
            int remaining = amount;
            int addedTotal = 0;

            // 1) Fill existing stacks
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];
                if (s.item != item || s.IsEmpty) continue;

                int space = cap - s.amount;
                if (space <= 0) continue;

                int add = Mathf.Min(space, remaining);
                s.amount += add;
                slots[i] = s;

                remaining -= add;
                addedTotal += add;
            }

            // 2) Use empty slots
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty) continue;

                int add = Mathf.Min(cap, remaining);
                s.item = item;
                s.amount = add;
                slots[i] = s;

                remaining -= add;
                addedTotal += add;
            }

            return addedTotal;
        }

        // Optional: expose read-only view for UI later
        public IReadOnlyList<ItemStack> Slots => slots;
        public int SlotCapacity => slotCapacity;
        public int StackLimit => stackLimit;
    }
}
