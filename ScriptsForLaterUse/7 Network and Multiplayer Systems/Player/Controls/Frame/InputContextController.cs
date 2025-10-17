using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// Attach to the same GameObject that has PlayerInput & NetworkObject
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkObject))]
public class InputContextController : NetworkBehaviour
{
    [Header("Default Action Map names (must exist in Input Actions asset)")]
    [SerializeField] private string gameplayMap = "Gameplay";
    [SerializeField] private string storeMap = "Store";
    [SerializeField] private string uiMap = "UI";
    [SerializeField] private string vehicleMap = "Vehicle";

    [Header("Context → Modules wiring (drag in the Inspector)")]
    [SerializeField] private List<MonoBehaviour> gameplayModules; // IControlModule
    [SerializeField] private List<MonoBehaviour> storeModules;    // IControlModule
    [SerializeField] private List<MonoBehaviour> uiModules;       // IControlModule
    [SerializeField] private List<MonoBehaviour> vehicleModules;  // IControlModule

    private readonly Stack<ControlContext> _contextStack = new();
    private readonly List<IControlModule> _allModules = new();
    private PlayerInput _playerInput;

    public ControlContext CurrentContext => _contextStack.Count > 0 ? _contextStack.Peek() : ControlContext.Gameplay;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        void collect(IEnumerable<MonoBehaviour> list)
        {
            if (list == null) return;
            foreach (var mb in list)
                if (mb is IControlModule mod && !_allModules.Contains(mod))
                    _allModules.Add(mod);
        }
        collect(gameplayModules);
        collect(storeModules);
        collect(uiModules);
        collect(vehicleModules);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        var pi = GetComponent<PlayerInput>();
        if (pi) pi.enabled = IsOwner;

        // Only the owner reads local input
        enabled = IsOwner;
        if (!IsOwner) return;

        // Default to Gameplay context
        ClearContexts();
        PushContext(ControlContext.Gameplay);
    }

    void Update()
    {
        if (!IsOwner) return;
        foreach (var mod in _activeModules) mod.Tick(Time.deltaTime);
        UpdateCursor();
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        foreach (var mod in _activeModules) mod.LateTick(Time.deltaTime);
    }

    // === Context management ===
    readonly List<IControlModule> _activeModules = new();

    public void PushContext(ControlContext ctx)
    {
        _contextStack.Push(ctx);
        ApplyContext();
    }

    public void PopContext()
    {
        if (_contextStack.Count > 1) // never pop the last
        {
            _contextStack.Pop();
            ApplyContext();
        }
    }

    public void ClearContexts()
    {
        _contextStack.Clear();
    }

    void ApplyContext()
    {
        if (!IsOwner) return;

        // Deactivate all
        foreach (var m in _activeModules) m.Deactivate();
        _activeModules.Clear();

        // Switch action map & enable modules
        var mapName = MapName(CurrentContext);
        if (!string.IsNullOrEmpty(mapName) && _playerInput != null)
            _playerInput.SwitchCurrentActionMap(mapName);

        foreach (var m in ModulesFor(CurrentContext))
        {
            m.Activate();
            _activeModules.Add(m);
        }

        UpdateCursor();
    }

    string MapName(ControlContext ctx) => ctx switch
    {
        ControlContext.Gameplay => gameplayMap,
        ControlContext.Store => storeMap,
        ControlContext.Vehicle => vehicleMap,
        ControlContext.UI => uiMap,
        _ => gameplayMap
    };

    IEnumerable<IControlModule> ModulesFor(ControlContext ctx)
    {
        List<MonoBehaviour> list = ctx switch
        {
            ControlContext.Gameplay => gameplayModules,
            ControlContext.Store => storeModules,
            ControlContext.Vehicle => vehicleModules,
            ControlContext.UI => uiModules,
            _ => gameplayModules
        };
        foreach (var mb in list)
            if (mb is IControlModule mod) yield return mod;
    }

    void UpdateCursor()
    {
        bool show = false;
        foreach (var m in _activeModules)
            show |= m.WantsCursorVisible;

        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
