using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElasticBlank : IBlank {

    public List<Vector3> points;

    public ElasticBlank(List<Vector3> points)
    {
        this.points = points;
    }

    public List<Vector3> GetPoints()
    {
        return points;
    }
}
