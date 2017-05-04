using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class LayerContainerUI : DrawingUI
{
    public static LayerContainerUI Instance { get { return _Instance; } }
    private static LayerContainerUI _Instance = null;

    #region Inspector Fields

    [SerializeField]
    private LayerBank bank;

    [SerializeField]
    private InputField layerHeight;

    [SerializeField]
    int drawingDepth = 2;

    [SerializeField]
    private Toggle showCoords;

    [SerializeField]
    private InputField drawingDepthInput;

    [SerializeField]
    private Transform LayerButtonsParent;

    [SerializeField]
    private Button scrollElementPrefab;

    [SerializeField]
    private Dictionary<Button, int> buttonToIndex = new Dictionary<Button, int>();
    #endregion


    private float curHeight = 0;
    private int curNum = 0;

    private void Awake()
    {
        _Instance = this;
    }

    public void OnDrawingDepthChanged(string val)
    {
        drawingDepth = int.Parse(val);
    }

    public void OnShowValueChanged(bool val)
    {
        foreach (var i in markers)
            i.transform.GetChild(0).gameObject.SetActive(val);
    }

    public void AddMarker(Vector3 coords)
    {
        var newMarker = Instantiate(markerPrefab);
        newMarker.GetComponent<DragableElement>().OnDropEventHandler += OnDropEvent;
        newMarker.transform.parent = gameObject.transform;
        newMarker.transform.position = new Vector3(coords.x, coords.y);
        markers.Add(newMarker);
        SortClockwise(markers);
        //InsertGORoundSortedList(markers, newMarker);
        if (markersParent != null)
            newMarker.transform.parent = markersParent;

        var txt = newMarker.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = "(" + coords.x + "; " + coords.y + ")";
            txt.gameObject.SetActive(showCoords.isOn);
        }

        List<Vector3> list = new List<Vector3>();
        list.Add(new Vector3(0, 3));
        list.Add(new Vector3(2, 2));
        list.Add(new Vector3(3, 0));
        list.Add(new Vector3(0, 0));

        CutManager cut = new CutManager(list);
        List<Blank> bls = new List<Blank>();

        Debug.Log(cut.MakeCutting(out bls));
        foreach (var b in bls)
        {
            Debug.Log(b.ToString());
        }
    }

    //private void InsertGORoundSortedList(List<GameObject> list, GameObject go)
    //{
    //    if (list.Count >= 3)
    //    {
    //        Vector3 centerPoint = Vector3.zero;
    //        foreach (var i in list)
    //            centerPoint += i.transform.localPosition;
    //        centerPoint /= list.Count;
    //        Vector3 tarVec = go.transform.localPosition - centerPoint;
    //        Vector3 nulVec = list[0].transform.localPosition - centerPoint;
    //        float tarAngle = PositiveAngleBetween(nulVec, tarVec);
    //        for (int i = 0; i < list.Count - 1; i++)
    //        {
    //            if (PositiveAngleBetween(nulVec, list[i].transform.localPosition) <= tarAngle &&
    //                PositiveAngleBetween(nulVec, list[i + 1].transform.localPosition) >= tarAngle)
    //            {
    //                list.Insert(i + 1, go);
    //                return;

    //            }

    //        }
    //    }
    //    list.Add(go);
    //}

    private float PositiveAngleBetween(Vector3 from, Vector3 to)
    {
        var curAngle = Vector3.Angle(from, to);
        curAngle += curAngle < 0 ? 360 : 0;
        return curAngle;
    }

    private void OnDropEvent(DragableElement el)
    {
        DrawLines();
    }

    public void OnClick()
    {
        var mp = Input.mousePosition;
        var newPos = Camera.allCameras[0].ScreenToWorldPoint(mp);
        AddMarker(mp);
        var posList = new List<Vector3>();
        foreach (var i in markers)
            posList.Add(i.transform.position);
        //renderer.SetPositions(posList.ToArray());
        DrawLines();
    }

    public void OnAcceptLayer()
    {
        if (curNum == bank.layers.Count)
            AddLayerToBank();
        else
            ModifiExistingLayer();
    }

    public void DrawPreviousLayers(int num = -1)
    {
        if (num < 0) num = bank.layers.Count;
        float j = drawingDepth;
        for (int i = num - 1; i >= 0 && i >= num - drawingDepth; i--)
        {
            DrawLayer(bank.layers[i], j / drawingDepth, false);
            j = j - 1;
        }
    }

    //public void RemoveLayer()
    //{
    //    if(curNum > 0 && curNum < bank.layers.Count)
    //    {
    //        bank.layers.RemoveAt(curNum);

    //        for (int i = curNum)
    //        {
    //            buttonToIndex[cu]
    //        }
    //        curNum = bank.layers.Count;
    //    }
    //}

    private void AddLayerToBank()
    {
        float height;
        List<Vector3> coordsList = new List<Vector3>();
        if (!float.TryParse(layerHeight.text, out height))
            height = 100;
        var delta = height;
        curHeight += bank.layers.Count == 0 ? 0 : delta;
        for (int i = 0; i < markers.Count; i++)
        {
            Vector3 pos = markers[i].transform.position + Vector3.forward * curHeight;
            coordsList.Add(pos);
        }
        if (coordsList.Count < 3)
            return;
        bank.AddNewLayer(coordsList, height);
        var newButton = Instantiate(scrollElementPrefab, LayerButtonsParent);
        buttonToIndex.Add(newButton, bank.layers.Count - 1);

        DropUI();

        DrawPreviousLayers();
        curNum++;
    }

    private void ModifiExistingLayer()
    {
        List<Vector3> coordsList = new List<Vector3>();
        var height = bank.layers[curNum][0].z;

        for (int i = 0; i < markers.Count; i++)
        {
            Vector3 pos = markers[i].transform.position + Vector3.forward * height;
            coordsList.Add(pos);
        }
        bank.ReplaceLayer(coordsList, height, curNum);
        DropUI();
        curNum = bank.layers.Count;
    }

    public void onScrollElementPressed(Button sender)
    {
        curNum = buttonToIndex[sender];
        DropUI();
        DrawPreviousLayers(curNum);
        for (int i = 0; i < bank.layers[curNum].Count; i++)
            AddMarker(bank.layers[curNum][i]);
        var posList = new List<Vector3>();
        foreach (var i in markers)
            posList.Add(i.transform.position);
        DrawLines();
    }

}
