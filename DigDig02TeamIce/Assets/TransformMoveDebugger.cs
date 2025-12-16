using UnityEngine;
using System.Diagnostics;

public class TransformMoveDebugger : MonoBehaviour
{
    Vector3 lastPosition;

    void Awake()
    {
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (transform.position != lastPosition)
        {
            UnityEngine.Debug.LogWarning(
                $"[MOVE DETECTED] {name} moved from {lastPosition} to {transform.position}\n" +
                new StackTrace(true)
            );
        }

        lastPosition = transform.position;
    }
}
