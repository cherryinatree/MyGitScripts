using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamingTools
{
    public class map_maker
    {
        public GameObject oneSquare;
        public GameObject TerrainSquare;
        public GameObject FillerSquare;
        public GameObject StartOneSquare;
        public GameObject BadStartOneSquare;
        public int width = 10;
        public int length = 10;
        public int totalItems = 2;
        public int[] Xs;
        public int[] Ys;
        public int[,] terrainLoacations;
        public List<int> raisedGround;

        private int raisedCount = 0;
        private float groundHeight = 0f;


        // Constructor for no added variables.
        public map_maker(int length = 10, int width = 10)
        {
            LoadVariables(length, width);
            GatherPrefabs();
            MakeTheMap();
        }


        private void GatherPrefabs()
        {
            oneSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
            TerrainSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
            FillerSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StartOneSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BadStartOneSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        private void LoadVariables(int length, int width)
        {
            this.length = length;
            this.width = width;
            terrainLoacations = new int[2, 2];
            raisedGround = new List<int>();
            Xs = new int[] { 3, 4, 5 };
            Ys = new int[] { 3, 4, 5 };
        }

        private void MakeTheMap()
        {
            terrainLoacations = new int[totalItems, 2];
            for (int items = 0; items < totalItems; items++)
            {
                terrainLoacations[items, 0] = Xs[items];
                terrainLoacations[items, 1] = Ys[items];
            }
            for (float x = 0; x < length; x++)
            {
                for (float i = 0; i < width; i++)
                {
                    groundHeight = 0f;
                    foreach (int a in raisedGround)
                    {
                        if (a == raisedCount)
                        {
                            groundHeight += 0.5f;
                        }
                    }
                    raisedCount++;
                    bool terrainHere = false;
                    for (int r = 0; r < terrainLoacations.GetLength(0); r++)
                    {
                        if (terrainLoacations[r, 0] == x && terrainLoacations[r, 1] == i)
                        {
                            var newSquare = GameObject.Instantiate(TerrainSquare, new Vector3(x, groundHeight, i), Quaternion.identity);
                            newSquare.transform.parent = GameObject.Find("GameMaster").transform;
                            terrainHere = true;
                            for (float high = groundHeight; high > 1f; high--)
                            {
                                newSquare = GameObject.Instantiate(FillerSquare, new Vector3(x, high - 1, i), Quaternion.identity);
                                newSquare.transform.parent = GameObject.Find("GameMaster").transform;

                            }
                        }
                    }
                    if (!terrainHere)
                    {
                        var newSquare = GameObject.Instantiate(oneSquare, new Vector3(x, groundHeight, i), Quaternion.identity);
                        newSquare.transform.parent = GameObject.Find("GameMaster").transform;
                        for (float high = groundHeight; high > 0.9f; high--)
                        {

                            newSquare = GameObject.Instantiate(FillerSquare, new Vector3(x, high - 1, i), Quaternion.identity);
                            newSquare.transform.parent = GameObject.Find("GameMaster").transform;
                        }
                    }
                }
            }/*
            for (float i = 0; i < width; i++)
            {
                var newSquare = GameObject.Instantiate(StartOneSquare, new Vector3(i, 0f, 0 - 1), Quaternion.identity);
                newSquare.transform.parent = GameObject.Find("GameMaster").transform;

            }
            for (float i = 0; i < width; i++)
            {
                var newSquare = GameObject.Instantiate(BadStartOneSquare, new Vector3(i, 0f, width), Quaternion.identity);
                newSquare.transform.parent = GameObject.Find("GameMaster").transform;
            }*/
        }
    }
}
