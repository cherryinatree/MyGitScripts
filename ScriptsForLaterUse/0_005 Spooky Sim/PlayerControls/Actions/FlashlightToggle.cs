using Cherry.Combat;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlashlightToggle : MonoBehaviour
{
    [Header("Assign one of these")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private GameObject flashlightObject;

    [Header("Optional")]
    [SerializeField] private AudioSource toggleSfx;
    [SerializeField] private bool startOn = false;

    [Header("Battery")]
    public BatteryConsumer batteryConsumer; // set drainPerSecond in inspector
    public float batteryConsumptionPerSecond = 0.5f;

    private bool _isOn;

    private void Awake()
    {
        _isOn = startOn;
        ApplyState();

        if (batteryConsumer == null)
        {
            batteryConsumer = GameObject.Find("Player").GetComponent<BatteryConsumer>();
        }
    }

    private void Update()
    {

        if (!batteryConsumer.CanUse() && IsOn)
        {
            _isOn = false;
            ApplyState();
        }


#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Toggle();
        }
#else
        // Fallback if you ever switch back to old input system
        if (Input.GetKeyDown(KeyCode.F))
        {
            Toggle();
        }
#endif
    }

    public void Toggle()
    {

        if (batteryConsumer != null)
        {
            if (!batteryConsumer.CanUse()) return; // no battery, can't turn on
        }

        _isOn = !_isOn;
        ApplyState();

        if (toggleSfx) toggleSfx.Play();
    }

    private void ApplyState()
    {
        if (flashlightLight != null)
            flashlightLight.enabled = _isOn;

        if (flashlightObject != null)
            flashlightObject.SetActive(_isOn);


        if (batteryConsumer != null)
        {
            if (!_isOn)
                batteryConsumer.StopDrain("ClickRaycaster");
            else
            batteryConsumer.StartDrain("ClickRaycaster", batteryConsumptionPerSecond);
        }
    }

    public bool IsOn => _isOn;
}
