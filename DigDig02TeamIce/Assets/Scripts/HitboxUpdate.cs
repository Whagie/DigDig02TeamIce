using UnityEngine;
using UnityEngine.SceneManagement;

public class HitboxUpdate : MonoBehaviour
{
    static HitboxUpdate instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Example: only spawn for scenes that start with "Level" or contain "Game"
        if (scene.name.Equals("MainMenu")) return;

        if (instance == null)
        {
            var go = new GameObject("HitboxUpdate");
            instance = go.AddComponent<HitboxUpdate>();
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
        }
    }

    void FixedUpdate()
    {
        HitboxManager.Update();
    }
}
