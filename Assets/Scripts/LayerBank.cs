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
    public CuttingUI cuttingUi;
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
        var storableMesh = MeshBuilder.BuildMeshFromLayers(layers);
        var verts = storableMesh.vertices;

        meshFilter.mesh.SetVertices(storableMesh.vertices);
        meshFilter.mesh.SetTriangles(storableMesh.triangles, 0);
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

                if (i < slices.Length - 1)
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
            var centr = LayerHelper.CalcCenterPoint(meshFilter.mesh);
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

    public void OnCutButton()
    {
        if (hats != null && hats.Count > 0)
        {
            cuttingUi.gameObject.SetActive(true);
            cuttingUi.MakeCuts(hats);
        }
    }

    public void OnHideCutButton()
    {
        cuttingUi.gameObject.SetActive(false);
    }

    public void AddNewSphere(List<int> triangle, Vector3 spawnPos)
    {
        var verts = new List<Vector3>(meshFilter.mesh.vertices);
        var triangles = new List<int>(meshFilter.mesh.triangles);
        verts.Add(spawnPos);
        triangles.Add(triangles[triangle[1]]);
        triangles.Add(triangles[triangle[0]]);
        triangles.Add(verts.Count - 1);
        triangles.Add(triangles[triangle[2]]);
        triangles.Add(triangles[triangle[1]]);
        triangles.Add(verts.Count - 1);
        triangles.Add(triangles[triangle[2]]);
        triangles.Add(triangles[triangle[0]]);
        triangles.Add(verts.Count - 1);

        foreach (var i in triangle)
            triangles.RemoveAt(i+1);

        meshFilter.mesh.SetVertices(verts);
        meshFilter.mesh.SetTriangles(triangles, 0);
        meshFilter.mesh.RecalculateNormals();
    }

    /// <summary>
    /// z1 > z2
    /// </summary>
    /// <param name="z1"></param>
    /// <param name="z2"></param>
    /// <returns></returns>
    public List<Vector3> Intersect2Layers(float z1, float z2)
    {
        List<Vector3> result = null;
        var points = new List<Vector3>() { new Vector3(0, 0, z1), new Vector3(1, 0, z1), new Vector3(0, 1, z1) };
        Mesh hat1;
        MeshSlicer.SliceMeshByPlane(meshFilter.mesh, points[0], points[1], points[2], out hat1);
        points = new List<Vector3>() { new Vector3(0, 0, z2), new Vector3(1, 0, z2), new Vector3(0, 1, z2) };
        Mesh hat2;
        MeshSlicer.SliceMeshByPlane(meshFilter.mesh, points[0], points[1], points[2], out hat2);
        var firstList = new List<Vector3>(hat1.vertices);
        firstList.RemoveAt(firstList.Count - 1);
        var secondList = new List<Vector3>(hat2.vertices);
        secondList.RemoveAt(secondList.Count - 1);

        return PolygonIntersector.IntersectPolygons(firstList, secondList);
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