// Assets/Dialogue/DialogueRunner.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // only if you use the new Input System
using System.Linq;

public class DialogueRunner : MonoBehaviour
{
    [System.Serializable]
    public class Speaker
    {
        public string id;                   // e.g., "AIHead"
        public string displayName = "AI";
        public AIFaceDecalController face; // your decal-based face controller
        public AudioSource voiceOverride;  // optional if face has no AudioSource
    }

    [Header("Content")]
    public DialogueGraph graph;

    [Header("Actors")]
    public List<Speaker> speakers = new List<Speaker>();

    [Header("UI")]
    public DialogueUI ui;

    [Header("Input")]
    public Key advanceKey = Key.Space; // or drive from UI only

    bool _running;
    Coroutine _routine;

    Dictionary<string, Speaker> _byId;

    void Awake()
    {
        _byId = speakers.ToDictionary(s => s.id, s => s);
    }

    public void StartDialogue(DialogueGraph g = null)
    {
        if (_running) StopDialogue();
        if (g) graph = g;
        if (!graph || !graph.entry) { Debug.LogWarning("DialogueRunner: Missing graph/entry."); return; }
        _routine = StartCoroutine(RunGraph(graph.entry));
    }

    public void StopDialogue()
    {
        if (_routine != null) StopCoroutine(_routine);
        _running = false;
        ui?.ClearLine();
    }

    IEnumerator RunGraph(DialogueNode node)
    {
        _running = true;

        var current = node;
        while (_running && current != null)
        {
            // play lines in order
            foreach (var line in current.lines)
            {
                // resolve speaker
                _byId.TryGetValue(line.speakerId, out var spk);
                var speakerName = spk != null ? spk.displayName : line.speakerId;
                ui?.ShowLine(speakerName, line.subtitle);

                // set mood (eyes) and play audio (lip-sync via face.Speak)
                if (spk != null && spk.face)
                {
                    spk.face.ApplyMood(ConvertMood(line.mood));
                    if (line.voice != null)
                    {
                        // If you provided voiceOverride, use it; otherwise face.Speak uses its assigned AudioSource
                        if (spk.voiceOverride)
                        {
                            spk.voiceOverride.Stop();
                            spk.voiceOverride.clip = line.voice;
                            spk.voiceOverride.Play();
                        }
                        else
                        {
                            spk.face.Speak(line.voice);
                        }
                    }
                }

                // wait: audio length OR input/timeout
                float waitFor = 0f;
                AudioSource playing = null;
                if (spk != null)
                {
                    playing = spk.voiceOverride ? spk.voiceOverride : (spk.face ? spk.face.voice : null);
                }

                if (playing && line.voice)
                {
                    // Wait for clip to finish, but allow user to skip
                    yield return WaitAudioOrAdvance(playing);
                }
                else
                {
                    // No audio: either auto-advance after min time or wait for input
                    float minTime = Mathf.Max(0.1f, line.minLineSeconds);
                    if (line.autoAdvance)
                    {
                        float t = 0f;
                        while (t < minTime && _running)
                        {
                            t += Time.unscaledDeltaTime;
                            if (AdvancePressed()) break; // allow skip
                            yield return null;
                        }
                    }
                    else
                    {
                        // wait for player input
                        yield return WaitForAdvance();
                    }
                }
            }

            // present choices (if any)
            if (current.choices != null && current.choices.Count > 0)
            {
                var options = new List<string>();
                foreach (var c in current.choices) options.Add(c.text);

                int picked = -1;
                ui?.ShowChoices(options, (i) => picked = i);

                // wait for click or key (1..N as shortcuts)
                while (picked < 0 && _running)
                {
                    // numeric shortcuts
                    for (int i = 0; i < options.Count; i++)
                    {
                        Key key = Key.Digit1 + i;
                        if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
                            picked = i;
                    }
                    yield return null;
                }

                ui?.ClearChoices();

                if (!_running) break;
                current = current.choices[picked].next;
            }
            else
            {
                // no choices: go to linear next
                current = current.next;
            }
        }

        ui?.ClearLine();
        _running = false;
    }

    IEnumerator WaitAudioOrAdvance(AudioSource src)
    {
        // waits while audio is playing; allow skip
        while (src && src.isPlaying && _running)
        {
            if (AdvancePressed())
                break;
            yield return null;
        }
        // if skipped, stop audio for clean cut
        if (src && src.isPlaying) src.Stop();
    }

    IEnumerator WaitForAdvance()
    {
        while (_running && !AdvancePressed())
            yield return null;
    }

    bool AdvancePressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[advanceKey].wasPressedThisFrame
            || Mouse.current?.leftButton.wasPressedThisFrame == true
            || Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
    }

    // Map LineMood -> AIFaceDecalController mood enum
    AIFaceDecalController.AIMood ConvertMood(LineMood m)
    {
        return m switch
        {
            LineMood.Happy => AIFaceDecalController.AIMood.Happy,
            LineMood.Sad => AIFaceDecalController.AIMood.Sad,
            LineMood.Angry => AIFaceDecalController.AIMood.Angry,
            LineMood.Surprised => AIFaceDecalController.AIMood.Surprised,
            LineMood.Thinking => AIFaceDecalController.AIMood.Thinking,
            LineMood.Anxious => AIFaceDecalController.AIMood.Anxious,
            _ => AIFaceDecalController.AIMood.Neutral
        };
    }
}
