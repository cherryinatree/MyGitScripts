using UnityEngine;

[AddComponentMenu("Cherry/World/Elevator Switch - Power")]
public class ElevatorPowerSwitch : MonoBehaviour
{
    [SerializeField] private ElevatorGridController elevator;
    [SerializeField] private bool startOn = false;

    private bool _isOn;

    private void Awake()
    {
        _isOn = startOn;
        if (elevator != null) elevator.SetPower(_isOn);
    }

    public void Interact()
    {
        _isOn = !_isOn;
        if (elevator != null) elevator.SetPower(_isOn);
    }

    public bool IsOn => _isOn;
}
