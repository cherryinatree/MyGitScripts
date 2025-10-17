using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner 
{

    public void SpawnRandomPosition(GameObject spawn, float howMany, Vector3 max, Vector3 min)
    {
        for(int i = 0; i < howMany; i++)
        {
            GameObject theSpawned = GameObject.Instantiate(spawn);
            theSpawned.transform.position = new Vector3(Random.Range(min.x,max.x), Random.Range(min.y, max.y), 0);
        }
    }
}
