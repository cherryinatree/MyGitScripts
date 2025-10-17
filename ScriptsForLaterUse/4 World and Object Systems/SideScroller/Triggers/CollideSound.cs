using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideSound : MonoBehaviour
{

    AudioSource soundEffect;

    private void Start()
    {
        soundEffect = GetComponent<AudioSource>();
    }


    private void OnCollisionEnter(Collision collision)
    {

        soundEffect.Play();
    }
}
