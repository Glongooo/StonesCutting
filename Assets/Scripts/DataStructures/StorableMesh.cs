using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorableMesh{
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector3> normals;

    public StorableMesh(List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.normals = normals;
    }
}
