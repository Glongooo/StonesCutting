using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragableElement : MonoBehaviour
{
    public Action<DragableElement> OnDragEventHandler;
    public Action<DragableElement> OnDropEventHandler;
    public bool interactable = true;
    public Vector3? startPos = null;

    public void OnDrag()
    {
        if (interactable)
        {
            if (startPos == null)
                startPos = transform.position;
            transform.position = Input.mousePosition;
            if (OnDragEventHandler != null)
                OnDragEventHandler(this);
        }
    }

    public void OnDrop()
    {
        if (interactable)
        {
            if (OnDropEventHandler != null)
                OnDropEventHandler(this);
            startPos = null;
        }
    }
}
