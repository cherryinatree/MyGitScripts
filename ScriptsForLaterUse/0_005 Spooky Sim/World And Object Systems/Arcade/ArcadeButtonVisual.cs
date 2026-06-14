using UnityEngine;

public class ArcadeButtonVisual : MonoBehaviour
{
    [Header("Button Motion")]
    [SerializeField] private Transform buttonTop;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.025f, 0f);
    [SerializeField] private float pressSpeed = 18f;

    private Vector3 releasedPosition;
    private bool pressed;

    private void Awake()
    {
        if (buttonTop == null)
            buttonTop = transform;

        releasedPosition = buttonTop.localPosition;
    }

    private void Update()
    {
        Vector3 target =
            pressed
                ? releasedPosition + pressedLocalOffset
                : releasedPosition;

        buttonTop.localPosition =
            Vector3.Lerp(
                buttonTop.localPosition,
                target,
                Time.deltaTime * pressSpeed);
    }

    public void SetPressed(bool value)
    {
        pressed = value;
    }
}