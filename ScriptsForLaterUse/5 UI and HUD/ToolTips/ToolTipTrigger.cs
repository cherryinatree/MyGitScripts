using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string header;

    [TextArea(5,10)]
    public string content;


    public void OnPointerEnter(PointerEventData eventData)
    {
        ToolTipSystem.current.Show(content, header);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        ToolTipSystem.current.Hide();
    }
}
