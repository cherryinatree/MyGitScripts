using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public abstract class ControlModuleBase : MonoBehaviour, IControlModule
{
    protected PlayerInput PlayerInput { get; private set; }
    protected NetworkBehaviour Net { get; private set; }

    public virtual bool WantsCursorVisible => false;

    protected virtual void Awake()
    {
        PlayerInput = GetComponentInParent<PlayerInput>();
        Net = GetComponentInParent<NetworkBehaviour>();
    }

    public virtual void Activate() { enabled = true; }
    public virtual void Deactivate() { enabled = false; }

    public virtual void Tick(float dt) { }
    public virtual void LateTick(float dt) { }

    protected InputAction Action(string name) =>
        PlayerInput != null ? PlayerInput.actions.FindAction(name, true) : null;
}
