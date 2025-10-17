using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Highlighter
{
    private static Color ButtonOrgional = Color.black;
    private static Color ButtonHighlight = Color.red;


    public static void HighlightImage(GameObject Brightme)
    {
        Brightme.SetActive(true);
    }
    public static void DehighlightImage(GameObject Brightme)
    {
        Brightme.SetActive(false);
    }
}
