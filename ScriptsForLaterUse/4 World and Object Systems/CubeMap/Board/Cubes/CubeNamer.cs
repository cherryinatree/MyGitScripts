using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CubeMaster))]
public class CubeNamer : Editor
{  /* 
    public override void OnInspectorGUI()
    {
     if (GUILayout.Button("Rename Cubes"))
        {
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("Ground");
            List<GameObject> newCubes = new List<GameObject>();
            Debug.Log(cubes.Length);
            for (int i = 0; i < cubes.Length; i++)
            {
                if (cubes[i].GetComponent<Cube>() != null)
                {
                    newCubes.Add(cubes[i]);
                }
            }

            Debug.Log(newCubes.Count);
            for (int i = 0; i < newCubes.Count; i++)
            {
                cubes[i].name = "Cube:" + i.ToString();
            }
        }
}*/
}
