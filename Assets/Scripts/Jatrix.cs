using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jatrix
{
    private float[][] elements;
    public int width, height;

    public Jatrix(int matrixWidth, int matrixHeight)
    {
        width = Mathf.Max(matrixWidth, 1);
        height = Mathf.Max(matrixHeight, 1);

        elements = new float[width][];

        for (int i = 0; i < width; ++i)
        {
            elements[i] = new float[height];

            for (int j = 0; j < height; ++j)
            {
                elements[i][j] = 0;
            }
        }
    }

    public static Jatrix Identity(int rows)
    {
        Jatrix toReturn = new Jatrix(rows, rows);

        for (int i = 0; i < rows; ++i)
        {
            toReturn[i, i] = 1;
        }

        return toReturn;
    }

    public Jatrix(Jatrix toCopy)
    {
        width = toCopy.width;
        height = toCopy.height;

        elements = new float[width][];

        for (int i = 0; i < width; ++i)
        {
            elements[i] = new float[height];

            for (int j = 0; j < height; ++j)
            {
                elements[i][j] = toCopy[i,j];
            }
        }
    }

    public float this[int i, int j]
    {
        get { return elements[i][j]; }
        set { elements[i][j] = value; }
    }

    public static Jatrix operator+(Jatrix a, Jatrix b)
    {
        if (a.width != b.width || a.height != b.height)
        {
            Debug.LogError("ERROR: MATRIX DIMENSION MISMATCH!");
            return null;
        }

        Jatrix toRet = new Jatrix(a.width, a.height);
        for (int i = 0; i < toRet.width; ++i)
        {
            for (int j = 0; j < toRet.height; ++j)
            {
                toRet[i,j] = a[i, j] + b[i, j];
            }
        }

        return toRet;
    }

    public static Jatrix operator +(Jatrix a)
    {
        return a;
    }

    public static Jatrix operator-(Jatrix a, Jatrix b)
    {
        if (a.width != b.width || a.height != b.height)
        {
            Debug.LogError("ERROR: MATRIX DIMENSION MISMATCH!");
            return null;
        }

        Jatrix toRet = new Jatrix(a.width, a.height);
        for (int i = 0; i < toRet.width; ++i)
        {
            for (int j = 0; j < toRet.height; ++j)
            {
                toRet[i, j] = a[i, j] - b[i, j];
            }
        }

        return toRet;
    }

    public static Jatrix operator -(Jatrix a)
    {
        Jatrix toRet = new Jatrix(a.width, a.height);
        for (int i = 0; i < toRet.width; ++i)
        {
            for (int j = 0; j < toRet.height; ++j)
            {
                toRet[i, j] = -a[i, j];
            }
        }

        return toRet;
    }

    public static Jatrix operator *(Jatrix a, Jatrix b)
    {
        if (a.width != b.height)
        {
            Debug.LogError("ERROR: MATRIX DIMENSION MISMATCH!");
            return null;
        }

        Jatrix toRet = new Jatrix(b.width, a.height);
        for (int i = 0; i < toRet.width; ++i)
        {
            for (int j = 0; j < toRet.height; ++j)
            {
                float sum = 0;

                for (int ii = 0; ii < a.width; ++ii)
                {
                    sum += a[ii, j] * b[i, ii];
                }

                toRet[i, j] = sum;
            }
        }

        return toRet;
    }

    public void DebugMatrix()
    {
        Debug.Log(width + ", " + height);

        for (int i = 0; i < width; ++i)
        {
            string line = "";
            for (int j = 0; j < height; ++j)
            {
                line += ("\t " + elements[i][j]);
            }

            Debug.Log(line);
        }
    }
}
