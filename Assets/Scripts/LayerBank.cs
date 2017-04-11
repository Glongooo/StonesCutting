using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public enum LayerBankState
{
    empty,
    drawMainMesh,
    drawSlices
}

public class LayerBank : MonoBehaviour
{
    [HideInInspector]
    public List<List<Vector3>> layers = new List<List<Vector3>>();
    [HideInInspector]
    public List<float> layersHeight = new List<float>();
    [Header("Main mesh part")]
    public MeshFilter meshFilter;
    public MeshFilter upperBound;
    public MeshFilter downBound;
    [Header("SlicesMeshPart")]
    public MeshFilter sliceMeshFilter;
    public MeshFilter sliceUpperBound;
    public MeshFilter sliceDownBound;

    public MeshControlSphere spherePrefab;
    public Transform dragSpheresParent;
    [Header("Materials")]
    public Material transparentWireMaterial;
    public Material solidWireMaterial;
    [Header("Input Fields")]
    public InputField slicesCount;

    [HideInInspector]
    public LayerBankState state = LayerBankState.empty;
    private Dictionary<IDragableMesh, Dictionary<MeshFilter, int>> sphereToVertex = new Dictionary<IDragableMesh, Dictionary<MeshFilter, int>>();
    private Dictionary<MeshFilter, int[]> meshToBackup = new Dictionary<MeshFilter, int[]>();

    private Mesh[] slices;
    private List<Mesh> hats;
    private int curSliceNum = 0;
    private bool isWired = false;

    public void AddNewLayer(List<Vector3> obj, float height)
    {
        layers.Add(obj);
        layersHeight.Add(height);
    }

    public void ReplaceLayer(List<Vector3> obj, float height, int index)
    {
        if (index < 0 || index >= layers.Count)
        {
            Debug.LogError("Index out of bounds");
            return;
        }
        layers[index] = obj;
        DropMesh();
        layersHeight[index] = height;
    }

    private float DistanceAsVec2(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
    }

