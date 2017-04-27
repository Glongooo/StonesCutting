using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class LayerContainerUI : MonoBehaviour
{
    public static LayerContainerUI Instance { get { return _Instance; } }
    private static LayerContainerUI _Instance = null;

    [SerializeField]
    private GameObject markerPrefab;

    [SerializeField]
    private Image linePrefab;

    [SerializeField]
    private float lineWidth = 5.0f;

    [SerializeField]
    private LineRenderer renderer;

    [SerializeField]
    private LayerBank bank;

    [SerializeField]
    private InputField layerHeight;

    [SerializeField]
    Transform linesParent;

    [SerializeField]
    Transform markersParent;

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

    [HideInInspector]
    public List<GameObject> markers;

    private float curHeight = 0;
    private int curNum = 0;

    private List<GameObject> lineParts = new List<GameObject>();

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
        list.Add(new Vector3(2, 2));
        list.Add(new Vector3(1, 4));
        list.Add(new Vector3(2, 5));
        list.Add(new Vector3(5, 4));
        list.Add(new Vector3(4, 2));

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

    private void DrawLines()
    {
        foreach (var go in lineParts)
            Destroy(go);
        if (markers.Count < 2) return;
        for (int i = 0; i < markers.Count - 1; i++)
        {
            DrawLine(markers[i].transform.position, markers[i + 1].transform.position);
        }
        DrawLine(markers[markers.Count - 1].transform.position, markers[0].transform.position);
    }

    private void DrawLine(Vector3 position1, Vector3 position2, float alpha = 1.0f, bool curLayer = true)
    {
        Vector3 differenceVector = position1 - position2;
        var newImg = (GameObject)Instantiate(linePrefab.gameObject, gameObject.transform);
        RectTransform imageRectTransform = ((Image)newImg.GetComponent<Image>()).rectTransform;
        var c = newImg.GetComponent<Image>().color;
        c.a = alpha;
        newImg.GetComponent<Image>().color = c;

        imageRectTransform.sizeDelta = new Vector2(differenceVector.magnitude, lineWidth);
        imageRectTransform.pivot = new Vector2(0, 0.5f);
        imageRectTransform.position = position1;
        float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
        newImg.transform.rotation = Quaternion.Euler(0, 0, 180.0f + angle);
        if (curLayer)
            lineParts.Add(newImg);
        if (linesParent != null)
            newImg.transform.parent = linesParent;

    }

    private Vector3 centerPoint;
    private void SortClockwise(List<GameObject> list)
    {
        Vector3 centerPoint = Vector3.zero;
        //centerPoint /= list.Count;
        foreach (var i in list)
            centerPoint += i.transform.localPosition;
        list.Sort(ClocwiseComparer);
    }

    private int ClocwiseComparer(GameObject a, GameObject b)
    {
        var posA = a.transform.localPosition;
        var posB = b.transform.localPosition;
        //  Variables to Store the atans
        double aTanA, aTanB;

        //  Fetch the atans
        aTanA = Math.Atan2(posA.y - centerPoint.y, posA.x - centerPoint.x);
        aTanB = Math.Atan2(posB.y - centerPoint.y, posB.x - centerPoint.x);

        //  Determine next point in Clockwise rotation
        if (aTanA < aTanB) return -1;
        else if (aTanB < aTanA) return 1;
        return 0;
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

    private void DrawLayer(List<Vector3> layer, float alpha, bool interactable)
    {
        var localMarkers = new List<GameObject>();
        foreach (var i in layer)
        {
            var newMarker = Instantiate(markerPrefab);
            newMarker.GetComponent<DragableElement>().interactable = interactable;
            newMarker.transform.parent = gameObject.transform;
            newMarker.transform.position = new Vector3(i.x, i.y);
            if (markersParent != null)
                newMarker.transform.parent = markersParent;
            var c = newMarker.GetComponent<Image>().color;
            c.a = alpha;
            newMarker.GetComponent<Image>().color = c;
            //newMarker.GetComponent<Collider>().enabled = false;
            newMarker.transform.parent = markersParent;
            Destroy(newMarker.transform.GetChild(0).gameObject);
            localMarkers.Add(newMarker);
            SortClockwise(localMarkers);
            //InsertGORoundSortedList(localMarkers, newMarker);
        }

        if (localMarkers.Count < 2) return;
        for (int i = 0; i < localMarkers.Count - 1; i++)
        {
            DrawLine(localMarkers[i].transform.position, localMarkers[i + 1].transform.position, alpha, false);
        }
        DrawLine(localMarkers[localMarkers.Count - 1].transform.position, localMarkers[0].transform.position, alpha, false);
    }

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

    private void DropUI()
    {
        foreach (var i in markers)
            Destroy(i);
        markers.Clear();
        for (int i = 0; i < markersParent.childCount; i++)
        {
            Destroy(markersParent.GetChild(i).gameObject);
        }

        foreach (var i in lineParts)
            Destroy(i);
        lineParts.Clear();

        for (int i = 0; i < linesParent.childCount; i++)
        {
            Destroy(linesParent.GetChild(i).gameObject);
        }
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
        //renderer.SetPositions(posList.ToArray());
        DrawLines();
    }

}
