using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonActionInvoker : MonoBehaviour {

    public void Click()
    {
        LayerContainerUI.Instance.onScrollElementPressed(GetComponent<Button>());
    }
	
}
