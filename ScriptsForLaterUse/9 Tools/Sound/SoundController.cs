using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamingTools
{
    public static class SoundController
    {


        public static void MakeSound(GameObject character, string sound)
        {
            AudioClip clip = ResourseLoader.GetAudio(sound);
            character.GetComponent<AudioSource>().clip = clip;
            character.GetComponent<AudioSource>().Play();
        }

        public static void SetMusic(string song)
        {

            AudioClip clip = ResourseLoader.GetAudio(song);

            GameObject.Find("PlayerCamera").GetComponent<AudioSource>().clip = clip;
            GameObject.Find("PlayerCamera").GetComponent<AudioSource>().Play();
        }
    }
}
