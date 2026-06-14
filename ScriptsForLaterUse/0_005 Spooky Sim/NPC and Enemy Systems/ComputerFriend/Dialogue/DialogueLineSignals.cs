// DialogueLineSignals.cs
using System;
using UnityEngine;

public static class DialogueLineSignals
{
    public struct LineContext
    {
        public string speakerId;
        public string lineKey;      // what you key your database by
        public AudioSource audio;   // optional
        public AudioClip clip;      // optional
    }

    public static event Action<LineContext> OnLinePresented;

    public static void RaiseLinePresented(string speakerId, string lineKey, AudioSource audio = null, AudioClip clip = null)
    {
        if (string.IsNullOrEmpty(lineKey)) return;

        OnLinePresented?.Invoke(new LineContext
        {
            speakerId = speakerId ?? "",
            lineKey = lineKey,
            audio = audio,
            clip = clip
        });
    }
}

