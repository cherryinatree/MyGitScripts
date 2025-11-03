// ClaySwapGroup.cs
using UnityEngine;
using System.Collections.Generic;

public class ClaySwapGroup : MonoBehaviour
{
    [Tooltip("If true, children become frames in Hierarchy order.")]
    public bool autoCollectChildren = true;

    [Tooltip("Optional: supply frames explicitly. If empty and autoCollectChildren=true, children are used.")]
    public List<GameObject> frames = new List<GameObject>();

    [Tooltip("Start with this frame active.")]
    public int startIndex = 0;

    [Tooltip("Deactivate all other frames when switching.")]
    public bool deactivateOthers = true;

    int _current = -1;

    public int Count => frames.Count;
    public int CurrentIndex => _current;

    void Awake()
    {
        if (autoCollectChildren)
        {
            frames.Clear();
            for (int i = 0; i < transform.childCount; i++)
                frames.Add(transform.GetChild(i).gameObject);
        }
        SetIndex(Mathf.Clamp(startIndex, 0, Mathf.Max(0, frames.Count - 1)));
    }

    public void SetIndex(int index)
    {
        if (frames.Count == 0) return;
        index = Mathf.Clamp(index, 0, frames.Count - 1);
        if (index == _current) return;

        for (int i = 0; i < frames.Count; i++)
        {
            if (!frames[i]) continue;
            bool on = (i == index);
            if (deactivateOthers || on) frames[i].SetActive(on);
        }
        _current = index;
    }

    public void SetActiveOnly(GameObject frameGO)
    {
        int idx = frames.IndexOf(frameGO);
        if (idx >= 0) SetIndex(idx);
    }
}
