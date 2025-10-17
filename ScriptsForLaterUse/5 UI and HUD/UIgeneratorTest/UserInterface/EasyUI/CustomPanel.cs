using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class CustomPanel : MonoBehaviour
{
    [Header("\n=======Panel Settings =========\n")]

    [Range(0f, 100f)]
    public float height = 100;
    [Range(0f, 100f)]
    public float width = 100;
    [Range(0f, 100f)]
    public float horizontal = 0, verticle = 50;

    private float ratioH = 100, ratioV = 100;


    [Header("\n======= Button Settings =========\n")]
    public Sprite ButtonImage;
    public ColorBlock ButtonColors = new ColorBlock
    {
        normalColor = Color.white,
        highlightedColor =
        Color.yellow,
        selectedColor = Color.green,
        disabledColor = Color.red,
        pressedColor = Color.cyan,
        colorMultiplier = 5,
        fadeDuration = 0
    };
    public Color32 text = new Color32(0,0,0,255);

    [Header("\n======= Add Button =========\n")]
    [SerializeField]
    public GeneratedButton[] buttons;


    public void AdjustPanel()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        

        RectTransform parent = transform.parent.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(parent.sizeDelta.x * (width / ratioH), parent.sizeDelta.y * (height / ratioV));  

        rt.position = new Vector3(parent.sizeDelta.x * (horizontal/ ratioH), parent.sizeDelta.y * (verticle / ratioV), 0);

        if (GetComponent<FlexibleGridLayout>() == null)
        {
            gameObject.AddComponent<FlexibleGridLayout>();
        }

        int buttonCount = 0;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                buttonCount++;
            }
        }
        if(buttonCount < buttons.Length)
        {
           
           MakeButton();
            
        }else if(buttonCount > buttons.Length)
        {
            RemoveButton();
        }
        buttonCount = 0;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                child.GetComponent<Button>().colors = ButtonColors;
                child.GetComponent<Image>().sprite = ButtonImage;
                child.GetChild(0).GetComponent<TextMeshProUGUI>().color = text;
                child.GetChild(0).GetComponent<TextMeshProUGUI>().text = buttons[buttonCount].Text;

                buttonCount++;
            }
        }
    }


    public enum ButtonType
    {
        Action,
        Tab,
        MenuNaviagtion,
        SceneChange
    }
    //[Header("\n======= Add Button =========\n")]
    //public ButtonType buttonType = ButtonType.Action;
    public void AddButton()
    {
            MakeButton();
    }


    private void MakeButton()
    {
        GameObject buttonObj = new GameObject("Button");
        buttonObj.AddComponent<RectTransform>(); // Required for UI positioning
        buttonObj.AddComponent<CanvasRenderer>(); // Required for rendering

        // Add and configure the Image component (required for the button to be visible)
        Image image = buttonObj.AddComponent<Image>();
        if (ButtonImage != null)
        {
            image.sprite = ButtonImage; // Example sprite, change as needed
        }
        else
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ButtonImage = image.sprite;
        }

        // Add and configure the Button component
        Button button = buttonObj.AddComponent<Button>();
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30); // Set size
        button.transform.SetParent(transform, false); // Parent to canvas
        button.colors = ButtonColors;


        // Optionally, configure the button to respond to clicks
        button.onClick.AddListener(() => Debug.Log("Button Clicked!"));

        // Add text to the button
        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform, false); // Parent to button

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Click Me";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        // Adjust text RectTransform to fill the button
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }




    public void RemoveButton()
    {
        GameObject button = null;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                button = child.gameObject;
            }
        }
        if (button != null)
        {
            DestroyImmediate(button);
        }
    }

}

[CustomEditor(typeof(CustomPanel))]
public class CustomPanelEditor1 : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            CustomPanel panel = (CustomPanel)target;
            panel.AdjustPanel();
        }

    }
}

