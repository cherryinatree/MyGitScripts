using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    Timer timer;
    public float delay = 4;
    // Start is called before the first frame update
    void Start()
    {
        if(timer == null)
        {
            timer = new Timer(delay);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (timer.ClockTick())
        {
            GameObject.Destroy(gameObject);
        }
    }

    public void NewTime(float time)
    {

        delay = time;
    }
}
