using System.Collections;
using UnityEngine;
using LightingSystem;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class LightAgent : MonoBehaviour
{
    [Header("Master Subscription")]
    [Tooltip("If not set, this agent will try to find a MasterLightController in the scene on Awake.")]
    public MasterLightController master;

    [Tooltip("Channel number for targeted control. -1 matches 'all' broadcasts; otherwise must match exactly.")]
    public int channel = 0;

    [Header("Phase & Index")]
    [Tooltip("Optional index used to shift phase between agents in patterns (e.g., 0,1,2...).")]
    public int sequenceIndex = 0;

    [Header("Initial State")]
    public bool startEnabled = true;

    private Light _light;
    private Color _baseColor;
    private float _baseIntensity;

    // Flicker state
    private Coroutine _flickerCo;
    private bool _isFlickering;

    // Pattern state
    private bool _patternActive;
    private PatternSettings _pattern;
    private float _startTime;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseColor = _light.color;
        _baseIntensity = _light.intensity;
        _light.enabled = startEnabled;

        if (master == null)
            master = FindObjectOfType<MasterLightController>();
    }

    private void OnEnable() => Subscribe(true);
    private void OnDisable() => Subscribe(false);

    private void Subscribe(bool enable)
    {
        if (master == null) return;

        if (enable)
        {
            master.OnToggle += HandleToggle;
            master.OnColor += HandleColor;
            master.OnIntensity += HandleIntensity;
            master.OnStartFlicker += HandleStartFlicker;
            master.OnStopFlicker += HandleStopFlicker;
            master.OnPattern += HandlePattern;
        }
        else
        {
            master.OnToggle -= HandleToggle;
            master.OnColor -= HandleColor;
            master.OnIntensity -= HandleIntensity;
            master.OnStartFlicker -= HandleStartFlicker;
            master.OnStopFlicker -= HandleStopFlicker;
            master.OnPattern -= HandlePattern;
        }
    }

    // -------- Event handlers (channel filtered) --------

    private bool ChannelMatches(int targetChannel) =>
        (targetChannel == -1) || (targetChannel == channel);

    private void HandleToggle(int targetChannel, bool on)
    {
        if (!ChannelMatches(targetChannel)) return;
        _light.enabled = on;
    }

    private void HandleColor(int targetChannel, Color c)
    {
        if (!ChannelMatches(targetChannel)) return;
        _baseColor = c;
        _light.color = c;
    }

    private void HandleIntensity(int targetChannel, float intensity)
    {
        if (!ChannelMatches(targetChannel)) return;
        _baseIntensity = Mathf.Max(0f, intensity);
        // If no pattern/flicker, apply immediately
        if (!_patternActive && !_isFlickering)
            _light.intensity = _baseIntensity;
    }

    private void HandleStartFlicker(int targetChannel, FlickerSettings settings)
    {
        if (!ChannelMatches(targetChannel)) return;
        if (_flickerCo != null) StopCoroutine(_flickerCo);
        _flickerCo = StartCoroutine(FlickerRoutine(settings));
    }

    private void HandleStopFlicker(int targetChannel)
    {
        if (!ChannelMatches(targetChannel)) return;
        if (_flickerCo != null) StopCoroutine(_flickerCo);
        _isFlickering = false;
        _flickerCo = null;
        // restore base intensity (or pattern will override in Update)
        if (!_patternActive) _light.intensity = _baseIntensity;
    }

    private void HandlePattern(int targetChannel, PatternSettings settings)
    {
        if (!ChannelMatches(targetChannel)) return;

        _pattern = settings;
        _patternActive = settings.mode != PatternMode.None;
        _startTime = Time.time; // reset phase origin

        // Immediately snap to base state
        if (!_patternActive)
        {
            _light.color = _baseColor;
            if (!_isFlickering) _light.intensity = _baseIntensity;
        }
    }

    // -------- Flicker --------
    private IEnumerator FlickerRoutine(FlickerSettings s)
    {
        _isFlickering = true;
        float endTime = (s.duration > 0f) ? Time.time + s.duration : float.PositiveInfinity;

        while (Time.time <= endTime)
        {
            float mult;
            if (s.hardOnOff)
            {
                mult = (Random.value > 0.5f) ? s.maxIntensityMultiplier : s.minIntensityMultiplier;
            }
            else
            {
                mult = Random.Range(s.minIntensityMultiplier, s.maxIntensityMultiplier);
            }

            // If pattern is active, flicker multiplies the pattern output.
            float target = GetPatternIntensityOrBase() * mult;
            _light.intensity = Mathf.Max(0f, target);

            float wait = Random.Range(s.minInterval, s.maxInterval);
            yield return new WaitForSeconds(wait);
        }

        _isFlickering = false;
        _flickerCo = null;
        if (!_patternActive)
            _light.intensity = _baseIntensity;
    }

    // -------- Pattern evaluation --------
    private void Update()
    {
        if (!_patternActive) return;

        float t = Time.time - _startTime;
        float phase = (sequenceIndex * _pattern.phaseOffsetPerAgent);
        float u = (t + phase) * _pattern.speed; // seconds * Hz = cycles
        float frac = u - Mathf.Floor(u);        // 0..1

        switch (_pattern.mode)
        {
            case PatternMode.Pulse:
                {
                    // intensity = base * (1 + amplitude * sin)
                    float w = Mathf.Sin(u * Mathf.PI * 2f);
                    float mult = 1f + _pattern.amplitude * w;
                    float intensity = _pattern.baseIntensity * mult;
                    _light.intensity = Mathf.Max(0f, intensity) * (_isFlickering ? 1f : 1f); // flicker multiplies in routine
                    _light.color = _baseColor;
                    break;
                }
            case PatternMode.Strobe:
                {
                    // square wave: on half cycle, off half cycle
                    bool on = frac < 0.5f;
                    float intensity = on ? _pattern.baseIntensity : 0f;
                    _light.intensity = intensity;   // flicker will multiply on top of this
                    _light.color = _baseColor;
                    break;
                }
            case PatternMode.ColorCycle:
                {
                    var grad = _pattern.colorGradient ?? DefaultRainbow();
                    _light.color = grad.Evaluate(frac);
                    _light.intensity = _pattern.baseIntensity;
                    break;
                }
        }
    }

    private float GetPatternIntensityOrBase()
    {
        if (!_patternActive) return _baseIntensity;

        float t = Time.time - _startTime;
        float phase = (sequenceIndex * _pattern.phaseOffsetPerAgent);
        float u = (t + phase) * _pattern.speed;
        float frac = u - Mathf.Floor(u);

        switch (_pattern.mode)
        {
            case PatternMode.Pulse:
                float w = Mathf.Sin(u * Mathf.PI * 2f);
                return Mathf.Max(0f, _pattern.baseIntensity * (1f + _pattern.amplitude * w));
            case PatternMode.Strobe:
                return (frac < 0.5f) ? _pattern.baseIntensity : 0f;
            case PatternMode.ColorCycle:
                return _pattern.baseIntensity;
            default:
                return _baseIntensity;
        }
    }

    private static Gradient DefaultRainbow()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.17f),
                new GradientColorKey(Color.green, 0.33f),
                new GradientColorKey(Color.cyan, 0.5f),
                new GradientColorKey(Color.blue, 0.67f),
                new GradientColorKey(new Color(0.5f, 0f, 1f), 0.83f),
                new GradientColorKey(Color.red, 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        return g;
    }
}
