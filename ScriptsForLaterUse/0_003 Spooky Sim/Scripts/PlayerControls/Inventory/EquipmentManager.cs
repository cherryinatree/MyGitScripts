using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cherry.Inventory
{







/// <summary>
/// Equipment manager with hotkey cycling (1-4) per slot, visual activation/instantiation,
/// and a quick "stow/draw" motion during swaps.
/// 
/// - Press 1 = RightHand, 2 = LeftHand, 3 = Rig, 4 = Vision (configurable).
/// - Cycles through items in the Inventory that match the pressed slot type.
/// - Swaps using your existing inventory semantics (returns current to inventory, consumes 1 of next).
/// - Optionally shows/hides (or spawns) the item's visual for that slot.
/// - Optional Animator triggers for fancier put-away/draw; otherwise a simple dip motion.
/// </summary>
public class EquipmentManager : MonoBehaviour
    {


















        [Header("Inventory")]
        [SerializeField] private Inventory inventory;

        // Indexed by (int)EquipmentSlotType
        [SerializeField] private ItemDefinition[] equipped;

        public event Action OnEquipmentChanged;

        #region Input bindings
        [Serializable]
        private struct KeyBinding
        {
            public EquipmentSlotType slot;
            public KeyCode key;
        }

        [Header("Hotkeys")]
        [Tooltip("Press these keys to cycle equipment for the given slot.")]
        [SerializeField]
        private KeyBinding[] keyBindings = new KeyBinding[]
        {
            new KeyBinding{ slot = (EquipmentSlotType)1, key = KeyCode.Alpha1 }, // RightHand
            new KeyBinding{ slot = (EquipmentSlotType)2, key = KeyCode.Alpha2 }, // LeftHand
            new KeyBinding{ slot = (EquipmentSlotType)3, key = KeyCode.Alpha3 }, // Rig
            new KeyBinding{ slot = (EquipmentSlotType)4, key = KeyCode.Alpha4 }, // Vision
        };
        #endregion

        #region Mounts & visuals
        [Serializable]
        public struct SlotMount
        {
            public EquipmentSlotType slot;
            public Transform mount; // where visuals attach/move
        }

        [Serializable]
        public struct VisualBinding
        {
            public ItemDefinition item;

            [Tooltip("Existing scene object to enable/disable (leave Prefab empty).")]
            public GameObject existingInstance;

            [Tooltip("If no existing instance, this prefab will be instantiated under the slot's mount and cached.")]
            public GameObject prefab;
        }

        [Header("Visuals")]
        [Tooltip("Where to parent visuals for each slot.")]
        [SerializeField] private List<SlotMount> slotMounts = new List<SlotMount>();

        [Tooltip("Map item -> (existing instance | prefab). If both are set, 'existingInstance' is used.")]
        [SerializeField] private List<VisualBinding> visualBindings = new List<VisualBinding>();

        [Tooltip("If true, we Destroy instantiated visuals when unequipped; otherwise we SetActive(false) and reuse.")]
        [SerializeField] private bool destroyInstantiatedOnUnequip = false;

        // runtime caches
        private readonly Dictionary<EquipmentSlotType, Transform> _mountBySlot = new();
        private readonly Dictionary<ItemDefinition, GameObject> _existingByItem = new();
        private readonly Dictionary<ItemDefinition, GameObject> _prefabByItem = new();
        private readonly Dictionary<ItemDefinition, GameObject> _spawnedCacheByItem = new();
        private readonly Dictionary<EquipmentSlotType, GameObject> _activeVisualBySlot = new();
        #endregion

        #region Swap motion
        [Header("Swap Motion")]
        [Tooltip("Optional animator on the player/hands for nicer motion.")]
        [SerializeField] private Animator swapAnimator;

        [Tooltip("Animator trigger to play when putting away current item.")]
        [SerializeField] private string putAwayTrigger = "PutAway";

        [Tooltip("Animator trigger to play when drawing the new item.")]
        [SerializeField] private string drawTrigger = "Draw";

        [Tooltip("If no Animator, we dip the mount by this distance (local units).")]
        [SerializeField] private float dipDistance = 0.15f;

        [Tooltip("Time for each half of the dip motion (down/up) if no Animator is present.")]
        [SerializeField] private float dipHalfTime = 0.12f;

        // per-slot lock to avoid re-entrant swaps
        private bool[] _slotSwapInProgress;
        #endregion

        private void Awake()
        {
            // Ensure equipped array covers enum
            if (equipped == null || equipped.Length == 0)
                equipped = new ItemDefinition[Enum.GetValues(typeof(EquipmentSlotType)).Length];

            _slotSwapInProgress = new bool[equipped.Length];

            // Build dictionaries for mounts and visuals
            _mountBySlot.Clear();
            foreach (var m in slotMounts)
                if (!_mountBySlot.ContainsKey(m.slot) && m.mount) _mountBySlot.Add(m.slot, m.mount);

            _existingByItem.Clear(); _prefabByItem.Clear();
            foreach (var vb in visualBindings)
            {
                if (vb.item == null) continue;
                if (vb.existingInstance != null) _existingByItem[vb.item] = vb.existingInstance;
                if (vb.prefab != null) _prefabByItem[vb.item] = vb.prefab;
            }

            foreach (EquipmentSlotType slot in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                var item = equipped[(int)slot];
                if (item != null)
                {
                    RefreshSlotVisual(slot, null, item);
                }
            }
        }


        #region Public API (unchanged + new)
        public ItemDefinition GetEquipped(EquipmentSlotType slot) => equipped[(int)slot];

        public bool TryEquipFromInventory(int invIndex)
        {
            if (inventory == null || !inventory.IsIndexValid(invIndex)) return false;
            var s = inventory.Slots[invIndex];
            if (s.IsEmpty || !s.item.IsEquipment) return false;
            return TryEquip(invIndex, s.item);
        }

        /// <summary>
        /// Swap-in item from the given inventory index (consumes 1),
        /// returns currently-equipped to inventory (if any).
        /// </summary>
        public bool TryEquip(int fromInventoryIndex, ItemDefinition item)
        {
            if (!item || !item.IsEquipment) return false;
            var slot = item.EquipSlot;
            var cur = equipped[(int)slot];

            // If equipping same item type that's already equipped, no-op success.
            if (cur == item) return true;

            // Try to stash current back to inventory
            if (cur != null)
            {
                if (!inventory.TryAddItem(cur, 1, out int leftover) || leftover > 0)
                    return false; // no space to swap
            }

            // Remove 1 of the new item from inventory and equip it
            if (!inventory.TryConsumeAtIndex(fromInventoryIndex, 1)) return false;

            equipped[(int)slot] = item;
            inventory.MarkDirty();

            // Visuals
            RefreshSlotVisual(slot, cur, item);

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool UnequipToInventory(EquipmentSlotType slot)
        {
            var cur = equipped[(int)slot];
            if (cur == null) return false;
            if (!inventory.TryAddItem(cur, 1, out int leftover) || leftover > 0) return false;
            equipped[(int)slot] = null;

            RefreshSlotVisual(slot, cur, null);
            OnEquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>Cycle to the next item (by inventory order) matching the slot type.</summary>
        public void CycleNextForSlot(EquipmentSlotType slot)
        {
            if (_slotSwapInProgress[(int)slot]) return;
            StartCoroutine(CycleNextRoutine(slot));
        }
        #endregion

        #region Cycling logic
        private IEnumerator CycleNextRoutine(EquipmentSlotType slot)
        {
            _slotSwapInProgress[(int)slot] = true;

            // 1) Play put-away motion
            yield return PlayPutAway(slot);

            var (foundIndex, targetItem, onlyOneAndSame) = FindNextItemIndexForSlot(slot);

            if (onlyOneAndSame)
            {
                // Nothing to change; just draw back
                yield return PlayDraw(slot);
                _slotSwapInProgress[(int)slot] = false;
                yield break;
            }

            if (foundIndex >= 0 && targetItem != null)
            {
                // Attempt swap
                TryEquip(foundIndex, targetItem);
            }
            // else: no candidate found => just draw back

            // 3) Play draw motion
            yield return PlayDraw(slot);

            _slotSwapInProgress[(int)slot] = false;
        }

        /// <summary>
        /// Finds the next distinct item in inventory for the given slot, cycling after currently-equipped.
        /// Returns (inventoryIndex, item, onlyOneAndSame as guard).
        /// </summary>
        private (int invIndex, ItemDefinition item, bool onlyOneAndSame) FindNextItemIndexForSlot(EquipmentSlotType slot)
        {
            var cur = equipped[(int)slot];

            // Collect distinct item types in inventory that match slot
            var distinct = new List<ItemDefinition>();
            var firstIndexForType = new Dictionary<ItemDefinition, int>();

            if (inventory != null && inventory.Slots != null)
            {
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    var s = inventory.Slots[i];
                    if (s.IsEmpty || s.item == null || !s.item.IsEquipment) continue;
                    if (s.item.EquipSlot != slot) continue;

                    if (!firstIndexForType.ContainsKey(s.item))
                    {
                        firstIndexForType[s.item] = i;
                        distinct.Add(s.item);
                    }
                }
            }

            if (distinct.Count == 0)
                return (-1, null, false);

            // If only 1 candidate and it's what we already have, don't swap
            if (distinct.Count == 1 && cur == distinct[0])
                return (-1, null, true);

            // Where are we in the set?
            int curIdx = (cur != null) ? distinct.IndexOf(cur) : -1;
            int nextIdx = (curIdx + 1 + distinct.Count) % distinct.Count;

            // If the next is actually the current (e.g., inventory has only current), advance again
            if (distinct[nextIdx] == cur)
                nextIdx = (nextIdx + 1) % distinct.Count;

            var target = distinct[nextIdx];
            int invIndex = firstIndexForType[target];
            return (invIndex, target, false);
        }
        #endregion

        #region Visuals
        private void RefreshSlotVisual(EquipmentSlotType slot, ItemDefinition oldItem, ItemDefinition newItem)
        {
            // Disable old
            if (oldItem != null)
                SetItemVisualActive(slot, oldItem, false);

            // Enable new
            if (newItem != null)
                SetItemVisualActive(slot, newItem, true);
            else
                _activeVisualBySlot.Remove(slot);
        }

        private void SetItemVisualActive(EquipmentSlotType slot, ItemDefinition item, bool active)
        {
            // existing mapped object?
            if (_existingByItem.TryGetValue(item, out var existing))
            {
                existing.SetActive(active);
                _activeVisualBySlot[slot] = active ? existing : null;
                return;
            }

            // spawned cached?
            if (_spawnedCacheByItem.TryGetValue(item, out var cached))
            {
                if (active)
                {
                    AttachToMount(slot, cached);
                    cached.SetActive(true);
                    _activeVisualBySlot[slot] = cached;
                }
                else
                {
                    if (destroyInstantiatedOnUnequip)
                    {
                        Destroy(cached);
                        _spawnedCacheByItem.Remove(item);
                    }
                    else
                    {
                        cached.SetActive(false);
                    }
                    _activeVisualBySlot[slot] = null;
                }
                return;
            }

            // need to spawn from prefab?
            if (active && _prefabByItem.TryGetValue(item, out var prefab) && prefab != null)
            {
                var mount = GetMount(slot);
                if (!mount)
                {
                    Debug.LogWarning($"No mount set for slot {slot}; cannot show visual for {item?.name}.");
                    return;
                }
                var inst = Instantiate(prefab, mount, false);
                ResetLocal(inst.transform);
                _spawnedCacheByItem[item] = inst;
                _activeVisualBySlot[slot] = inst;
                inst.SetActive(true);
                return;
            }

            // If we got here, we have nothing to show/hide (no mapping), which is fine.
        }

        // Helper: re-parent a visual to the correct slot mount and reset local transform.
        private void AttachToMount(EquipmentSlotType slot, GameObject go)
        {
            if (!go) return;
            var mount = GetMount(slot);
            if (!mount) return;

            go.transform.SetParent(mount, false);
            ResetLocal(go.transform); // sets localPosition/Rotation/Scale
        }

        private Transform GetMount(EquipmentSlotType slot)
        {
            return _mountBySlot.TryGetValue(slot, out var t) ? t : null;
        }

        private static void ResetLocal(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        #endregion

        #region Motion helpers
        private IEnumerator PlayPutAway(EquipmentSlotType slot)
        {
            if (swapAnimator != null && !string.IsNullOrEmpty(putAwayTrigger))
            {
                swapAnimator.SetTrigger(putAwayTrigger);
                // Small delay helps hide pop during equip even if animator has 0-len state
                yield return new WaitForSeconds(dipHalfTime);
                yield break;
            }

            // Fallback dip on the slot's mount
            var mount = GetMount(slot);
            if (!mount) { yield break; }

            Vector3 start = mount.localPosition;
            Vector3 end = start + Vector3.down * dipDistance;

            float t = 0f;
            while (t < dipHalfTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / dipHalfTime);
                mount.localPosition = Vector3.Lerp(start, end, a);
                yield return null;
            }
        }

        private IEnumerator PlayDraw(EquipmentSlotType slot)
        {
            if (swapAnimator != null && !string.IsNullOrEmpty(drawTrigger))
            {
                swapAnimator.SetTrigger(drawTrigger);
                yield return new WaitForSeconds(dipHalfTime);
                yield break;
            }

            var mount = GetMount(slot);
            if (!mount) { yield break; }

            Vector3 end = mount.localPosition; // currently at dipped pos (or wherever it is)
            Vector3 target = end - Vector3.down * dipDistance; // move back up by same amount

            float t = 0f;
            while (t < dipHalfTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / dipHalfTime);
                mount.localPosition = Vector3.Lerp(end, target, a);
                yield return null;
            }
        }
        #endregion
    }

}

