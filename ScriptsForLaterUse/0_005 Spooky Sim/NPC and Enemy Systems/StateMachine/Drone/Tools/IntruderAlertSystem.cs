using System;

public static class IntruderAlertSystem
{
    public static bool AlertActive { get; private set; }
    public static event Action<bool> OnAlertChanged;

    public static void SetAlert(bool active)
    {
        if (AlertActive == active) return;
        AlertActive = active;
        OnAlertChanged?.Invoke(AlertActive);
    }
}
