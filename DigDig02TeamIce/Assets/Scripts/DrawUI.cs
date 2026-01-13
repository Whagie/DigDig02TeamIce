using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawUI
{
    private class TextEntry
    {
        public string text;
        public Vector2 position;
        public Color color;
        public int size;
    }

    static readonly List<TextEntry> entries = new();
    private static GUIStyle style;
    private static bool initialized;

    public static int EntryCount => entries.Count;

    public static void Draw(string text, Vector2 position, Color color, int size = 16)
    {
        EnsureInit();

        entries.Add(new TextEntry
        {
            text = text,
            position = position,
            color = color,
            size = size
        });
    }

    private static void EnsureInit()
    {
        if (initialized)
            return;

        var go = new GameObject("~DebugText");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DebugTextRunner>();

        initialized = true;
    }

    private class DebugTextRunner : MonoBehaviour
    {
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Safe place to touch GUI.skin
            if (style == null)
                style = new GUIStyle(GUI.skin.label);

            foreach (var e in entries)
            {
                style.fontSize = e.size * 5;
                style.normal.textColor = e.color;

                GUI.Label(
                    new Rect(e.position.x, e.position.y, 1000f, 1000f),
                    e.text,
                    style
                );
            }

            entries.Clear();
        }
    }
}
