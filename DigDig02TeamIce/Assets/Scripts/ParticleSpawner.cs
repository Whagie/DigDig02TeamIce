using UnityEngine;

public static class ParticleSpawner
{
    public static event System.Action<Vector3> OnSendEnergy;

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default)
    {
        Object.Instantiate(prefab, position, rotation);
    }

    public static void Spawn(GameObject prefab, Transform parent)
    {
        Object.Instantiate(prefab, parent);
    }

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        Object.Instantiate(prefab, position, rotation, parent);
    }

    public static void SpawnEnergy(Transform start, bool companionRecieveEnergy = true, float middlePosDistance = 4f)
    {
        GameObject prefab = VFX.EnergyRibbons;

        var instance = Object.Instantiate(prefab, start);
        EnergyParticleManager particleManager = instance.GetComponent<EnergyParticleManager>();
        Companion companion = GameObject.FindObjectOfType<Companion>();
        if (companion == null)
        {
            Debug.Log("Companion is null!");
        }

        Vector3 enemyPos = start.position;
        Vector3 playerPos = companion.player.transform.position;

        // Direction *away* from the companion (so the curve bends back)
        Vector3 direction = (enemyPos - playerPos).normalized;

        // Midpoint halfway in Y between the two
        float midY = enemyPos.y + (playerPos.y - enemyPos.y) / 2f;

        // Final middle position = enemy position + offset backward along the direction
        Vector3 middlePos = enemyPos + direction * middlePosDistance;
        middlePos.y = midY;

        GameObject empty = new GameObject("EnergyCurveMidpoint");
        empty.transform.position = middlePos;

        particleManager.StartPos = start;
        particleManager.EndPos = companion.transform;
        particleManager.MiddlePos = empty.transform;

        // Optional: destroy the VFX prefab after its lifetime
        float maxLifetime = 3; // match your particle lifetime
        Object.Destroy(empty, maxLifetime);
        Object.Destroy(instance, maxLifetime);

        if (companionRecieveEnergy)
        {
            OnSendEnergy?.Invoke(empty.transform.position);
        }
    }
}
