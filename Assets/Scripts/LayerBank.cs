using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public enum LayerBankState
{
    empty,
    drawMainMesh,
    drawSlices,
    allSlices
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
    private List<GameObject> slicesAll;
    private Vector3 lastSliceVector;

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
        sliceUpperBound.gameObject.SetActive(i != slices.Length - 1);
    }

    public void BuildMesh()
    {
        #region Layers Preparations
        {
            #region Generate Additional Points
            int maxInd = 0;
            for (int i = 0; i < layers.Count; i++)
                if (layers[maxInd].Count < layers[i].Count)
                    maxInd = i;

            //for (int i = maxInd; i < layers.Count - 1; i++)
            //{
            //    //LayerHelper.BalanceLayers(layers[i], layers[i + 1]);
            //    LayerHelper.BalanceLayersClockwise(layers[i], layers[i + 1]);
            //}
            //for (int i = maxInd; i > 0; i--)
            //{
            //    //LayerHelper.BalanceLayers(layers[i - 1], layers[i]);
            //    LayerHelper.BalanceLayersClockwise(layers[i - 1], layers[i]);
            //}
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
        #region Bounds - legacy

        //#region Down Bound
        //List<Vector3> downVert = layers[0];
        //List<int> downInd = new List<int>();
        //for (int i = 1; i < layers[0].Count - 1; i++)
        //{
        //    downInd.Add(0);
        //    downInd.Add(i);
        //    downInd.Add(i + 1);

        //    downInd.Add(i + 1);
        //    downInd.Add(i);
        //    downInd.Add(0);
        //}
        //downBound.mesh.SetVertices(downVert);
        //downBound.mesh.SetTriangles(downInd, 0);
        //downBound.GetComponent<MeshCollider>().sharedMesh = downBound.mesh;
        //#endregion

        //#region Upper Bound
        //List<Vector3> up = layers[layers.Count - 1];
        //List<int> upInd = new List<int>();
        //for (int i = 1; i < layers[0].Count - 1; i++)
        //{
        //    upInd.Add(0);
        //    upInd.Add(i + 1);
        //    upInd.Add(i);

        //    upInd.Add(0);
        //    upInd.Add(i);
        //    upInd.Add(i + 1);
        //}
        //upperBound.mesh.SetVertices(up);
        //upperBound.mesh.SetTriangles(upInd, 0);
        //upperBound.GetComponent<MeshCollider>().sharedMesh = upperBound.mesh;
        //#endregion

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
        var cp = CalcCenterPoint(layers[0].ToArray());
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void ShowAllSlices()
    {
        if (state == LayerBankState.allSlices)
        {
            foreach (var i in slicesAll)
                Destroy(i);
            slicesAll = new List<GameObject>();
            state = LayerBankState.drawSlices;
            SetSlice(0);
            ToggleSliceMeshComponent(true);
            return;
        }
        List<Mesh> meshes = new List<Mesh>();
        slicesAll = new List<GameObject>();
        if (state == LayerBankState.drawSlices)
        {
            state = LayerBankState.allSlices;
            for (int i = 0; i < slices.Length; i++)
            {
                var verts = new List<Vector3>(slices[i].vertices);
                var triangles = new List<int>(slices[i].triangles).ToArray();
                for (int j = 0; j < verts.Count; j++)
                {
                    verts[j] += lastSliceVector * 15.0f * i;
                }
                var mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetTriangles(triangles, 0);
                var newCenter = Instantiate(sliceMeshFilter.gameObject, sliceMeshFilter.transform.parent).GetComponent<MeshFilter>();
                newCenter.gameObject.SetActive(true);
                newCenter.mesh = mesh;
                slicesAll.Add(newCenter.gameObject);
                if (i > 0)
                {
                    var newDown = Instantiate(sliceDownBound, sliceDownBound.transform.parent).GetComponent<MeshFilter>();
                    newDown.gameObject.SetActive(true);
                    Mesh downmesh = new Mesh();
                    var downVerts = new List<Vector3>(hats[i].vertices);
                    for (int j = 0; j < downVerts.Count; j++)
                    {
                        downVerts[j] += lastSliceVector * 15.0f * i;
                    }
                    newDown.mesh.SetVertices(downVerts);
                    newDown.mesh.SetTriangles(hats[i].triangles, 0);
                    slicesAll.Add(newDown.gameObject);
                }

                if (i < slices.Length-1)
                {
                    var newDown = Instantiate(sliceUpperBound, sliceUpperBound.transform.parent).GetComponent<MeshFilter>();
                    newDown.gameObject.SetActive(true);
                    Mesh downmesh = new Mesh();
                    var downVerts = new List<Vector3>(hats[i + 1].vertices);
                    for (int j = 0; j < downVerts.Count; j++)
                    {
                        downVerts[j] += lastSliceVector * 15.0f * i;
                    }
                    newDown.mesh.SetVertices(downVerts);
                    newDown.mesh.SetTriangles(hats[i + 1].triangles, 0);
                    slicesAll.Add(newDown.gameObject);
                }
            }

            ToggleSliceMeshComponent(false);
        }
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
            if (Vector3.Magnitude(plane.normal + (centr - points[0])) < Vector3.Magnitude(centr - points[0]))
            {
                var temp = points[0];
                points[0] = points[1];
                points[1] = temp;
            }
            plane = new Plane(points[0], points[1], points[2]);
            float size = 0;
            foreach (var i in meshFilter.mesh.vertices)
            {
                size = Mathf.Max(size, plane.GetDistanceToPoint(i));
            }

            slices = MeshSlicer.SliceInPeaces(slicesNum, meshFilter.mesh, size, points, out hats);
            lastSliceVector = plane.normal.normalized;
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

public class LayerHelper
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

    private static float DistanceAsVec2(Vector3 a, Vector3 b)
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
}