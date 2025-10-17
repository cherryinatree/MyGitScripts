using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectables : MonoBehaviour
{


    public enum CollectType { Points, Lives, Charges, Shield, Star, Super};
    public CollectType Collected = CollectType.Points;

    public int ItemBouns = 1;
    public int ItemBouns2 = 3;


    AudioSource soundEffect;

    private void Start()
    {
        soundEffect = GameObject.Find("SoundEffectCollect").GetComponent<AudioSource>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            Rewards();
        }
    }

    private void Rewards()
    {
        if (Collected == CollectType.Points)
        {
            GameSingleton.Instance.save.Score += ItemBouns;
        }
        else if (Collected == CollectType.Lives)
        {
            if (GameSingleton.Instance.save.Lives < 5)
            {
                GameSingleton.Instance.save.Lives += ItemBouns;
            }
        }
        else if (Collected == CollectType.Charges)
        {
           // if (GameSingleton.Instance.save.Charges < 5)
           // {
                GameSingleton.Instance.save.Charges += ItemBouns;
           // }
        }
        else if (Collected == CollectType.Shield)
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<Health>().Defense();
        }
        else if (Collected == CollectType.Star)
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<AbilitySpawner>().currentAbility = 1;
        }
        else if (Collected == CollectType.Super)
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<AbilitySpawner>().currentAbility = 1;
            GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().Speed *= ItemBouns2;
            GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().JumpHeight *= 2;
            GameObject.FindGameObjectWithTag("Player").GetComponent<Health>().Defense();
            GameSingleton.Instance.save.Charges += ItemBouns;
        }

        soundEffect.Play();
        Destroy(gameObject);
    }
}
