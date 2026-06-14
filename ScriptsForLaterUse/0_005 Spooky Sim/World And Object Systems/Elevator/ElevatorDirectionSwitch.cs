using UnityEngine;

[AddComponentMenu("Cherry/World/Elevator Switch - Direction")]
public class ElevatorDirectionSwitch : MonoBehaviour
{
    public enum Axis { Vertical, Horizontal }
    public enum SwitchStyle { TwoPosition, ThreePosition }

    [SerializeField] private ElevatorGridController elevator;
    [SerializeField] private Axis axis = Axis.Vertical;
    [SerializeField] private SwitchStyle style = SwitchStyle.TwoPosition;

    // -1 = negative (Down/Left), 0 = Neutral, +1 = positive (Up/Right)
    [SerializeField] private int startState = 0;

    private int _state;

    private void Awake()
    {
        _state = Mathf.Clamp(startState, -1, 1);
        Apply();
    }

    public void Interact()
    {
        if (style == SwitchStyle.TwoPosition)
        {
            // toggle between -1 and +1 (no neutral)
            _state = (_state >= 0) ? -1 : +1;
        }
        else
        {
            // cycle: -1 -> 0 -> +1 -> 0 -> -1 ...
            _state = _state switch
            {
                -1 => 0,
                0 => +1,
                +1 => 0,
                _ => 0
            };
        }

        Apply();
    }

    private void Apply()
    {
        if (elevator == null) return;

        if (axis == Axis.Vertical) elevator.SetManualVertical(_state);
        else elevator.SetManualHorizontal(_state);
    }

    public int State => _state;
}
