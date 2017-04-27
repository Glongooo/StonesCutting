﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutManager {


    private List<int> fun = new List<int>(new int[1000]); // 0's by default

    private Dictionary<int, List<Blank>> bestBlanks = new Dictionary<int, List<Blank>>();

    private List<Vector3> initPoly;
    private List<Vector3> initVectors = new List<Vector3>();
    private List<Vector3> rotPoly;
    private List<Vector3> rotVectors;
    
    private int a = 1;
    private int b = 1;

    private int maxX = 0;
    private int step;

    public CutManager(List<Vector3> initPoly)
    {
        this.initPoly = initPoly;
        rotPoly = new List<Vector3>(new Vector3[initPoly.Count]);

        for (int i = 0; i < initPoly.Count - 1; i++)
            initVectors.Add(initPoly[i + 1] - initPoly[i]);
        initVectors.Add(initPoly[0] - initPoly[initPoly.Count - 1]);

        rotVectors = new List<Vector3>(new Vector3[initVectors.Count]);

        step = GCD(a, b);
    }

    public int MakeCutting(out List<Blank> blanks)
    {
        ChooseBaseAndRotate();

        int lastMax = Math.Min(a, b);

        for (int x = Math.Min(a, b); x <= maxX; x += step)
        {
            List<Blank> b1 = new List<Blank>();
            List<Blank> b2 = new List<Blank>();
            int p1 = 0, p2 = 0;

            if (x >= a)
            {
                CutStripe(x, a, out b1, out p1);
                p1 += fun[x - a];
            }
            if (x >= b)
            {
                CutStripe(x, b, out b2, out p2);
                p2 += fun[x - b];
            }

            int maxP = Math.Max(p1, p2);

            if (p1 > p2)
                bestBlanks[x] = b1;
            else
                bestBlanks[x] = b2;

            fun[x] = maxP;
            lastMax = x;
        }
        //blanks = null;
        blanks = bestBlanks[lastMax];
        return fun[lastMax];
    }

    // Cut stripe [x - delta, x]
    private void CutStripe(int x, int delta, out List<Blank> blanks, out int price)
    {
        blanks = new List<Blank>();
        price = 0;

        // Vec nums
        int xUp, xDown;
        int deltaUp, deltaDown;

        FindIntersectNums(x, delta, out xUp, out xDown, out deltaUp, out deltaDown);
        Vector3 supY = GetMinIntersectionsByY(x, delta, xUp, deltaUp);
        Vector3 infY = GetMaxIntersectionsByY(x, delta, xDown, deltaDown);

        int height = (delta == a) ? b : a;
        // TODO 
        int curY = (int) infY.y;

        while (curY <= supY.y - height)
        {
            Blank blank = new Blank(
                new Vector3(x - delta, curY),
                delta,
                height);
            blanks.Add(blank);

            price += height * delta;
            
            curY += height;
        }
    }

    private void ChooseBaseAndRotate()
    {
        double maxDist = 0;
        int maxDistNum = -1;
        Vector3 center = Vector3.zero;

        for (int i = 0; i < initVectors.Count ; i++)
        {
            center += initPoly[i];
            double dist = Vector3.Magnitude(initVectors[i]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxDistNum = i;
            }
        }

        center /= initVectors.Count;

        Debug.Log("base vector: " + initVectors[maxDistNum]);

        // Пока что считаем, что изначально фигура лежит в 1 координатной четверти
        int mul = 1;
        Debug.Log("center: " + center);
        if (center.y > initVectors[maxDistNum].y)
            mul = -1;

        float angle = mul * Vector3.Angle(initVectors[maxDistNum], new Vector3(0, 1));
        Debug.Log("rotation angle: " + angle);

        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        Matrix4x4 m = Matrix4x4.identity;
        m.SetTRS(new Vector3(0, 0, 0), rotation, new Vector3(1, 1, 1));
        Debug.Log("mult: " + m.MultiplyPoint3x4(new Vector3(0, 0)));
        Debug.Log("mult: " + m.MultiplyPoint3x4(new Vector3(0, 1)));
        Debug.Log("mult: " + m.MultiplyPoint3x4(new Vector3(1, 0)));
        Debug.Log("mult: " + m.MultiplyPoint3x4(new Vector3(1, 1)));
        Debug.Log("mult: " + m.MultiplyPoint3x4(new Vector3(-1, -1)));

        Debug.Log(initVectors[maxDistNum].ToString());

        Vector3 newVec;

        rotPoly[maxDistNum] = initPoly[maxDistNum];
        
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

        Vector3 baseVec = rotPoly[0] + rotVectors[0];
        Debug.Log("rotPoly");
        for (int i = 0; i < rotPoly.Count; i++)
        {
            rotPoly[i] -= new Vector3(baseVec.x, 0);
            Debug.Log(rotPoly[i]);
            if (rotPoly[i].x > maxX)
            {
                maxX = Mathf.RoundToInt(rotPoly[i].x); 
            }
        }
        Debug.Log("rotPoly end");
    }
    
    private void FindIntersectNums(int x, int delta, out int upVecXNum, out int downVecXNum, out int upVecDeltaNum, out int downVecDeltaNum)
    {
        int n1 = -1, n2 = -1;
        int n3 = -1, n4 = -1;
        for (int i = 0; i < rotPoly.Count - 1; i++)
        {
            if (rotPoly[i + 1].x == rotPoly[i].x)
                continue;

            if (rotPoly[i + 1].x > rotPoly[i].x)
            {
                if (x >= Mathf.RoundToInt(rotPoly[i].x) && x <= Mathf.RoundToInt(rotPoly[i + 1].x))
                    n1 = i;

                if (x - delta >= Mathf.RoundToInt(rotPoly[i].x) && x - delta <= Mathf.RoundToInt(rotPoly[i + 1].x))
                    n3 = i;
            }
            else
            {
                if (x >= Mathf.RoundToInt(rotPoly[i + 1].x) && x <= Mathf.RoundToInt(rotPoly[i].x))
                    n2 = i;
                if (x - delta >= Mathf.RoundToInt(rotPoly[i + 1].x) && x - delta <= Mathf.RoundToInt(rotPoly[i].x))
                    n4 = i;
            }
        }

        if (x <= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x) && x >= Mathf.RoundToInt(rotPoly[0].x))
            n2 = rotPoly.Count - 1;
        if (x - delta <= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x) && x - delta >= Mathf.RoundToInt(rotPoly[0].x))
            n4 = rotPoly.Count - 1;

        upVecXNum = n1;
        downVecXNum = n2;
        upVecDeltaNum = n3;
        downVecDeltaNum = n4;
    }


    private Vector3 GetMinIntersectionsByY(int x, int delta, int vecNumX, int vecNumDelta)
    {
        Vector3 inter1;
        Vector3 inter2;
        LinesIntersection(out inter1, rotPoly[vecNumX], rotVectors[vecNumX], new Vector3(x, 0, 0), new Vector3(0, 1, 0));
        LinesIntersection(out inter2, rotPoly[vecNumDelta], rotVectors[vecNumDelta], new Vector3(x - delta, 0, 0), new Vector3(0, 1, 0));

        return inter1.y < inter2.y ? inter1 : inter2;
    }

    private Vector3 GetMaxIntersectionsByY(int x, int delta, int vecNumX, int vecNumDelta)
    {
        Vector3 inter1;
        Vector3 inter2;
        LinesIntersection(out inter1, rotPoly[vecNumX], rotVectors[vecNumX], new Vector3(x, 0, 0), new Vector3(0, -1, 0));
        LinesIntersection(out inter2, rotPoly[vecNumDelta], rotVectors[vecNumDelta], new Vector3(x - delta, 0, 0), new Vector3(0, 1, 0));
        return inter1.y < inter2.y ? inter2 : inter1;
    }

    public static bool LinesIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    public static int GCD(int a, int b)
    {
        return b == 0 ? a : GCD(b, a % b);
    }
}