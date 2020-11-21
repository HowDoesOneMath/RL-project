using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Matrix class, named Jatrix
public class Jatrix
{
    //Necessary matrix data
    private float[][] elements;
    public int width, height;

    //New matrix constructor, takes in width and height, initializes all elements to 0
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

    //Quickly makes an identity matrix
    public static Jatrix Identity(int rows)
    {
        Jatrix toReturn = new Jatrix(rows, rows);

        for (int i = 0; i < rows; ++i)
        {
            toReturn[i, i] = 1;
        }

        return toReturn;
    }

    //Copy constructor, allows us to efficiently duplicate matrics
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

    //Allows us to use array access, so instead of Matrix.elements[i][j] we can do Matrix[i,j]
    public float this[int i, int j]
    {
        get { return elements[i][j]; }
        set { elements[i][j] = value; }
    }

    //Used to instantly initialize the matrix to a given value - currently deprecated
    public void SetAs(float val)
    {
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                elements[i][j] = val;
            }
        }
    }

    //Overloaded matrix addition
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

    //Overloaded positive operator
    public static Jatrix operator +(Jatrix a)
    {
        return a;
    }

    //Overloaded subtraction operator
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

    //Overloaded negative operator
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

    //Overloaded multiplication operator
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

    //Overloaded multiplication operator, using a float
    public static Jatrix operator *(Jatrix a, float b)
    {
        Jatrix toRet = new Jatrix(a.width, a.height);
        for (int i = 0; i < toRet.width; ++i)
        {
            for (int j = 0; j < toRet.height; ++j)
            {
                toRet[i, j] = a[i, j] * b;
            }
        }

        return toRet;
    }

    //Allows us to see all the elements of a matrix
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
