using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        Vector3 desiredPosition = new Vector3(target.position.x, 0f, target.position.z);
        Vector3 offset = desiredPosition - transform.position;

        float distance = offset.magnitude;

        if (distance > maxDistance)
        {
            recentering = true;
        }

        if (distance < 0.1f)
        {
            recentering = false;
        }

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
