public interface IMotor
{
    void SetMoveInput(UnityEngine.Vector2 move);
    void SetLookInput(UnityEngine.Vector2 look);
    void Jump();
    void SetSprinting(bool sprint);
}
