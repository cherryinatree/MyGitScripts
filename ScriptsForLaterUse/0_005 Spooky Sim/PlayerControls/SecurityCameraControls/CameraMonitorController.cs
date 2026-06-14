using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Cherry.Cameras
{
    public interface IMonitorModuleProvider
    {
        bool HasMonitorModuleEquipped { get; }
    }

    [DisallowMultipleComponent]
    public class CameraMonitorController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ShipSecurityCameraRegistry registry;
        // add at top with the other fields
        [SerializeField] private SecurityMonitorFX fx;

        [Header("HUD")]
        [SerializeField] private GameObject monitorRoot;     // panel to show/hide
        [SerializeField] private RawImage monitorImage;      // displays RenderTexture
        [SerializeField] private TMP_Text monitorLabel;      // optional: "Cam 2/7 - Cargo"
        [SerializeField] private bool autoEnterOnHudOpen = true;

        [Header("Render Texture")]
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 512;

        [Header("Input (New Input System)")]
        [Tooltip("Cycle next camera (e.g., E)")]
        [SerializeField] private InputActionReference nextCam;
        [Tooltip("Cycle previous camera (e.g., Q)")]
        [SerializeField] private InputActionReference prevCam;
        [Tooltip("Exit/close camera mode (e.g., Esc or Tab release)")]
        [SerializeField] private InputActionReference exitCamMode;
        [Tooltip("Look delta (Mouse delta / Right Stick)")]
        [SerializeField] private InputActionReference look;
        [Tooltip("Move (WASD / Left Stick) controls slide X/Y")]
        [SerializeField] private InputActionReference move;
        [Tooltip("Zoom (Mouse scroll Y / triggers). Optional.")]
        [SerializeField] private InputActionReference zoom;

        [Header("Optional: Disable these while in camera mode")]
        [SerializeField] private Behaviour[] disableWhileInCameraMode;

        private RenderTexture _rt;
        private SecurityCameraRig _active;
        private int _index;

        private bool _hudOpen;
        private bool _inCameraMode;

        private IMonitorModuleProvider _moduleProvider;

        private void Awake()
        {
            if (registry == null) registry = FindFirstObjectByType<ShipSecurityCameraRegistry>();
            _moduleProvider = GetComponentInParent<IMonitorModuleProvider>();
            if (_moduleProvider == null)
            {
                var concrete = FindFirstObjectByType<MonitorModuleActiveProvider>();
                _moduleProvider = concrete;
            }


            EnsureRT();

            if (monitorRoot != null) monitorRoot.SetActive(false);
            if (monitorImage != null) monitorImage.texture = _rt;
        }

        private void OnDestroy()
        {
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
        }

        private void EnsureRT()
        {
            if (_rt != null) return;

            _rt = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "SecurityMonitorRT"
            };
            _rt.Create();
        }

        // Call these from your HUD open/close logic
        public void NotifyHudOpened()
        {
            _hudOpen = true;
            RefreshVisibility();

            if (autoEnterOnHudOpen && CanUseMonitor())
                EnterCameraMode();
        }

        public void NotifyHudClosed()
        {
            _hudOpen = false;
            ExitCameraMode();
            RefreshVisibility();
        }

        private bool CanUseMonitor()
        {
            // If you don’t have a module system yet, just return true here for testing.
            if (_moduleProvider == null) return false;
            return _moduleProvider.HasMonitorModuleEquipped;
        }
        private void RefreshVisibility()
        {
            bool canUse = _hudOpen && CanUseMonitor();

            if (monitorRoot != null)
                monitorRoot.SetActive(canUse);

            // If HUD is open and module just became active, enter camera mode automatically
            if (canUse)
            {
                if (!_inCameraMode)
                    EnterCameraMode();
            }
            else
            {
                // If module turned off (or HUD closed), force exit
                ExitCameraMode();
            }
        }

        private void Update()
        {
            // If you want it to update live when module gets equipped/unequipped while HUD open:
            if (_hudOpen)
                RefreshVisibility();

            if (!_inCameraMode || _active == null) return;

            float dt = Time.unscaledDeltaTime; // HUD usually runs unscaled
            Vector2 lookDelta = look != null ? look.action.ReadValue<Vector2>() : Vector2.zero;
            Vector2 moveDelta = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;

            float zoomDelta = 0f;
            if (zoom != null)
            {
                // For mouse scroll, value is usually big per frame. We treat it as "delta-like".
                zoomDelta = zoom.action.ReadValue<float>();
            }

            _active.ApplyControl(lookDelta, moveDelta, zoomDelta, dt);
        }

        private void OnEnable()
        {
            if (nextCam != null) nextCam.action.performed += _ => Cycle(+1);
            if (prevCam != null) prevCam.action.performed += _ => Cycle(-1);
            if (exitCamMode != null) exitCamMode.action.performed += _ => ExitCameraMode();
        }

        private void OnDisable()
        {
            if (nextCam != null) nextCam.action.performed -= _ => Cycle(+1);
            if (prevCam != null) prevCam.action.performed -= _ => Cycle(-1);
            if (exitCamMode != null) exitCamMode.action.performed -= _ => ExitCameraMode();
        }

        private void EnterCameraMode()
        {

            if (_inCameraMode) return;
            if (!_hudOpen || !CanUseMonitor()) return;
            if (fx != null) fx.SetEnabled(true);

            _inCameraMode = true;
            SetPlayerControlsEnabled(false);

            EnableAction(nextCam, true);
            EnableAction(prevCam, true);
            EnableAction(exitCamMode, true);
            EnableAction(look, true);
            EnableAction(move, true);
            EnableAction(zoom, true);

            if (registry == null || registry.Cameras.Count == 0)
            {
                SetLabel("No cameras found");
                return;
            }

            _index = Mathf.Clamp(_index, 0, registry.Cameras.Count - 1);
            SetActiveCamera(_index);
        }

        private void ExitCameraMode()
        {
            if (fx != null) fx.SetEnabled(false);

            if (!_inCameraMode) return;

            _inCameraMode = false;
            SetPlayerControlsEnabled(true);

            EnableAction(nextCam, false);
            EnableAction(prevCam, false);
            EnableAction(exitCamMode, false);
            EnableAction(look, false);
            EnableAction(move, false);
            EnableAction(zoom, false);

            if (_active != null)
            {
                _active.Deactivate();
                _active = null;
            }
        }

        private void Cycle(int dir)
        {
            if (!_inCameraMode) return;
            if (registry == null || registry.Cameras.Count == 0) return;

            _index = (_index + dir) % registry.Cameras.Count;
            if (_index < 0) _index += registry.Cameras.Count;

            SetActiveCamera(_index);
        }

        private void SetActiveCamera(int i)
        {
            if (registry == null || registry.Cameras.Count == 0) return;

            SecurityCameraRig next = registry.Cameras[i];
            if (next == null) return;

            if (_active != null) _active.Deactivate();
            _active = next;
            if (fx != null) fx.StaticBurst(0.15f, 0.9f);

            _active.Activate(_rt);

            SetLabel($"{i + 1}/{registry.Cameras.Count}  •  {_active.DisplayName}");
        }

        private void SetLabel(string text)
        {
            if (monitorLabel != null) monitorLabel.text = text;
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            if (disableWhileInCameraMode == null) return;

            for (int i = 0; i < disableWhileInCameraMode.Length; i++)
            {
                if (disableWhileInCameraMode[i] != null)
                    disableWhileInCameraMode[i].enabled = enabled;
            }
        }

        private static void EnableAction(InputActionReference a, bool enable)
        {
            if (a == null || a.action == null) return;
            if (enable) a.action.Enable();
            else a.action.Disable();
        }
    }
}
