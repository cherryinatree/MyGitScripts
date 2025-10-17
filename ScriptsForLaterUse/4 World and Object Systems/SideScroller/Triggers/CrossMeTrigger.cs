using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossMeTrigger : MonoBehaviour
{
    public bool IwasCrossed = false;

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Player")
        {
            IwasCrossed=true;
        }
    }
}
