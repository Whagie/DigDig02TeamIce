using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EditorObjectFinder : MonoBehaviour
{
    public static void FindEntitiesInEditor()
    {
        var entities = FindObjectsOfType<Entity>();
        Debug.Log("Found " + entities.Length + " entities in scene");
    }

    public static List<CustomWindZone> FindWindObjectsInEditor(CustomWindZone wind)
    {
        var winds = FindObjectsOfType<CustomWindZone>().Where(a => a.Main && !a.Equals(wind)).ToList();
        return winds;
    }
}
