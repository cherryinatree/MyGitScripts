using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastFloor : MonoBehaviour
{
    private float speed = 15;
    private float changeAmount = 7;
    private float maxVel = 20;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Player")
        {

            Vector3 playerVel = collision.transform.GetComponent<Rigidbody>().velocity;
            if (playerVel.x < maxVel)
            {
                collision.gameObject.GetComponent<Player>()._body.velocity = new Vector3(Sign(playerVel.x),
                    playerVel.y,playerVel.z);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.transform.tag == "Player")
        {
            float input = other.transform.GetComponent<Player>()._inputs.x;
            Vector3 playerVel = other.transform.GetComponent<Rigidbody>().velocity;




            if(Sign(playerVel.x) == Sign(input) || input == 0)
            {
                input = playerVel.x;
            }
            if (playerVel.x < maxVel && playerVel.x > -maxVel)
            {
                other.gameObject.GetComponent<Player>()._body.velocity = new Vector3(ChangeVel(input),
                    playerVel.y, playerVel.z);
                Debug.Log("vel: "+playerVel.x);
            }
        }
    }


    private float ChangeVel(float vel)
    {

        if (vel < 0)
        {
            vel -= changeAmount;
        }
        else
        {
            vel += changeAmount;
        }

        return vel;
    }
    

    private float Sign(float vel)
    {

        if (vel < 0)
        {
            return -1;
        }
        else
        {
            return 1;
        }

    }

}
