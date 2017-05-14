using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer
{
    public static Mesh[] SliceMeshByPlane(Mesh mesh, Vector3 one, Vector3 two, Vector3 three, out Mesh hat)
    {
        Mesh[] result = new Mesh[2];

        var vertices = mesh.vertices;
        #region Side Arrays
        Dictionary<int, int> PositiveIndices = new Dictionary<int, int>();
        Dictionary<int, int> NegativeIndices = new Dictionary<int, int>();
        List<Vector3> positiveVertices = new List<Vector3>();
        List<Vector3> negativeVertices = new List<Vector3>();
        List<int> PositiveSideTriangles = new List<int>();
        List<int> NegativeSideTriangles = new List<int>();
        List<int> PositiveHatIndices = new List<int>();
        List<int> NegativeHatIndices = new List<int>();
        #endregion

        #region Determine Vertex/Side relations
        var plane = new Plane(one, two, three);
        int p = 0;
        int n = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (plane.GetSide(vertices[i]))
            {
                PositiveIndices.Add(i, p);
                positiveVertices.Add(vertices[i]);
                p++;
            }
            else
            {
                NegativeIndices.Add(i, n);
                negativeVertices.Add(vertices[i]);
                n++;
            }
        }
        #endregion

        if (PositiveIndices.Count < 1 || NegativeIndices.Count < 1)
        {
            result[0] = mesh;
            hat = null;
            return result;
        }

        #region Meshes triangles generation
        var triangles = mesh.GetTriangles(0);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            //Если треугольник целиком лежит по одну сторону от секущей плоскости - добавляем его целиком
            #region NotIntersectedTriangles
            if (PositiveIndices.ContainsKey(triangles[i]) &&
                PositiveIndices.ContainsKey(triangles[i + 1]) &&
                PositiveIndices.ContainsKey(triangles[i + 2]))
            {
                PositiveSideTriangles.Add(PositiveIndices[triangles[i]]);
                PositiveSideTriangles.Add(PositiveIndices[triangles[i + 1]]);
                PositiveSideTriangles.Add(PositiveIndices[triangles[i + 2]]);
                continue;
            }

            if (NegativeIndices.ContainsKey(triangles[i]) &&
                NegativeIndices.ContainsKey(triangles[i + 1]) &&
                NegativeIndices.ContainsKey(triangles[i + 2]))
            {
                NegativeSideTriangles.Add(NegativeIndices[triangles[i]]);
                NegativeSideTriangles.Add(NegativeIndices[triangles[i + 1]]);
                NegativeSideTriangles.Add(NegativeIndices[triangles[i + 2]]);
                continue;
            }
            #endregion

            //Если вершины треугольника находятся не на одной стороне, то находим пересечения 
            //Линий образованных ребрабми с секущей плоскостью и если пересечения принадлежать ребрам треугольника
            //Записываем точку пересечения в массив
            #region Intersection Points Calculation
            List<int> curTriangle = new List<int>();
            curTriangle.Add(triangles[i]);
            curTriangle.Add(triangles[i + 1]);
            curTriangle.Add(triangles[i + 2]);
            curTriangle.Add(triangles[i]);
            List<Vector3> points = new List<Vector3>();
            for (int k = 0; k < 3; k++)
            {
                Vector3 origin = vertices[curTriangle[k]];
                Vector3 direction = vertices[curTriangle[k + 1]] - origin;
                Ray ray = new Ray(origin, direction.normalized);
                float enter;
                if (plane.Raycast(ray, out enter) &&
                enter <= Vector3.Magnitude(direction))
                {
                    Vector3 point = origin + direction.normalized * enter;
                    points.Add(point);
                }
            }
            #endregion

            #region Add intersection points in points lists
            foreach (var point in points)
            {
                NegativeHatIndices.Add(n);
                negativeVertices.Add(point);
                n++;
                PositiveHatIndices.Add(p);
                positiveVertices.Add(point);
                p++;
            }
            #endregion

            #region Generation triangles with new vertices
            List<int>[] ind = { new List<int>(), new List<int>() };

            for (int k = 0; k < 3; k++)
            {
                if (PositiveIndices.ContainsKey(curTriangle[k]))
                    ind[1].Add(curTriangle[k]);
                else
                    ind[0].Add(curTriangle[k]);
            }

            for (int k = 0; k < ind.Length; k++)
            {

                List<int> sideTriangles;
                List<Vector3> sideVertices;
                int locind;
                Dictionary<int, int> sideIndices;
                if (k == 0)
                {
                    sideTriangles = NegativeSideTriangles;
                    sideIndices = NegativeIndices;
                    sideVertices = negativeVertices;
                    locind = n;
                }
                else
                {
                    sideVertices = positiveVertices;
                    sideTriangles = PositiveSideTriangles;
                    sideIndices = PositiveIndices;
                    locind = p;
                }
                if (ind[k].Count > 1)
                {
                    sideTriangles.Add(sideIndices[ind[k][0]]);
                    sideTriangles.Add(locind - 1);
                    sideTriangles.Add(locind - 2);


                    var upVector = (vertices[ind[k][1]] - vertices[ind[k][0]]).normalized;
                    var downVector = (sideVertices[locind - 2] - sideVertices[locind - 1]).normalized;

                    if (Vector3.Magnitude(upVector + downVector) > 1.0f)
                    {
                        sideTriangles.Add(sideIndices[ind[k][0]]);
                        sideTriangles.Add(sideIndices[ind[k][1]]);
                        sideTriangles.Add(locind - 2);
                    }
                    else
                    {
                        sideTriangles.Add(sideIndices[ind[k][0]]);
                        sideTriangles.Add(sideIndices[ind[k][1]]);
                        sideTriangles.Add(locind - 1);
                    }
                }
                else
                {
                    sideTriangles.Add(sideIndices[ind[k][0]]);
                    sideTriangles.Add(locind - 1);
                    sideTriangles.Add(locind - 2);
                }

            }

            #endregion


        }
        #endregion

        #region Initializing result meshes
        Mesh positiveMesh = new Mesh();
        positiveMesh.SetVertices(positiveVertices);
        positiveMesh.SetTriangles(PositiveSideTriangles.ToArray(), 0);
        //positiveMesh.RecalculateNormals();
        Mesh negativeMesh = new Mesh();
        negativeMesh.SetVertices(negativeVertices);
        negativeMesh.SetTriangles(NegativeSideTriangles.ToArray(), 0);
        //negativeMesh.RecalculateNormals();

        #region hatGeneration
        hat = new Mesh();
        var hatVerts = new List<Vector3>();
        var hatTriangles = new List<int>();
        var hatCenterPoint = Vector3.zero;
        for (int i = 0; i < NegativeHatIndices.Count; i++)
        {
            hatCenterPoint += negativeVertices[NegativeHatIndices[i]];
            hatVerts.Add(negativeVertices[NegativeHatIndices[i]]);
        }
        hatCenterPoint /= hatVerts.Count;
        //(new ClockwiseSort()).SortClockwise(hatVerts);

        //var planeList = ToPlane2d(hatVerts, one, two, three);

        var sorted = SortIn2DPlane(hatVerts, one, two, three);
        hatVerts = sorted;


        hatVerts.Add(hatCenterPoint);


        for (int i = 0; i < hatVerts.Count - 2; i++)
        {
            hatTriangles.Add(i);
            hatTriangles.Add(i + 1);
            hatTriangles.Add(hatVerts.Count - 1);

            hatTriangles.Add(i + 1);
            hatTriangles.Add(i);
            hatTriangles.Add(hatVerts.Count - 1);
        }
        hatTriangles.Add(0);
        hatTriangles.Add(hatVerts.Count - 2);
        hatTriangles.Add(hatVerts.Count - 1);

        hatTriangles.Add(0);
        hatTriangles.Add(hatVerts.Count - 1);
        hatTriangles.Add(hatVerts.Count - 2);

        hat.SetVertices(hatVerts);
        hat.SetTriangles(hatTriangles, 0);
        //hat.RecalculateNormals();
        #endregion

        result[0] = negativeMesh;
        result[1] = positiveMesh;
        #endregion

        return result;
    }

    public static Mesh[] SliceInPeaces(int n, Mesh mesh, float maxLen, List<Vector3> points, out List<Mesh> hats)
    {
        var verts = mesh.vertices;
        hats = new List<Mesh>();
        Vector3 centerPoint = Vector3.zero;
        foreach (var i in verts)
        {
            centerPoint += i;
        }
        centerPoint /= verts.Length;
        Mesh[] result = new Mesh[n];
        var delta = maxLen / n;
        Mesh[] curres = new Mesh[2];
        var tmpmesh = mesh;

        var plane = new Plane(points[0], points[1], points[2]);
        var dir = plane.normal.normalized;
        for (int i = 1; i < n; i++)
        {
            Mesh hat;
            curres = SliceMeshByPlane(tmpmesh, points[0] + dir * i * delta, points[1] + dir * i * delta, points[2] + dir * i * delta, out hat);
            result[i - 1] = curres[0];
            tmpmesh = curres[1];
            hats.Add(hat);
        }
        result[n - 1] = curres[1];

        return result;

    }

    private static List<Vector2> ToPlane2d(List<Vector3> vertices, Vector3 one, Vector3 two, Vector3 three)
    {
        List<Vector2> result = new List<Vector2>();
        var plane = new Plane(one, two, three);
        var norm = plane.normal;
        var tan1 = two - one;
        var tan2 = two - three;
        Vector3.OrthoNormalize(ref tan1, ref tan2, ref norm);
        Matrix4x4 toNewSpace = new Matrix4x4();
        toNewSpace.SetRow(0, tan1);
        toNewSpace.SetRow(1, tan2);
        toNewSpace.SetRow(2, norm);
        toNewSpace[3, 3] = 1.0F;
        Matrix4x4 scale = new Matrix4x4();
        scale[0, 0] = 1;
        scale[1, 1] = 1.0F;
        scale[2, 2] = 1.0F;
        scale[3, 3] = 1.0F;
        Matrix4x4 fromNewSpace = toNewSpace.transpose;
        Matrix4x4 trans = toNewSpace * scale * fromNewSpace;
        foreach (var i in vertices)
        {
            var nv = toNewSpace.MultiplyPoint3x4(i);
            result.Add(new Vector2(nv.x, nv.y));
        }
        return result;

    }

    private static List<Vector3> SortIn2DPlane(List<Vector3> vertices, Vector3 one, Vector3 two, Vector3 three)
    {
        List<Vector3> result = new List<Vector3>();
        var plane = new Plane(one, two, three);
        var norm = plane.normal;
        var tan1 = two - one;
        var tan2 = two - three;
        Vector3.OrthoNormalize(ref tan1, ref tan2, ref norm);
        Matrix4x4 toNewSpace = new Matrix4x4();
        toNewSpace.SetRow(0, tan1);
        toNewSpace.SetRow(1, tan2);
        toNewSpace.SetRow(2, norm);
        toNewSpace[3, 3] = 1.0F;
        Matrix4x4 scale = new Matrix4x4();
        scale[0, 0] = 1;
        scale[1, 1] = 1.0F;
        scale[2, 2] = 1.0F;
        scale[3, 3] = 1.0F;
        Matrix4x4 fromNewSpace = toNewSpace.transpose;
        foreach (var i in vertices)
        {
            var nv = toNewSpace.MultiplyPoint3x4(i);
            result.Add(nv);
        }

        (new ClockwiseSort()).SortClockwise(result);
        List<Vector3> backRes = new List<Vector3>();
        foreach (var i in result)
            backRes.Add(fromNewSpace.MultiplyPoint3x4(i));

        return backRes;

    }
}

