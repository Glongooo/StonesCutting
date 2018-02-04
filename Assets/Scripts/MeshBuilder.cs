using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshBuilder
{
    public static StorableMesh BuildMeshFromLayers(List<List<Vector3>> layers)
    {
        #region Layers Preparations
        {
            #region Generate Additional Points
            int maxInd = 0;
            for (int i = 0; i < layers.Count; i++)
                if (layers[maxInd].Count < layers[i].Count)
                    maxInd = i;
            #endregion

            #region rotatingLayers
            var startPos = layers[0][0];
            for (int i = 1; i < layers.Count; i++)
            {
                float minDst = float.MaxValue;
                int minInd = 0;
                for (int j = 0; j < layers[i].Count; j++)
                {
                    minDst = minDst > LayerHelper.DistanceAsVec2(layers[i][j], startPos) ? LayerHelper.DistanceAsVec2(layers[i][j], startPos) : minDst;
                    minInd = minDst > LayerHelper.DistanceAsVec2(layers[i][j], startPos) ? j : minInd;
                }

                if (minInd > 0)
                {
                    List<Vector3> newList = layers[i].GetRange(minInd, layers[i].Count - minInd);
                    newList.AddRange(layers[i].GetRange(0, minInd + 1));
                }
            }
            #endregion

        }
        #endregion

        List<Vector3> vertices = new List<Vector3>();
        Vector3 averagePoint = Vector3.zero;
        int count = 0;
        foreach (var i in layers)
            foreach (var j in i)
            {
                averagePoint += j;
                count++;
            }
        averagePoint = averagePoint / count;

        for (int i = 0; i < layers.Count; i++)
            for (int j = 0; j < layers[i].Count; j++)
                layers[i][j] -= averagePoint;

        List<int> triangles = new List<int>();
        List<Vector3> bigList = new List<Vector3>();

        List<List<int>> nums = new List<List<int>>();
        List<Vector3> verts = new List<Vector3>();
        for (int i = 0; i < layers.Count; i++)
        {
            List<int> layerInd = new List<int>();
            for (int j = 0; j < layers[i].Count; j++)
            {
                layerInd.Add(verts.Count);
                verts.Add(layers[i][j]);
            }
            nums.Add(layerInd);
        }

        List<int> Tr = new List<int>();
        for (int i = 0; i < layers.Count - 1; i++)
        {
            int u = 0;
            int d = 0;
            while (d < layers[i].Count - 1 ||
                u < layers[i + 1].Count - 1)
            {
                float magUp = float.MaxValue;
                float magDown = float.MaxValue;
                if (d < layers[i].Count - 1)
                    magUp = Vector3.Magnitude(layers[i + 1][u] - layers[i][d + 1]);
                if (u < layers[i + 1].Count - 1)
                    magDown = Vector3.Magnitude(layers[i][d] - layers[i + 1][u + 1]);
                if (magUp < magDown && d < layers[i].Count - 1)
                {
                    Tr.AddRange(LayerHelper.CreateTriangle(nums[i + 1][u], nums[i][d], nums[i][d + 1]));
                    d++;
                }
                else
                {
                    Tr.AddRange(LayerHelper.CreateTriangle(nums[i][d], nums[i + 1][u + 1], nums[i + 1][u]));
                    u++;
                }
            }
            Tr.AddRange(LayerHelper.CreateTriangle(nums[i][0], nums[i + 1][0], nums[i][nums[i].Count - 1]));
            Tr.AddRange(LayerHelper.CreateTriangle(nums[i + 1][nums[i + 1].Count - 1], nums[i + 1][0], nums[i][nums[i].Count - 1]));
        }



        #region hats for main mesh
        var cp = LayerHelper.CalcCenterPoint(layers[0].ToArray());
        verts.Add(cp);
        for (int i = 0; i < nums[0].Count - 1; i++)
        {
            Tr.Add(verts.Count - 1);
            Tr.Add(nums[0][i]);
            Tr.Add(nums[0][i + 1]);
        }
        Tr.Add(verts.Count - 1);
        Tr.Add(nums[0][nums[0].Count - 1]);
        Tr.Add(nums[0][0]);

        cp = LayerHelper.CalcCenterPoint(layers[layers.Count - 1].ToArray());
        verts.Add(cp);
        for (int i = 0; i < nums[nums.Count - 1].Count - 1; i++)
        {
            Tr.Add(verts.Count - 1);
            Tr.Add(nums[nums.Count - 1][i]);
            Tr.Add(nums[nums.Count - 1][i + 1]);
        }
        Tr.Add(verts.Count - 1);
        Tr.Add(nums[nums.Count - 1][nums[nums.Count - 1].Count - 1]);
        Tr.Add(nums[nums.Count - 1][0]);

        #endregion

        return new StorableMesh(verts, Tr, null);

        //meshFilter.mesh.SetVertices(verts);
        //meshFilter.mesh.SetTriangles(Tr, 0);
        //meshFilter.mesh.RecalculateNormals();

        //meshFilter.GetComponent<MeshCollider>().sharedMesh = meshFilter.mesh;

        //for (int i = 0; i < verts.Count; i++)
        //{
        //    var go = Instantiate(spherePrefab, dragSpheresParent);
        //    go.OnPositionChange += OnSpherePositionChange;
        //    go.transform.localPosition = verts[i];
        //    sphereToVertex.Add(go, new Dictionary<MeshFilter, int>() { { meshFilter, i } });
        //    if (i < layers[0].Count)
        //        sphereToVertex[go].Add(downBound, i);
        //    if (i >= verts.Count - layers[layers.Count - 1].Count)
        //        sphereToVertex[go].Add(upperBound, i - verts.Count + layers[layers.Count - 1].Count);
        //}

        //ToggleMainMeshComponents(true);

        //state = LayerBankState.drawMainMesh;

    }
}
