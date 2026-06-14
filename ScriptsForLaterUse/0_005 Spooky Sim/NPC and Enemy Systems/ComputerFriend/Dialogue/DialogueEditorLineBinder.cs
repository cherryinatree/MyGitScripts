// DialogueEditorLineBinder.cs (only change: pick voice source automatically if null)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DialogueEditor;

public class DialogueEditorLineBinder : MonoBehaviour
{
    [SerializeField] private AudioSource voiceSource;

    readonly Dictionary<NodeEventHolder, UnityAction> _bindings = new();

    public void BindConversation(NPCConversation convo)
    {
        if (convo == null)
        {
            Debug.LogError("[LineBinder] convo is null.");
            return;
        }

        // auto-grab ConversationManager audio source if not assigned
        if (voiceSource == null && ConversationManager.Instance != null)
            voiceSource = ConversationManager.Instance.GetComponent<AudioSource>();

        Unbind();

        var holders = convo.GetComponentsInChildren<NodeEventHolder>(true);
       // Debug.Log($"[LineBinder] Binding {holders.Length} nodes for speaker '{convo.DefaultName}'");

        foreach (var h in holders)
        {
            if (h == null || h.Event == null) continue;

            AudioClip clip = h.Audio;

            // Your log shows the key is clip.name, so keep that:
            string lineKey = (clip != null && !string.IsNullOrEmpty(clip.name))
                ? clip.name
                : $"Node_{h.NodeID}";

            string speakerId = convo.DefaultName;

            UnityAction action = () =>
            {
                Debug.Log($"[LineBinder] Line fired: key='{lineKey}', speaker='{speakerId}', clip='{(clip ? clip.name : "null")}'");
                DialogueLineSignals.RaiseLinePresented(speakerId, lineKey, voiceSource, clip);
            };

            h.Event.AddListener(action);
            _bindings[h] = action;
        }
    }

    public void Unbind()
    {
        foreach (var kvp in _bindings)
            if (kvp.Key && kvp.Key.Event != null)
                kvp.Key.Event.RemoveListener(kvp.Value);

        _bindings.Clear();
    }

    void OnDisable() => Unbind();
}
