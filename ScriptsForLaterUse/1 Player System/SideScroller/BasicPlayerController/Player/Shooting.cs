using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    public float Damage = 1;
    public float ShotDamage()
    {
        return Damage;
    }



    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Enemy")
        {
            Destroy(collision.gameObject);
        }
        Destroy(gameObject);
    }
}
