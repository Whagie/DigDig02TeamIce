using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParticleSpawner
{
    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default)
    {
        Object.Instantiate(prefab, position, rotation);
    }
}
