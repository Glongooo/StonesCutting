using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Drag_n_drop : MonoBehaviour
{
    GameObject target;
    bool isMouseDrag;
    private Vector3 screenPosition;
    private Vector3 offset;
    public GraphicRaycaster raycaster = null;
    public static List<Vector3> lastHitPlane = null;

    private void Start()
    {
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hitInfo;
            target = ReturnClickedObject(out hitInfo);
            //target = ReturnClicledObjectGraphic();
            if (target != null)
            {
                isMouseDrag = true;
                Debug.Log("target position :" + target.transform.position);
                //Convert world position to screen position.
                screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                offset = target.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z));
            }
            else if (hitInfo.collider is MeshCollider)
            {
                lastHitPlane = new List<Vector3>();
                var meshCol = hitInfo.collider as MeshCollider;
                var triangle = meshCol.sharedMesh.triangles;
                lastHitPlane.Add(meshCol.sharedMesh.vertices[triangle[hitInfo.triangleIndex * 3]]);
                lastHitPlane.Add(meshCol.sharedMesh.vertices[triangle[hitInfo.triangleIndex * 3 + 1]]);
                lastHitPlane.Add(meshCol.sharedMesh.vertices[triangle[hitInfo.triangleIndex * 3 + 2]]);
            }
            else
            {
                lastHitPlane = null;
            }
        }
        if (Input.GetMouseButtonUp(1))
        {
            isMouseDrag = false;
        }
        if (isMouseDrag)
        {
            //track mouse position.
            Vector3 currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);
            //convert screen position to world position with offset changes.
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenSpace) + offset;
            //It will update target gameobject's current postion.
            target.transform.position = currentPosition;
            var dragable = target.GetComponent<IDragableMesh>();
            if (dragable != null)
                dragable.NotifiListeners();
        }
    }



    GameObject ReturnClickedObject(out RaycastHit hit)
    {
        GameObject target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
        {
            target = hit.collider.gameObject;
        }
        if (target == null ||
            target.tag != "dragable")
        {
            return null;
        }
        return target;
    }

    GameObject ReturnClicledObjectGraphic()
    {
        var pdata = new PointerEventData(EventSystem.current);
        List<RaycastResult> result = new List<RaycastResult>();
        raycaster.Raycast(pdata, result);
        Debug.Log(result);
        return null;
    }

}
