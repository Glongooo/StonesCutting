using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuttingUI : DrawingUI
{
    private void Start()
    {
        List<Vector3> list = new List<Vector3>();
        list.Add(new Vector3(2, 2));
        list.Add(new Vector3(1, 4));
        list.Add(new Vector3(2, 5));
        list.Add(new Vector3(5, 4));
        list.Add(new Vector3(4, 2));

        CutManager cut = new CutManager(list);
        List<Blank> bls = new List<Blank>();

        Debug.Log(cut.MakeCutting(out bls));
        //foreach (var b in bls)
        //{
        //    Debug.Log(b.ToString());
        //}

        var firstSlice = new DrawingObject();
        firstSlice.points = list;
        for (int i = 0; i < firstSlice.points.Count; i++)
        {
            firstSlice.points[i] *= 100;
        }
        foreach (var i in bls)
        {
            var bl = new DrawingObject();
            bl.points = new List<Vector3>();
            bl.points.Add(i.v1 * 100);
            bl.points.Add(i.v2 * 100);
            bl.points.Add(i.v3 * 100);
            bl.points.Add(i.v4 * 100);
            firstSlice.innerObjects.Add(bl);
        }

        slices.Add(firstSlice);
        curSliceIndex = 0;
        DrawSlice();

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
}
