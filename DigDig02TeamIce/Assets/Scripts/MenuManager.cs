using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    public CanvasGroup ParentPauseMenu;
    public CanvasGroup MainPauseMenu;
    public CanvasGroup MainSettingsMenu;
    public CanvasGroup AudioMenu;

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

        TurnOffGroup(ParentPauseMenu);
        TurnOffGroup(MainSettingsMenu);
        TurnOffGroup(AudioMenu);
        TurnOnGroup(MainPauseMenu);
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
        TurnOnGroup(ParentPauseMenu);
        TurnOnGroup(MainPauseMenu);

        TurnOffGroup(MainSettingsMenu);
        TurnOffGroup(AudioMenu);
    }
    public void Unpause()
    {
        PauseManager.instance.UnpauseGame();

        TurnOffGroup(ParentPauseMenu);
        TurnOffGroup(AudioMenu);
        TurnOffGroup(MainSettingsMenu);

        TurnOnGroup(MainPauseMenu);
    }

    public void OpenMainPauseMenu()
    {
        TurnOnGroup(MainPauseMenu);

        TurnOffGroup(MainSettingsMenu);
        TurnOffGroup(AudioMenu);
    }

    public void OpenMainSettings()
    {
        TurnOnGroup(MainSettingsMenu);

        TurnOffGroup(MainPauseMenu);
        TurnOffGroup(AudioMenu);
    }

    public void OpenAudioSettings()
    {
        TurnOnGroup(AudioMenu);

        TurnOffGroup(MainPauseMenu);
        TurnOffGroup(MainSettingsMenu);
    }

    private void TurnOffGroup(CanvasGroup group)
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private void TurnOnGroup(CanvasGroup group)
    {
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
