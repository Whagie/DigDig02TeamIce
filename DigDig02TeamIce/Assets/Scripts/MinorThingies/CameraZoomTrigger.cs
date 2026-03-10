using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZoomTrigger : MonoBehaviour
{
    public float DesiredCameraDistance;
    public float ZoomInDuration = 1f;
    public float ZoomBackDuration = 1f;

    private CameraMovement _camera;
    private Player player;

    public bool Activated = true;

    private void Start()
    {
        _camera = FindObjectOfType<CameraMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Activated)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;
            _camera.ZoomIn(DesiredCameraDistance, ZoomInDuration);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Activated)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null && p == player)
        {
            ZoomBack();
        }
    }

    public void ZoomBack()
    {
        _camera.ZoomBack(ZoomBackDuration);
        player = null;
    }
}
