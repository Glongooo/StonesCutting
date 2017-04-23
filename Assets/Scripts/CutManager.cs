using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutManager {

    private List<int> fun = new List<int>(new int[1000]); // 0's by default

    private List<Vector3> initPoly;
    private List<Vector3> initVectors = new List<Vector3>();
    private List<Vector3> rotPoly;
    private List<Vector3> rotVectors;

    private int a;
    private int b;

    public CutManager(List<Vector3> initPoly)
    {
        this.initPoly = initPoly;
        rotPoly = new List<Vector3>(new Vector3[initPoly.Count]);

        for (int i = 0; i < initPoly.Count - 1; i++)
            initVectors.Add(initPoly[i + 1] - initPoly[i]);
        initVectors.Add(initPoly[0] - initPoly[initPoly.Count - 1]);

        rotVectors = new List<Vector3>(new Vector3[initVectors.Count]);
    }

    //public List<Vector3> MakeCutting()
    //{
        
    //}

    public void ChooseBase()
    {
        double maxDist = 0;
        int maxDistNum = -1;

        for (int i = 0; i < initVectors.Count ; i++)
        {
            double dist = Vector3.Magnitude(initVectors[i]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxDistNum = i;
            }
        }

        float angle = Vector3.Angle(initVectors[maxDistNum], new Vector3(0, 1));
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        Matrix4x4 m = Matrix4x4.identity;
        m.SetTRS(new Vector3(0, 0, 0), rotation, new Vector3(1, 1, 1));

        Debug.Log(initVectors[maxDistNum].ToString());

        Vector3 newVec;

        rotPoly[maxDistNum] = initPoly[maxDistNum];

        Debug.Log(rotVectors.Count + " " + initPoly.Count);
        for (int i = maxDistNum; i < rotPoly.Count - 1; i++)
        {
            newVec = m.MultiplyPoint3x4(initVectors[i]);
            rotVectors[i] = newVec;
            rotPoly[i + 1] = rotPoly[i] + newVec;
        }

        newVec = m.MultiplyPoint3x4(initVectors[initVectors.Count - 1]);
        rotVectors[initVectors.Count - 1] = newVec;
        rotPoly[0] = rotPoly[rotPoly.Count - 1] + newVec;

        for (int i = 0; i < maxDistNum; i++)
        {
            newVec = m.MultiplyPoint3x4(initVectors[i]);
            rotVectors[i] = newVec;
            rotPoly[i + 1] = rotPoly[i] + newVec;
        }



        foreach (var i in rotPoly)
        {
            Debug.Log("" + i.ToString());
        }

        //Vector3 newVec = m.MultiplyPoint3x4(initVectors[initVectors.Count - 1]);
        //Vector3 newPoint = newVec + initPoly[initPoly.Count - 1];
        //Debug.Log(initPoly[0].ToString() + " new: " + newPoint.ToString());
        //initPoly[0] = newPoint;

        //for (int i = 0; i < initVectors.Count - 1; i++)
        //{
        //    newVec = m.MultiplyPoint3x4(initVectors[i]);
        //    newPoint = newVec + initPoly[i];
        //    Debug.Log(initPoly[i + 1].ToString() + " new: " + newPoint.ToString());
        //    initPoly[0] = newPoint;
        //}

        //for (int i = 0; i < initVectors.Count ; i++)
        //{
        //    newVec = m.MultiplyPoint3x4(initVectors[i]);
        //    Debug.Log(initVectors[i].ToString() + " new: " + newVec.ToString());
        //}
    }

    private List<int> xs = new List<int>();

    //private void FindIntersectNums(int x, out int upVecNum, out int downVecNum)
    //{

    //}
}
