using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blank {

    // Clockwise sorted vertices
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
    public Vector3 v4;

    /* 
     * v2---v3
     * |    |
     * |    |b
     * |  a |
     * v1---v4
     */

    public Blank(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
        this.v4 = v4;
    }

    public Blank(Vector3 v1, int a, int b)
    {
        this.v1 = v1;
        this.v2 = v1 + new Vector3(0, b);
        this.v3 = v1 + new Vector3(a, b);
        this.v4 = v1 + new Vector3(0, b);
    }
}
