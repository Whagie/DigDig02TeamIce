#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

public class EditorObjectFinder : MonoBehaviour
{
    public static void FindEntitiesInEditor()
    {
#if UNITY_EDITOR
        var entities = FindObjectsOfType<Entity>();
        Debug.Log("Found " + entities.Length + " entities in scene");
#endif
    }

    public static List<CustomWindZone> FindWindObjectsInEditor(CustomWindZone wind)
    {
#if UNITY_EDITOR
        var winds = FindObjectsOfType<CustomWindZone>().Where(a => a.Main && !a.Equals(wind)).ToList();
        return winds;
#endif
    }
}
