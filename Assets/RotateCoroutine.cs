using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCoroutine : MonoBehaviour
{

    [SerializeField]
    float step = 0.25f;

    private void OnEnable()
    {
        StartCoroutine(rotationCoroutine());
    }

    private IEnumerator rotationCoroutine()
    {
        Vector3 startRot = transform.rotation.eulerAngles;
        Vector3 currot = startRot;
        while (true)
        {
            currot.z += step;
            transform.eulerAngles = currot;
            yield return null;
        }
    }
}
