using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class LayerContainerUI : DrawingUI
{
    public static LayerContainerUI Instance { get { return _Instance; } }
    private static LayerContainerUI _Instance = null;

    private Stack<AbstractCommand> UndoStack = new Stack<AbstractCommand>();
    private Stack<AbstractCommand> RedoStack = new Stack<AbstractCommand>();

    #region Commands
    private class MoveMarkerCommand : AbstractCommand
    {
        private Vector3 from;
        private Vector3 to;
        private GameObject marker;

        public MoveMarkerCommand(Vector3 from, Vector3 to, GameObject marker)
        {
            this.from = from;
            this.to = to;
            this.marker = marker;
        }

        public override void Do(object context)
        {
            marker.transform.position = to;
            (context as LayerContainerUI).DrawLines();
        }

        public override void Undo(object context)
        {
            marker.transform.position = from;
            (context as LayerContainerUI).DrawLines();
        }
    }

    private class AddMarkerCommand : AbstractCommand
    {
        Vector3 coords;
        GameObject marker = null;

        public AddMarkerCommand(Vector3 coords)
        {
            this.coords = coords;
        }

        public override void Do(object context)
        {
            marker = (context as LayerContainerUI).AddMarker(coords);
        }

        public override void Undo(object context)
        {
            (context as LayerContainerUI).RemoveMarker(marker);
            marker = null;
        }
    }

    #endregion

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

    public void RemoveMarker(GameObject marker)
    {
        markers.Remove(marker);
        if(markers.Count != 0)
        {
            SortClockwise(markers);
            DrawLines();
        }
        Destroy(marker);
    }

    public GameObject AddMarker(Vector3 coords)
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

        return newMarker;
    }

    private float PositiveAngleBetween(Vector3 from, Vector3 to)
    {
        var curAngle = Vector3.Angle(from, to);
        curAngle += curAngle < 0 ? 360 : 0;
        return curAngle;
    }

    private void OnDropEvent(DragableElement el)
    {
        var command = new MoveMarkerCommand((Vector3)el.startPos, el.transform.position, el.gameObject);
        command.Do(this);
        UndoStack.Push(command);
        RedoStack.Clear();
    }

    public void OnClick()
    {
        var mp = Input.mousePosition;
        var newPos = Camera.allCameras[0].ScreenToWorldPoint(mp);

        var command = new AddMarkerCommand(mp);
        command.Do(this);
        UndoStack.Push(command);
        RedoStack.Clear();
        //AddMarker(mp);
        DrawLines();
    }

    public void OnAcceptLayer()
    {
        FlushUndo();
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
    
    private void FlushUndo()
    {
        UndoStack.Clear();
        RedoStack.Clear();
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

    public void onScrollElementPressed(Button sender)
    {
        curNum = buttonToIndex[sender];
        DropUI();
        DrawPreviousLayers(curNum);
        FlushUndo();
        for (int i = 0; i < bank.layers[curNum].Count; i++)
            AddMarker(bank.layers[curNum][i]);
        var posList = new List<Vector3>();
        foreach (var i in markers)
            posList.Add(i.transform.position);
        DrawLines();
    }

    public void UndoButtonClick()
    {
        if(UndoStack.Count > 0)
        {
            var command = UndoStack.Pop();
            command.Undo(this);
            RedoStack.Push(command);
        }
    }

    public void RedoButtonClick()
    {
        if (RedoStack.Count > 0)
        {
            var command = RedoStack.Pop();
            command.Do(this);
            UndoStack.Push(command);
            SortClockwise(markers);
            DrawLines();
        }
    }

}