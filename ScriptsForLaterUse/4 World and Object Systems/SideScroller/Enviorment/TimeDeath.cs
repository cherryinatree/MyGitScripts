using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeDeath : MonoBehaviour
{
    public float delay = 3;
    private float timer = 0;


    private void Start()
    {
        timer = delay;
    }

    // Update is called once per frame
    void Update()
    {

        timer -= Time.deltaTime;
        if (timer < 0)
        {
            Destroy(gameObject);
        }
    }
}