public class PolygonIntersector
{
    public class Intersect
    {
        public int result;
        public Vector3 point;
    }

    public class IntersectionPoint
    {
        public IntersectionPoint otherPoint;
        public Vector3 position;
        public bool insideOther = false;
        public int originalListPos = -1;
        public int pointPos;
        public bool stable = false;
        public bool dirty = false;
        public IntersectionPoint(Vector3 position)
        {
            this.position = position;
        }

        public IntersectionPoint(Vector3 position, bool insideOther)
        {
            this.position = position;
            this.insideOther = insideOther;
        }

        public IntersectionPoint(Vector3 position, bool insideOther, bool stable, int originalPos)
        {
            this.position = position;
            this.insideOther = insideOther;
            this.stable = stable;
            this.originalListPos = originalPos;
        }
    }

    public static void CreateInterPoints(List<Vector3> first, List<Vector3> second, out List<IntersectionPoint> firstout, out List<IntersectionPoint> secondOut)
    {
        firstout = new List<IntersectionPoint>();
        secondOut = new List<IntersectionPoint>();
        for (int i = 0; i < first.Count; i++)
        {
            firstout.Add(new IntersectionPoint(first[i], IsPointInsidePoly(second, first[i]), true, i));
        }

        for (int i = 0; i < second.Count; i++)
        {
            secondOut.Add(new IntersectionPoint(second[i], IsPointInsidePoly(first, second[i]), true, i));
        }
    }

