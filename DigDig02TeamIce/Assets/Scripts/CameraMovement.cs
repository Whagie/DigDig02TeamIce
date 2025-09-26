using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraMovement : MonoBehaviour
{
    private Player player;
    private Transform target;

    public float maxDistance = 5f;
    public float moveSmoothSpeed = 2f;
    public float recenterSmoothSpeed = 4f;

    private Vector3 velocity = Vector3.zero;
    private bool recentering = false;

    void Start()
    {
        player = TrackerHost.Current.Get<Player>();
    }    

    void LateUpdate()
    {
        target = player.transform != null ? player.transform : null;
        if (target == null) return;

        Vector3 desiredPosition = target.position;
        Vector3 offset = desiredPosition - transform.position;

        float distance = offset.magnitude;

        // If player leaves dead zone -> start recentering
        if (distance > maxDistance)
        {
            recentering = true;
        }

        // If camera has basically reached the player -> stop recentering
        if (distance < 0.05f)
            recentering = false;

        if (recentering)
        {
            float smooth = (distance > maxDistance)
                ? moveSmoothSpeed
                : recenterSmoothSpeed;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                1f / smooth
            );
        }
    }
}
