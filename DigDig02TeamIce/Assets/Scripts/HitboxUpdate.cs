using UnityEngine;

public class HitboxUpdate : MonoBehaviour
{
    void FixedUpdate()
    {
        // Run all hitbox/hurtbox interactions once per physics frame
        HitboxManager.Update();
    }

    // Called automatically when a new scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var updater = new GameObject("HitboxUpdate");
        updater.AddComponent<HitboxUpdate>();
        updater.hideFlags = HideFlags.HideInHierarchy;
    }
}