using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadScene()
    {
        Scene sceneToLoad = SceneManager.GetSceneByName("Outside01");
        Debug.Log(sceneToLoad.name);
        if (!sceneToLoad.IsValid())
        {
            // Get second loaded scene in build scene list (Main Menu is zero)
            sceneToLoad = SceneManager.GetSceneByBuildIndex(1);
            Debug.Log(sceneToLoad.name);
        }

        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}
