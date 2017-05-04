using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuttingUI : DrawingUI
{
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
        DrawLayer(obj.points, 1.0f, false);
        foreach (var i in obj.innerObjects)
            DrawDrawingObject(i);
    }
}
