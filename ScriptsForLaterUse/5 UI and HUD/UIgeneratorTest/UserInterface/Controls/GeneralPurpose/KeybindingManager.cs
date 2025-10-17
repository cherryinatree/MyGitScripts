using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindingManager : MonoBehaviour
{
    // Dictionary to hold the current keybindings
    public Dictionary<string, KeyCode> keybindings = new Dictionary<string, KeyCode>();

    void Start()
    {
        keybindings = new Dictionary<string, KeyCode>();
        // Initialize default keybindings
        InitializeDefaultKeybindings();
    }

    private void InitializeDefaultKeybindings()
    {
        // Set default keybindings
        keybindings.Clear();
        keybindings.Add("MoveUp", KeyCode.W);
        keybindings.Add("MoveDown", KeyCode.S);
        keybindings.Add("MoveLeft", KeyCode.A);
        keybindings.Add("MoveRight", KeyCode.D);
        keybindings.Add("Jump", KeyCode.Space);
        keybindings.Add("Fire", KeyCode.Mouse0);
        keybindings.Add("Cycle", KeyCode.Tab);
        keybindings.Add("Back", KeyCode.Escape);
        // Add more default keybindings as needed
    }

    public void ChangeKeybinding(string action, KeyCode newKey)
    {
        // Check if the action exists in the dictionary
        if (keybindings.ContainsKey(action))
        {
            // Update the keybinding
            keybindings[action] = newKey;
        }
        else
        {
            Debug.LogError("Action not found in keybindings: " + action);
        }
    }

    // Optional: Method to save keybindings to PlayerPrefs or another storage system
    // Optional: Method to load keybindings from PlayerPrefs or another storage system
}
