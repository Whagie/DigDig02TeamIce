using UnityEngine;
using System.Collections.Generic;

public static class Particles
{
    private static Dictionary<string, GameObject> lookup;

    public static GameObject P_spark { get; private set; }

    static Particles()
    {
        var prefabs = Resources.LoadAll<GameObject>("Particles");
        lookup = new Dictionary<string, GameObject>();

        foreach (var prefab in prefabs)
        {
            lookup[prefab.name] = prefab;

            // Auto-map by name
            switch (prefab.name)
            {
                case nameof(P_spark): P_spark = prefab; break;
            }
        }
    }

    // Currently unused
    public static GameObject Get(string name) =>
        lookup.TryGetValue(name, out var prefab) ? prefab : null;
}
