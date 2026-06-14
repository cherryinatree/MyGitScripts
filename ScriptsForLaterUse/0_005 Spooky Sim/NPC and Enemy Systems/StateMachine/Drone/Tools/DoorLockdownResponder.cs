using UnityEngine;

[DisallowMultipleComponent]
public class DoorLockdownResponder : MonoBehaviour
{
    [SerializeField] private IRobotDoorInteractable door;

    private void Awake()
    {
        if (door == null) door = GetComponent<IRobotDoorInteractable>();
    }

    private void OnEnable()
    {
        IntruderAlertSystem.OnAlertChanged += Handle;
        Handle(IntruderAlertSystem.AlertActive);
    }

    private void OnDisable()
    {
        IntruderAlertSystem.OnAlertChanged -= Handle;
    }

    private void Handle(bool alert)
    {
        if (door == null) return;
        door.IsLocked = alert;
    }
}
