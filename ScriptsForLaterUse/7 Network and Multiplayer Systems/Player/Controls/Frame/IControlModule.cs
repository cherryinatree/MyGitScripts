public interface IControlModule
{
    /// Called when this module becomes active for the local player.
    void Activate();

    /// Called when this module is deactivated for the local player.
    void Deactivate();

    /// Per-frame tick for local player (read input, feed motors/abilities).
    void Tick(float dt);

    /// Optional: late update visual polish
    void LateTick(float dt);

    /// Whether this module wants the cursor visible/locked
    bool WantsCursorVisible { get; }
}
