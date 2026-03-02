using System.Collections.Generic;
using UnityEngine;

public class PushableGrid : MonoBehaviour
{
    [HideInInspector] public int GridWidth;
    [HideInInspector] public int GridHeight;

    public float GridMargin = 2;

    [SerializeField] private List<PushableGridPoint> points = new();

    // Editor selection
    [SerializeField] private List<Vector2Int> selected = new();

    Dictionary<Vector2Int, PushableGridPoint> lookup;

    public IReadOnlyList<PushableGridPoint> Points => points;
    public List<Vector2Int> Selected => selected;

#if UNITY_EDITOR
    void OnEnable()
    {
        RebuildLookup();
        UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
    }

    void OnDisable()
    {
        UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnUndoRedo()
    {
        RebuildLookup();

        selected.RemoveAll(c => c.x >= GridWidth || c.y >= GridHeight);
    }
#endif

    void RebuildLookup()
    {
        lookup ??= new Dictionary<Vector2Int, PushableGridPoint>();
        lookup.Clear();

        foreach (var p in points)
            lookup[p.Coord] = p;
    }

    public void RebuildGrid()
    {
        RebuildLookup();

        for (int x = 0; x < GridWidth; x++)
            for (int z = 0; z < GridHeight; z++)
            {
                var c = new Vector2Int(x, z);
                if (!lookup.ContainsKey(c))
                {
                    var p = new PushableGridPoint { Coord = c };
                    points.Add(p);
                    lookup[c] = p;
                }
            }

        points.RemoveAll(p =>
            p.Coord.x >= GridWidth || p.Coord.y >= GridHeight);

        selected.RemoveAll(c =>
            c.x >= GridWidth || c.y >= GridHeight);

        RebuildLookup();
    }

    void EnsureLookup()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<Vector2Int, PushableGridPoint>();
        foreach (var p in points)
            lookup[p.Coord] = p;
    }

    public PushableGridPoint Get(Vector2Int c)
    {
        EnsureLookup();
        return lookup.TryGetValue(c, out var p) ? p : null;
    }

    public bool IsCellBlocked(Vector2Int coord, PushableObject requester)
    {
        var p = Get(coord);
        if (p == null)
            return true;

        if (!p.Active)
            return true;

        if (p.Occupied && p.Occupier != requester)
            return true;

        return false;
    }

    public PushableGridPoint TravelX(Vector2Int from, int steps)
    {
        Vector2Int to = new Vector2Int(from.x + steps, from.y);
        return lookup.TryGetValue(to, out var p) ? p : null;
    }

    public PushableGridPoint TravelY(Vector2Int from, int steps)
    {
        Vector2Int to = new Vector2Int(from.x, from.y + steps);
        return lookup.TryGetValue(to, out var p) ? p : null;
    }

    public void SetOccupiedArea(Vector2Int origin, int sizeX, int sizeZ, PushableObject owner)
    {
        // Clear previous occupancy by this object
        foreach (var p in points)
        {
            if (p.Occupied && p.Occupier == owner)
            {
                p.Occupied = false;
                p.Occupier = null;
            }
        }

        // Fill new area
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                Vector2Int c = origin + new Vector2Int(x, z);
                var p = Get(c);

                if (p == null)
                    continue;

                p.Occupied = true;
                p.Occupier = owner;
            }
        }
    }

    public void ClearOccupiedArea(PushableObject owner)
    {
        // Clear previous occupancy by this object
        foreach (var p in points)
        {
            if (p.Occupied && p.Occupier == owner)
            {
                p.Occupied = false;
                p.Occupier = null;
            }
        }
    }

    public void ClearAllOccupied()
    {
        foreach (var p in points)
        {
            p.Occupied = false;
            p.Occupier = null;
        }
    }
}

[System.Serializable]
public class PushableGridPoint
{
    public Vector2Int Coord;
    public bool Active = true;
    public bool Occupied;
    public PushableObject Occupier;
}
