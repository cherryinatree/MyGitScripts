using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamingTools
{
    public static class ParseMesh
    {
        public static List<Vector3> ParseVectors(List<Vector3> vectors, float length, float width, float startX, float startZ)
        {

            List<Vector3> group = new List<Vector3>();
            foreach (Vector3 vect in vectors)
            {
                if (vect.x <= startX + length && vect.x >= startX)
                {
                    if (vect.z <= startZ + width && vect.z >= startZ)
                    {
                        group.Add(vect);
                    }
                }
            }

            return group;
        }


        public static float GetMeshStartX(List<Vector3> vectors)
        {
            float x = vectors[0].x;
            foreach (Vector3 vect in vectors)
            {
                if (vect.x < x)
                {
                    x = vect.x;
                }
            }
            return x;
        }
        public static float GetMeshStartZ(List<Vector3> vectors)
        {
            float z = vectors[0].z;
            foreach (Vector3 vect in vectors)
            {
                if (vect.z < z)
                {
                    z = vect.z;
                }
            }
            return z;
        }
        public static float GetMeshEndX(List<Vector3> vectors)
        {
            float x = vectors[0].x;
            foreach (Vector3 vect in vectors)
            {
                if (vect.x > x)
                {
                    x = vect.x;
                }
            }
            return x;
        }
        public static float GetMeshEndZ(List<Vector3> vectors)
        {
            float z = vectors[0].z;
            foreach (Vector3 vect in vectors)
            {
                if (vect.z > z)
                {
                    z = vect.z;
                }
            }
            return z;
        }
    }
}
