using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuttingUI : DrawingUI
{
    private void Start()
    {
        //List<Vector3> list = new List<Vector3>();
        //list.Add(new Vector3(2, 2));
        //list.Add(new Vector3(1, 4));
        //list.Add(new Vector3(2, 5));
        //list.Add(new Vector3(5, 4));
        //list.Add(new Vector3(4, 2));

        //CutManager cut = new CutManager(list);
        //List<Blank> bls = new List<Blank>();
        //Debug.Log(cut.MakeCutting(out bls));

        //var firstSlice = CutSlice(list);
        //slices.Add(firstSlice);
        //curSliceIndex = 0;
        //DrawSlice();

    }

    private bool VEquals(Vector3 v1, Vector3 v2)
    {
        return Mathf.Round(v1.x) == Mathf.Round(v2.x) && Mathf.Round(v1.y) == Mathf.Round(v2.y) && Mathf.Round(v1.z) == Mathf.Round(v2.z);
    }

    private DrawingObject CutSlice(List<Vector3> slice)
    {
        var res = new DrawingObject();
        List<Vector3> slice1 = new List<Vector3>();
        slice1.Add(slice[0]);
        res.points = slice;
        for (int i = 1; i < slice.Count; i++)
        {
            if (VEquals(slice[i - 1], slice[i]))
                continue;
            slice1.Add(slice[i]);
        }
        CutManager cut = new CutManager(slice1);
        List<Blank> bls = new List<Blank>();
        List<Vector3> poly = new List<Vector3>();
        int sum = cut.MakeCutting(out bls, out poly);
        res.points = poly;
        Debug.Log("Sum: " + sum);
        foreach (var i in bls)
        {
            var bl = new DrawingObject();
            bl.points = new List<Vector3>();
            bl.points.Add(i.v1);
            bl.points.Add(i.v2);
            bl.points.Add(i.v3);
            bl.points.Add(i.v4);
            res.innerObjects.Add(bl);
        }

        return res;
    }

    public class DrawingObject
    {
        public List<Vector3> points = new List<Vector3>();
        public List<DrawingObject> innerObjects = new List<DrawingObject>();
    }

    private int curSliceIndex = 0;

    public List<DrawingObject> slices = new List<DrawingObject>();

    private void DrawSlice()
    {
        if (curSliceIndex >= 0 && curSliceIndex < slices.Count)
        {
            DropUI();
            DrawDrawingObject(slices[curSliceIndex], true);
        }
    }

    private void DrawDrawingObject(DrawingObject obj, bool onCenter = false)
    {
        if (onCenter)
        {
            var centerPoint = LayerHelper.CalcCenterPoint(obj.points.ToArray());
            var canvCenter = transform.position;
            var vec = canvCenter - centerPoint;
            LayerHelper.MoveListByVector(obj.points, vec);
            foreach (var i in obj.innerObjects)
                LayerHelper.MoveListByVector(i.points, vec);
        }
        DrawLayer(obj.points, 1.0f, false, false);
        foreach (var i in obj.innerObjects)
            DrawDrawingObject(i);
    }

    public void MakeCuts(List<List<Vector3>> slabs)
    {
        foreach (var sl in slabs)
        {
            Vector3 offset = Vector3.zero;
            foreach (var j in sl)
            {
                if (j.x < offset.x)
                    offset.x = j.x;
                if (j.y < offset.y)
                    offset.y = j.y;
            }
            for (int j = 0; j < sl.Count; j++)
                sl[j] -= offset;
            var cut = CutSlice(sl);
            slices.Add(cut);
        }

        DrawSlice();
    }

    public void MakeCuts(List<Mesh> meshes)
    {
        List<List<Vector3>> slabs = new List<List<Vector3>>();
        foreach (var i in meshes)
        {
            var nlist = new List<Vector3>(i.vertices);
            if (nlist.Count < 4)
                continue;
            nlist.RemoveAt(nlist.Count - 1);
            slabs.Add(nlist);
        }
        MakeCuts(slabs);
    }

    public void OnNextButton()
    {
        curSliceIndex++;
        if (curSliceIndex >= slices.Count)
            curSliceIndex = slices.Count - 1;
        DrawSlice();
    }

    public void OnPreviousButton()
    {
        curSliceIndex--;
        if (curSliceIndex < 0)
            curSliceIndex = 0;
        DrawSlice();
    }

}