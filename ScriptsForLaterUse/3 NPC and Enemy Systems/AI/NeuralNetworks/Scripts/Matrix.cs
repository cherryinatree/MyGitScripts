using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix
{
    private int rows;
    private int cols;
    public float[,] matrix;
    public float[,] results;
    public Matrix(int row, int col)
    {
        rows = row;
        cols = col;
        matrix = new float[row, col];

        for(int i = 0; i<rows; i++)
        {
            for (int x = 0; x < rows; x++)
            {
                matrix[i, x] = 0;
            }
        }
    }

    public void ScaleRandomize()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i, x] = Random.Range(-1f,1f);
            }
        }
    }
    public void ScaleMultiply(float n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i, x] *= n;
            }
        }
    }

    public void ScaleAdd(float n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i, x] += n;
            }
        }
    }
    public float[,] ScaleDotMultiply(float[,] n)
    {
        results = new float[rows, rows];
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                float sum = 0;
                for (int k = 0; k < cols; k++)
                {
                    sum += matrix[i, k] * n[k, x];
                }
                results[i, x] = sum;
            }
        }
        return results;
    }
    public void ScaleMultiply(float[,] n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                
                matrix[i, x] *= n[i, x];
                
            }
        }
    }

    public static void ScaleMultiply(float[,] m1, float[,] m2, int rows, int cols)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {

                m1[i, x] *= m2[i, x];

            }
        }
    }


    public float[,] Transpose()
    {
        results = new float[cols, rows];
        for (int i = 0; i < cols; i++)
        {
            for (int x = 0; x < rows; x++)
            {
                results[i, x] = matrix[x, i];
            }
        }
        return results;
    }


    public void ScaleAdd(float[,] n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i, x] += n[i,x];
            }
        }
    }

}
