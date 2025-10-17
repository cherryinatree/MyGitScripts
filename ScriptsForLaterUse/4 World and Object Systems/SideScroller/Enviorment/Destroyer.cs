using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public string targetTag;

    private Timer timer;


    private void Start()
    {
        timer = new Timer(2);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == targetTag)
        {
            if (targetTag == "Player")
            {
                if(gameObject.transform.tag == "MOB")
                {

                    collision.gameObject.GetComponent<Health>().Damaged(true);
                }
                else
                {
                    collision.gameObject.GetComponent<Health>().Damaged(false);
                }
            }
            else
            {

                Destroy(collision.gameObject);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {

        if (collision.transform.tag == targetTag)
        {
            if (targetTag == "Player")
            {
                if (timer.ClockTick())
                {
                    if (gameObject.transform.tag == "MOB")
                    {

                        collision.gameObject.GetComponent<Health>().Damaged(true);
                    }
                    else
                    {
                        collision.gameObject.GetComponent<Health>().Damaged(false);
                    }
                }
            }
            else
            {

                Destroy(collision.gameObject);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        timer.RestartTimer();
    }
}
