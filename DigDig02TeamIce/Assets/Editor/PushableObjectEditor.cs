using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(PushableObject))]
public class PushableObjectEditor : Editor
{
    static bool picking;
    static PushableObject pickingTarget;

    PushableObject obj;

    const float ArrowLength = 1.2f;
    const float BodyLength = 0.7f;
    const float BodyWidth = 0.25f;
    const float HeadWidth = 0.5f;

    void OnEnable()
    {
        obj = (PushableObject)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Grid Origin", EditorStyles.boldLabel);

        if (obj.HasOrigin)
        {
            GUILayout.Label($"Origin: {obj.OriginCoord}");
            if (GUILayout.Button("Clear Origin"))
            {
                obj.OnClearOrigin();
                EditorUtility.SetDirty(obj);
            }
        }
        else
        {
            GUILayout.Label("Origin: <none>");
        }

        GUI.enabled = obj.Grid != null && !picking;
        if (GUILayout.Button("Pick Origin Point"))
        {
            picking = true;
            pickingTarget = obj;
            SceneView.RepaintAll();
        }
        GUI.enabled = true;

        if (picking && pickingTarget == obj)
        {
            EditorGUILayout.HelpBox(
                "Click a grid point in the Scene view to set the origin.",
                MessageType.Info
            );
        }

        if (GUILayout.Button("Spawn Bookshelves"))
        {
            SpawnBookshelves();
        }
    }

    void OnSceneGUI()
    {
        if (obj == null)
            return;

        // --- Always draw ---
        if (obj.Grid != null)
            DrawGrid(obj.Grid);

        DrawPushArrows(obj);

        // --- Only intercept input while picking ---
        if (picking && pickingTarget == obj)
            HandleOriginPicking(obj);
    }

    void HandleOriginPicking(PushableObject obj)
    {
        Event e = Event.current;

        HandleUtility.AddDefaultControl(
        GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Plane plane = new Plane(
                Vector3.up,
                obj.Grid.transform.position
            );

            if (!plane.Raycast(ray, out float dist))
                return;

            Vector3 world = ray.GetPoint(dist);

            Vector2Int coord = WorldToGrid(obj.Grid, world);

            if (coord.x >= 0 && coord.x < obj.Grid.GridWidth &&
                coord.y >= 0 && coord.y < obj.Grid.GridHeight)
            {
                obj.OriginCoord = coord;
                obj.HasOrigin = true;
                obj.OnGetOrigin();

                picking = false;
                pickingTarget = null;

                EditorUtility.SetDirty(obj);
                e.Use();
            }
        }
    }

    Vector2Int WorldToGrid(PushableGrid grid, Vector3 world)
    {
        Vector3 local = world - grid.transform.position;

        int x = Mathf.RoundToInt(local.x / grid.GridMargin);
        int z = Mathf.RoundToInt(local.z / grid.GridMargin);

        return new Vector2Int(x, z);
    }

    void SpawnBookshelves()
    {

        PushableGridPoint p = obj.Grid.Get(obj.OriginCoord);
        Vector3 pos =
                obj.Grid.transform.position + obj.CellExtents() +
                new Vector3(
                    p.Coord.x * obj.Grid.GridMargin,
                    0f,
                    p.Coord.y * obj.Grid.GridMargin
                );

        GameObject prefab = Resources.Load<GameObject>("Bookshelves/Middle/Bookshelf_Middle_A");

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Spawn Pushable Children");

        for (int z = 0; z < obj.LengthOnGridZ / 2; z++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(obj.transform);
            instance.transform.position = pos + (new Vector3(0f, 0f, 2f) * z);

            for (int x = 0; x < obj.LengthOnGridX / 2; x++)
            {
                GameObject instance2 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance2.transform.SetParent(obj.transform);
                instance2.transform.position = pos + (new Vector3(2f, 0f, 0f) * x);

                Undo.RegisterCreatedObjectUndo(instance2, "Spawn Pushable Children");
            }
        }

        Undo.CollapseUndoOperations(group);
    }

    void DrawGrid(PushableGrid grid)
    {
        foreach (var p in grid.Points)
        {
            Vector3 pos =
                grid.transform.position +
                new Vector3(
                    p.Coord.x * grid.GridMargin,
                    0f,
                    p.Coord.y * grid.GridMargin
                );

            if (p.Occupied)
                Handles.color = Color.magenta;
            else if (p.Active)
                Handles.color = Color.blue;
            else
                Handles.color = Color.red;

            Handles.SphereHandleCap(
                0,
                pos,
                Quaternion.identity,
                0.3f,
                EventType.Repaint
            );
        }
    }

    void DrawPushArrows(PushableObject obj)
    {
        Vector3 origin = obj.transform.position + Vector3.up * 0.75f;

        CompareFunction prevFunction = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        if (obj.CanPushX)
        {
            DrawArrow(
                origin + Vector3.right * 0.5f,
                Vector3.right,
                Color.cyan
            );
        }

        if (obj.CanPushZ)
        {
            DrawArrow(
                origin + Vector3.forward * 0.5f,
                Vector3.forward,
                Color.cyan
            );
        }

        Handles.zTest = prevFunction;
    }

    void DrawArrow(Vector3 origin, Vector3 direction, Color color)
    {
        //float scale = HandleUtility.GetHandleSize(origin);
        float scale = 1f;

        direction.Normalize();

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, direction).normalized;

        float halfBody = BodyWidth * 0.5f * scale;
        float halfHead = HeadWidth * 0.5f * scale;

        Vector3 bodyEnd = origin + direction * BodyLength * scale;
        Vector3 tip = origin + direction * ArrowLength * scale;

        // Outline points (clockwise)
        Vector3 p1 = origin + right * halfBody;          // back top
        Vector3 p2 = bodyEnd + right * halfBody;         // front top
        Vector3 p3 = bodyEnd + right * halfHead;         // flare top
        Vector3 p4 = tip;                                // arrow tip
        Vector3 p5 = bodyEnd - right * halfHead;         // flare bottom
        Vector3 p6 = bodyEnd - right * halfBody;         // front bottom
        Vector3 p7 = origin - right * halfBody;          // back bottom

        Handles.color = color;

        Handles.DrawLine(p1, p2);
        Handles.DrawLine(p2, p3);
        Handles.DrawLine(p3, p4);
        Handles.DrawLine(p4, p5);
        Handles.DrawLine(p5, p6);
        Handles.DrawLine(p6, p7);
        Handles.DrawLine(p7, p1);
    }
}
