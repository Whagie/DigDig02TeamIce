using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParryEffect : MonoBehaviour
{
    private Transform cam;

    public float delay = 0f;
    void Start()
    {
        cam = Camera.main.transform;
        Destroy(gameObject, this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + delay);
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            // Make the quad face the cam
            transform.forward = cam.forward;
        }
    }
}
