using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DoorTriggerInteraction))]
public class DoorTriggerInteractionGizmo : Editor
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForDoor(DoorTriggerInteraction door, GizmoType gizmoType)
    {
        if (door.CurrentDoorPosition == DoorTriggerInteraction.DoorToSpawnAt.None)
            return;

        // Define colors for each enum value
        Color color = Color.white;
        switch (door.CurrentDoorPosition)
        {
            case DoorTriggerInteraction.DoorToSpawnAt.One: color = Color.red; break;
            case DoorTriggerInteraction.DoorToSpawnAt.Two: color = Color.green; break;
            case DoorTriggerInteraction.DoorToSpawnAt.Three: color = Color.yellow; break;
            case DoorTriggerInteraction.DoorToSpawnAt.Four: color = Color.cyan; break;
        }

        float extentsY = 0f;
        // Draw the bounding box
        var renderer = door.GetComponent<Renderer>();
        if (renderer != null)
        {
            Handles.color = color;
            Handles.DrawWireCube(renderer.bounds.center, renderer.bounds.size * 0.95f);
        }
        else
        {
            Collider collider = door.GetComponent<Collider>();
            if (collider != null)
            {
                Handles.color = color;
                Handles.DrawWireCube(collider.bounds.center, collider.bounds.size * 0.95f);
                extentsY = collider.bounds.extents.y;
            }
            else
            {
                // Fallback: small cube at transform
                Handles.color = color;
                Handles.DrawWireCube(door.transform.position, Vector3.one * 2f);
            }
        }

        // Draw text label
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 26;

        Handles.Label(door.transform.position + new Vector3(0f, (extentsY / 2), 0f), door.CurrentDoorPosition.ToString(), style);
    }
}
