using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cubeSpawn : MonoBehaviour
{
    public GameObject[] cubes;
    public cubeSpawn(GameObject cube, float howMany, GameObject devider)
    {
        cubes = new GameObject[int.Parse(howMany.ToString())];
        for(int i = 0; i < howMany; i++)
        {
            float height = Random.Range(-5f, 5f);
            cubes[i] = Instantiate(cube, new Vector3(Random.Range(-10f,10f), height, 0), Quaternion.identity);
            if(height>= devider.transform.position.y)
            {
                cubes[i].name = "1";
            }
            else
            {
                cubes[i].name = "-1";
            }
        }
    }
}
