using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ThemeSettings")]
[System.Serializable]
public class UiTheme : ScriptableObject
{
    [System.Serializable]
    public class Buttons1
    {
        [Header("Text")]
        public Sprite ButtonImage;
        public Color NormalColor;
        public Color HighlightColor;
        public Color PressedColor;
        public Color SelectedColor;

        public Color32 text;
    }
    [Header("PRESETS")]
    public Buttons1 custom1;
}
