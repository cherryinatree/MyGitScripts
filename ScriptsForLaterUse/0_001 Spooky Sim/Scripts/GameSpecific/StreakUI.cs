// StreakHUD.cs
using System;
using UnityEngine;
using UnityEngine.UI;   // for legacy UGUI Text (optional)
using TMPro;           // for TextMeshPro (optional)

public class StreakHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your AnomalyRoomGenerator in the scene. If left empty, the script will try to find one.")]
    public ProcGen.Anomalies.AnomalyRoomGenerator generator;

    [Tooltip("Assign a TextMeshProUGUI (TMP) label here (optional).")]
    public TMP_Text tmpLabel;

    [Tooltip("Assign a legacy UGUI Text label here (optional).")]
    public TMP_Text uguiLabel;

    [Header("Display")]
    [Tooltip("Use {0} where the streak number should appear.")]
    public string format = "Rooms Correct: {0}";

    [Header("Behavior")]
    [Tooltip("If true, try to auto-locate the generator at runtime.")]
    public bool autoFindGenerator = true;

    // Subscribe on enable, unsubscribe on disable (safe if scene reloads)
    private void OnEnable()
    {
        TryFindGenerator();

        if (generator != null)
        {
            generator.OnStreakChanged += HandleStreakChanged;
            // Initialize immediately with current value
            HandleStreakChanged(generator.CorrectStreak);
        }
        else
        {
            SetText(string.Format(format, 0));
        }
    }

    private void OnDisable()
    {
        if (generator != null)
            generator.OnStreakChanged -= HandleStreakChanged;
    }

    // Event handler from AnomalyRoomGenerator
    private void HandleStreakChanged(int value)
    {
        SetText(string.Format(format, value));
    }

    // --- Helpers --------------------------------------------------------

    private void TryFindGenerator()
    {
        if (generator || !autoFindGenerator) return;

#if UNITY_2023_1_OR_NEWER
        generator = FindFirstObjectByType<ProcGen.Anomalies.AnomalyRoomGenerator>();
#else
        generator = FindObjectOfType<AnomalyRoomGenerator>();
#endif
    }

    private void SetText(string s)
    {
        if (tmpLabel) tmpLabel.text = s;
       // if (uguiLabel) uguiLabel.text = s;
    }

    // Optional: quick test in editor (remove if you don't want it)
#if UNITY_EDITOR
    private void Update()
    {
        // Press '+' to bump the label (useful if generator isn't hooked yet)
        //if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        //d    SetText(string.Format(format, ParseCurrent() + 1));
    }

    private int ParseCurrent()
    {
        string current = tmpLabel ? tmpLabel.text : (uguiLabel ? uguiLabel.text : "0");
        // naive parse: look for the last space and parse what follows
        int idx = current.LastIndexOf(' ');
        if (idx >= 0 && int.TryParse(current.Substring(idx + 1), out var n)) return n;
        return 0;
    }
#endif
}
