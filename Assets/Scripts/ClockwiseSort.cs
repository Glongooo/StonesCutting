using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockwiseSort{
    private Vector3 centerPoint;
    public void SortClockwise(List<Vector3> list)
    {
        Vector3 centerPoint = Vector3.zero;
        foreach (var i in list)
            centerPoint += i;
        centerPoint /= list.Count;
        list.Sort(ClocwiseComparer);
    }
    private int ClocwiseComparer(Vector3 a, Vector3 b)
    {
        var posA = a;
        var posB = b;
        //  Variables to Store the atans
        double aTanA, aTanB;

        //  Fetch the atans
        aTanA = Math.Atan2(posA.y - centerPoint.y, posA.x - centerPoint.x);
        aTanB = Math.Atan2(posB.y - centerPoint.y, posB.x - centerPoint.x);

        //  Determine next point in Clockwise rotation
        if (aTanA < aTanB) return -1;
        else if (aTanB < aTanA) return 1;
        return 0;
    }
}
