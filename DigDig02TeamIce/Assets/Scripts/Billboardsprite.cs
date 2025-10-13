using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Transform cam;
    public Vector3 target;

    [SerializeField] private bool LockOnTarget;
    [SerializeField] private float OffsetY = 6f;
    void Start()
    {
        cam = Camera.main.transform;
    }

    void Update()
    {
        if (cam != null)
        {
            // Make the quad face the cam
            transform.forward = cam.forward;
            
            if (LockOnTarget)
            {
                transform.position = Player.currentTarget.transform.position + new Vector3(0, OffsetY, 0);
                //transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180f, transform.rotation.w);
            }
            else
            {
                transform.position = target;
            }
        }
    }
}
