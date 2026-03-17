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

    public static void SpawnEnergy(Transform origStart, bool companionRecieveEnergy = true, float middlePosDistance = 4f, bool cloneTransform = false)
    {
        Transform start;
        GameObject tempTransform = null;
        if (cloneTransform)
        {
            tempTransform = new GameObject("TempEnergyCurveStartPoint");
            tempTransform.transform.position = origStart.position;
            start = tempTransform.transform;
        }
        else
        {
            start = origStart;
        }

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
        if (tempTransform != null)
            Object.Destroy(tempTransform, maxLifetime);

        if (companionRecieveEnergy)
        {
            OnSendEnergy?.Invoke(empty.transform.position);
        }
    }

    public static void SuckEnergy(Transform origTarget, bool cloneTransform = false)
    {
        Transform target;
        GameObject tempTransform = null;
        if (cloneTransform)
        {
            tempTransform = new GameObject("TempEnergyCurveTargetPoint");
            tempTransform.transform.position = origTarget.position;
            target = tempTransform.transform;
        }
        else
        {
            target = origTarget;
        }

        Companion companion = GameObject.FindObjectOfType<Companion>();
        if (companion == null)
        {
            Debug.Log("Companion is null!");
        }

        GameObject prefab = VFX.EnergyRibbons;

        Quaternion rot = target.rotation * Quaternion.Euler(90f, 0f, 0f);

        var instance = Object.Instantiate(prefab, companion.transform.position, rot, target);
        EnergyParticleManager particleManager = instance.GetComponent<EnergyParticleManager>();

        GameObject empty = new GameObject("EnergyCurveMidpoint");
        empty.transform.position = GetOffsetPoint(companion.transform, target, 1f, Random.Range(0f, 180f));

        particleManager.StartPos = companion.transform;
        particleManager.EndPos = target;
        particleManager.MiddlePos = empty.transform;

        // Optional: destroy the VFX prefab after its lifetime
        float maxLifetime = 3; // match your particle lifetime
        Object.Destroy(empty, maxLifetime);
        Object.Destroy(instance, maxLifetime);
        if (tempTransform != null)
            Object.Destroy(tempTransform, maxLifetime);
    }

    private static Vector3 GetOffsetPoint(Transform a, Transform b, float radius, float angleDeg)
    {
        // 1. Midpoint
        Vector3 center = Vector3.Lerp(a.position, b.position, 0.25f);

        // 2. Direction between objects
        Vector3 forward = (b.position - a.position).normalized;

        // 3. Build perpendicular basis
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

        // Handle edge case (if forward is vertical)
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(forward, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, forward);

        // 4. Circle offset
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 offset =
            Mathf.Cos(rad) * right * radius +
            Mathf.Sin(rad) * up * radius;

        return center + offset;
    }
}
