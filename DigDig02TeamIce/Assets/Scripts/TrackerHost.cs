using UnityEngine;

public class TrackerHost : MonoBehaviour
{
    public static Tracker Current { get; private set; }

    void Awake()
    {
        Current = new Tracker(); // fresh tracker for this scene
    }

    // Called automatically when a new scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var hostGO = new GameObject("TrackerHost");
        hostGO.AddComponent<TrackerHost>();
        // Hide in hierarchy
        hostGO.hideFlags = HideFlags.HideInHierarchy;
    }
}