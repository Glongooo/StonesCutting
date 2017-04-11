using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour {

    public Transform target;
    public float minFov = 1.0f;
    public float maxFov = 85.0f;
    public float sensitivity = 0.1f;

    private float speed = 5.0f;
  
 void Update()
    {
        if (Input.GetMouseButton(0))
        {
            transform.LookAt(target);
            transform.RotateAround(target.position, Vector3.up, Input.GetAxis("Mouse X") * speed);
            transform.RotateAround(target.position, Vector3.left, Input.GetAxis("Mouse Y") * speed);
        }
        float fov = Camera.main.fieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        Camera.main.fieldOfView = fov;
    }
}
