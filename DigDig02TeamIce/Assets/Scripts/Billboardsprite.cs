using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Transform cam;
    public Vector3 target;
    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            // Make the quad face the cam
            transform.forward = cam.forward;
            transform.position = target;
        }
    }
}