    public static List<Vector3> IntersectPolygons(List<Vector3> first, List<Vector3> second)
    {
        List<Vector3> res = new List<Vector3>();
        List<IntersectionPoint> firstList;
        List<IntersectionPoint> secondList;
        CreateInterPoints(first, second, out firstList, out secondList);
        AddIntersectionPoints(first, second, firstList, secondList);
        WriteIndexes(firstList);
        WriteIndexes(secondList);
        List<IntersectionPoint> currentList = firstList;
        int currentIndex = 0;
        bool end = false;
        while (!end)
        {
            if (currentList[currentIndex].insideOther)
            {
                res.Add(currentList[currentIndex].position);
                currentList[currentIndex].dirty = true;
                if (!currentList[currentIndex + 1].insideOther)
                {
                    var prevlist = currentList;
                    currentList = currentList == firstList ? secondList : firstList;
                    currentIndex = currentList.FindIndex((el) => { return el == prevlist[currentIndex]; });
                    res.Add(currentList[currentIndex].position);
                    currentList[currentIndex].dirty = true;
                }
            }
            currentIndex++;

            if (currentIndex >= currentList.Count - 1 ||
                currentList[currentIndex].dirty)
                break;
        }
        ClockwiseSort sort = new ClockwiseSort();
        sort.SortClockwise(res);
        return res;
    }

