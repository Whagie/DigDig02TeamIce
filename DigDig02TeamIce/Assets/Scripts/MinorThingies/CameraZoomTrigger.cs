using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZoomTrigger : MonoBehaviour
{
    public float DesiredCameraDistance;
    public float ZoomInDuration = 1f;
    public float ZoomBackDuration = 1f;

    [Tooltip("Sets override camera target. Leave null for no effect")]
    public Transform TargetPosition = null;

    [Tooltip("Sets override camera target as local offset. Leave null for no effect")]
    public Vector3 TargetOffset = Vector3.zero;

    [SerializeField] public List<MovePoints> PointsToMoveTo = new(0);
    [SerializeField] public List<ZoomPoints> PointsToZoomTo = new(0);

    [Serializable]
    public class MovePoints
    {
        public Transform Transform;
        public Vector3 Offset;
        public float DurationBeforeNext;
        public float MoveSpeedMultiplier;
    }

    [Serializable]
    public class ZoomPoints
    {
        public float DesiredDistance;
        public float DurationToZoom;
        public float DurationBeforeNext;
    }

    public bool IterateThroughPointsOnlyOnce = true;
    public bool LockPlayerMovementUntilIterated = true;

    private bool playedOnce = false;
    private float previousDistance = -85f;

    private bool moving = false;
    private bool zooming = false;
    private bool playerLocked = false;

    private CameraMovement _camera;
    private Player player;

    public bool Activated = true;

    private Coroutine moveRoutine;
    private Coroutine zoomRoutine;

    private void Start()
    {
        _camera = FindObjectOfType<CameraMovement>();
        previousDistance = _camera._camera.transform.localPosition.z;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Activated || player != null)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;

            bool either = false;
            if (PointsToMoveTo.Count > 0)
            {
                if (!playedOnce || !IterateThroughPointsOnlyOnce)
                {
                    moveRoutine = StartCoroutine(IterateThroughMovePoints());
                    either = true;
                }
            }
            if (PointsToZoomTo.Count > 0)
            {
                if (!playedOnce || !IterateThroughPointsOnlyOnce)
                {
                    zoomRoutine = StartCoroutine(IterateThroughZoomPoints());
                    either = true;
                }
            }

            if (either)
            {
                playedOnce = true;
                if (LockPlayerMovementUntilIterated && !playerLocked)
                {
                    playerLocked = true;
                    StartCoroutine(MovePlayer());
                }

                return;
            }

            _camera.ZoomIn(DesiredCameraDistance, ZoomInDuration);

            if (TargetPosition != null)
            {
                _camera.SetOverrideTarget(TargetPosition, 1f);
            }
            if (TargetOffset != Vector3.zero)
            {
                _camera.SetOverrideTarget(TargetOffset, 1f);
            }
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

        if (TargetPosition != null)
        {
            _camera.ClearOverrideTarget();
        }
        if (TargetOffset != Vector3.zero)
        {
            _camera.ClearOverrideTargetOffset();
        }

        player = null;
    }

    private IEnumerator IterateThroughMovePoints()
    {
        moving = true;

        foreach (var point in PointsToMoveTo)
        {
            if (point.Transform != null)
            {
                _camera.SetOverrideTarget(point.Transform, 1f);
            }
            else
            {
                _camera.ClearOverrideTarget();
            }

            if (point.Offset != Vector3.zero)
            {
                _camera.SetOverrideTarget(point.Offset, 1f);
            }
            else
            {
                _camera.ClearOverrideTargetOffset();
            }

            if (point.MoveSpeedMultiplier != 0f && point.MoveSpeedMultiplier != 1f)
            {
                _camera.SetOverrideMoveSpeedMultiplier(point.MoveSpeedMultiplier);
            }

            yield return new WaitForSeconds(point.DurationBeforeNext);
        }

        _camera.ClearOverrideTarget();
        _camera.ClearOverrideTargetOffset();
        _camera.ClearOverrideMoveSpeedMultiplier();

        moving = false;
        moveRoutine = null;
    }

    private IEnumerator IterateThroughZoomPoints()
    {
        zooming = true;

        previousDistance = _camera._camera.transform.localPosition.z;

        foreach (var point in PointsToZoomTo)
        {
            _camera.ZoomIn(point.DesiredDistance, point.DurationToZoom);

            yield return new WaitForSeconds(point.DurationBeforeNext);
        }

        _camera.ZoomBack(ZoomBackDuration, previousDistance);

        zooming = false;
        zoomRoutine = null;
    }

    private IEnumerator MovePlayer()
    {
        while (SceneSwapManager.LoadFromDoor)
        {
            yield return null;
        }

        player.MovementOverride = true;
        yield return new WaitForSeconds(0.25f);

        player.animator.SetLayerWeight(0, 0.5f);

        Collider col = gameObject.GetComponent<Collider>();
        Vector3 startPos = player.transform.position;
        Vector3 targetPos = col.bounds.center;
        targetPos.y -= col.bounds.extents.y;
        Vector3 dir = startPos - targetPos;
        Vector3 dir2 = transform.forward;
        dir.Normalize();
        dir2.Normalize();
        dir.y = 0f;
        dir2.y = 0f;
        Quaternion startRot = player.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir2, Vector3.up) * Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

        float time = 0f;
        float duration = 0.75f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            player.transform.position = Vector3.Lerp(startPos, targetPos, t);
            player.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            player.animator.SetFloat("Move", 1f);
            player.animator.SetFloat("MoveX", 0f);
            player.animator.SetFloat("MoveZ", 1f);

            yield return null;
        }

        player.animator.SetLayerWeight(0, 1f);
        player.transform.position = targetPos;
        player.animator.SetFloat("Move", 0f);
        player.animator.SetFloat("MoveX", 0f);
        player.animator.SetFloat("MoveZ", 0f);

        while (moving || zooming)
        {
            yield return null;
        }

        player.MovementOverride = false;
    }
}
