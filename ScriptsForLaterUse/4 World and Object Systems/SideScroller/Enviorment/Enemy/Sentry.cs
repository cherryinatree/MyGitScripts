using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentry : MonoBehaviour
{
    public Transform[] points;
    private int current;
    public float speed;
    public bool changeLook = true;
    public bool isOn = true;
    public bool trackPlayer = false;
    private GameObject player;
    public float followDistance = 5;
    private float marginOfError = 1.5f;
    private bool margin = false;

    private bool playerInZone = false;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        current = 0;
        player = GameObject.FindGameObjectWithTag("Player");

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isOn)
        {
            if (points != null)
            {
                if (points[current] != null)
                {
                    if(Vector3.Distance(transform.position, points[current].position) < marginOfError)
                    {
                        margin = true;
                    }
                    if (!margin)
                    {

                        if (points.Length == 2 && trackPlayer)
                        {
                            if (player != null)
                            {
                                if (player.transform.position.x > points[0].position.x &&
                                    player.transform.position.x < points[1].position.x)
                                {

                                    playerInZone = true;
                                    MoveTowards(player.transform);
                                }
                                else if (player.transform.position.x < points[0].position.x &&
                                    player.transform.position.x > points[1].position.x && trackPlayer)
                                {

                                    playerInZone = true;
                                    MoveTowards(player.transform);
                                }
                                else
                                {
                                    playerInZone = false;
                                    MoveTowards(points[current]);
                                }
                            }
                        }
                        else
                        {

                            playerInZone = false;
                            MoveTowards(points[current]);
                        }

                    }
                    else
                    {
                        margin = false;
                        playerInZone = false;
                        PointChange();
                    }
                }
            }
        }
    }


    private void MoveTowards(Transform towards)
    {

        
        Vector3 lookingAt = new Vector3(towards.position.x, points[current].position.y, points[current].position.z);
        //transform.position = Vector3.MoveTowards(transform.position, points[current].position, speed * Time.deltaTime);
        rb.MovePosition(Vector3.MoveTowards(transform.position, lookingAt, speed * Time.deltaTime));
        if (changeLook)
        {
            if(lookingAt.x+transform.position.x < 0.7f && lookingAt.x + transform.position.x > -0.7f)
            {

                transform.LookAt(Camera.main.transform);
            }
            transform.LookAt(lookingAt);
        }
    }

    private void PointChange()
    {

        current = (current + 1) % points.Length;
    }

    public void NextPoint()
    {
        /* if (!playerInZone)
         {
             nextPoint = true;
         }*/
        margin = false;
        current = (current + 1) % points.Length;
    }
}
