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

    private DrawingObject CutSlice(List<Vector3> slice)
    {
        var res = new DrawingObject();
        res.points = slice;
        CutManager cut = new CutManager(slice);
        List<Blank> bls = new List<Blank>();
        foreach (var i in bls)
        {
            var bl = new DrawingObject();
            bl.points = new List<Vector3>();
            bl.points.Add(i.v1 * 100);
            bl.points.Add(i.v2 * 100);
            bl.points.Add(i.v3 * 100);
            bl.points.Add(i.v4 * 100);
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
            DrawDrawingObject(slices[curSliceIndex]);
        }
    }

    private void DrawDrawingObject(DrawingObject obj)
    {
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

}