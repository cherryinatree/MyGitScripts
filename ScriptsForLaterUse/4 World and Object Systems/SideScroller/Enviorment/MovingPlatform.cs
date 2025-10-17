using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{

    public bool hasPause = false;
    public bool isPlayerActivated = false;
    private bool isActivated = true;

    public bool isUpDown = false;
    public bool isStartUpLeft = false;
    public float distance = 3;
    public float speed = 3;
    public float pauseLength = 2;
    public float puaseStartAfter = 4;

    private Vector3[] points;
    private Vector3 origionalPosition;
    private bool nextPoint = false;
    private int current;

    private Vector3 left;
    private Vector3 right;
    private Vector3 up;
    private Vector3 down;

    private Rigidbody rb;

    private float timer;
    private float pauseTimer;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0;
        pauseTimer = 0;

        origionalPosition = transform.position;
        left = transform.position + new Vector3(-distance, 0, 0);
        right = transform.position + new Vector3(distance, 0, 0);
        up = transform.position + new Vector3(0, distance, 0);
        down = transform.position + new Vector3(0, -distance, 0);

        points = new Vector3[2];
        if (isUpDown)
        {
            points[0] = up;
            points[1] = down;
        }
        else
        {
            points[0] = left;
            points[1] = right;
        }
        if (isStartUpLeft)
        {

            current = 0;
        }
        else
        {
            current = 1;
        }
        rb = GetComponent<Rigidbody>();
        
        if (isPlayerActivated)
        {
            isActivated = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isActivated)
        {
            timer += Time.deltaTime;
            if (points != null)
            {
                if (points[current] != null)
                {
                    if (isUpDown)
                    {
                        if (transform.position.y != points[current].y && !nextPoint)
                        {
                            if (hasPause)
                            {
                                if(timer < puaseStartAfter)
                                {
                                    rb.MovePosition(Vector3.MoveTowards(transform.position, points[current], speed * Time.deltaTime));
                                }
                                else
                                {

                                    pauseTimer += Time.deltaTime;
                                    if (pauseTimer > pauseLength)
                                    {
                                        pauseTimer = 0;
                                        timer = 0;
                                    }
                                }
                            }
                            else
                            {
                                rb.MovePosition(Vector3.MoveTowards(transform.position, points[current], speed * Time.deltaTime));
                            }
                        }
                        else
                        {
                            current = (current + 1) % points.Length;
                            nextPoint = false;
                        }
                    }
                    else
                    {
                        if (transform.position.x != points[current].x && !nextPoint)
                        {
                            if (hasPause)
                            {
                                if (timer < puaseStartAfter)
                                {
                                    rb.MovePosition(Vector3.MoveTowards(transform.position, points[current], speed * Time.deltaTime));
                                }
                                else
                                {
                                    pauseTimer += Time.deltaTime;
                                    if (pauseTimer > pauseLength)
                                    {
                                        pauseTimer = 0;
                                        timer = 0;
                                    }
                                }
                            }
                            else
                            {
                                rb.MovePosition(Vector3.MoveTowards(transform.position, points[current], speed * Time.deltaTime));
                            }
                        }
                        else
                        {
                            current = (current + 1) % points.Length;
                            nextPoint = false;
                        }
                    }
                }
            }
        }
    }
    public void NextPoint()
    {
        nextPoint = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isPlayerActivated)
        {
            if (collision.transform.tag == "Player")
            {
                isActivated = true;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (isPlayerActivated)
        {
            if (collision.transform.tag == "Player")
            {
                isActivated = false;
            }
        }
    }
}
