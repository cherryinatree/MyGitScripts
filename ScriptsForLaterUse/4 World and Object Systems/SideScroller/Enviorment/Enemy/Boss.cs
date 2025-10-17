using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public int stages = 2;
    public int[] stageTriggers;
    private int currentStage =0;

    private EnemyHealth health;

    public Spawner[] fire;

    public float[] dropChance = { 25, 50, 75 };
    public int[] gunFireStage = { 1, 3, 5 };

    public Transform[] pointsStage0;
    public Transform[] pointsStage0Drop;
    public Transform[] pointsStage1;
    public Transform[] pointsStage2;
    public Transform[] pointsStage3;
    public Transform[] pointsStage4;
    private int current;
    public float speed = 2;
    private float origionalSpeed = 2;
    public float dropSpeed = 15;
    private bool nextPoint = false;
    private bool dropNow = false;
    private bool rise = false;

    Timer quickRunTime;
    private bool quickRun = false;

    // Start is called before the first frame update
    void Start()
    {
        origionalSpeed = speed;
           quickRunTime = new Timer(1);
        health = GetComponent<EnemyHealth>();
        current = 0;
    }

    // Update is called once per frame
    void Update()
    {
        QuickRun();
        StageSelect();
    }

    private void QuickRun()
    {
        if (quickRun)
        {
            speed = 30;
            if (quickRunTime.ClockTick())
            {
                quickRun = false;
            }
        }
        else
        {
            quickRunTime.RestartTimer();
            speed = origionalSpeed;
        }
    }

    private void StageSelect()
    {
        for (int i = 0; i < stageTriggers.Length; i++)
        {

            if (health.health < stageTriggers[i])
            {
                currentStage = i+1;
            }
        }

        Stage0();
    }

    private void Stage0()
    {
        if (!dropNow)
        {
            if (pointsStage0 != null)
            {
                if (pointsStage0[current] != null)
                {
                    if (transform.position.x != pointsStage0[current].position.x && !nextPoint)
                    {

                        transform.position = Vector3.MoveTowards(transform.position, pointsStage0[current].position, 
                            speed * (currentStage+1) * Time.deltaTime);
                        //transform.LookAt(Camera.main.transform);
                    }
                    else
                    {
                        int rand = Random.Range(0, 100);

                        if (rand > dropChance[currentStage])
                        {

                            current = (current + 1) % pointsStage0.Length;
                            nextPoint = false;
                            FireGuns();
                        }
                        else
                        {

                            dropNow = true;
                        }
                    }
                }
            }
        }
        else
        {
            if (pointsStage0Drop != null)
            {
                if (pointsStage0Drop[current] != null)
                {
                    if (transform.position.y != pointsStage0Drop[current].position.y && !nextPoint && !rise)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, pointsStage0Drop[current].position, dropSpeed * Time.deltaTime);
                       // transform.LookAt(Camera.main.transform);
                    }
                    else
                    {
                        if (rise == false)
                        {
                            rise = true;
                            FireGuns();
                        }

                        if (transform.position.y != pointsStage0[current].position.y && !nextPoint)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, pointsStage0[current].position, speed * Time.deltaTime);
                           // transform.LookAt(Camera.main.transform);
                        }
                        else
                        {
                            current = (current + 1) % pointsStage0.Length;
                            nextPoint = false;
                            FireGuns();
                            dropNow = false;
                            rise = false;
                        }
                    }
                }
                else
                {
                    dropNow = false;
                }
            }
            else
            {
                dropNow = false;
            }
        }
    }
    private void Stage1()
    {

    }
    private void Stage2()
    {
    }

    private void FireGuns()
    {
        if (!rise)
        {
            for (int i = 0; i < fire.Length; i++)
            {
                if (i < gunFireStage[currentStage])
                {
                    fire[i].Fire();
                }
            }
        }
        else
        {

            fire[3].Fire();
            fire[4].Fire();
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Player")
        {
            quickRun = true;
            NextPoint();
        }
    }


    public void NextPoint()
    {
        nextPoint = true;
    }
}
