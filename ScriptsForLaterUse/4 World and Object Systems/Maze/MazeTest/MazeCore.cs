// =============================
// File: MazeCore.cs
// =============================
using System;
using System.Collections.Generic;
using UnityEngine;


namespace MazeKit
{
    [Serializable]
    public class MazeGrid
    {
        public readonly int width;
        public readonly int height;
        public readonly Cell[,] cells;


        [Serializable]
        public class Cell
        {
            public bool visited;
            public bool N = true, E = true, S = true, W = true; // true = wall present
        }


        public MazeGrid(int w, int h)
        {
            width = Mathf.Max(2, w);
            height = Mathf.Max(2, h);
            cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = new Cell();
        }


        public int OpenMask(int x, int y)
        {
            // bit mask: 1=Left(W -X), 2=Right(E +X), 4=Down(S -Z), 8=Up(N +Z)
            int m = 0;
            var c = cells[x, y];
            if (!c.W) m |= 1;
            if (!c.E) m |= 2;
            if (!c.S) m |= 4;
            if (!c.N) m |= 8;
            return m;
        }


        public static int DegreeFromMask(int mask)
        {
            int d = 0; while ((mask & 0x0F) != 0) { d += (mask & 1); mask >>= 1; }
            return d;
        }
    }


    public struct MazeResult
    {
        public MazeGrid grid;
        public Vector2Int start; // fixed (0,0)
        public Vector2Int end; // farthest from start
    }


}