using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingUI : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField]
    protected GameObject markerPrefab;

    [SerializeField]
    protected Image linePrefab;

    [SerializeField]
    protected float lineWidth = 5.0f;

    [SerializeField]
    protected Transform linesParent;

    [SerializeField]
    protected Transform markersParent;
    #endregion

    [HideInInspector]
    public List<GameObject> markers;

    protected List<GameObject> lineParts = new List<GameObject>();

    protected void DrawLine(Vector3 position1, Vector3 position2, float alpha = 1.0f, bool curLayer = true)
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

    protected void DrawLines()
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

    protected void DropUI()
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

    protected void DrawLayer(List<Vector3> layer, float alpha, bool interactable)
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

    protected Vector3 centerPoint;
    protected void SortClockwise(List<GameObject> list)
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

}
