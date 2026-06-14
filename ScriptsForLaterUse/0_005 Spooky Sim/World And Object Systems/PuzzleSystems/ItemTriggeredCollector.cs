using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cherry.Inventory;

namespace Cherry.Puzzles
{
    public interface ICollectorEffect
    {
        void OnFed(ItemTriggeredCollector collector, ItemDefinition item, int amountFed, int progress, int required);
        void OnActivated(ItemTriggeredCollector collector, ItemDefinition item);
        void OnDeactivated(ItemTriggeredCollector collector, ItemDefinition item);
    }

    [AddComponentMenu("Cherry/Puzzles/Item Triggered Collector")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ItemTriggeredCollector : MonoBehaviour
    {
        [Serializable]
        public class Reaction
        {
            [Header("Item Match")]
            public ItemDefinition item;

            [Header("Cost / Requirement")]
            [Min(1)] public int requiredAmount = 1;

            [Tooltip("If true, consumes from the incoming WorldItemDrop.")]
            public bool consumeOnFeed = true;

            [Header("Activation Behavior")]
            [Tooltip("0 = stays active until Deactivate() is called.")]
            [Min(0f)] public float activeDuration = 0f;

            [Tooltip("If true, collector will not accept items while active.")]
            public bool lockWhileActive = true;

            [Tooltip("If true, progress resets when deactivated.")]
            public bool resetProgressOnDeactivate = true;

            [Header("Events")]
            public UnityEvent onFed;
            public UnityEvent onActivated;
            public UnityEvent onDeactivated;

            [Header("Optional Effects (components implementing ICollectorEffect)")]
            public List<MonoBehaviour> effects = new();
        }

        [Header("Reactions")]
        [SerializeField] private List<Reaction> reactions = new();

        [Header("Debug")]
        [SerializeField] private bool logDebug;

        public bool IsActive => _isActive;
        public ItemDefinition ActiveItem => _activeReaction?.item;
        public int Progress => _progress; 
        public int Required => _activeReaction != null ? _activeReaction.requiredAmount : 0;

        private Reaction _activeReaction;
        private bool _isActive;
        private float _activeUntil;
        private int _progress;

        private void Awake()
        {
            // Collector typically uses trigger hits
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
        }

        private void Update()
        {
            if (!_isActive) return;

            if (_activeReaction != null && _activeReaction.activeDuration > 0f && Time.time >= _activeUntil)
                Deactivate();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Incoming "ammo" / thrown item / blob should have WorldItemDrop somewhere on it
            var drop = other.GetComponentInParent<WorldItemDrop>();
            if (drop == null) return;

            if (!drop.TryPeek(out var item, out var amt)) return;

            TryFeedFromWorldDrop(drop, item, amt);
        }

        /// <summary>
        /// Feed using a WorldItemDrop payload. Will consume only what it needs.
        /// </summary>
        private bool TryFeedFromWorldDrop(WorldItemDrop drop, ItemDefinition item, int availableAmt)
        {
            var reaction = FindReaction(item);
            if (reaction == null)
            {
                if (logDebug) Debug.Log($"{name}: Rejected {item.name} (no reaction).", this);
                return false;
            }

            if (_isActive && reaction.lockWhileActive)
            {
                if (logDebug) Debug.Log($"{name}: Ignored feed while active (lockWhileActive).", this);
                return false;
            }

            // If active on a different item, deactivate first to keep behavior deterministic.
            if (_isActive && _activeReaction != reaction)
                Deactivate();

            _activeReaction ??= reaction;

            int needed = Mathf.Max(0, reaction.requiredAmount - _progress);
            if (needed == 0)
            {
                // Already satisfied, just activate if not active (should be rare).
                if (!_isActive) Activate(reaction);
                return false;
            }

            // Only take what we need (prevents over-consuming big stacks)
            int take = Mathf.Clamp(availableAmt, 1, needed);

            // Consume from payload (optional)
            if (reaction.consumeOnFeed)
            {
                bool empty = drop.Consume(take);
                if (empty && drop) Destroy(drop.gameObject);
            }

            _progress += take;

            reaction.onFed?.Invoke();
            CallEffects(reaction, fx => fx.OnFed(this, item, take, _progress, reaction.requiredAmount));

            if (logDebug) Debug.Log($"{name}: Fed {item.name} (+{take}) -> {_progress}/{reaction.requiredAmount}", this);

            if (_progress >= reaction.requiredAmount)
                Activate(reaction);

            return true;
        }

        /// <summary>
        /// If you ever want to feed without a physical WorldItemDrop (ex: ray directly),
        /// call this and handle inventory consumption elsewhere.
        /// </summary>
        public bool TryFeed(ItemDefinition item, int amount = 1)
        {
            if (item == null) return false;

            var reaction = FindReaction(item);
            if (reaction == null) return false;

            if (_isActive && reaction.lockWhileActive) return false;

            if (_isActive && _activeReaction != reaction)
                Deactivate();

            _activeReaction ??= reaction;

            int needed = Mathf.Max(0, reaction.requiredAmount - _progress);
            int take = Mathf.Clamp(amount, 1, Mathf.Max(1, needed));
            _progress += take;

            reaction.onFed?.Invoke();
            CallEffects(reaction, fx => fx.OnFed(this, item, take, _progress, reaction.requiredAmount));

            if (_progress >= reaction.requiredAmount)
                Activate(reaction);

            return true;
        }

        public void Deactivate()
        {
            if (!_isActive) return;

            var reaction = _activeReaction;
            var item = reaction?.item;

            _isActive = false;

            reaction?.onDeactivated?.Invoke();
            if (reaction != null)
                CallEffects(reaction, fx => fx.OnDeactivated(this, item));

            if (reaction != null && reaction.resetProgressOnDeactivate)
                _progress = 0;

            _activeReaction = null;

            if (logDebug) Debug.Log($"{name}: Deactivated ({item?.name}).", this);
        }

        private void Activate(Reaction reaction)
        {
            _isActive = true;

            if (reaction.activeDuration > 0f)
                _activeUntil = Time.time + reaction.activeDuration;

            reaction.onActivated?.Invoke();
            CallEffects(reaction, fx => fx.OnActivated(this, reaction.item));

            if (logDebug)
            {
                string dur = reaction.activeDuration > 0f ? $"{reaction.activeDuration:0.##}s" : "until Deactivate()";
                Debug.Log($"{name}: Activated by {reaction.item.name} ({dur}).", this);
            }
        }

        private Reaction FindReaction(ItemDefinition item)
        {
            for (int i = 0; i < reactions.Count; i++)
            {
                var r = reactions[i];
                if (r != null && r.item == item) return r;
            }
            return null;
        }

        private static void CallEffects(Reaction reaction, Action<ICollectorEffect> call)
        {
            if (reaction.effects == null) return;

            for (int i = 0; i < reaction.effects.Count; i++)
            {
                var mb = reaction.effects[i];
                if (mb is ICollectorEffect fx) call(fx);
            }
        }
    }
}