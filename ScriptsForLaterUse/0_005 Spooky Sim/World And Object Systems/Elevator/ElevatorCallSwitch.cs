using UnityEngine;

[AddComponentMenu("Cherry/World/Elevator Switch - Call Stop")]
public class ElevatorCallSwitch : MonoBehaviour
{
    [SerializeField] private ElevatorGridController elevator;
    [SerializeField] private int stopIndex = 0;

    public void Interact()
    {
        if (elevator != null) elevator.CallStop(stopIndex);
    }
}
