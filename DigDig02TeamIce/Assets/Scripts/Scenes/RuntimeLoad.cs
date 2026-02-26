using System.Collections.Generic;
using UnityEngine;

public class RuntimeLoad : MonoBehaviour
{
    [SerializeField] private List<GameObject> objectsToActivate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ActivateHiddenObjects()
    {
        foreach (var activator in FindObjectsOfType<RuntimeLoad>(true))
        {
            activator.ActivateObjects();
        }
    }

    public void ActivateObjects()
    {
        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
