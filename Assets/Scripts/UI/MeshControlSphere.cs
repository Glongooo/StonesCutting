using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDragableMesh
{
    Action<IDragableMesh> OnPositionChange { get; set; }

    void NotifiListeners();

}

public class MeshControlSphere : MonoBehaviour, IDragableMesh
{
    public Action<IDragableMesh> OnPositionChange
    {
        get
        {
            return _OnPositionChange;
        }

        set
        {
            _OnPositionChange = value;
        }
    }
    private Action<IDragableMesh> _OnPositionChange;

    public void NotifiListeners()
    {
        if (_OnPositionChange != null)
            _OnPositionChange(this);
    }
}
