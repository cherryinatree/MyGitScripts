// Assets/Dialogue/DialogueUI.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    public TMP_Text nameLabel;
    public TMP_Text subtitleLabel;
    public Transform choicesRoot;
    public Button choiceButtonPrefab;

    Action<int> _onChoice;

    public void ShowLine(string speakerName, string subtitle)
    {
        if (nameLabel) nameLabel.text = speakerName ?? "";
        if (subtitleLabel) subtitleLabel.text = subtitle ?? "";
        ClearChoices();
    }

    public void ClearLine()
    {
        if (subtitleLabel) subtitleLabel.text = "";
        if (nameLabel) nameLabel.text = "";
        ClearChoices();
    }

    public void ShowChoices(List<string> options, Action<int> onChoiceSelected)
    {
        ClearChoices();
        _onChoice = onChoiceSelected;
        for (int i = 0; i < options.Count; i++)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesRoot);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = options[i];
            int index = i;
            btn.onClick.AddListener(() => { _onChoice?.Invoke(index); });
        }
    }

    public void ClearChoices()
    {
        _onChoice = null;
        for (int i = choicesRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesRoot.GetChild(i).gameObject);
        }
    }
}
