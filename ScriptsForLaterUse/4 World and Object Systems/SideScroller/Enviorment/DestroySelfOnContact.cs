using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{
    public bool avoidMobs = true;
    private void OnCollisionEnter(Collision collision)
    {
        if (avoidMobs)
        {
            if (collision.transform.tag != "MOB")
            {

                Destroy(gameObject);
            }
        }
        else
        {

            Destroy(gameObject);
        }
    }
}
