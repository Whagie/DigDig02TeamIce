using UnityEngine;
using System.Collections.Generic;

public static class DrawMethods
{
    public static void Line(Vector3 from, Vector3 to, Color color, float duration = 0)
    {
        Debug.DrawLine(from, to, color, duration);
    }

    public static void WireSphere(Vector3 center, float radius, Color color, int segments = 32, float duration = 0)
    {
        Vector3 lastPoint = center + Vector3.forward * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            Debug.DrawLine(lastPoint, nextPoint, color, duration);
            lastPoint = nextPoint;
        }
    }

    public static void WireCircle(Vector3 center, float radius, Quaternion rotation, Color color, int segments = 32, float duration = 0)
    {
        Vector3 lastPoint = center + rotation * (Vector3.right * radius);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 nextPoint = center + rotation * new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Debug.DrawLine(lastPoint, nextPoint, color, duration);
            lastPoint = nextPoint;
        }
    }

    public static void WireCube(Vector3 center, Vector3 size, Color color, float duration = 0)
    {
        Vector3 half = size * 0.5f;

        // 8 corners of the cube
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-half.x, -half.y, -half.z);
        corners[1] = center + new Vector3(half.x, -half.y, -half.z);
        corners[2] = center + new Vector3(half.x, -half.y, half.z);
        corners[3] = center + new Vector3(-half.x, -half.y, half.z);

        corners[4] = center + new Vector3(-half.x, half.y, -half.z);
        corners[5] = center + new Vector3(half.x, half.y, -half.z);
        corners[6] = center + new Vector3(half.x, half.y, half.z);
        corners[7] = center + new Vector3(-half.x, half.y, half.z);

        // bottom face
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);

        // top face
        Debug.DrawLine(corners[4], corners[5], color, duration);
        Debug.DrawLine(corners[5], corners[6], color, duration);
        Debug.DrawLine(corners[6], corners[7], color, duration);
        Debug.DrawLine(corners[7], corners[4], color, duration);

        // vertical edges
        Debug.DrawLine(corners[0], corners[4], color, duration);
        Debug.DrawLine(corners[1], corners[5], color, duration);
        Debug.DrawLine(corners[2], corners[6], color, duration);
        Debug.DrawLine(corners[3], corners[7], color, duration);
    }

    public static void DrawVisionCone(Transform transform, VisionCone cone, Color color)
    {
        Vector3 coneOrigin = transform.position + cone.offset;

        // Get cone forward vector
        Quaternion coneRotation = transform.rotation * cone.GetRotation();
        Vector3 coneForward = coneRotation * Vector3.forward;

        int rays = 10;
        float halfAngle = cone.angle * 0.5f;

        // draw the edge rays
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * coneForward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * coneForward;

        Debug.DrawLine(coneOrigin, coneOrigin + leftDir * cone.length, color);
        Debug.DrawLine(coneOrigin, coneOrigin + rightDir * cone.length, color);

        // intermediate rays for smoother visualization
        for (int i = 1; i < rays; i++)
        {
            float t = i / (float)rays;
            float lerpAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0, lerpAngle, 0) * coneForward;
            Debug.DrawLine(coneOrigin, coneOrigin + dir * cone.length, color);
        }
    }
}
