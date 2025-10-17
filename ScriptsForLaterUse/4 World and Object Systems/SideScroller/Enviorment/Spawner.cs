using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;

    public float delay = 3;
    private float timer = 0;

    public float speed = 2;
    public bool isOn = true;

    public Directrion.JumpType jumpType = Directrion.JumpType.Right;

    private bool isTriggerEnd;
    private bool isTriggerStart;

    public CrossMeTrigger triggerEnd;
    public CrossMeTrigger triggerStart;

    public SpawnAdjuster spawnAdjuster;

    public int maxAmountOfSpawns = -1;
    private int spawnCount = 0;

    private void Start()
    {
        timer = delay;

        if (triggerStart != null)
        {

            isTriggerStart = false;
        }
        else
        {
            isTriggerStart = true;
        }
        isTriggerEnd = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isOn)
        {
            if (isTriggerStart)
            {
                if (!isTriggerEnd)
                {

                    if(maxAmountOfSpawns == -1)
                    {
                        Spawn();
                    }
                    else
                    {
                        
                        if (maxAmountOfSpawns > spawnCount)
                        {
                            Spawn();
                        }
                    }



                    if (triggerEnd != null)
                    {
                        isTriggerEnd = triggerEnd.IwasCrossed;
                    }
                }
            }
            else
            {
                if (triggerStart != null)
                {
                    isTriggerStart = triggerStart.IwasCrossed;
                }
            }
        }
    }

    private void Spawn()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
        {
            GameObject clone = Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation);
            clone.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * speed);

            if (clone.GetComponent<Forward>())
            {
                clone.GetComponent<Forward>().jumpType = jumpType;
            }
            timer = delay;

            if(spawnAdjuster != null)
            {
                spawnAdjuster.AdjustMe(clone);
            }
            spawnCount++;
        }
    }

    public void Fire()
    {
        GameObject clone = Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation);
        clone.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * speed);

        if (clone.GetComponent<Forward>())
        {
            clone.GetComponent<Forward>().jumpType = jumpType;
        }
        if (spawnAdjuster != null)
        {
            Debug.Log(2);
            spawnAdjuster.AdjustMe(clone);
        }
    }
}