    public void Update()
    {
        if (state == LayerBankState.drawSlices)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
                ShowNextSlice(1);
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ShowNextSlice(-1);
        }
    }

    private void ShowNextSlice(int delta)
    {
        if (curSliceNum + delta >= 0 &&
            curSliceNum + delta < slices.Length)
        {
            curSliceNum += delta;
            SetSlice(curSliceNum);
        }

    }

    private void SetSlice(int i)
    {
        var w = isWired;
        if (w)
            SetCenterSolidView();
        sliceMeshFilter.mesh = slices[i];
        
        sliceDownBound.mesh = hats[i];
        sliceUpperBound.mesh = hats[i + 1];
        if (w)
            SetCenterWiredView();
        sliceDownBound.gameObject.SetActive(i > 0);
        sliceUpperBound.gameObject.SetActive(i!=slices.Length-1);
    }

    public void BuildMesh()
    {
        #region Layers Preparations
        {
            #region Generate Additional Points
            int maxCount = 0;
            int maxInd = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                maxCount = maxCount < layers[i].Count ? layers[i].Count : maxCount;
                maxInd = maxCount < layers[i].Count ? i : maxInd;
            }
            for (int i = maxInd; i < layers.Count - 1; i++)
            {
                int nextInd = i + 1;
                HashSet<int> usedVertices = new HashSet<int>();
                while (layers[i].Count > layers[i + 1].Count)
                {
                    float maxDst = float.MaxValue;
                    int maxNum = 0;
                    int maxMinNum = 0;
                    for (int t = 0; t < layers[i].Count; t++)
                    {
                        if (usedVertices.Contains(t)) continue;
                        float minDst = float.MaxValue;
                        int tempMinNum = 0;
                        for (int u = 0; u < layers[nextInd].Count; u++)
                        {
                            minDst = DistanceAsVec2(layers[i][t], layers[nextInd][u]) < minDst ? DistanceAsVec2(layers[i][t], layers[nextInd][u]) : minDst;
                            tempMinNum = DistanceAsVec2(layers[i][t], layers[nextInd][u]) < minDst ? u : tempMinNum;
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
                        int leftNeighbour = maxMinNum == 0 ? layers[nextInd].Count - 1 : maxMinNum - 1;
                        int righNeighBour = maxMinNum == layers[nextInd].Count - 1 ? 0 : maxMinNum + 1;
                        if (DistanceAsVec2(layers[i][maxNum], layers[nextInd][leftNeighbour]) < DistanceAsVec2(layers[i][maxNum], layers[nextInd][righNeighBour]))
                        {
                            var v = (layers[nextInd][leftNeighbour] + layers[nextInd][maxMinNum]) / 2;
                            layers[nextInd].Insert(leftNeighbour, v);
                        }
                        else
                        {
                            var v = (layers[nextInd][righNeighBour] + layers[nextInd][maxMinNum]) / 2;
                            layers[nextInd].Insert(righNeighBour, v);
                        }

                    }

                }
            }

            for (int i = maxInd; i > 0; i--)
            {
                int nextInd = i - 1;
                HashSet<int> usedVertices = new HashSet<int>();
                while (layers[i].Count > layers[i + 1].Count)
                {
                    float maxDst = float.MaxValue;
                    int maxNum = 0;
                    int maxMinNum = 0;
                    for (int t = 0; t < layers[i].Count; t++)
                    {
                        if (usedVertices.Contains(t)) continue;
                        float minDst = float.MaxValue;
                        int tempMinNum = 0;
                        for (int u = 0; u < layers[nextInd].Count; u++)
                        {
                            minDst = DistanceAsVec2(layers[i][t], layers[nextInd][u]) < minDst ? DistanceAsVec2(layers[i][t], layers[nextInd][u]) : minDst;
                            tempMinNum = DistanceAsVec2(layers[i][t], layers[nextInd][u]) < minDst ? u : tempMinNum;
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
                        int leftNeighbour = maxMinNum == 0 ? layers[nextInd].Count - 1 : maxMinNum - 1;
                        int righNeighBour = maxMinNum == layers[nextInd].Count - 1 ? 0 : maxMinNum + 1;
                        if (DistanceAsVec2(layers[i][maxNum], layers[nextInd][leftNeighbour]) < DistanceAsVec2(layers[i][maxNum], layers[nextInd][righNeighBour]))
                        {
                            var v = (layers[nextInd][leftNeighbour] + layers[nextInd][maxMinNum]) / 2;
                            layers[nextInd].Insert(leftNeighbour, v);
                        }
                        else
                        {
                            var v = (layers[nextInd][righNeighBour] + layers[nextInd][maxMinNum]) / 2;
                            layers[nextInd].Insert(righNeighBour, v);
                        }

                    }

                }
            }




            #endregion

            //#region ClockWiseSorting
            //foreach (var i in layers)
            //    SortClockwise(i);
            //#endregion

            #region rotatingLayers
            var startPos = layers[0][0];
            for (int i = 1; i < layers.Count; i++)
            {
                float minDst = float.MaxValue;
                int minInd = 0;
                for (int j = 0; j < layers[i].Count; j++)
                {
                    minDst = minDst > DistanceAsVec2(layers[i][j], startPos) ? DistanceAsVec2(layers[i][j], startPos) : minDst;
                    minInd = minDst > DistanceAsVec2(layers[i][j], startPos) ? j : minInd;
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
        //for (int i = 0; i < layers.Count; i++)
        //{
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

        #region Down Bound
        List<Vector3> downVert = layers[0];
        List<int> downInd = new List<int>();
        for (int i = 1; i < layers[0].Count - 1; i++)
        {
            downInd.Add(0);
            downInd.Add(i);
            downInd.Add(i + 1);

            downInd.Add(i + 1);
            downInd.Add(i);
            downInd.Add(0);
        }
        downBound.mesh.SetVertices(downVert);
        downBound.mesh.SetTriangles(downInd, 0);
        downBound.GetComponent<MeshCollider>().sharedMesh = downBound.mesh;
        #endregion

        #region Upper Bound
        List<Vector3> up = layers[layers.Count - 1];
        List<int> upInd = new List<int>();
        for (int i = 1; i < layers[0].Count - 1; i++)
        {
            upInd.Add(0);
            upInd.Add(i + 1);
            upInd.Add(i);

            upInd.Add(0);
            upInd.Add(i);
            upInd.Add(i + 1);
        }
        upperBound.mesh.SetVertices(up);
        upperBound.mesh.SetTriangles(upInd, 0);
        upperBound.GetComponent<MeshCollider>().sharedMesh = upperBound.mesh;
        #endregion

        #region centerGeneration
        //for (int i = 0; i < layers.Count - 1; i++)
        //{
        //    for (int j = 0; j < layers[i].Count; j++)
        //    {
        //        vertices.Add(layers[i][j]);
        //        // if (i < layers.Count - 1)
        //        // { }
        //        bool[] busy = new bool[layers[i + 1].Count];
        //        double minLength = 100500;
        //        int indMin = -1;
        //        for (int k = 0; k < layers[i + 1].Count; k++)
        //        {
        //            double dist = Vector3.Distance(layers[i][j], layers[i + 1][k]);
        //            if (dist < minLength)
        //            {
        //                minLength = dist;
        //                indMin = k;
        //            }
        //        }
        //        if (busy[indMin])
        //        {
        //            Vector3 vec1 = new Vector3();
        //            Vector3 vec2 = new Vector3();
        //            int ind1;
        //            int ind2;
        //            try
        //            {
        //                ind1 = indMin - 1;
        //                vec1 = layers[i + 1][ind1];
        //            }
        //            catch (System.Exception e)
        //            {
        //                ind1 = layers.Count - 1;
        //                vec1 = layers[i + 1][ind1];
        //            }
        //            try
        //            {
        //                ind2 = indMin + 1;
        //                vec2 = layers[i + 1][ind2];
        //            }
        //            catch (System.Exception e)
        //            {
        //                ind2 = 0;
        //                vec2 = layers[i + 1][ind2];
        //            }
        //            double dist1 = Vector3.Distance(layers[i][j], vec1);
        //            double dist2 = Vector3.Distance(layers[i][j], vec2);
        //            if (dist1 < dist2)
        //                indMin = ind1;
        //            else
        //                indMin = ind2;
        //        }
        //        busy[indMin] = true;

        //        bigList.Add(layers[i][j]);
        //        if (j >= layers[i].Count - 1)
        //            bigList.Add(layers[i][0]);
        //        else
        //            bigList.Add(layers[i][j + 1]);
        //        bigList.Add(layers[i + 1][indMin]);

        //    }
        //}

        #endregion

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

            for (int j = 0; j < layers[i].Count - 1; j++)
            {

                Tr.Add(nums[i + 1][j]);
                Tr.Add(nums[i][j]);
                Tr.Add(nums[i + 1][j + 1]);


                Tr.Add(nums[i + 1][j + 1]);
                Tr.Add(nums[i][j]);
                Tr.Add(nums[i][j + 1]);
            }

            Tr.Add(nums[i + 1][layers[i].Count - 1]);
            Tr.Add(nums[i][layers[i].Count - 1]);
            Tr.Add(nums[i + 1][0]);

            Tr.Add(nums[i + 1][0]);
            Tr.Add(nums[i][layers[i].Count - 1]);
            Tr.Add(nums[i][0]);
        }


        #region hats for main mesh
        var cp = CalcCenterPoint(layers[0].ToArray());
        verts.Add(cp);
        for(int i = 0; i < nums[0].Count - 1; i++)
        {
            Tr.Add(verts.Count - 1);
            Tr.Add(nums[0][i]);
            Tr.Add(nums[0][i + 1]);
        }
        Tr.Add(verts.Count - 1);
        Tr.Add(nums[0][nums[0].Count - 1]);
        Tr.Add(nums[0][0]);

        cp = CalcCenterPoint(layers[layers.Count - 1].ToArray());
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

        meshFilter.mesh.SetVertices(verts);
        meshFilter.mesh.SetTriangles(Tr, 0);
        meshFilter.mesh.RecalculateNormals();

        meshFilter.GetComponent<MeshCollider>().sharedMesh = meshFilter.mesh;

        for (int i = 0; i < verts.Count; i++)
        {
            var go = Instantiate(spherePrefab, dragSpheresParent);
            go.OnPositionChange += OnSpherePositionChange;
            go.transform.localPosition = verts[i];
            sphereToVertex.Add(go, new Dictionary<MeshFilter, int>() { { meshFilter, i } });
            if (i < layers[0].Count)
                sphereToVertex[go].Add(downBound, i);
            if (i >= verts.Count - layers[layers.Count - 1].Count)
                sphereToVertex[go].Add(upperBound, i - verts.Count + layers[layers.Count - 1].Count);
        }

        ToggleMainMeshComponents(true);

        state = LayerBankState.drawMainMesh;
    }

    private void OnSpherePositionChange(IDragableMesh sphere)
    {
        if (sphereToVertex.ContainsKey(sphere))
        {
            var dic = sphereToVertex[sphere];
            foreach (var i in dic)
            {
                var vts = i.Key.mesh.vertices;
                vts[i.Value] = (sphere as MeshControlSphere).transform.localPosition;
                i.Key.mesh.vertices = vts;
            }
        }
    }

    public void DropMesh()
    {
        state = LayerBankState.empty;
        //layers.Clear();
        //layersHeight.Clear();
        for (int i = 0; i < dragSpheresParent.childCount; i++)
            Destroy(dragSpheresParent.GetChild(i).gameObject);
        ToggleMainMeshComponents(false);
        ToggleSliceMeshComponent(false);
    }

    public void SetCenterWiredView()
    {
        int[] backup;
        if (!meshToBackup.ContainsKey(meshFilter))
        {
            backup = AddRevertTriangles(meshFilter);
            meshToBackup.Add(meshFilter, backup);
            meshFilter.GetComponent<MeshRenderer>().material = transparentWireMaterial;
        }

        if (!meshToBackup.ContainsKey(sliceMeshFilter))
        {
            backup = AddRevertTriangles(sliceMeshFilter);
            meshToBackup.Add(sliceMeshFilter, backup);
            sliceMeshFilter.GetComponent<MeshRenderer>().material = transparentWireMaterial;
        }
        isWired = true;
    }

    private int[] AddRevertTriangles(MeshFilter m)
    {
        var backup = m.mesh.GetTriangles(0);
        var triangles = new List<int>(backup);
        for (int i = 0; i < backup.Length; i += 3)
        {
            triangles.Add(backup[i + 1]);
            triangles.Add(backup[i]);
            triangles.Add(backup[i + 2]);
        }

        m.mesh.SetTriangles(triangles, 0);
        return backup;
    }

    public void SetCenterSolidView()
    {
        isWired = false;
        if (meshToBackup.ContainsKey(meshFilter))
        {
            meshFilter.mesh.SetTriangles(meshToBackup[meshFilter], 0);
            meshFilter.GetComponent<MeshRenderer>().material = solidWireMaterial;
            meshToBackup.Remove(meshFilter);
        }

        if (meshToBackup.ContainsKey(sliceMeshFilter))
        {
            sliceMeshFilter.mesh.SetTriangles(meshToBackup[sliceMeshFilter], 0);
            sliceMeshFilter.GetComponent<MeshRenderer>().material = solidWireMaterial;
            meshToBackup.Remove(sliceMeshFilter);
        }
    }

    public void OnSliceButtonClick()
    {
        if (state == LayerBankState.empty) return;
        if (state == LayerBankState.drawSlices)
        {
            ToggleSliceMeshComponent(false);
            ToggleMainMeshComponents(true);
            state = LayerBankState.drawMainMesh;
            return;
        }
        if (state == LayerBankState.drawMainMesh)
        {
            hats = new List<Mesh>();
            state = LayerBankState.drawSlices;
            ToggleMainMeshComponents(false);
            ToggleSliceMeshComponent(true);
            var zmin = layers[0][0].z;
            var zmax = layers[layers.Count - 1][0].z;
            int slicesNum;
            if (!int.TryParse(slicesCount.text, out slicesNum))
                slicesNum = 5;
            List<Vector3> points;
            if (Drag_n_drop.lastHitPlane != null)
                points = Drag_n_drop.lastHitPlane;
            else
            {
                points = new List<Vector3>() { new Vector3(0, 0, zmin), new Vector3(1, 0, zmin), new Vector3(0, 1, zmin) };
            }
            var centr = CalcCenterPoint(meshFilter.mesh);
            var plane = new Plane(points[0], points[1], points[2]);
            if( Vector3.Magnitude(plane.normal + (centr - points[0])) < Vector3.Magnitude(centr - points[0]))
            {
                var temp = points[0];
                points[0] = points[1];
                points[1] = temp;
            }
            plane = new Plane(points[0], points[1], points[2]);
           float size = 0;
            foreach(var i in meshFilter.mesh.vertices)
            {
                size = Mathf.Max( size, plane.GetDistanceToPoint(i));
            }


            slices = MeshSlicer.SliceInPeaces(slicesNum, meshFilter.mesh, size, points, out hats);
            hats.Add(upperBound.mesh);
            hats.Insert(0, downBound.mesh);
            SetSlice(0);
            //StartCoroutine(DrawSlices(slices, hats));
        }
    }

    private Vector3 CalcCenterPoint(Mesh mesh)
    {
        var vrts = mesh.vertices;
        return CalcCenterPoint(vrts);

    }

    private Vector3 CalcCenterPoint(Vector3[] vrts)
    {
        Vector3 result = Vector3.zero; ;
        foreach (var i in vrts)
            result += i;
        result /= vrts.Length;
        return result;
    }

    private void ToggleMainMeshComponents(bool value)
    {
        meshFilter.gameObject.SetActive(value);
        upperBound.gameObject.SetActive(value);
        downBound.gameObject.SetActive(value);
        dragSpheresParent.gameObject.SetActive(value);
    }

    private void ToggleSliceMeshComponent(bool value)
    {
        sliceDownBound.gameObject.SetActive(value);
        sliceUpperBound.gameObject.SetActive(value);
        sliceMeshFilter.gameObject.SetActive(value);
    }

    private IEnumerator DrawSlices(Mesh[] slices, List<Mesh> hats)
    {
        while (true)
        {
            for (int i = 0; i < slices.Length; i++)
            {
                meshFilter.mesh = slices[i];
                downBound.mesh = hats[i];
                upperBound.mesh = hats[i + 1];
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

}