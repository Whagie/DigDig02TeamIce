using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PushableGrid))]
public class PushableGridEditor : Editor
{
    PushableGrid grid;

    Vector2 dragStart;
    bool dragging;

    void OnEnable()
    {
        grid = (PushableGrid)target;
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(
                GUIUtility.GetControlID(FocusType.Passive));
        }

        DrawPoints();
        HandleSelection(e);

        if (dragging)
            DrawSelectionRect(e.mousePosition);

        if (GUI.changed)
            EditorUtility.SetDirty(grid);
    }

    void DrawPoints()
    {
        foreach (var p in grid.Points)
        {
            Vector3 pos = GridToWorld(p.Coord);

            bool isSelected = grid.Selected.Contains(p.Coord);

            if (isSelected)
                Handles.color = Color.yellow;
            else if (p.Occupied)
                Handles.color = Color.magenta;
            else if (p.Active)
                Handles.color = Color.blue;
            else
                Handles.color = Color.red;

            float size = isSelected ? 0.4f : 0.3f;
            bool ctrl = Event.current.control;

            Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);

            if (!ctrl && Handles.Button(pos, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                Undo.RecordObject(grid, "Select Grid Point");

                if (!Event.current.shift)
                    grid.Selected.Clear();

                ToggleSelection(p.Coord);
                Event.current.Use();
            }
        }
    }

    void HandleSelection(Event e)
    {
        // CTRL = rect select
        if (e.control)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                dragging = true;
                dragStart = e.mousePosition;
                e.Use();
            }

            if (e.type == EventType.MouseUp && dragging)
            {
                dragging = false;
                SelectPointsInRect(dragStart, e.mousePosition);
                e.Use();
            }

            return; // important: stop here so single-click logic doesn't run
        }

        // NO CTRL = single click / clear
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Undo.RecordObject(grid, "Clear Selection");
            grid.Selected.Clear();
            e.Use();
        }
    }

    void SelectPointsInRect(Vector2 a, Vector2 b)
    {
        Rect r = Utils.GetScreenRect(a, b);

        Undo.RecordObject(grid, "Box Select Grid Points");

        if (!Event.current.shift)
            grid.Selected.Clear();

        foreach (var p in grid.Points)
        {
            Vector2 screen = HandleUtility.WorldToGUIPoint(GridToWorld(p.Coord));
            if (r.Contains(screen))
                grid.Selected.Add(p.Coord);
        }
    }

    void DrawSelectionRect(Vector2 current)
    {
        Rect r = Utils.GetScreenRect(dragStart, current);
        Handles.BeginGUI();
        Utils.DrawRect(r, new Color(1f, 1f, 0f, 0.1f), Color.yellow);
        Handles.EndGUI();
    }

    void ToggleSelection(Vector2Int c)
    {
        if (!grid.Selected.Remove(c))
            grid.Selected.Add(c);
    }

    Vector3 GridToWorld(Vector2Int c)
    {
        return grid.transform.position +
            new Vector3(c.x * grid.GridMargin, 0f, c.y * grid.GridMargin);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        // --- Delayed grid size fields ---
        var widthProp = serializedObject.FindProperty("GridWidth");
        var heightProp = serializedObject.FindProperty("GridHeight");

        int newWidth = EditorGUILayout.DelayedIntField("Grid Width", widthProp.intValue);
        int newHeight = EditorGUILayout.DelayedIntField("Grid Height", heightProp.intValue);

        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        bool sizeChanged = newWidth != widthProp.intValue ||
                           newHeight != heightProp.intValue;

        if (sizeChanged)
        {
            Undo.RecordObject(target, "Resize Grid");

            widthProp.intValue = newWidth;
            heightProp.intValue = newHeight;

            serializedObject.ApplyModifiedProperties();
            grid.RebuildGrid();
            EditorUtility.SetDirty(grid);
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }

        GUILayout.Space(8);

        // --- Draw everything ELSE normally ---
        DrawPropertiesExcluding(
            serializedObject,
            "GridWidth",
            "GridHeight"
        );

        serializedObject.ApplyModifiedProperties();

        // --- Selection controls ---
        GUILayout.Space(10);
        GUILayout.Label("Selection", EditorStyles.boldLabel);

        GUILayout.Label($"Selected: {grid.Selected.Count}");

        if (GUILayout.Button("Set Active"))
            SetSelected(true);

        if (GUILayout.Button("Set Inactive"))
            SetSelected(false);

        GUILayout.Space(10);

        if (GUILayout.Button("Clear All Occupancy"))
        {
            Undo.RecordObject(grid, "Clear Grid Occupancy");
            grid.ClearAllOccupied();
            EditorUtility.SetDirty(grid);
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Hold Ctrl to rect select", MessageType.Info);
    }

    void SetSelected(bool value)
    {
        Undo.RecordObject(grid, "Toggle Grid Points");

        foreach (var c in grid.Selected)
        {
            var p = grid.Get(c);
            if (p != null)
                p.Active = value;
        }

        EditorUtility.SetDirty(grid);
    }
}

public static class Utils
{
    public static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static void DrawRect(Rect r, Color fill, Color outline)
    {
        EditorGUI.DrawRect(r, fill);
        Handles.DrawSolidRectangleWithOutline(r, fill, outline);
    }
}
