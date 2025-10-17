using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Matrix
{
    public int rows;
    public int cols;
    public float[,] matrix;
    public float[,] results;



    /******************************************************************************************************
     *
     *
     *
     *
     *                                          Constructor
     *
     *
     *
     *
     * ***************************************************************************************************/

    public Matrix(int row, int col)
    {
        rows = row;
        cols = col;
        matrix = new float[row,col];


        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < col; x++)
            {
                matrix[i,x] = 0;
            }
        }
    }



    /******************************************************************************************************
     *
     *
     *
     *
     *                              Randomize the variables in the Matrix
     *
     *
     *
     *
     * ***************************************************************************************************/

    public void Randomize()
    {
        
        //Random r = new Random();
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i,x] = (float)((Random.Range(210, 250) - 100) / 100.0);
            }
        }
    }




    /******************************************************************************************************
     *
     *
     *
     *
     *                                          Scaler
     *
     *
     *
     *
     * ***************************************************************************************************/


    public void ScaleMultiply(float n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i,x] *= n;
            }
        }
    }

    public Matrix ScalerMultiply(float n)
    {
        Matrix results = new Matrix(rows, cols);
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i,x] *= n;
                results.matrix[i,x] = matrix[i,x];
            }
        }
        return results;
    }

    public void ScaleAdd(float n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i,x] += n;
            }
        }
    }

    public static void ScaleMultiply(float[][] m1, float[][] m2, int rows, int cols)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {

                m1[i][x] *= m2[i][x];

            }
        }
    }

    /******************************************************************************************************
     *
     *
     *
     *
     *                                          ElementWise
     *
     *
     *
     *
     * ***************************************************************************************************/

    public void ElementWiseAdd(Matrix n)
    {
        for (int i = 0; i < n.rows; i++)
        {
            for (int x = 0; x < n.cols; x++)
            {

                matrix[i,x] += n.matrix[i,x];
            }
        }
    }


    public static Matrix ElementWiseSubtract(Matrix a, Matrix b)
    {

        Matrix results = new Matrix(a.rows, b.cols);
        if (a.matrix.Length == b.matrix.Length)
        {
            for (int i = 0; i < a.rows; i++)
            {
                for (int x = 0; x < a.cols; x++)
                {
                    results.matrix[i,x] = a.matrix[i,x] - b.matrix[i,x];
                }
            }
        }
        else if (a.matrix.Length == 1)
        {
            for (int i = 0; i < a.rows; i++)
            {
                for (int x = 0; x < a.cols; x++)
                {
                    results.matrix[i,x] = a.matrix[0,0] - b.matrix[i,x];
                }
            }
        }
        else
        {
            results = null;
        }
        // Debug.Log("results: " + results.matrix[0,0] + " answer: " + a.matrix[0, 0] + " output: " + b.matrix[0, 0]);
        return results;
    }

    public void ElementWiseMultiply(Matrix n)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < cols; x++)
            {
                matrix[i,x] *= n.matrix[i,x];

            }
        }
    }

    public static Matrix ElementWiseMultiply(Matrix a, Matrix b)
    {
        Matrix guess = new Matrix(a.rows, a.cols);
        for (int i = 0; i < a.rows; i++)
        {
            guess.matrix[i,0] = 0;
            for (int x = 0; x < b.cols; x++)
            {

                guess.matrix[i,x] += a.matrix[i,x] * b.matrix[i,x];

            }
        }

        return guess;
    }





    public static Matrix FromArray(float[] array)
    {
        Matrix newMatrix = new Matrix(array.Length, 1);

        for (int i = 0; i < array.Length; i++)
        {

            newMatrix.matrix[i,0] = array[i];
        }

        return newMatrix;
    }


    /******************************************************************************************************
     *
     *
     *
     *
     *                                          Dot Product
     *
     *
     *
     *
     * ***************************************************************************************************/


    public Matrix DotMultiply(Matrix n)
    {
        Matrix result = new Matrix(rows, rows);
        for (int i = 0; i < rows; i++)
        {
            for (int x = 0; x < rows; x++)
            {
                float sum = 0;
                for (int k = 0; k < cols; k++)
                {
                    sum += matrix[i,k] * n.matrix[k,x];
                }
                result.matrix[i,x] = sum;
            }
        }
        return result;
    }
    public static Matrix DotMultiply(Matrix a, Matrix b)
    {

        Matrix result = new Matrix(a.rows, b.cols);
        for (int i = 0; i < result.rows; i++)
        {
            for (int j = 0; j < result.cols; j++)
            {
                // Dot product of values in col
                float sum = 0;
                for (int k = 0; k < a.cols; k++)
                {
                    sum += a.matrix[i,k] * b.matrix[k,j];
                }
                result.matrix[i,j] = sum;
            }
        }
        return result;
    }


    /******************************************************************************************************
     *
     *
     *
     *
     *                                          Transpose
     *
     *
     *
     *
     * ***************************************************************************************************/

    public static Matrix Transpose(Matrix a)
    {
        Matrix results = new Matrix(a.cols, a.rows);
        for (int i = 0; i < a.cols; i++)
        {
            for (int x = 0; x < a.rows; x++)
            {
                results.matrix[i,x] = a.matrix[x,i];
            }
        }
        return results;
    }


    /******************************************************************************************************
     *
     *
     *
     *
     *                                             Map
     *
     *
     *
     *
     * ***************************************************************************************************/


    public static float[] toArray(Matrix n)
    {
        float[] array = new float[n.rows];
        for (int i = 0; i < n.rows; i++)
        {
            for (int x = 0; x < n.cols; x++)
            {
                array[i] += n.matrix[i,x];
            }
        }


        return array;
    }



    public void SigmoidMatrix()
    {


        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i,j] = Sigmoid(matrix[i,j]);
            }
        }
    }



    private float Sigmoid(float x)
    {
        float k = 1 / (1 + Mathf.Pow(x, -x));

        if (k >= 0.5)
        {
            return 1;
        }
        else
        {
            return 0;
        }


    }



    public static Matrix DeltaSigmoidMatrix(Matrix a)
    {
        Matrix results = new Matrix(a.rows, a.cols);

        for (int i = 0; i < a.rows; i++)
        {
            for (int j = 0; j < a.cols; j++)
            {
                results.matrix[i,j] = DeltaSigmoid(a.matrix[i,j]);
            }
        }

        return results;
    }

    private static float DeltaSigmoid(float x)
    {
        return 1 / (1 + Mathf.Pow(x, -x));
        //return x * (1 - x);
    }

}
