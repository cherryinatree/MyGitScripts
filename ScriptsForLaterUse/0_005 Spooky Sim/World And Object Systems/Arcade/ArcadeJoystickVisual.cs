using UnityEngine;

public class ArcadeJoystickVisual : MonoBehaviour
{
    [Header("Joystick Pivot")]
    [Tooltip("This should be an empty object at the base of the joystick, not the knob mesh itself.")]
    [SerializeField] private Transform tiltPivot;

    [Header("Tilt")]
    [SerializeField] private float maxTiltAngle = 18f;
    [SerializeField] private float tiltSpeed = 14f;

    [Header("Axis Tuning")]
    [SerializeField] private bool invertLeftRight;
    [SerializeField] private bool invertUpDown;

    [Tooltip("Use X/Z tilt. If your model is built sideways, try changing the pivot hierarchy instead of rotating the mesh object.")]
    [SerializeField] private bool swapAxes;

    private Quaternion restingRotation;
    private Vector2 input;

    private void Awake()
    {
        if (tiltPivot == null)
            tiltPivot = transform;

        restingRotation = tiltPivot.localRotation;
    }

    private void Update()
    {
        float x = invertLeftRight ? -input.x : input.x;
        float y = invertUpDown ? -input.y : input.y;

        float tiltX;
        float tiltZ;

        if (!swapAxes)
        {
            tiltX = y * maxTiltAngle;
            tiltZ = -x * maxTiltAngle;
        }
        else
        {
            tiltX = -x * maxTiltAngle;
            tiltZ = y * maxTiltAngle;
        }

        Quaternion target =
            restingRotation *
            Quaternion.Euler(
                tiltX,
                tiltZ,
                0);

        tiltPivot.localRotation =
            Quaternion.Slerp(
                tiltPivot.localRotation,
                target,
                Time.deltaTime * tiltSpeed);
    }

    public void SetInput(Vector2 value)
    {
        input = Vector2.ClampMagnitude(value, 1f);
    }

    public void ResetJoystick()
    {
        input = Vector2.zero;
    }
}