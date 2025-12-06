using UnityEngine;
using UnityEngine.InputSystem;

namespace Cherry.Inventory
{
    [AddComponentMenu("Cherry/Inventory/Right Click Eject (Selected Slot)")]
    public class InventoryRightClickEjectSelected : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryUI inventoryUI;      // selection comes from here
        [SerializeField] private Camera aimCamera;             // if null uses Camera.main
        [SerializeField] private Transform spawnOriginOverride; // if null uses camera transform

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference rightClickAction;

        [Header("Eject Rate")]
        [SerializeField] private bool ejectWhileHeld = true;
        [SerializeField, Min(0.01f)] private float ejectInterval = 0.12f;

        [Header("World Drop Prefab (PHYSICS OBJECT)")]
        [Tooltip("This prefab should have: Collider + Rigidbody, AND HarvestableItemSource (so beam can pick it up).")]
        [SerializeField] private GameObject worldDropPrefab;

        [Tooltip("Prefab that flies to the player when harvested by beam (must have PickupMover).")]
        [SerializeField] private GameObject pickupFlyPrefab;

        [Header("Spawn Offsets")]
        [SerializeField, Min(0f)] private float spawnForward = 0.7f;
        [SerializeField] private float spawnUp = -0.05f;
        [SerializeField, Min(0.01f)] private float spawnRadius = 0.12f;
        [SerializeField] private LayerMask spawnBlockMask = ~0;

        [Header("Throw")]
        [SerializeField, Min(0f)] private float throwVelocity = 8.5f;
        [SerializeField, Min(0f)] private float upwardVelocity = 0.75f;
        [SerializeField, Min(0f)] private float randomAngularVelocity = 10f;

        [Header("Collision")]
        [SerializeField] private bool ignorePlayerCollisions = true;

        private bool _held;
        private float _timer;

        private Inventory Inv => inventoryUI != null ? inventoryUI.GetInventory() : null;
        private Camera Cam => aimCamera != null ? aimCamera : Camera.main;
        private Transform Origin => spawnOriginOverride != null ? spawnOriginOverride : (Cam != null ? Cam.transform : transform);

        private void OnEnable()
        {
            if (rightClickAction == null || rightClickAction.action == null)
            {
                Debug.LogError($"{name}: Assign rightClickAction (Mouse Right Button).", this);
                enabled = false;
                return;
            }

            rightClickAction.action.started += OnPress;
            rightClickAction.action.canceled += OnRelease;
            rightClickAction.action.Enable();
        }

        private void OnDisable()
        {
            if (rightClickAction != null && rightClickAction.action != null)
            {
                rightClickAction.action.started -= OnPress;
                rightClickAction.action.canceled -= OnRelease;
                rightClickAction.action.Disable();
            }
            _held = false;
        }

        private void OnPress(InputAction.CallbackContext _)
        {
            _held = true;
            _timer = 0f;

            // eject immediately
            TryEjectOne();
        }

        private void OnRelease(InputAction.CallbackContext _)
        {
            _held = false;
        }

        private void Update()
        {
            if (!_held || !ejectWhileHeld) return;

            _timer += Time.deltaTime;
            while (_timer >= ejectInterval)
            {
                _timer -= ejectInterval;

                if (!TryEjectOne())
                {
                    _held = false;
                    break;
                }
            }
        }

        private bool TryEjectOne()
        {
            if (Inv == null || Cam == null || worldDropPrefab == null) return false;

            int selected = inventoryUI.SelectedIndex;
            if (selected < 0 || selected >= Inv.Slots.Count) return false;

            var stack = Inv.Slots[selected];
            if (stack.IsEmpty) return false;

            // take ONE
            if (!Inv.TryTakeOne(selected, out var itemDef)) return false;

            // spawn
            Vector3 dir = Cam.transform.forward;
            Vector3 spawnPos = ComputeSafeSpawn(Origin.position, dir, Cam.transform.up);
            Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

            var go = Instantiate(worldDropPrefab, spawnPos, spawnRot);
            var drop = go.GetComponent<Cherry.Inventory.WorldItemDrop>();
            if (drop != null) drop.Set(itemDef, 1);
            // configure beam-harvest pickup behavior (your existing system)
            var harvest = go.GetComponent<HarvestableItemSource>();
            if (harvest != null)
            {
                harvest.item = itemDef;
                harvest.quantity = 1;
                harvest.perPickup = 1;
                harvest.pickupPrefab = pickupFlyPrefab;
                harvest.reserveOnSpawn = false;
                harvest.spawnCooldown = 0.5f;

                // IMPORTANT: recommended patch below adds this flag (safe even if missing)
                // harvest.scaleWithQuantity = false;
            }
            else
            {
                Debug.LogWarning($"{name}: worldDropPrefab has no HarvestableItemSource, so your beam won't pick it up.", go);
            }

            // physics throw
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();

            if (ignorePlayerCollisions) IgnoreCollisionsWithPlayer(go);

            rb.linearVelocity = dir * throwVelocity + Cam.transform.up * upwardVelocity;
            if (randomAngularVelocity > 0f)
                rb.angularVelocity = Random.insideUnitSphere * randomAngularVelocity;

            return true;
        }

        private Vector3 ComputeSafeSpawn(Vector3 origin, Vector3 forward, Vector3 up)
        {
            Vector3 desired = origin + forward * spawnForward + up * spawnUp;
            float dist = Vector3.Distance(origin, desired);

            if (dist > 0.001f &&
                Physics.SphereCast(origin, spawnRadius, forward, out var hit, dist, spawnBlockMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0.05f, hit.distance - spawnRadius);
                return origin + forward * safeDist + up * spawnUp;
            }

            return desired;
        }

        private void IgnoreCollisionsWithPlayer(GameObject spawned)
        {
            var playerCols = GetComponentsInChildren<Collider>(true);
            var spawnedCols = spawned.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < playerCols.Length; i++)
                for (int j = 0; j < spawnedCols.Length; j++)
                    if (playerCols[i] && spawnedCols[j])
                        Physics.IgnoreCollision(playerCols[i], spawnedCols[j], true);
        }
    }
}