    private static void AddIntersectionPoints(
        List<Vector3> first,
        List<Vector3> second,
        List<IntersectionPoint> firstIntersections,
        List<IntersectionPoint> secondIntersections)
    {
        for (int i = 0; i < first.Count - 1; i++)
        {
            var a1 = first[i];
            var a2 = first[i + 1];
            for (int j = 0; j < second.Count - 1; j++)
            {
                var b1 = second[j];
                var b2 = second[j + 1];
                var point = LineIntersect(a1, a2, b1, b2);
                if (point.result > 0)
                {
                    var ind = firstIntersections.FindIndex((el) => { return el.originalListPos == i; });
                    ind++;
                    while (!firstIntersections[ind].stable &&
                        Vector3.Magnitude(firstIntersections[ind].position - a1) > Vector3.Magnitude(point.point - a1))
                        ind++;

                    var ind2 = firstIntersections.FindIndex((el) => { return el.originalListPos == i; });
                    ind2++;
                    while (!secondIntersections[ind2].stable &&
                        Vector3.Magnitude(secondIntersections[ind].position - b1) > Vector3.Magnitude(point.point - b1))
                        ind2++;

                    IntersectionPoint interPoint = new IntersectionPoint(point.point, true, false, -1);
                    firstIntersections.Insert(ind, interPoint);
                    secondIntersections.Insert(ind2, interPoint);
                }
            }
        }
    }

    private static void WriteIndexes(List<IntersectionPoint> points)
    {
        for (int i = 0; i < points.Count; i++)
            points[i].pointPos = i;
    }

    private static Intersect LineIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        var res = new Intersect();
        res.result = -1;
        res.point = new Vector3();

        float d = (a1.x - a2.x) * (b2.y - b1.y) - (a1.y - a2.y) * (b2.x - b1.x);
        float da = (a1.x - b1.x) * (b2.y - b1.y) - (a1.y - b1.y) * (b2.x - b1.x);
        float db = (a1.x - a2.x) * (a1.y - b1.y) - (a1.y - a2.y) * (a1.x - b1.x);

        if (System.Math.Abs(d) < 0.000001)
        {
            res.result = 0;
        }
        else
        {
            float ta = da / d;
            float tb = db / d;
            if ((0 <= ta) && (ta <= 1) && (0 <= tb) && (tb <= 1))
            {
                res.result = 1;
                res.point.x = a1.x + ta * (a2.x - a1.x);
                res.point.y = a1.y + ta * (a2.y - a1.y);
            }
        }
        return res;
    }

    private static bool IsPointInsidePoly(List<Vector3> poly, Vector3 point)
    {
        bool c = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if ((((poly[i].y <= point.y) && (point.y < poly[j].y)) || ((poly[j].y <= point.y) && (point.y < poly[i].y))) &&
              (point.x > (poly[j].x - poly[i].x) * (point.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                c = !c;
        }
        return c;
    }
}