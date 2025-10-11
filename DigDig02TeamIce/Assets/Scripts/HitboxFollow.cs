using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxFollow : MonoBehaviour
{
    [SerializeField] private Transform follow;
    void Start()
    {
        if (follow != null)
        {
            transform.SetPositionAndRotation(follow.position, follow.rotation);
            transform.localScale = follow.localScale;
        }
    }

    void Update()
    {
        if (follow != null)
        {
            transform.SetPositionAndRotation(follow.position, follow.rotation);
            transform.localScale = follow.localScale;
        }
    }
}
