using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode()]
[RequireComponent (typeof(RectTransform))]
public class ToolTips : MonoBehaviour
{
    public TextMeshProUGUI headerField;
    public TextMeshProUGUI contentField;

    public LayoutElement LayoutElement;

    public int characterWrapLimit;

    public RectTransform rectTransform;

    public void SetText(string content, string header = "")
    {
        if (string.IsNullOrEmpty(header))
        {
            headerField.gameObject.SetActive(false);
        }
        else
        {
            headerField.gameObject.SetActive(true);
            headerField.text = header;
        }

        contentField.text = content;

        int headerLength = headerField.text.Length;
        int contentLength = contentField.text.Length;

        LayoutElement.enabled = (headerLength > characterWrapLimit || contentLength > characterWrapLimit) ? true : false;

        PositionUpdate();
    }

    private void PositionUpdate()
    {
        Vector2 position = Input.mousePosition;

        float pivotX = position.x / Screen.width;
        float pivotY = position.y / Screen.height;

        rectTransform.pivot = new Vector2(pivotX, pivotY);
        transform.position = position;
    }
}