using UnityEngine;
using UnityEngine.UI;        // for Slider, Image
using TMPro;                 // optional (only if you use TextMeshPro)

/**
 * Attach this to a HUD GameObject.
 * - Assign: PlayerTerror (on the player), Slider, (optional) Fill Image, (optional) TMP Text.
 * - It auto-syncs slider max/value and can colorize + pulse at high terror.
 */
public class TerrorHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerTerror playerTerror;      // your terror meter component
    public Slider slider;                  // UI slider to visualize terror
    public Image fillImage;                // optional: slider fill image for color
    public TextMeshProUGUI valueText;      // optional: numeric readout

    [Header("Display")]
    [Tooltip("Smooth the slider toward the real value.")]
    public float smoothing = 10f;
    [Tooltip("Map terror % to color. Left=0%, Right=100%.")]
    public Gradient colorByPercent;        // assign in inspector (e.g., green→yellow→red)
    [Tooltip("Format for valueText (use {0} for integer, {1:0} for percent).")]
    public string textFormat = "{0} / {1} ({2:0}%)";

    [Header("Pulse At High Terror")]
    public bool pulseWhenHigh = true;
    [Range(0, 100)] public float pulseThreshold = 80f;
    [Tooltip("How strongly the fill scales during pulse.")]
    public float pulseScale = 1.08f;
    public float pulseSpeed = 5f;

    // internals
    private float _displayValue; // smoothed value
    private Vector3 _fillBaseScale = Vector3.one;

    void Awake()
    {
        if (!playerTerror) playerTerror = FindObjectOfType<PlayerTerror>();
        if (!slider)
        {
            slider = GetComponentInChildren<Slider>();
        }

        if (fillImage)
            _fillBaseScale = fillImage.rectTransform.localScale;
    }

    void Start()
    {
        if (playerTerror && slider)
        {
            slider.minValue = 0f;
            slider.maxValue = playerTerror.Max;
            _displayValue = Mathf.Clamp(playerTerror.Current, 0f, playerTerror.Max);
            slider.value = _displayValue;
            UpdateVisuals(instant: true);
        }
    }

    void Update()
    {
        if (!playerTerror || !slider) return;

        // keep max synced in case it changes at runtime
        if (!Mathf.Approximately(slider.maxValue, playerTerror.Max))
            slider.maxValue = playerTerror.Max;

        float target = Mathf.Clamp(playerTerror.Current, 0f, playerTerror.Max);

        // smooth slider value
        float speed = Mathf.Max(0f, smoothing);
        _displayValue = (speed > 0f)
            ? Mathf.Lerp(_displayValue, target, Time.deltaTime * speed)
            : target;

        slider.value = _displayValue;

        UpdateVisuals(instant: false);
    }

    private void UpdateVisuals(bool instant)
    {
        float pct = (slider.maxValue > 0f) ? (_displayValue / slider.maxValue) : 0f;

        // color by percent
        if (fillImage && colorByPercent.colorKeys.Length > 0)
            fillImage.color = colorByPercent.Evaluate(pct);

        // text
        if (valueText)
        {
            float current = Mathf.Round(_displayValue);
            float max = slider.maxValue;
            float percent100 = pct * 100f;
            valueText.text = string.Format(textFormat, current, max, percent100);
        }

        // pulse when high
        if (pulseWhenHigh && fillImage)
        {
            if (pct * 100f >= pulseThreshold)
            {
                float s = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
                fillImage.rectTransform.localScale = _fillBaseScale * s;
            }
            else
            {
                fillImage.rectTransform.localScale = _fillBaseScale;
            }
        }
    }
}
