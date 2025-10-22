using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VFX
{
    private static Dictionary<string, GameObject> lookup;

    public static GameObject EnergyRibbons { get; private set; }
    public static GameObject Construct_GainEnergy { get; private set; }

    static VFX()
    {
        var prefabs = Resources.LoadAll<GameObject>("VFX");
        lookup = new Dictionary<string, GameObject>();

        foreach (var prefab in prefabs)
        {
            lookup[prefab.name] = prefab;

            // Auto-map by name
            switch (prefab.name)
            {
                case nameof(EnergyRibbons): EnergyRibbons = prefab; break;
                case nameof(Construct_GainEnergy): Construct_GainEnergy = prefab; break;
            }
        }
    }

    public static GameObject GetVFXPrefab(string resourcePath)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
            Debug.LogWarning($"Failed to load VFX prefab at Resources/{resourcePath}");

        return prefab;
    }
}
