using TMPro;
using UnityEngine;

public class UI_ShowAbilityBuffer : MonoBehaviour
{

    public InputBuffer characterInputBuffer;
    public TextMeshProUGUI bufferText;

    // Update is called once per frame
    void Update()
    {
        ConvertBufferToString();
    }

    private void ConvertBufferToString()
    {
        string buttonString = string.Empty;
        if (characterInputBuffer != null)
        {
            for (int i = 0; i < characterInputBuffer.buttonsPressed.Count; i++)
            {
                switch (characterInputBuffer.buttonsPressed[i]) { 
                
                    case InputBuffer.InputBufferButtons.Left:
                        buttonString += "Left ";
                            break;
                    case InputBuffer.InputBufferButtons.Right:
                        buttonString += "Right ";
                        break;
                    case InputBuffer.InputBufferButtons.Up:
                        buttonString += "Up ";
                        break;
                    case InputBuffer.InputBufferButtons.Down:
                        buttonString += "Down ";
                        break;
                    case InputBuffer.InputBufferButtons.A:
                        buttonString += "A ";
                        break;
                    case InputBuffer.InputBufferButtons.B:
                        buttonString += "B ";
                        break;
                    case InputBuffer.InputBufferButtons.X:
                        buttonString += "X ";
                        break;
                    case InputBuffer.InputBufferButtons.Y:
                        buttonString += "Y ";
                        break;
                    default:
                        break;
                
                }
            }
        }

        bufferText.text = buttonString;
    }
}
