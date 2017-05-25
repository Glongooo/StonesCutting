using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutManager {


    private List<int> fun = new List<int>(new int[1000]); // 0's by default

    private Dictionary<int, List<IBlank>> bestBlanks = new Dictionary<int, List<IBlank>>();

    private List<Vector3> initPoly;
    private List<Vector3> initVectors = new List<Vector3>();
    private List<Vector3> rotPoly;
    private List<Vector3> rotVectors;

    private int[] stripSizes;
    //private int a = 30;
    //private int b = 30;

    private int maxX = 0;
    private int step;

    private Vector3 _shift = new Vector3(0, 0);
    private float _angle = 0f;

    // initPoly - отсортированный по часовой стрелке массив вершин
    public CutManager(List<Vector3> initPoly, int a, int b)
    {
        //this.a = a;
        //this.b = b;
        this.initPoly = initPoly;
        Debug.Log("square: " + PolySquare(initPoly));
        Debug.Log("testsquare: " + PolySquare(new List<Vector3>() { new Vector3(-1, -1),
        new Vector3(0, 2),
        new Vector3(2, 2),
        new Vector3(2, 0), }));
        rotPoly = new List<Vector3>(new Vector3[initPoly.Count]);

        float minY = 0;
        float minX = 0;
        for (int i = 0; i < initPoly.Count; i++)
        {
            if (initPoly[i].y < minY)
                minY = initPoly[i].y;
            if (initPoly[i].x < minX)
                minX = initPoly[i].x;
        }

        float shiftX = -minX;
        float shiftY = -minY;

        _shift += new Vector3(shiftX, shiftY);

        for (int i = 0; i < initPoly.Count; i++)
        {
            initPoly[i] += new Vector3(shiftX, shiftY);
        }

        for (int i = 0; i < initPoly.Count - 1; i++)
            initVectors.Add(initPoly[i + 1] - initPoly[i]);
        initVectors.Add(initPoly[0] - initPoly[initPoly.Count - 1]);

        rotVectors = new List<Vector3>(new Vector3[initVectors.Count]);

        stripSizes = new int[] { a, b, 20, 35 };
        step = GCD(stripSizes);
    }

    public int MakeCutting(out List<IBlank> blanks, out List<Vector3> rotpoly)
    {
        ChooseBaseAndRotate();

        int lastMax = MinInList(stripSizes);

        for (int x = lastMax; x <= maxX; x += step)
        {
            if (!bestBlanks.ContainsKey(x))
                bestBlanks.Add(x, new List<IBlank>());
            
            int maxPrice = 0;
            int stripSizeOfMax = -1;
            List<IBlank> maxBls = new List<IBlank>();
            for (int i = 0; i < stripSizes.Length; i++)
            {
                int currStripPrice;
                List<IBlank> bls;
                if (x >= stripSizes[i])
                {
                    CutStripe(x, stripSizes[i], out bls, out currStripPrice);

                    if (fun[x - stripSizes[i]] + currStripPrice >= maxPrice)
                    {
                        maxPrice = fun[x - stripSizes[i]] + currStripPrice;
                        stripSizeOfMax = stripSizes[i];
                        maxBls = bls;
                    }
                }
            }

            if (bestBlanks.ContainsKey(x - stripSizeOfMax))
                bestBlanks[x].AddRange(bestBlanks[x - stripSizeOfMax]);
            bestBlanks[x].AddRange(maxBls);

            fun[x] = maxPrice;
        }
        
        lastMax = IndMaxInList(fun);
        blanks = bestBlanks[lastMax];
        rotpoly = rotPoly;
        //foreach (var b in blanks)
        //{
        //    Quaternion rotation = Quaternion.Euler(0, 0, -_angle);
        //    Matrix4x4 m = Matrix4x4.identity;
        //    m.SetTRS(new Vector3(0, 0, 0), rotation, new Vector3(1, 1, 1));
        //    Vector3 v1, v2, v3, v4;
        //    v1 = b.v2 - b.v1;
        //    v2 = b.v3 - b.v2;
        //    v3 = b.v4 - b.v3;
        //    v4 = b.v1 - b.v4;
            
        //    //v1 = m.MultiplyPoint3x4(v1);
        //    //v2 = m.MultiplyPoint3x4(v2);
        //    //v3 = m.MultiplyPoint3x4(v3);
        //    //v4 = m.MultiplyPoint3x4(v4);

        //    b.v2 = b.v1 + v1;
        //    b.v3 = b.v2 + v2;
        //    b.v4 = b.v3 + v3;


        //    b.v1 -= _shift;
        //    b.v2 -= _shift;
        //    b.v3 -= _shift;
        //    b.v4 -= _shift;

        //    b.v1.y += 50;
        //    b.v2.y += 50;
        //    b.v3.y += 50;
        //    b.v4.y += 50;
        //}
        return fun[lastMax];
    }

    private Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }

    // Cut stripe [x - delta, x]
    private void CutStripe(int x, int delta, out List<IBlank> blanks, out int price)
    {
        blanks = new List<IBlank>();
        price = 0;

        // Vec nums
        int xUpNum, xDownNum;
        int deltaUpNum, deltaDownNum;

        FindIntersectNums(x, delta, out xUpNum, out xDownNum, out deltaUpNum, out deltaDownNum);

        //Vector3 secSupVec, secInfVec;
        //Vector3 vecSupY = GetIntersections(x, delta, xUp, deltaUp, out secSupVec);
        //Vector3 vecInfY = GetMaxIntersectionByY(x, delta, xDown, deltaDown, out secInfVec);

        Vector3 xDownInter, xUpInter, deltaDownInter, deltaUpInter;
        float xDownY, xUpY, deltaDownY, deltaUpY;

        GetIntersections(x, delta, xUpNum, deltaUpNum, out xUpInter, out deltaUpInter);
        GetIntersections(x, delta, xDownNum, deltaDownNum, out xDownInter, out deltaDownInter);

        xDownY = xDownInter.y;
        xUpY = xUpInter.y;
        deltaDownY = deltaDownInter.y;
        deltaUpY = deltaUpInter.y;

        int height;
        if (delta == stripSizes[0]) height = stripSizes[1];
        else if (delta == stripSizes[1]) height = stripSizes[0];
        else if (delta == stripSizes[2]) height = stripSizes[3];
        else height = stripSizes[2];
        //int height = (delta == a) ? b : a;
        // TODO 

        float coef = 0.2f;

        int infY = Mathf.RoundToInt(GetMaxIntersetionByY(deltaDownInter, xDownInter).y);
        int supY = Mathf.RoundToInt(GetMinIntersetionByY(deltaUpInter, xUpInter).y - height);

        int curY = infY - Mathf.RoundToInt(coef * height);
        float maxHeight = supY + Mathf.RoundToInt(coef * height);

        while (curY <= maxHeight)
        {
            IBlank blank;

            // Если ниже, чем область
            if (curY < infY)
            {
                if (curY < deltaDownY && curY < xDownY)
                {
                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        deltaDownInter,
                        new Vector3(x - delta, curY + height),
                        new Vector3(x, curY + height),
                        xDownInter,
                    });
                }
                else if (curY > deltaDownY && curY < xDownY)
                {
                    Vector3 inter;
                    LinesIntersection(out inter, deltaDownInter, xDownInter - deltaDownInter,
                        new Vector3(delta, curY), new Vector3(1, 0));

                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        new Vector3(x - delta, curY),
                        new Vector3(x - delta, curY + height),
                        new Vector3(x, curY + height),
                        xDownInter,
                        inter
                    });
                }
                else
                {
                    Vector3 inter;
                    LinesIntersection(out inter, deltaDownInter, xDownInter - deltaDownInter,
                        new Vector3(delta, curY), new Vector3(1, 0));

                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        deltaDownInter,
                        new Vector3(x - delta, curY + height),
                        new Vector3(x, curY + height),
                        new Vector3(x, curY),
                        inter
                    });
                }
            }
            // Если выше, чем область
            else if (curY > supY)
            {
                if (deltaUpY < curY + height && xUpY < curY + height)
                {
                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        new Vector3(x - delta, curY),
                        deltaUpInter,
                        xUpInter,
                        new Vector3(x, curY),
                    });
                }
                else if (curY + height > deltaUpY && curY + height < xUpY)
                {
                    Vector3 inter;
                    LinesIntersection(out inter, deltaUpInter, xUpInter - deltaUpInter, 
                        new Vector3(delta, curY + height), new Vector3(1, 0));

                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        new Vector3(x - delta, curY),
                        deltaUpInter,
                        inter,
                        new Vector3(x, curY + height),
                        new Vector3(x, curY),
                    });
                }
                else
                {
                    Vector3 inter;
                    LinesIntersection(out inter, deltaUpInter, xUpInter - deltaUpInter,
                        new Vector3(delta, curY + height), new Vector3(1, 0));

                    blank = new ElasticBlank(new List<Vector3>()
                    {
                        new Vector3(x - delta, curY),
                        new Vector3(x - delta, curY + height),
                        inter,
                        xUpInter,
                        new Vector3(x, curY),
                    });
                }
            }
            // Если внутри области
            else
            {
                blank = new Blank(
                   new Vector3(x - delta, curY),
                   delta,
                   height);
            }
            blanks.Add(blank);

            //price += height * delta;
            price += PolySquare(blank.GetPoints());

            curY += height;
        }
    }

    private void ChooseBaseAndRotate()
    {
        double maxDist = 0;
        int maxDistNum = -1;
        Vector3 center = Vector3.zero;

        for (int i = 0; i < initVectors.Count; i++)
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
        Debug.Log("center: " + center);
        float angle = Vector3.Angle(initVectors[maxDistNum], new Vector3(0, 1));

        if (initVectors[maxDistNum].x < 0)
            angle = -angle;

        _angle = angle;

        Debug.Log("rotation angle: " + angle);

        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        Matrix4x4 m = Matrix4x4.identity;
        m.SetTRS(new Vector3(0, 0, 0), rotation, new Vector3(1, 1, 1));

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

        Vector3 baseVec = rotPoly[maxDistNum];
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
        _shift -= new Vector3(baseVec.x, 0);
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

        if (rotPoly[rotPoly.Count - 1].x != rotPoly[0].x) {
            if (rotPoly[0].x > rotPoly[rotPoly.Count - 1].x)
            {
                if (x >= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x) && x <= Mathf.RoundToInt(rotPoly[0].x))
                    n1 = rotPoly.Count - 1;

                if (x - delta >= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x) && x - delta <= Mathf.RoundToInt(rotPoly[0].x))
                    n3 = rotPoly.Count - 1;
            }
            else
            {
                if (x >= Mathf.RoundToInt(rotPoly[0].x) && x <= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x))
                    n2 = rotPoly.Count - 1;
                if (x - delta >= Mathf.RoundToInt(rotPoly[0].x) && x - delta <= Mathf.RoundToInt(rotPoly[rotPoly.Count - 1].x))
                    n4 = rotPoly.Count - 1;
            }
        }

        upVecXNum = n1;
        downVecXNum = n2;
        upVecDeltaNum = n3;
        downVecDeltaNum = n4;
    }

    public Vector3 GetMinIntersetionByY(Vector3 inter1, Vector3 inter2)
    {
        return inter1.y < inter2.y ? inter1 : inter2;
    }

    public Vector3 GetMaxIntersetionByY(Vector3 inter1, Vector3 inter2)
    {
        return inter1.y < inter2.y ? inter2 : inter1;
    }

    private void GetIntersections(int x, int delta, int vecNumX, int vecNumDelta, out Vector3 xInter, out Vector3 deltaInter)
    {
        LinesIntersection(out xInter, rotPoly[vecNumX], rotVectors[vecNumX], new Vector3(x, 0, 0), new Vector3(0, 1, 0));
        LinesIntersection(out deltaInter, rotPoly[vecNumDelta], rotVectors[vecNumDelta], new Vector3(x - delta, 0, 0), new Vector3(0, 1, 0));
    }

    //private Vector3 GetMaxIntersectionByY(int x, int delta, int vecNumX, int vecNumDelta, out Vector3 govnoCodeSecondIntersection)
    //{
    //    Vector3 inter1;
    //    Vector3 inter2;
    //    LinesIntersection(out inter1, rotPoly[vecNumX], rotVectors[vecNumX], new Vector3(x, 0, 0), new Vector3(0, -1, 0));
    //    LinesIntersection(out inter2, rotPoly[vecNumDelta], rotVectors[vecNumDelta], new Vector3(x - delta, 0, 0), new Vector3(0, 1, 0));

    //    Vector3 minVec;
    //    if (inter1.y < inter2.y)
    //    {
    //        minVec = inter2;
    //        govnoCodeSecondIntersection = inter1;
    //    }
    //    else
    //    {
    //        minVec = inter1;
    //        govnoCodeSecondIntersection = inter2;
    //    }
    //    return minVec;
    //}

    public static bool LinesIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        linePoint1.z = 0;
        lineVec1.z = 0;
        linePoint2.z = 0;
        lineVec2.z = 0;

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.000001f && crossVec1and2.sqrMagnitude > 0.000001f)
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

    private int PolySquare(List<Vector3> vcs)
    {
        float square = 0;
        for (int i = 0; i < vcs.Count - 1; i++)
            square += (vcs[i].x + vcs[i + 1].x) * (vcs[i].y - vcs[i + 1].y);

        return Mathf.RoundToInt(Math.Abs( 1 / 2f * square));
    }

    private int MinInList(int[] list)
    {
        int min = 100500;
        foreach (int i in list)
            if (i < min)
                min = i;
        return min;
    }

    private int IndMaxInList(List<int> list)
    {
        int indMin = -1;
        int max = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i] >= max)
            {
                max = list[i];
                indMin = i;
            }
        return indMin;
    }

    public static int GCD(int a, int b)
    {
        return b == 0 ? a : GCD(b, a % b);
    }

    public static int GCD(int[] nums)
    {
        int d = nums[0];
        for (int i = 1; i < nums.Length; i++)
        {
            d = GCD(d, nums[i]);
        }

        return d;
    }
}
