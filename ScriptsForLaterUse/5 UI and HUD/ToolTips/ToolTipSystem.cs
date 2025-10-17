using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipSystem : MonoBehaviour
{

    public static ToolTipSystem current;
    public ToolTips toolTips;
    public float delay = 2;

    private static List<bool> isCursorStillOn;

    private void Awake()
    {
        isCursorStillOn = new List<bool>();
        current = this;
    }

    public void Show(string content, string header = "")
    {
        current.toolTips.SetText(content,header);
        isCursorStillOn.Add(true);
        Invoke("ShowInvokeMe", delay);
    }

    public void Hide()
    {
        if (isCursorStillOn.Count > 0)
        {
            isCursorStillOn[isCursorStillOn.Count - 1] = false;
        }
        current.toolTips.gameObject.SetActive(false);
    }

    private void ShowInvokeMe()
    {
        if (isCursorStillOn[0])
        {
            current.toolTips.gameObject.SetActive(true);
        }
        isCursorStillOn.Remove(isCursorStillOn[0]);
    }
}
