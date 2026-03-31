using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CanvasColorGroup : MonoBehaviour
{
    public List<Graphic> Graphics = new();
    public Dictionary<Graphic, Color> OriginalColors = new();

    private void Awake() => Refresh();
    private void OnEnable() => Refresh();
    private void OnTransformChildrenChanged() => Refresh();
    private void OnValidate() => Refresh();

    private void Refresh()
    {
        Graphics.Clear();
        GetComponentsInChildren(true, Graphics);

        OriginalColors.Clear();

        foreach (var g in Graphics)
        {
            if (g == null) continue;
            OriginalColors[g] = g.color;
        }
    }
}