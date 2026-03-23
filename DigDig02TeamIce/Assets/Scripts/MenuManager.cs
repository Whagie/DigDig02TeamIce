using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    public CanvasGroup PauseMenuGroup;

    private Player player;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();

        if (PauseMenuGroup != null)
        {
            PauseMenuGroup.alpha = 0f;
            PauseMenuGroup.interactable = false;
            PauseMenuGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (UserInput.PausePressed && !player.Dead && !SceneFadeManager.instance.IsFadingIn && !SceneFadeManager.instance.IsFadingOut)
        {
            if (!PauseManager.instance.IsPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
    }

    public void Pause()
    {
        PauseManager.instance.PauseGame();
        if (PauseMenuGroup != null)
        {
            PauseMenuGroup.alpha = 1f;
            PauseMenuGroup.interactable = true;
            PauseMenuGroup.blocksRaycasts = true;
        }
    }
    public void Unpause()
    {
        PauseManager.instance.UnpauseGame();
        if (PauseMenuGroup != null)
        {
            PauseMenuGroup.alpha = 0f;
            PauseMenuGroup.interactable = false;
            PauseMenuGroup.blocksRaycasts = false;
        }
    }
}
