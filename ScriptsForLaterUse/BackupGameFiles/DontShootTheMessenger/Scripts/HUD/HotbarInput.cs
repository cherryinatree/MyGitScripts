// HotbarInput.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarInput : MonoBehaviour
{
    public HotbarUIController hotbar;

    void Update()
    {
        if (!hotbar) return;

        // Number keys 1..8
        for (int i = 0; i < hotbar.hotbarSize && i < 8; i++)
        {
            if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame ||
                Keyboard.current[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame)
            {
                hotbar.SelectIndex(i);
            }
        }

        // Mouse wheel
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0.5f) hotbar.Cycle(-1);
        if (scroll < -0.5f) hotbar.Cycle(+1);
    }
}
