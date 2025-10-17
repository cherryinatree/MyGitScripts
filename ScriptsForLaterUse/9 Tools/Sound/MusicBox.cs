using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicBox : MonoBehaviour
{
    public string songName = "BattleSong1";

    // Start is called before the first frame update
    void Start()
    {
        songName = "Sounds/Music/" + songName;
        GamingTools.SoundController.SetMusic(songName);
    }

}
