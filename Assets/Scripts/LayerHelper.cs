using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Helpers
{
    public static class LayerHelper
    {

        public static void BalanceLayers(List<Vector3> smallLayer, List<Vector3> bigLayer)
        {
            if (smallLayer.Count > bigLayer.Count)
            {
                var tmp = smallLayer;
                smallLayer = bigLayer;
                bigLayer = tmp;
            }

            HashSet<int> usedVertices = new HashSet<int>();
            while (bigLayer.Count > smallLayer.Count)
            {
                float maxDst = 0;
                int maxNum = 0;
                int maxMinNum = 0;
                for (int t = 0; t < bigLayer.Count; t++)
                {
                    if (usedVertices.Contains(t)) continue;
                    float minDst = float.MaxValue;
                    int tempMinNum = 0;
                    for (int u = 0; u < smallLayer.Count; u++)
                    {
                        minDst = DistanceAsVec2(bigLayer[t], smallLayer[u]) < minDst ? DistanceAsVec2(bigLayer[t], smallLayer[u]) : minDst;
                        tempMinNum = DistanceAsVec2(bigLayer[t], smallLayer[u]) < minDst ? u : tempMinNum;
                    }
                    if (maxDst < minDst)
                    {
                        maxDst = minDst;
                        maxNum = t;
                        maxMinNum = tempMinNum;
                    }
                }
                usedVertices.Add(maxNum);
                {
                    int leftNeighbour = maxMinNum == 0 ? smallLayer.Count - 1 : maxMinNum - 1;
                    int righNeighBour = maxMinNum == smallLayer.Count - 1 ? 0 : maxMinNum + 1;
                    if (DistanceAsVec2(bigLayer[maxNum], smallLayer[leftNeighbour]) < DistanceAsVec2(bigLayer[maxNum], bigLayer[righNeighBour]))
                    {
                        var v = (smallLayer[leftNeighbour] + smallLayer[maxMinNum]) / 2;
                        smallLayer.Insert(leftNeighbour, v);
                    }
                    else
                    {
                        var v = (smallLayer[righNeighBour] + smallLayer[maxMinNum]) / 2;
                        smallLayer.Insert(righNeighBour, v);
                    }

                }

            }
        }

        public static void BalanceLayersClockwise(List<Vector3> smallLayer, List<Vector3> bigLayer)
        {
            if (smallLayer.Count > bigLayer.Count)
            {
                var tmp = smallLayer;
                smallLayer = bigLayer;
                bigLayer = tmp;
            }

            HashSet<int> usedVertices = new HashSet<int>();
            while (bigLayer.Count > smallLayer.Count)
            {
                var bigLayerAtans = CalculateAtans(bigLayer);
                var smallLayerAtans = CalculateAtans(smallLayer);

                float maxDst = 0;
                int maxNum = 0;
                int maxMinNum = 0;
                for (int t = 0; t < bigLayer.Count; t++)
                {
                    if (usedVertices.Contains(t)) continue;
                    float minDst = float.MaxValue;
                    int tempMinNum = 0;
                    for (int u = 0; u < smallLayer.Count; u++)
                    {
                        var value = Mathf.Abs(bigLayerAtans[t] - smallLayerAtans[u]);
                        if (value < minDst)
                        {
                            minDst = value;
                            tempMinNum = u;
                        }
                    }
                    if (maxDst < minDst)
                    {
                        maxDst = minDst;
                        maxNum = t;
                        maxMinNum = tempMinNum;
                    }
                }
                usedVertices.Add(maxNum);
                {
                    int leftNeighbour = maxMinNum == 0 ? smallLayer.Count - 1 : maxMinNum - 1;
                    int righNeighBour = maxMinNum == smallLayer.Count - 1 ? 0 : maxMinNum + 1;
                    if (DistanceAsVec2(bigLayer[maxNum], smallLayer[leftNeighbour]) < DistanceAsVec2(bigLayer[maxNum], bigLayer[righNeighBour]))
                    {
                        var v = (smallLayer[leftNeighbour] + smallLayer[maxMinNum]) / 2;
                        smallLayer.Insert(leftNeighbour, v);
                    }
                    else
                    {
                        var v = (smallLayer[righNeighBour] + smallLayer[maxMinNum]) / 2;
                        smallLayer.Insert(righNeighBour, v);
                    }

                }

            }
        }

        public static void EqualizeLayers()
        {

        }

        public static float DistanceAsVec2(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
        }

        private static List<float> CalculateAtans(List<Vector3> points)
        {
            var centerPoint = Vector3.zero;
            foreach (var i in points)
                centerPoint += i;
            centerPoint /= points.Count;
            return CalculateAtans(points, centerPoint);
        }

        private static List<float> CalculateAtans(List<Vector3> points, Vector3 centerPoint)
        {
            var result = new List<float>();
            for (int i = 0; i < points.Count; i++)
            {
                var atan = Math.Atan2(points[i].y - centerPoint.y, points[i].x - centerPoint.x);
                result.Add((float)atan);
            }
            return result;
        }

        public static List<int> CreateTwoTriangles(int a, int b, int c)
        {
            var result = new List<int>();

            result.Add(a);
            result.Add(b);
            result.Add(c);

            result.Add(b);
            result.Add(a);
            result.Add(c);

            return result;
        }

        public static List<int> CreateTriangle(int a, int b, int c)
        {
            var result = new List<int>();

            result.Add(a);
            result.Add(b);
            result.Add(c);
            return result;
        }

        public static Vector3 CalcCenterPoint(Mesh mesh)
        {
            var vrts = mesh.vertices;
            return CalcCenterPoint(vrts);

        }

        public static Vector3 CalcCenterPoint(Vector3[] vrts)
        {
            Vector3 result = Vector3.zero; ;
            foreach (var i in vrts)
                result += i;
            result /= vrts.Length;
            return result;
        }

        public static void MoveListByVector(List<Vector3> inp, Vector3 vec)
        {
            for (int i = 0; i < inp.Count; i++)
            {
                inp[i] += vec;
            }
        }

    }

    public static class MeshHelper
    {
        public static void TurnOutNormals(Mesh mesh)
        {
            var center = LayerHelper.CalcCenterPoint(mesh);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!SameOriented(vertices[i] - center, normals[i]))
                    normals[i] = -normals[i];
            }
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i + 1]] - vertices[triangles[i]];
                var b = vertices[triangles[i + 2]] - vertices[triangles[i + 1]];
                var triCenter = LayerHelper.CalcCenterPoint(new Vector3[] { vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]] });
                var cross = Vector3.Cross(a, b);
                if (!SameOriented(triCenter - center, cross))
                    RevertTriangleNormal(triangles, i);
            }
            mesh.triangles = triangles;
            mesh.normals = normals;
        }

        private static bool SameOriented(Vector3 a, Vector3 b)
        {
            if (Vector3.Dot(a, b) > 0)
                return true;
            else return false;
        }

        private static void RevertTriangleNormal(int[] triangles, int trNum)
        {
            var tmp = triangles[trNum];
            triangles[trNum] = triangles[trNum + 1];
            triangles[trNum + 1] = tmp;
        }
        
    }
}
